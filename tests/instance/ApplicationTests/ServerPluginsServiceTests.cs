using Application.ServerPluginsFolder;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TestHelper.TestLoggerFolder;
using TestHelper.TestSetup;
using TestHelper.UnitTestFolderFolder;
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
        var httpClient = new HttpClient();
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);
        Directory.CreateDirectory(Path.Combine(testFolder, "addons"));

        // Act
        var downloadMetamod = await ServerPluginsService.DownloadMetamod(httpClient, testFolder);

        // Assert
        if (downloadMetamod.IsError)
        {
            _outputHelper.WriteLine($"Failed to install metamod. {downloadMetamod}");
            Assert.Fail();
        }

        var addonsFolder = Path.Combine(testFolder, "addons");
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
        var httpClient = new HttpClient();
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);
        Directory.CreateDirectory(Path.Combine(testFolder, "addons"));
        
        // Act
        var downloadCounterStrikeSharp = await ServerPluginsService.DownloadCounterStrikeSharp(httpClient, testFolder);

        // Assert
        if (downloadCounterStrikeSharp.IsError)
        {
            _outputHelper.WriteLine($"Failed to install CounterStrikeSharp. {downloadCounterStrikeSharp}");
            Assert.Fail();
        }

        var addonsFolder = Path.Combine(testFolder, "addons");
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "metamod")));
        Assert.True(File.Exists(Path.Combine(addonsFolder, "metamod", "counterstrikesharp.vdf")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "counterstrikesharp")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "counterstrikesharp", "api")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "counterstrikesharp", "gamedata")));
        Assert.True(File.Exists(Path.Combine(addonsFolder, "counterstrikesharp", "gamedata", "gamedata.json")));
    }

    [Fact]
    public async Task TestInstallPlugins()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var serverPluginsService = provider.GetRequiredService<ServerPluginsService>();

        // Act
        var installPlugins = serverPluginsService.InstallPlugins();

        // Assert
        if (installPlugins.IsError)
        {
            _outputHelper.WriteLine($"Failed to install plugins. {installPlugins}");
            Assert.Fail();
        }
    }

    [Fact]
    public async Task CreateCoreCfgTest()
    {
        // Arrange
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);

        // Act
        ServerPluginsService.CreateCoreCfg(testFolder);

        // Assert
        var filePath = Path.Combine(testFolder, "core.json");
        Assert.True(File.Exists(filePath));
        var fileContent = await File.ReadAllTextAsync(filePath);
        Assert.Contains(@"""PublicChatTrigger"": [ ""."" ],", fileContent);
    }
}