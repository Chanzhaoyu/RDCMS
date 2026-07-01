namespace RDCMS.Common;

public class Result<T>
{
    public int Code { get; set; } = 200;
    public string Message { get; set; } = "success";
    public T? Data { get; set; }

    public static Result<T> Success(T data, string message = "success")
        => new Result<T> { Code = 200, Message = message, Data = data };

    public static Result<T> Fail(string message, ErrorCode code = ErrorCode.BadRequest)
        => new Result<T> { Code = (int)code, Message = message, Data = default };

    public static Result<T> Fail(ErrorCode code)
        => new Result<T> { Code = (int)code, Message = code.ToString(), Data = default };
}

public class Result
{
    public int Code { get; set; } = 200;
    public string Message { get; set; } = "success";

    public static Result Success(string message = "success")
        => new Result { Code = 200, Message = message };

    public static Result Fail(string message, ErrorCode code = ErrorCode.BadRequest)
        => new Result { Code = (int)code, Message = message };

    public static Result Fail(ErrorCode code)
        => new Result { Code = (int)code, Message = code.ToString() };
}