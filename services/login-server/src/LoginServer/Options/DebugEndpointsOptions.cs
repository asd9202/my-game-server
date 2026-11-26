namespace MyGame.LoginServer.Options;

public sealed class DebugEndpointsOptions
{
    public const string SectionName = "DebugEndpoints";

    /// <summary>기본값 false — 운영 환경에 디버그 엔드포인트가 노출되지 않도록 명시적으로 켜야 한다.</summary>
    public bool Enabled { get; init; }
}
