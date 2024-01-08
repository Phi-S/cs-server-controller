using Application.ServerPluginsFolder;
using Domain;
using Microsoft.Extensions.Options;
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
    public async Task TestMetamodDownload()
    {
        // Arrange
        var folderGuid = Guid.NewGuid();
        _outputHelper.WriteLine($"FolderGuid: {folderGuid}");
        var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "cs-controller-instance-unit-tests", folderGuid.ToString());
        var addonsFolder = Path.Combine(dataFolder, "addons");
        Directory.CreateDirectory(addonsFolder);
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

        // Act
        var downloadMetamod = await service.DownloadMetamod(dataFolder);

        // Assert
        if (downloadMetamod.IsError)
        {
            _outputHelper.WriteLine($"Failed to install metamod. {downloadMetamod}");
            Assert.Fail();
        }

        Assert.True(File.Exists(Path.Combine(addonsFolder, "metamod.vdf")));
        Assert.True(File.Exists(Path.Combine(addonsFolder, "metamod_x64.vdf")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "metamod")));
        Assert.True(File.Exists(Path.Combine(addonsFolder, "metamod", "metaplugins.ini")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "metamod", "bin")));
    }

    [Fact]
    public async Task TestCounterStrikeSharpDownload()
    {
        // Arrange
        var folderGuid = Guid.NewGuid();
        _outputHelper.WriteLine($"FolderGuid: {folderGuid}");
        var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "cs-controller-instance-unit-tests", folderGuid.ToString());
        var addonsFolder = Path.Combine(dataFolder, "addons");
        Directory.CreateDirectory(addonsFolder);

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

        // Act
        var downloadCounterStrikeSharp = await service.DownloadCounterStrikeSharp(dataFolder);

        // Assert
        if (downloadCounterStrikeSharp.IsError)
        {
            _outputHelper.WriteLine($"Failed to install CounterStrikeSharp. {downloadCounterStrikeSharp}");
            Assert.Fail();
        }

        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "metamod")));
        Assert.True(File.Exists(Path.Combine(addonsFolder, "metamod", "counterstrikesharp.vdf")));

        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "counterstrikesharp")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "counterstrikesharp", "api")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "counterstrikesharp", "gamedata")));
        Assert.True(File.Exists(Path.Combine(addonsFolder, "counterstrikesharp", "gamedata", "gamedata.json")));
    }
}