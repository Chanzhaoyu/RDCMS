using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RDCMS.Infrastructure.Cache;
using RDCMS.Infrastructure.Data;
using RDCMS.Infrastructure.Repositories;
using StackExchange.Redis;

namespace RDCMS.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database") ??
                               throw new InvalidOperationException("ConnectionStrings:Database 未配置");

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));

        services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, serverVersion, mysql =>
        {
            // 全局默认拆分查询：分页 + 多集合 Include/投影 时避免笛卡尔积放大行数。
            // 单集合查询想回到 JOIN 模式，可在该查询尾部显式 .AsSingleQuery()。
            mysql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

            // 容器编排/云环境 MySQL 可能短暂不可用，自动重试最多 3 次
            mysql.EnableRetryOnFailure(3);
        }));

        var redisConnection = configuration.GetConnectionString("Redis")
                              ?? throw new InvalidOperationException("ConnectionStrings:Redis 未配置");

        // 延迟连接 + 优雅降级：Connect 只在第一次解析时执行。
        // AbortOnConnectFail=false 使 Redis 不可用时不会把整个应用拖垮，
        // 后续 Redis 恢复后 ConnectionMultiplexer 会自动重建连接。
        var redisOptions = ConfigurationOptions.Parse(redisConnection);
        redisOptions.AbortOnConnectFail = false;
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        return services;
    }
}