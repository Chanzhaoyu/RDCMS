using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RDCMS.Infrastructure.Data;
using StackExchange.Redis;

namespace RDCMS.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ---- MySQL ----
        var connStr = configuration.GetConnectionString("Database")
                      ?? throw new InvalidOperationException("ConnectionStrings:Database 未配置");
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 36)),
                mysql => mysql.EnableRetryOnFailure(3)));

        // ---- Redis ----
        var redisConn = configuration.GetConnectionString("Redis")
                        ?? throw new InvalidOperationException("ConnectionStrings:Redis 未配置");
        var redisOptions = ConfigurationOptions.Parse(redisConn);
        redisOptions.AbortOnConnectFail = false; // Redis 挂了不拖垮应用
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisOptions));

        return services;
    }
}