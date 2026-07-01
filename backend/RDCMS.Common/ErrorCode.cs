namespace RDCMS.Common;

/// <summary>
/// 业务错误码。前缀规则：1xxx 用户/认证，2xxx 角色，3xxx 文章，4xxx 菜单，5xxx 文件。
/// HTTP 级别错误码（400/401/403/404/500）复用标准值，其余为自定义业务码。
/// </summary>
public enum ErrorCode
{
    Success = 200,
    BadRequest = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    InternalError = 500,

    UserNotFound = 1001,
    UserDisabled = 1002,
    UsernameExists = 1003,
    PasswordWrong = 1004,
    RefreshTokenInvalid = 1005,
    RefreshTokenExpired = 1006,
    RefreshTokenReused = 1007,
    AccountLocked = 1008,
    CaptchaRequired = 1009,
    CaptchaInvalid = 1010,
}