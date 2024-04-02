using Application.PluginsFolder;
using Domain;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests.PluginsFolder;

public class MetaModServiceTests
{
    private readonly ITestOutputHelper _outputHelper;

    public MetaModServiceTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task InstallOrUpdateTest()
    {
        // Arrange
        var (applicationServices, _) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var pluginService =
            provider.GetRequiredService<PluginInstallerService>();
        var installedPluginVersionsService =
            provider.GetRequiredService<InstalledPluginsService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        // Creates sample gameinfo.gi path
        Directory.CreateDirectory(options.Value.CSGO_FOLDER);
        await File.WriteAllTextAsync(Path.Combine(options.Value.CSGO_FOLDER, "gameinfo.gi"),
            "\t\t\tGame_LowViolence\tcsgo_lv // Perfect World content override");

        // Act
        var updateOrInstall = await pluginService.UpdateOrInstall("Metamod:Source", "2.0.0-git1282");

        // Assert
        updateOrInstall.IsError.Should().BeFalse(updateOrInstall.ErrorMessage());
        var addonsFolder = Path.Combine(options.Value.CSGO_FOLDER, "addons");
        Directory.Exists(Path.Combine(addonsFolder, "metamod")).Should().BeTrue();
        File.Exists(Path.Combine(addonsFolder, "metamod.vdf")).Should().BeTrue();
        File.Exists(Path.Combine(addonsFolder, "metamod_x64.vdf")).Should().BeTrue();
        Directory.Exists(Path.Combine(addonsFolder, "metamod")).Should().BeTrue();
        File.Exists(Path.Combine(addonsFolder, "metamod", "metaplugins.ini")).Should().BeTrue();
        Directory.Exists(Path.Combine(addonsFolder, "metamod", "bin")).Should().BeTrue();

        var isInstalled =
            await installedPluginVersionsService.IsInstalled("Metamod:Source", "2.0.0-git1282");
        isInstalled.IsError.Should().BeFalse(isInstalled.ErrorMessage());
        isInstalled.Value.Should().BeTrue();
    }
}