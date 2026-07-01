using StackExchange.Redis;
using System.Text.Json;

namespace RDCMS.Infrastructure.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry ?? TimeSpan.FromHours(2));
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
        const int batchSize = 500;
        var buffer = new List<RedisKey>(batchSize);

        await foreach (var key in server.KeysAsync(pattern: $"{prefix}*", pageSize: 1000))
        {
            buffer.Add(key);
            if (buffer.Count >= batchSize)
            {
                await _db.KeyDeleteAsync(buffer.ToArray());
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
            await _db.KeyDeleteAsync(buffer.ToArray());
    }

    public async Task<IList<string>> GetByPrefixAsync(string prefix)
    {
        var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
        var result = new List<string>();
        await foreach (var key in server.KeysAsync(pattern: $"{prefix}*", pageSize: 1000))
            result.Add(key!);
        return result;
    }

    public async Task<long> IncrementAsync(string key, TimeSpan? expiryIfNew = null)
    {
        var newValue = await _db.StringIncrementAsync(key);
        if (newValue == 1 && expiryIfNew.HasValue)
            await _db.KeyExpireAsync(key, expiryIfNew.Value);
        return newValue;
    }

    public Task<bool> ExistsAsync(string key)
    {
        return _db.KeyExistsAsync(key);
    }
}