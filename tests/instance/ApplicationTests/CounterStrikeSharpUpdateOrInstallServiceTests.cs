using Application.CounterStrikeSharpUpdateOrInstallFolder;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared;
using TestHelper.TestSetup;
using TestHelper.UnitTestFolderFolder;
using Xunit.Abstractions;

namespace ApplicationTests;

public class CounterStrikeSharpUpdateOrInstallServiceTests
{
    private readonly ITestOutputHelper _outputHelper;

    public CounterStrikeSharpUpdateOrInstallServiceTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task TestMetamodInstall()
    {
        // Arrange
        var (applicationServices, unitTestFolder) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var counterStrikeSharpUpdateOrInstallService =
            provider.GetRequiredService<CounterStrikeSharpUpdateOrInstallService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        // Act
        var downloadMetamod = await counterStrikeSharpUpdateOrInstallService.InstallMetamod();

        // Assert
        if (downloadMetamod.IsError)
        {
            _outputHelper.WriteLine($"Failed to install metamod. {downloadMetamod}");
            Assert.Fail();
        }

        var addonsFolder = Path.Combine(options.Value.SERVER_FOLDER, "game", "csgo", "addons");
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
        var (applicationServices, unitTestFolder) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var counterStrikeSharpUpdateOrInstallService =
            provider.GetRequiredService<CounterStrikeSharpUpdateOrInstallService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        // Act
        var downloadMetamod = await counterStrikeSharpUpdateOrInstallService.InstallMetamod();
        var downloadCounterStrikeSharp = await counterStrikeSharpUpdateOrInstallService.InstallCounterStrikeSharp();

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

        var addonsFolder = Path.Combine(options.Value.SERVER_FOLDER, "game", "csgo", "addons");
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
        var (applicationServices, unitTestFolder) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var counterStrikeSharpUpdateOrInstallService =
            provider.GetRequiredService<CounterStrikeSharpUpdateOrInstallService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        // Act
        var downloadCounterStrikeSharp = await counterStrikeSharpUpdateOrInstallService.InstallCounterStrikeSharp();

        // Assert
        if (downloadCounterStrikeSharp.IsError)
        {
            _outputHelper.WriteLine($"Failed to install CounterStrikeSharp. {downloadCounterStrikeSharp}");
            Assert.Fail();
        }

        var addonsFolder = Path.Combine(options.Value.SERVER_FOLDER, "game", "csgo", "addons");
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "metamod")));
        Assert.True(File.Exists(Path.Combine(addonsFolder, "metamod", "counterstrikesharp.vdf")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "counterstrikesharp")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "counterstrikesharp", "api")));
        Assert.True(Directory.Exists(Path.Combine(addonsFolder, "counterstrikesharp", "gamedata")));
        Assert.True(File.Exists(Path.Combine(addonsFolder, "counterstrikesharp", "gamedata", "gamedata.json")));
    }

    [Fact]
    public async Task CreateCoreCfgTest()
    {
        // Arrange
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);

        // Act
        CounterStrikeSharpUpdateOrInstallService.CreateCoreCfg(testFolder);

        // Assert
        var filePath = Path.Combine(testFolder, "core.json");
        Assert.True(File.Exists(filePath));
        var fileContent = await File.ReadAllTextAsync(filePath);
        Assert.Contains(@"""PublicChatTrigger"": [ ""."", ""!"" ],", fileContent);
    }

    [Fact]
    public async Task TestFullUpdateOrInstall()
    {
        // Arrange
        var (applicationServices, unitTestFolder) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var serverPluginsService = provider.GetRequiredService<CounterStrikeSharpUpdateOrInstallService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        // Creates sample gameinfo.gi path
        var csgoFolder = Path.Combine(
            options.Value.SERVER_FOLDER,
            "game",
            "csgo");
        Directory.CreateDirectory(csgoFolder);
        await File.WriteAllTextAsync(Path.Combine(csgoFolder, "gameinfo.gi"),
            "\t\t\tGame_LowViolence\tcsgo_lv // Perfect World content override");

        // Act
        var updateOrInstall = await serverPluginsService.StartUpdateOrInstall();

        // Assert
        if (updateOrInstall.IsError)
        {
            Assert.Fail(updateOrInstall.ErrorMessage());
        }

        Assert.False(updateOrInstall.IsError);
    }
}