using System.Net;
using System.Net.Http.Headers;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc.Testing;
using MyGame.LoginServer.Endpoints;
using MyGame.LoginServer.Protobuf;
using MyGame.Protocol.Debug.V1;

namespace MyGame.LoginServer.Tests;

public sealed class DebugEchoEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EchoPath = "/v1/debug/echo";

    [Fact]
    public async Task Echo_ReturnsSameMessageAndClientTime_WithServerTime()
    {
        var client = factory.CreateClient();
        var request = new EchoRequest { Message = "hello 서버", ClientTimeUnixMs = 1_700_000_000_000 };
        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        using var response = await client.PostAsync(EchoPath, ToContent(request), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ProtobufMediaType.Value, response.Content.Headers.ContentType?.MediaType);
        var body = EchoResponse.Parser.ParseFrom(
            await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        Assert.Equal(request.Message, body.Message);
        Assert.Equal(request.ClientTimeUnixMs, body.ClientTimeUnixMs);
        Assert.InRange(body.ServerTimeUnixMs, before, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task Echo_EmptyBody_ReturnsDefaultMessage()
    {
        // proto3 에서 빈 바이트열은 모든 필드가 기본값인 유효한 메시지다.
        var client = factory.CreateClient();

        using var response = await client.PostAsync(EchoPath, ToContent([]), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = EchoResponse.Parser.ParseFrom(
            await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        Assert.Equal(string.Empty, body.Message);
    }

    [Fact]
    public async Task Echo_MalformedProtobuf_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        // 필드 1(length-delimited)의 길이를 10 으로 선언했지만 실제 바이트가 부족한 잘린 메시지.
        byte[] truncated = [0x0A, 0x0A, 0x41];

        using var response = await client.PostAsync(EchoPath, ToContent(truncated), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Echo_NonProtobufContentType_ReturnsUnsupportedMediaType()
    {
        var client = factory.CreateClient();
        using var content = new StringContent("""{"message":"hi"}""", MediaTypeHeaderValue.Parse("application/json"));

        using var response = await client.PostAsync(EchoPath, content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Echo_WhenDisabled_ReturnsNotFound()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.UseSetting(DebugEndpoints.EnabledConfigKey, "false"))
            .CreateClient();

        using var response = await client.PostAsync(
            EchoPath, ToContent(new EchoRequest { Message = "hi" }), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Healthz_ReturnsOk()
    {
        var client = factory.CreateClient();

        using var response = await client.GetAsync("/healthz", TestContext.Current.CancellationToken);

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
