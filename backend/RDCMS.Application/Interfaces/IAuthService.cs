using RDCMS.Application.DTOs.Auth;

namespace RDCMS.Application.Interfaces;

/// <summary>
/// 认证服务：负责用户登录、Token 刷新/撤销、获取用户信息。
/// </summary>
public interface IAuthService
{
    /// <summary>用户登录：校验凭据，返回 Access Token + Refresh Token</summary>
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ip = null, CancellationToken ct = default);

    /// <summary>获取当前登录用户的基本信息</summary>
    Task<UserInfoResponse> GetUserInfoAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// 用 refresh token 换新一对 token。验证 RT 哈希 → 旋转（撤销旧、发新）→ 检测滥用：
    /// 已撤销但还在用 → 把整条链一次性废掉，疑似被盗。
    /// </summary>
    Task<RefreshTokenResponse> RefreshAsync(string refreshToken, string? ip = null, CancellationToken ct = default);

    /// <summary>用户主动登出：撤销当前 RT</summary>
    Task RevokeAsync(string refreshToken, string? ip = null, CancellationToken ct = default);
}