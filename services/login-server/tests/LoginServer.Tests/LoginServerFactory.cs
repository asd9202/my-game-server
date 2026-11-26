using Microsoft.AspNetCore.Mvc.Testing;

namespace MyGame.LoginServer.Tests;

/// <summary>
/// 저장소의 config/local/login-server.json 을 읽어 서버를 띄우는 테스트용 팩토리.
/// 서버는 LOGIN_SERVER_CONFIG_FILE 이 없으면 시작하지 않으므로 호스트 생성 전에 설정한다.
/// </summary>
public sealed class LoginServerFactory : WebApplicationFactory<Program>
{
    private const string SolutionFileName = "MyGameServer.slnx";

    static LoginServerFactory()
    {
        var configFile = Path.Combine(FindRepositoryRoot(), "config", "local", "login-server.json");
        Environment.SetEnvironmentVariable("LOGIN_SERVER_CONFIG_FILE", configFile);
    }

    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException($"{SolutionFileName} 를 찾을 수 없습니다: {AppContext.BaseDirectory}");
    }
}
