using Microsoft.Extensions.Options;
using MyGame.LoginServer.Endpoints;
using MyGame.LoginServer.Http;
using MyGame.LoginServer.Options;

var builder = WebApplication.CreateBuilder(args);

// 서비스 설정은 config/<환경>/login-server.json 에 둔다(AGENTS.md §2). 경로는 환경 변수로 받으며 없으면 시작하지 않는다.
const string ConfigFileKey = "LOGIN_SERVER_CONFIG_FILE";
var configFile = builder.Configuration[ConfigFileKey];
if (string.IsNullOrWhiteSpace(configFile))
{
    throw new InvalidOperationException($"필수 환경 변수 {ConfigFileKey} 가 설정되지 않았습니다.");
}
builder.Configuration.AddJsonFile(Path.GetFullPath(configFile), optional: false, reloadOnChange: false);
// 설정 파일보다 환경 변수·명령줄 인자가 우선하도록 다시 추가한다.
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

builder.Services.AddOptions<RequestLimitsOptions>()
    .BindConfiguration(RequestLimitsOptions.SectionName)
    .Validate(o => o.MaxBodyBytes > 0, $"{RequestLimitsOptions.SectionName}:{nameof(RequestLimitsOptions.MaxBodyBytes)} 는 0 보다 커야 합니다.")
    .ValidateOnStart();
builder.Services.AddOptions<DebugEndpointsOptions>()
    .BindConfiguration(DebugEndpointsOptions.SectionName);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks();
// 바디 역직렬화 실패 등 바인딩 오류는 개발 환경에서도 예외 페이지(500)가 아닌 400 으로 응답한다.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

var app = builder.Build();

app.UseMiddleware<RequestBodyLimitMiddleware>();

app.MapHealthChecks("/healthz");

if (app.Services.GetRequiredService<IOptions<DebugEndpointsOptions>>().Value.Enabled)
{
    app.MapDebugEndpoints();
}

app.Run();

// WebApplicationFactory<Program> 에서 참조하기 위해 공개한다.
public partial class Program;
