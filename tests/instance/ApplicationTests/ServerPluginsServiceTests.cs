using Application.ServerPluginsFolder;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task TestMetamodInstall()
    {
        // Arrange
        var httpClient = new HttpClient();
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);

        // Act
        var downloadMetamod = await ServerPluginsService.InstallMetamod(httpClient, testFolder);

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
    public async Task TestCounterStrikeSharpInstall()
    {
        // Arrange
        var httpClient = new HttpClient();
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);

        // Act
        var downloadCounterStrikeSharp = await ServerPluginsService.InstallCounterStrikeSharp(httpClient, testFolder);
        var downloadMetamod = await ServerPluginsService.InstallMetamod(httpClient, testFolder);

        // Assert
        if (downloadCounterStrikeSharp.IsError)
        {
            _outputHelper.WriteLine($"Failed to install CounterStrikeSharp. {downloadCounterStrikeSharp}");
            Assert.Fail();
        }

        if (downloadMetamod.IsError)
        {
            _outputHelper.WriteLine($"Failed to install metamod. {downloadMetamod}");
            Assert.Fail();
        }

        var addonsFolder = Path.Combine(testFolder, "addons");
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "metamod")));
        Assert.True(File.Exists(Path.Combine(addonsFolder, "metamod", "counterstrikesharp.vdf")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "counterstrikesharp")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "counterstrikesharp", "api")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "counterstrikesharp", "gamedata")));
        Assert.True(File.Exists(Path.Combine(addonsFolder, "counterstrikesharp", "gamedata", "gamedata.json")));

        Assert.True(File.Exists(Path.Combine(addonsFolder, "metamod.vdf")));
        Assert.True(File.Exists(Path.Combine(addonsFolder, "metamod_x64.vdf")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "metamod")));
        Assert.True(File.Exists(Path.Combine(addonsFolder, "metamod", "metaplugins.ini")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "metamod", "bin")));
    }

    [Fact]
    public async Task TestMetamodAndCounterStrikeSharpInstall()
    {
        // Arrange
        var httpClient = new HttpClient();
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);

        // Act
        var downloadCounterStrikeSharp = await ServerPluginsService.InstallCounterStrikeSharp(httpClient, testFolder);

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
        var (serviceCollection, testFolder) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var serverPluginsService = provider.GetRequiredService<ServerPluginsService>();
        var pluginsFolder = Path.Combine(testFolder, "server", "game", "csgo", "addons", "counterstrikesharp",
            "plugins", "disabled");
        Directory.CreateDirectory(pluginsFolder);

        // Act
        var installPlugins = serverPluginsService.InstallOrUpdatePlugins();

        // Assert
        if (installPlugins.IsError)
        {
            _outputHelper.WriteLine($"Failed to install plugins. {installPlugins}");
            Assert.Fail();
        }
    }
    
    [Fact]
    public async Task TestInstallPlugins_UpdateExistingPlugins()
    {
        // Arrange
        var (serviceCollection, testFolder) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var serverPluginsService = provider.GetRequiredService<ServerPluginsService>();
        var pluginsFolder = Path.Combine(testFolder, "server", "game", "csgo", "addons", "counterstrikesharp",
            "plugins", "disabled");
        Directory.CreateDirectory(pluginsFolder);

        // Act
        var installPlugins = serverPluginsService.InstallOrUpdatePlugins();
        _outputHelper.WriteLine("==========================================");
        var updatePlugins = serverPluginsService.InstallOrUpdatePlugins();
        
        // Assert
        if (installPlugins.IsError)
        {
            _outputHelper.WriteLine($"Failed to install plugins. {installPlugins}");
            Assert.Fail();
        }
        
        if (updatePlugins.IsError)
        {
            _outputHelper.WriteLine($"Failed to update plugins. {updatePlugins}");
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