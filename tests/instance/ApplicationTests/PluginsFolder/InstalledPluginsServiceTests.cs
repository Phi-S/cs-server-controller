using System.Text.Json;
using Application.PluginsFolder;
using Domain;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared;
using Shared.ApiModels;
using TestHelper.RandomHelperFolder;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests.PluginsFolder;

public class InstalledPluginsServiceTests
{
    private readonly ITestOutputHelper _outputHelper;

    private const string JsonFileName = "installed-plugins.json";

    public InstalledPluginsServiceTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task UpdateOrInstallTest_Single_Ok()
    {
        // Arrange
        var (applicationServices, _) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var installedPluginVersionsService =
            provider.GetRequiredService<InstalledPluginsService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        var installedVersionsJsonPath = Path.Combine(options.Value.DATA_FOLDER, JsonFileName);
        File.Exists(installedVersionsJsonPath).Should().BeTrue();

        // Act
        var name = RandomHelper.RandomString();
        var version = RandomHelper.RandomString();
        var updateOrInstall = await installedPluginVersionsService.UpdateOrInstall(name, version);

        // Assert
        updateOrInstall.IsError.Should().BeFalse(updateOrInstall.ErrorMessage());
        File.Exists(installedVersionsJsonPath).Should().BeTrue();
        var json = await File.ReadAllTextAsync(installedVersionsJsonPath);
        var installedVersions = JsonSerializer.Deserialize<List<InstalledPluginVersionsModel>>(json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        installedVersions.Should()
            .NotBeNullOrEmpty()
            .And.ContainSingle();

        installedVersions!.First().Name.Should().Be(name.ToLower().Trim());
        installedVersions!.First().Version.Should().Be(version.ToLower().Trim());
    }

    [Fact]
    public async Task UpdateOrInstallTest_Single_Ok_Update()
    {
        // Arrange
        var (applicationServices, _) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var installedPluginVersionsService =
            provider.GetRequiredService<InstalledPluginsService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        var installedVersionsJsonPath = Path.Combine(options.Value.DATA_FOLDER, JsonFileName);
        File.Exists(installedVersionsJsonPath).Should().BeTrue();

        // Act
        var name = RandomHelper.RandomString();
        var version = RandomHelper.RandomString();
        var install = await installedPluginVersionsService.UpdateOrInstall(name, version);
        install.IsError.Should().BeFalse(install.ErrorMessage());

        var updatedVersion = RandomHelper.RandomString();
        var update = await installedPluginVersionsService.UpdateOrInstall(name, updatedVersion);

        // Assert
        update.IsError.Should().BeFalse(update.ErrorMessage());
        File.Exists(installedVersionsJsonPath).Should().BeTrue();
        var json = await File.ReadAllTextAsync(installedVersionsJsonPath);
        var installedVersions = JsonSerializer.Deserialize<List<InstalledPluginVersionsModel>>(json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        installedVersions.Should()
            .NotBeNullOrEmpty()
            .And.ContainSingle();

        installedVersions!.First().Name.Should().Be(name.ToLower().Trim());
        installedVersions!.First().Version.Should().Be(updatedVersion.ToLower().Trim());
    }

    [Fact]
    public async Task UpdateOrInstallTest_Single_NOK_NameEmpty()
    {
        // Arrange
        var (applicationServices, _) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var installedPluginVersionsService =
            provider.GetRequiredService<InstalledPluginsService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        var installedVersionsJsonPath = Path.Combine(options.Value.DATA_FOLDER, JsonFileName);
        File.Exists(installedVersionsJsonPath).Should().BeTrue();

        // Act
        var name = "";
        var version = RandomHelper.RandomString();
        var updateOrInstall = await installedPluginVersionsService.UpdateOrInstall(name, version);

        // Assert
        updateOrInstall.IsError.Should().BeTrue();
        updateOrInstall.FirstError.Description.Should().Be("Name is empty");
    }

    [Fact]
    public async Task UpdateOrInstallTest_Single_NOK_VersionEmpty()
    {
        // Arrange
        var (applicationServices, _) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var installedPluginVersionsService =
            provider.GetRequiredService<InstalledPluginsService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        var installedVersionsJsonPath = Path.Combine(options.Value.DATA_FOLDER, JsonFileName);
        File.Exists(installedVersionsJsonPath).Should().BeTrue();

        // Act
        var name = RandomHelper.RandomString();
        var version = "";
        var updateOrInstall = await installedPluginVersionsService.UpdateOrInstall(name, version);

        // Assert
        updateOrInstall.IsError.Should().BeTrue();
        updateOrInstall.FirstError.Description.Should().Be("Version is empty");
    }

    [Fact]
    public async Task UpdateOrInstallTest_Multiple_Ok()
    {
        // Arrange
        var (applicationServices, _) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var installedPluginVersionsService =
            provider.GetRequiredService<InstalledPluginsService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        var installedVersionsJsonPath = Path.Combine(options.Value.DATA_FOLDER, JsonFileName);
        File.Exists(installedVersionsJsonPath).Should().BeTrue();

        // Act
        var name1 = RandomHelper.RandomString();
        var version1 = RandomHelper.RandomString();
        var updateOrInstall1 = await installedPluginVersionsService.UpdateOrInstall(name1, version1);

        var name2 = RandomHelper.RandomString();
        var version2 = RandomHelper.RandomString();
        var updateOrInstall2 = await installedPluginVersionsService.UpdateOrInstall(name2, version2);

        // Assert
        updateOrInstall1.IsError.Should().BeFalse(updateOrInstall1.ErrorMessage());
        updateOrInstall2.IsError.Should().BeFalse(updateOrInstall2.ErrorMessage());
        File.Exists(installedVersionsJsonPath).Should().BeTrue();
        var json = await File.ReadAllTextAsync(installedVersionsJsonPath);
        var installedVersions = JsonSerializer.Deserialize<List<InstalledPluginVersionsModel>>(json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        installedVersions.Should()
            .NotBeNullOrEmpty()
            .And.Subject.Count().Should().Be(2);

        var entry1 = installedVersions!.First();
        var entry2 = installedVersions!.Last();

        entry1.Name.Should().Be(name1.ToLower().Trim());
        entry1.Version.Should().Be(version1.ToLower().Trim());

        entry2.Name.Should().Be(name2.ToLower().Trim());
        entry2.Version.Should().Be(version2.ToLower().Trim());
    }

    [Fact]
    public async Task UpdateOrInstallTest_Multiple_Ok_Update()
    {
        // Arrange
        var (applicationServices, _) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var installedPluginVersionsService =
            provider.GetRequiredService<InstalledPluginsService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        var installedVersionsJsonPath = Path.Combine(options.Value.DATA_FOLDER, JsonFileName);
        File.Exists(installedVersionsJsonPath).Should().BeTrue();

        // Act
        var name1 = RandomHelper.RandomString();
        var version1 = RandomHelper.RandomString();
        var updateOrInstall1 = await installedPluginVersionsService.UpdateOrInstall(name1, version1);
        updateOrInstall1.IsError.Should().BeFalse(updateOrInstall1.ErrorMessage());

        var name2 = RandomHelper.RandomString();
        var version2 = RandomHelper.RandomString();
        var updateOrInstall2 = await installedPluginVersionsService.UpdateOrInstall(name2, version2);
        updateOrInstall2.IsError.Should().BeFalse(updateOrInstall2.ErrorMessage());

        var version2Update = RandomHelper.RandomString();
        var update = await installedPluginVersionsService.UpdateOrInstall(name2, version2Update);

        // Assert
        update.IsError.Should().BeFalse(update.ErrorMessage());
        File.Exists(installedVersionsJsonPath).Should().BeTrue();
        var json = await File.ReadAllTextAsync(installedVersionsJsonPath);
        var installedVersions = JsonSerializer.Deserialize<List<InstalledPluginVersionsModel>>(json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        installedVersions.Should()
            .NotBeNullOrEmpty()
            .And.Subject.Count().Should().Be(2);

        var entry1 = installedVersions!.First();
        var entry2 = installedVersions!.Last();

        entry1.Name.Should().Be(name1.ToLower().Trim());
        entry1.Version.Should().Be(version1.ToLower().Trim());

        entry2.Name.Should().Be(name2.ToLower().Trim());
        entry2.Version.Should().Be(version2Update.ToLower().Trim());
    }

    [Fact]
    public async Task GetAllTest_Single_Ok()
    {
        // Arrange
        var (applicationServices, _) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var installedPluginVersionsService =
            provider.GetRequiredService<InstalledPluginsService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        var installedVersionsJsonPath = Path.Combine(options.Value.DATA_FOLDER, JsonFileName);
        File.Exists(installedVersionsJsonPath).Should().BeTrue();

        var name = RandomHelper.RandomString();
        var version = RandomHelper.RandomString();
        var updateOrInstall = await installedPluginVersionsService.UpdateOrInstall(name, version);

        updateOrInstall.IsError.Should().BeFalse(updateOrInstall.ErrorMessage());
        File.Exists(installedVersionsJsonPath).Should().BeTrue();

        // Act
        var getAll = await installedPluginVersionsService.GetAll();

        // Assert
        getAll.IsError.Should().BeFalse(getAll.ErrorMessage());
        getAll.Value.Should().NotBeNull()
            .And.Subject.Count().Should().Be(1);

        var entry = getAll.Value.First();
        entry.Name.Should().Be(name.ToLower().Trim());
        entry.Version.Should().Be(version.ToLower().Trim());
    }

    [Fact]
    public async Task GetAllTest_Multiple_Ok()
    {
        // Arrange
        var (applicationServices, _) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var installedPluginVersionsService =
            provider.GetRequiredService<InstalledPluginsService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        var installedVersionsJsonPath = Path.Combine(options.Value.DATA_FOLDER, JsonFileName);
        File.Exists(installedVersionsJsonPath).Should().BeTrue();

        var name1 = RandomHelper.RandomString();
        var version1 = RandomHelper.RandomString();
        var updateOrInstall1 = await installedPluginVersionsService.UpdateOrInstall(name1, version1);
        updateOrInstall1.IsError.Should().BeFalse(updateOrInstall1.ErrorMessage());

        var name2 = RandomHelper.RandomString();
        var version2 = RandomHelper.RandomString();
        var updateOrInstall2 = await installedPluginVersionsService.UpdateOrInstall(name2, version2);
        updateOrInstall2.IsError.Should().BeFalse(updateOrInstall2.ErrorMessage());

        // Act
        var getAll = await installedPluginVersionsService.GetAll();

        // Assert
        getAll.IsError.Should().BeFalse(getAll.ErrorMessage());
        getAll.Value.Should().NotBeNull()
            .And.Subject.Count().Should().Be(2);

        var entry1 = getAll.Value.First();
        entry1.Name.Should().Be(name1.ToLower().Trim());
        entry1.Version.Should().Be(version1.ToLower().Trim());

        var entry2 = getAll.Value.Last();
        entry2.Name.Should().Be(name2.ToLower().Trim());
        entry2.Version.Should().Be(version2.ToLower().Trim());
    }

    [Fact]
    public async Task IsInstalledTest_Single_Ok()
    {
        // Arrange
        var (applicationServices, _) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var installedPluginVersionsService =
            provider.GetRequiredService<InstalledPluginsService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        var installedVersionsJsonPath = Path.Combine(options.Value.DATA_FOLDER, JsonFileName);
        File.Exists(installedVersionsJsonPath).Should().BeTrue();

        var name = RandomHelper.RandomString();
        var version = RandomHelper.RandomString();
        var updateOrInstall = await installedPluginVersionsService.UpdateOrInstall(name, version);

        updateOrInstall.IsError.Should().BeFalse(updateOrInstall.ErrorMessage());
        File.Exists(installedVersionsJsonPath).Should().BeTrue();

        // Act
        var isInstalled = await installedPluginVersionsService.IsInstalled(name, version);

        // Assert
        isInstalled.IsError.Should().BeFalse(isInstalled.ErrorMessage());
        isInstalled.Value.Should().BeTrue();
    }

    [Fact]
    public async Task IsInstalledTest_Multiple_Ok()
    {
        // Arrange
        var (applicationServices, _) = ServicesSetup.GetApplication(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var installedPluginVersionsService =
            provider.GetRequiredService<InstalledPluginsService>();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        var installedVersionsJsonPath = Path.Combine(options.Value.DATA_FOLDER, JsonFileName);
        File.Exists(installedVersionsJsonPath).Should().BeTrue();

        var name1 = RandomHelper.RandomString();
        var version1 = RandomHelper.RandomString();
        var updateOrInstall1 = await installedPluginVersionsService.UpdateOrInstall(name1, version1);
        updateOrInstall1.IsError.Should().BeFalse(updateOrInstall1.ErrorMessage());

        var name2 = RandomHelper.RandomString();
        var version2 = RandomHelper.RandomString();
        var updateOrInstall2 = await installedPluginVersionsService.UpdateOrInstall(name2, version2);
        updateOrInstall2.IsError.Should().BeFalse(updateOrInstall2.ErrorMessage());

        // Act
        var isInstalled1 = await installedPluginVersionsService.IsInstalled(name1, version1);
        var isInstalled2 = await installedPluginVersionsService.IsInstalled(name2, version2);

        // Assert
        isInstalled1.IsError.Should().BeFalse(isInstalled2.ErrorMessage());
        isInstalled1.Value.Should().BeTrue();
        isInstalled2.IsError.Should().BeFalse(isInstalled2.ErrorMessage());
        isInstalled2.Value.Should().BeTrue();
    }
}