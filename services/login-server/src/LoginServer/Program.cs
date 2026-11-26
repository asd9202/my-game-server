using MyGame.LoginServer.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks();
// 바디 역직렬화 실패 등 바인딩 오류는 개발 환경에서도 예외 페이지(500)가 아닌 400 으로 응답한다.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

var app = builder.Build();

app.MapHealthChecks("/healthz");

if (app.Configuration.GetValue<bool>(DebugEndpoints.EnabledConfigKey))
{
    app.MapDebugEndpoints();
}

app.Run();

// WebApplicationFactory<Program> 에서 참조하기 위해 공개한다.
public partial class Program;
