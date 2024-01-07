using Application.ServerPluginsFolder;
using Domain;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
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
        var options = Options.Create(new AppOptions
        {
            APP_NAME = "null",
            IP_OR_DOMAIN = "null",
            PORT = "null",
            STEAM_USERNAME = "null",
            STEAM_PASSWORD = "null",
            LOGIN_TOKEN = "null",
            DATA_FOLDER = @"C:\programming\files\test"
        });
        var httpClient = new HttpClient();
        var service = new ServerPluginsService(options, httpClient);
        await service.InstallBase();
    }
}