using Application.ServerPluginsFolder;
using Domain;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using TestHelper.TestLoggerFolder;
using Xunit.Abstractions;

namespace ApplicationTests;

public class ServerPluginsServiceTests
{
    private readonly ITestOutputHelper _outputHelper;

    public ServerPluginsServiceTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task TestDownload()
    {
        var folderGuid = Guid.NewGuid();
        var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "cs-controller-instance-unit-tests", folderGuid.ToString());
        var options = Options.Create(new AppOptions
        {
            APP_NAME = "null",
            IP_OR_DOMAIN = "null",
            PORT = "null",
            STEAM_USERNAME = "null",
            STEAM_PASSWORD = "null",
            LOGIN_TOKEN = "null",
            DATA_FOLDER = dataFolder
        });
        var logger = new XunitLogger<ServerPluginsService>(_outputHelper);
        var httpClient = new HttpClient();
        var service = new ServerPluginsService(logger, options, httpClient);
        await service.InstallBase();
    }
}