using MyGame.LoginServer.Protobuf;
using MyGame.Protocol.Debug.V1;

namespace MyGame.LoginServer.Endpoints;

public static partial class DebugEndpoints
{
    /// <summary>설정 키. 기본값 false — 운영 환경에 디버그 엔드포인트가 노출되지 않도록 명시적으로 켜야 한다.</summary>
    public const string EnabledConfigKey = "DebugEndpoints:Enabled";

    private const string LoggerCategory = "MyGame.LoginServer.Debug";

    public static IEndpointRouteBuilder MapDebugEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/debug");
        group.MapPost("/echo", Echo)
            .Accepts<EchoRequest>(ProtobufMediaType.Value)
            .Produces<EchoResponse>(StatusCodes.Status200OK, ProtobufMediaType.Value)
            .Produces(StatusCodes.Status400BadRequest);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Echo received. MessageLength={MessageLength}")]
    private static partial void LogEchoReceived(ILogger logger, int messageLength);
}
