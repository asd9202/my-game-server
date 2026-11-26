using Google.Protobuf;

namespace MyGame.LoginServer.Protobuf;

public static class ProtobufEndpointExtensions
{
    /// <summary>
    /// protobuf 요청/응답 엔드포인트를 등록한다. Accepts/Produces 메타데이터를 항상 붙여
    /// Content-Type 불일치를 라우팅 단계에서 415 로 거부하도록 보장한다.
    /// </summary>
    public static RouteHandlerBuilder MapProtobufPost<TRequest, TResponse>(
        this IEndpointRouteBuilder endpoints, string pattern, Delegate handler)
        where TRequest : IMessage<TRequest>
        where TResponse : IMessage<TResponse> =>
        endpoints.MapPost(pattern, handler)
            .Accepts<TRequest>(ProtobufMediaType.Value)
            .Produces<TResponse>(StatusCodes.Status200OK, ProtobufMediaType.Value)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status413PayloadTooLarge)
            .Produces(StatusCodes.Status415UnsupportedMediaType);
}
