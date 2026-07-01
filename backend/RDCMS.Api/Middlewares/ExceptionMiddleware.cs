using System.Net;
using System.Text.Json;
using RDCMS.Common;

namespace RDCMS.Api.Middlewares;

/// <summary>
/// 全局异常中间件
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BizException ex)
        {
            _logger.LogWarning(ex, "Business exception: {Code}, TraceId={TraceId}", ex.Code, context.TraceIdentifier);
            await WriteResponseAsync(context, (int)ex.Code, ex.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Request cancelled by client, TraceId={TraceId}", context.TraceIdentifier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception, TraceId={TraceId}", context.TraceIdentifier);
            await WriteResponseAsync(context, (int)ErrorCode.InternalError, "服务器内部错误，请稍后再试");
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static async Task WriteResponseAsync(HttpContext context, int code, string message)
    {
        if (context.Response.HasStarted) return;

        context.Response.Headers.Clear();
        context.Response.Body.SetLength(0);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.OK;

        var result = JsonSerializer.Serialize(
            new { code, message, data = (object?)null, traceId = context.TraceIdentifier },
            JsonOptions);
        await context.Response.WriteAsync(result);
    }
}