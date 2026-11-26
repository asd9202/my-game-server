using Google.Protobuf;

namespace MyGame.LoginServer.Protobuf;

/// <summary>protobuf 응답을 application/x-protobuf 로 직렬화해 내려준다.</summary>
public sealed class ProtobufResult<T>(T message, int statusCode = StatusCodes.Status200OK) : IResult
    where T : IMessage<T>
{
    public T Message { get; } = message;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        response.StatusCode = statusCode;
        response.ContentType = ProtobufMediaType.Value;
        response.ContentLength = Message.CalculateSize();
        await response.Body.WriteAsync(Message.ToByteArray(), httpContext.RequestAborted);
    }
}

public static class ProtobufResults
{
    public static ProtobufResult<T> Ok<T>(T message)
        where T : IMessage<T> => new(message);
}
