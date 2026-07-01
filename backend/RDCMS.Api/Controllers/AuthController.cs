using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RDCMS.Api.Extensions;
using RDCMS.Application.DTOs.Auth;
using RDCMS.Application.Interfaces;
using RDCMS.Common;

namespace RDCMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 用户名密码登录
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, ip);
        return Ok(result);
    }

    /// <summary>
    /// 当前登录用户的资料
    /// </summary>
    [HttpGet("userinfo")]
    public async Task<ActionResult<UserInfoResponse>> GetUserInfo()
    {
        var userId = User.GetUserId();
        var result = await _authService.GetUserInfoAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// 用 refresh token 换新 token
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RefreshAsync(request.RefreshToken, ip);
        return Ok(result);
    }

    /// <summary>
    /// 用户主动登出：撤销当前 refresh token
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _authService.RevokeAsync(request.RefreshToken, ip);
        return Ok();
    }
}