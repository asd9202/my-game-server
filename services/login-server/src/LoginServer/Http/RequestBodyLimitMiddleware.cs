using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using MyGame.LoginServer.Options;

namespace MyGame.LoginServer.Http;

/// <summary>
/// 요청 본문 크기 상한을 서버 구현과 무관하게 강제한다.
/// Kestrel 전역 설정에만 의존하면 TestServer 에서는 적용되지 않아 검증할 수 없으므로 앱 수준에서 처리한다.
/// </summary>
public sealed partial class RequestBodyLimitMiddleware(
    RequestDelegate next,
    IOptions<RequestLimitsOptions> options,
    ILogger<RequestBodyLimitMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestBodyLimitMiddleware> _logger = logger;
    private readonly long _maxBodyBytes = options.Value.MaxBodyBytes;

    public async Task InvokeAsync(HttpContext context)
    {
        // Content-Length 가 있으면 본문을 읽기 전에 바로 거절한다.
        if (context.Request.ContentLength > _maxBodyBytes)
        {
            LogBodyTooLarge(_logger, _maxBodyBytes);
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        // chunked 전송처럼 길이를 미리 알 수 없는 경우는 서버(Kestrel)가 읽는 도중 상한을 넘으면 예외를 던진다.
        var sizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
        {
            sizeFeature.MaxRequestBodySize = _maxBodyBytes;
        }

        try
        {
            await _next(context);
        }
        catch (BadHttpRequestException ex)
            when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge && !context.Response.HasStarted)
        {
            // 클라이언트 오류이므로 서버 오류(fail) 로그로 남기지 않고 413 으로 응답한다.
            LogBodyTooLarge(_logger, _maxBodyBytes);
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Request body too large. MaxBodyBytes={MaxBodyBytes}")]
    private static partial void LogBodyTooLarge(ILogger logger, long maxBodyBytes);
}
