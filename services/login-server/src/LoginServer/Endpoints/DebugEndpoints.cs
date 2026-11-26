using MyGame.LoginServer.Protobuf;
using MyGame.Protocol.Debug.V1;

namespace MyGame.LoginServer.Endpoints;

public static partial class DebugEndpoints
{
    private const string LoggerCategory = "MyGame.LoginServer.Debug";

    public static IEndpointRouteBuilder MapDebugEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/debug");
        group.MapProtobufPost<EchoRequest, EchoResponse>("/echo", Echo);
        return app;
    }

    private static ProtobufResult<EchoResponse> Echo(
        ProtobufBody<EchoRequest> body,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory)
    {
        var request = body.Message;
        var logger = loggerFactory.CreateLogger(LoggerCategory);
        // 메시지 내용은 로그에 남기지 않는다(개인정보가 섞일 수 있음, AGENTS.md §4.3).
        LogEchoReceived(logger, request.Message.Length);

        return ProtobufResults.Ok(new EchoResponse
        {
            Message = request.Message,
            ClientTimeUnixMs = request.ClientTimeUnixMs,
            ServerTimeUnixMs = timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
        });
    }

    // 부하 테스트에서 대량 호출되므로 Debug 레벨로 남긴다.
    [LoggerMessage(Level = LogLevel.Debug, Message = "Echo received. MessageLength={MessageLength}")]
    private static partial void LogEchoReceived(ILogger logger, int messageLength);
}
