using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RDCMS.Common.Helpers;

/// <summary>
/// JWT / RefreshToken 工具
/// <para>
/// access token 走标准 HS256 签名；refresh token 是一段 64 字节随机串（base64url），
/// 数据库只存它的 SHA256 哈希，避免库泄漏导致全部 RT 失效。
/// </para>
/// </summary>
public static class JwtHelper
{
    /// <summary>
    /// 生成 access JWT。expireMinutes 默认 15 分钟，配套 refresh 旋转使用。
    /// 返回 (token, expiresAt) —— 让上层一次拿到过期时间，避免再次解析 JWT。
    /// </summary>
    public static (string Token, DateTime ExpiresAt) GenerateToken(
        int userId,
        string username,
        IList<string> roles,
        string secretKey,
        string issuer,
        string audience,
        int expireMinutes = 15)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(expireMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    /// <summary>
    /// 生成一段加密随机的 refresh token（base64url，64 字节熵）。
    /// 明文只在登录 / 刷新响应里返回一次；DB 存 HashRefreshToken 后的哈希。
    /// </summary>
    public static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Base64UrlEncode(bytes);
    }

    /// <summary>SHA256(token) 的 base64url 编码。比 base64 短，且对 URL/Header 友好</summary>
    public static string HashRefreshToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}