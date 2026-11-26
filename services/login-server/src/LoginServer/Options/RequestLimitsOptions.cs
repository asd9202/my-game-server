namespace MyGame.LoginServer.Options;

public sealed class RequestLimitsOptions
{
    public const string SectionName = "RequestLimits";

    /// <summary>요청 본문 최대 바이트 수. 필수 설정이며 0 이하이면 시작 시점에 실패한다.</summary>
    public long MaxBodyBytes { get; init; }
}
