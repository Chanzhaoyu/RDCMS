using Microsoft.EntityFrameworkCore;
using RDCMS.Domain.Entities;

namespace RDCMS.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 所有继承 BaseEntity 的实体，CreatedAt 由 DB 打时间戳
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entity.ClrType)) continue;
            modelBuilder.Entity(entity.ClrType)
                .Property(nameof(BaseEntity.CreatedAt))
                .HasDefaultValueSql("(UTC_TIMESTAMP(6))")
                .ValueGeneratedOnAdd();
        }

        // ============================================================================
        // 注意：以下所有索引字段必须配置 HasMaxLength 才能让 MySQL 正确创建 UNIQUE 索引。
        // ============================================================================

        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Username).HasMaxLength(100);
            e.HasIndex(u => u.Username).IsUnique();
            e.HasQueryFilter(u => !u.IsDeleted);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.Property(rt => rt.TokenHash).HasMaxLength(100);
            e.HasIndex(rt => rt.TokenHash).IsUnique();
            e.HasQueryFilter(rt => !rt.IsDeleted);
        });
    }
}