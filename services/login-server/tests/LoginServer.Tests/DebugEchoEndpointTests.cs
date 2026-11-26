using System.Net;
using System.Net.Http.Headers;
using Google.Protobuf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using MyGame.LoginServer.Options;
using MyGame.LoginServer.Protobuf;
using MyGame.Protocol.Debug.V1;

namespace MyGame.LoginServer.Tests;

public sealed class DebugEchoEndpointTests(LoginServerFactory factory) : IClassFixture<LoginServerFactory>
{
    private const string EchoPath = "/v1/debug/echo";
    private const string DebugEnabledKey = $"{DebugEndpointsOptions.SectionName}:{nameof(DebugEndpointsOptions.Enabled)}";
    private const string MaxBodyBytesKey = $"{RequestLimitsOptions.SectionName}:{nameof(RequestLimitsOptions.MaxBodyBytes)}";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Echo_ReturnsSameMessageAndClientTime_WithServerTime()
    {
        using var client = factory.CreateClient();
        var request = new EchoRequest { Message = "hello 서버", ClientTimeUnixMs = 1_700_000_000_000 };
        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        using var response = await client.PostAsync(EchoPath, ToContent(request), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ProtobufMediaType.Value, response.Content.Headers.ContentType?.MediaType);
        var body = EchoResponse.Parser.ParseFrom(await response.Content.ReadAsByteArrayAsync(Ct));
        Assert.Equal(request.Message, body.Message);
        Assert.Equal(request.ClientTimeUnixMs, body.ClientTimeUnixMs);
        Assert.InRange(body.ServerTimeUnixMs, before, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task Echo_EmptyBody_ReturnsDefaultMessage()
    {
        // proto3 에서 빈 바이트열은 모든 필드가 기본값인 유효한 메시지다.
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(EchoPath, ToContent([]), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = EchoResponse.Parser.ParseFrom(await response.Content.ReadAsByteArrayAsync(Ct));
        Assert.Equal(string.Empty, body.Message);
    }

    [Fact]
    public async Task Echo_MalformedProtobuf_ReturnsBadRequest()
    {
        using var client = factory.CreateClient();
        // 필드 1(length-delimited)의 길이를 10 으로 선언했지만 실제 바이트가 부족한 잘린 메시지.
        byte[] truncated = [0x0A, 0x0A, 0x41];

        using var response = await client.PostAsync(EchoPath, ToContent(truncated), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Echo_NonProtobufContentType_ReturnsUnsupportedMediaType()
    {
        using var client = factory.CreateClient();
        using var content = new StringContent("""{"message":"hi"}""", MediaTypeHeaderValue.Parse("application/json"));

        using var response = await client.PostAsync(EchoPath, content, Ct);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Echo_BodyLargerThanLimit_ReturnsPayloadTooLarge()
    {
        const int limit = 16;
        await using var limited = factory.WithWebHostBuilder(
            builder => builder.UseSetting(MaxBodyBytesKey, limit.ToString()));
        using var client = limited.CreateClient();
        // 형식이 올바른 메시지여야 파싱 실패(400)가 아니라 크기 초과(413)를 검증할 수 있다.
        var request = new EchoRequest { Message = new string('a', limit * 2) };

        using var response = await client.PostAsync(EchoPath, ToContent(request), Ct);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Echo_WhenDisabled_ReturnsNotFound()
    {
        await using var disabled = factory.WithWebHostBuilder(
            builder => builder.UseSetting(DebugEnabledKey, "false"));
        using var client = disabled.CreateClient();

        using var response = await client.PostAsync(EchoPath, ToContent(new EchoRequest { Message = "hi" }), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Startup_InvalidMaxBodyBytes_Fails()
    {
        await using var invalid = factory.WithWebHostBuilder(
            builder => builder.UseSetting(MaxBodyBytesKey, "0"));

        Assert.Throws<OptionsValidationException>(() => invalid.CreateClient());
    }

    [Fact]
    public async Task Healthz_ReturnsOk()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/healthz", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static ByteArrayContent ToContent(IMessage message) => ToContent(message.ToByteArray());

    private static ByteArrayContent ToContent(byte[] bytes)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(ProtobufMediaType.Value);
        return content;
    }
}
