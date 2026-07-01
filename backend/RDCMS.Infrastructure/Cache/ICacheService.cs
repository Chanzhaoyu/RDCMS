using StackExchange.Redis;

namespace RDCMS.Infrastructure.Cache;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
    Task<IList<string>> GetByPrefixAsync(string prefix);

    /// <summary>原子自增；返回自增后的值。当返回值 == 1（key 刚创建）时按 expiryIfNew 设 TTL，避免每次自增都刷新过期时间。</summary>
    Task<long> IncrementAsync(string key, TimeSpan? expiryIfNew = null);

    /// <summary>判断 key 是否存在（不取值不反序列化），用于"是否被锁"这种纯标志位检查</summary>
    Task<bool> ExistsAsync(string key);
}