namespace RDCMS.Common;

/// <summary>
/// 业务异常
/// </summary>
public class BizException : Exception
{
    public ErrorCode Code { get; }

    public BizException(ErrorCode code) : base(code.ToString())
    {
        Code = code;
    }

    public BizException(ErrorCode code, string message) : base(message)
    {
        Code = code;
    }
}