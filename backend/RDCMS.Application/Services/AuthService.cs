using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RDCMS.Application.DTOs.Auth;
using RDCMS.Application.Interfaces;
using RDCMS.Common;
using RDCMS.Common.Helpers;
using RDCMS.Domain.Entities;
using RDCMS.Domain.Enums;
using RDCMS.Infrastructure.Cache;
using RDCMS.Infrastructure.Data;

namespace RDCMS.Application.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;
    private readonly ILogger<AuthService> _logger;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _accessTokenMinutes;
    private readonly int _refreshTokenDays;

    public AuthService(
        AppDbContext db,
        ICacheService cache,
        ILogger<AuthService> logger,
        IConfiguration configuration
    )
    {
        _db = db;
        _cache = cache;
        _logger = logger;
        _jwtSecret = configuration["Jwt:Secret"]
                     ?? throw new InvalidOperationException("Jwt:Secret 未配置");
        _jwtIssuer = configuration["Jwt:Issuer"]
                     ?? throw new InvalidOperationException("Jwt:Issuer 未配置");
        _jwtAudience = configuration["Jwt:Audience"]
                       ?? throw new InvalidOperationException("Jwt:Audience 未配置");
        _accessTokenMinutes = configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 15;
        _refreshTokenDays = configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 30;
    }

    /// <summary>
    /// 登录
    /// </summary>
    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ip = null, CancellationToken ct = default)
    {
        var username = request.Username;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

        if (user == null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
            throw new BizException(ErrorCode.PasswordWrong, "用户名或密码错误");

        if (user.Status == Status.Disabled)
            throw new BizException(ErrorCode.UserDisabled, "账号已被禁用");

        var (accessToken, accessExp) = JwtHelper.GenerateToken(
            user.Id, user.Username, [], _jwtSecret, _jwtIssuer, _jwtAudience, _accessTokenMinutes);

        var (refreshTokenPlain, refreshExp, _) = await IssueRefreshTokenAsync(user.Id, ip, ct);

        return new LoginResponse
        {
            Token = accessToken,
            AccessTokenExpiresAt = accessExp,
            RefreshToken = refreshTokenPlain,
            RefreshTokenExpiresAt = refreshExp,
            Username = user.Username,
            Nickname = user.Nickname,
            Avatar = user.Avatar
        };
    }

    /// <summary>
    /// 获取当前登录用户的基本信息
    /// </summary>
    public async Task<UserInfoResponse> GetUserInfoAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
        {
            throw new BizException(ErrorCode.UserNotFound, "用户不存在");
        }

        return new UserInfoResponse
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Email = user.Email,
            Phone = user.Phone,
            Avatar = user.Avatar
        };
    }

    /// <summary>
    /// 用 refresh token 换新一对 token。
    /// 1. SHA256 哈希 → 查 RT 行
    /// 2. 已撤销但 ReplacedByTokenId 已存在 → 旧链 token 被重用，视为失窃，连同后代全部 Revoke
    /// 3. 已过期 → RefreshTokenExpired
    /// 4. 正常 → 撤销旧 RT、发新 RT、刷新 access token
    /// </summary>
    public async Task<RefreshTokenResponse> RefreshAsync(string refreshToken, string? ip = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new BizException(ErrorCode.RefreshTokenInvalid, "refresh token 不能为空");

        var hash = JwtHelper.HashRefreshToken(refreshToken);
        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);

        if (existing == null)
            throw new BizException(ErrorCode.RefreshTokenInvalid, "无效的 refresh token");

        // 重用检测：已撤销 RT 仍被使用 → 整条链失活
        if (existing.RevokedAt != null)
        {
            _logger.LogWarning(
                "RefreshToken reuse detected. UserId={UserId}, TokenId={TokenId}, IP={Ip}",
                existing.UserId, existing.Id, ip);
            await RevokeDescendantsAsync(existing, ip, ct);
            throw new BizException(ErrorCode.RefreshTokenReused, "refresh token 已失效，请重新登录");
        }

        if (DateTime.UtcNow >= existing.ExpiresAt)
            throw new BizException(ErrorCode.RefreshTokenExpired, "refresh token 已过期，请重新登录");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == existing.UserId, ct);
        if (user == null || user.Status == Status.Disabled)
            throw new BizException(ErrorCode.UserDisabled, "账号已被禁用或不存在");

        // 旋转：发新 RT，旧 RT 标记 Revoked + ReplacedBy
        var (newPlain, newExp, newEntity) = await IssueRefreshTokenAsync(user.Id, ip, ct);

        existing.RevokedAt = DateTime.UtcNow;
        existing.RevokedByIp = ip;
        existing.ReplacedByTokenId = newEntity.Id;
        await _db.SaveChangesAsync(ct);

        var (accessToken, accessExp) = JwtHelper.GenerateToken(
            user.Id, user.Username, [], _jwtSecret, _jwtIssuer, _jwtAudience, _accessTokenMinutes);

        return new RefreshTokenResponse
        {
            Token = accessToken,
            AccessTokenExpiresAt = accessExp,
            RefreshToken = newPlain,
            RefreshTokenExpiresAt = newExp
        };
    }

    /// <summary>登出：把当前 RT 撤销。Access token 已签发的部分等其自然过期（≤ 15min）</summary>
    public async Task RevokeAsync(string refreshToken, string? ip = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var hash = JwtHelper.HashRefreshToken(refreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);
        if (existing == null || existing.RevokedAt != null) return;

        existing.RevokedAt = DateTime.UtcNow;
        existing.RevokedByIp = ip;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<(string Plain, DateTime ExpiresAt, RefreshToken Entity)> IssueRefreshTokenAsync(
        int userId, string? ip, CancellationToken ct = default
    )
    {
        var plain = JwtHelper.GenerateRefreshToken();
        var hash = JwtHelper.HashRefreshToken(plain);
        var expires = DateTime.UtcNow.AddDays(_refreshTokenDays);

        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = expires,
            CreatedByIp = ip
        };
        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(ct);
        return (plain, expires, entity);
    }

    private async Task RevokeDescendantsAsync(RefreshToken start, string? ip, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var current = start;
        var safety = 0; // 防止数据异常造成的死循环

        while (current.ReplacedByTokenId != null && safety++ < 64)
        {
            var next = await _db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Id == current.ReplacedByTokenId, ct);
            if (next == null) break;
            if (next.RevokedAt == null)
            {
                next.RevokedAt = now;
                next.RevokedByIp = ip;
            }

            current = next;
        }

        // 同时把这个用户所有未撤销 RT 也清掉，确保彻底登出
        await _db.RefreshTokens
            .Where(rt => rt.UserId == start.UserId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(rt => rt.RevokedAt, now)
                .SetProperty(rt => rt.RevokedByIp, ip), ct);

        await _db.SaveChangesAsync(ct);
    }
}