using System.Diagnostics.CodeAnalysis;
using Google.Protobuf;
using Microsoft.Net.Http.Headers;

namespace MyGame.LoginServer.Protobuf;

/// <summary>
/// protobuf 요청 바디 바인더. 엔드포인트마다 직접 파싱하지 않도록 역직렬화를 여기 한 곳에 모은다 (AGENTS.md §4.2).
/// Minimal API 는 BindAsync 가 null 을 돌려주면 400 으로 응답한다.
/// </summary>
public sealed class ProtobufBody<T>
    where T : IMessage<T>, new()
{
    private static readonly MessageParser<T> _parser = new(() => new T());

    private ProtobufBody(T message) => Message = message;

    public T Message { get; }

    public static async ValueTask<ProtobufBody<T>?> BindAsync(HttpContext context)
    {
        var request = context.Request;
        // MapProtobufPost 로 등록하면 라우팅 단계에서 이미 415 로 걸러지므로 여기는 방어용이다.
        if (!IsProtobufContentType(request.ContentType))
        {
            return null;
        }

        // 본문 크기 상한은 RequestBodyLimitMiddleware 가 강제한다(초과 시 413).
        using var buffer = new MemoryStream();
        await request.Body.CopyToAsync(buffer, context.RequestAborted);

        if (!TryParse(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), out var message))
        {
            return null;
        }

        return new ProtobufBody<T>(message);
    }

    private static bool IsProtobufContentType(string? contentType) =>
        MediaTypeHeaderValue.TryParse(contentType, out var parsed)
        && parsed.MediaType.Equals(ProtobufMediaType.Value, StringComparison.OrdinalIgnoreCase);

    private static bool TryParse(ReadOnlySpan<byte> bytes, [NotNullWhen(true)] out T? message)
    {
        try
        {
            message = _parser.ParseFrom(bytes);
            return true;
        }
        catch (InvalidProtocolBufferException)
        {
            message = default;
            return false;
        }
    }
}
