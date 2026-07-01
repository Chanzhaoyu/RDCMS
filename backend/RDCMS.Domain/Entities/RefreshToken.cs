namespace RDCMS.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }

    /// <summary>
    /// SHA256(token) 的 base64，唯一索引
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    /// <summary>
    ///  非 null 表示已撤销 / 已被替换
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// 替换它的下一代 RT 的 Id（可空）
    /// </summary>
    public int? ReplacedByTokenId { get; set; }

    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }

    /// <summary>
    /// 计算属性：当前时刻是否仍可用
    /// </summary>
    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}