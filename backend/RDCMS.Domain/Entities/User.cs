using RDCMS.Domain.Enums;

namespace RDCMS.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public Status Status { get; set; } = Status.Enabled;
    public string? Remark { get; set; }
}