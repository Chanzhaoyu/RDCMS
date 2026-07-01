using System.ComponentModel.DataAnnotations;

namespace RDCMS.Application.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "用户名不能为空")] public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")] public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    /// <summary>
    /// access JWT。短 TTL（默认 15 分钟），过期后用 RefreshToken 换新
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// access token 过期时间（UTC ISO），前端可据此提前 refresh
    /// </summary>
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary>
    /// refresh token（明文，仅此一次返回）。前端需安全存储；后端只存 SHA256 哈希
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// refresh token 过期时间（UTC）
    /// </summary>
    public DateTime RefreshTokenExpiresAt { get; set; }

    public string Username { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Avatar { get; set; }
}

public class UserInfoResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
}

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "refreshToken 不能为空")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
}