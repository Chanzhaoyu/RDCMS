using System.Security.Claims;
using RDCMS.Common;

namespace RDCMS.Api.Extensions;

/// <summary>
/// ClaimsPrincipal 扩展方法，安全解析 JWT Claim。
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// 从 JWT 的 NameIdentifier claim 中解析当前用户 ID。
    /// claim 缺失或非整数时抛出 BizException（401），由 ExceptionMiddleware 统一处理。
    /// </summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(value) || !int.TryParse(value, out var userId))
        {
            throw new BizException(ErrorCode.Unauthorized, "未登录或登录已过期");
        }

        return userId;
    }
}