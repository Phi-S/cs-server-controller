using Application.ConfigEditorFolder;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared;
using TestHelper.RandomHelperFolder;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests;

public class ConfigEditorServiceTests
{
    private readonly ITestOutputHelper _outputHelper;

    public ConfigEditorServiceTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    private static string GetConfigFolder(string serverFolder) => Path.Combine(serverFolder, "game", "csgo", "cfg");
    private static string GetEditedConfigFolder(string dataFolder) => Path.Combine(dataFolder, "edited-config");

    [Fact]
    public async Task GetExistingConfigsTest()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();
        var configFolder = GetConfigFolder(options.Value.SERVER_FOLDER);
        Directory.CreateDirectory(configFolder);

        var shouldBeConfigs = new List<string>();
        for (var i = 0; i < RandomHelper.RandomInt(100); i++)
        {
            var configName = $"{RandomHelper.RandomString(50)}.cfg";
            var configPath = Path.Combine(configFolder, configName);
            await File.WriteAllTextAsync(configPath, RandomHelper.RandomString());
            shouldBeConfigs.Add(configName);
        }

        var configEditorService = provider.GetRequiredService<ConfigEditorService>();

        // Act
        var existingConfigs = configEditorService.GetExistingConfigs();

        // Assert
        if (existingConfigs.IsError)
        {
            _outputHelper.WriteLine(existingConfigs.ErrorMessage());
            Assert.Fail();
        }

        Assert.Equal(shouldBeConfigs.Count, existingConfigs.Value.Count);
        foreach (var shouldBeConfig in shouldBeConfigs)
        {
            Assert.Contains(shouldBeConfig, existingConfigs.Value);
        }
    }

    [Fact]
    public async Task GetExistingConfigsTest_WithEditedConfigs()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        var configFolder = GetConfigFolder(options.Value.SERVER_FOLDER);
        Directory.CreateDirectory(configFolder);
        var shouldBeConfigs = new List<string>();
        for (var i = 0; i < RandomHelper.RandomInt(100); i++)
        {
            var configName = $"{RandomHelper.RandomString(50)}.cfg";
            var configPath = Path.Combine(configFolder, configName);
            await File.WriteAllTextAsync(configPath, RandomHelper.RandomString());
            shouldBeConfigs.Add(configName);
        }

        var editedConfigFolder = GetEditedConfigFolder(options.Value.DATA_FOLDER);
        Directory.CreateDirectory(editedConfigFolder);
        for (var i = 0; i < RandomHelper.RandomInt(100); i++)
        {
            var configName = $"{RandomHelper.RandomString(50)}.cfg";
            var configPath = Path.Combine(editedConfigFolder, configName);
            await File.WriteAllTextAsync(configPath, RandomHelper.RandomString());
            shouldBeConfigs.Add(configName);
        }

        var configEditorService = provider.GetRequiredService<ConfigEditorService>();

        // Act
        var existingConfigs = configEditorService.GetExistingConfigs();

        // Assert
        if (existingConfigs.IsError)
        {
            _outputHelper.WriteLine(existingConfigs.ErrorMessage());
            Assert.Fail();
        }

        Assert.Equal(shouldBeConfigs.Count, existingConfigs.Value.Count);
        foreach (var shouldBeConfig in shouldBeConfigs)
        {
            Assert.Contains(shouldBeConfig, existingConfigs.Value);
        }
    }

    [Fact]
    public async Task GetExistingConfigsTest_WithEditedConfigsDistinct()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        var configFolder = GetConfigFolder(options.Value.SERVER_FOLDER);
        Directory.CreateDirectory(configFolder);
        var shouldBeConfigs = new List<string>();
        for (var i = 0; i < RandomHelper.RandomInt(100); i++)
        {
            var configName = $"{RandomHelper.RandomString(50)}.cfg";
            var configPath = Path.Combine(configFolder, configName);
            await File.WriteAllTextAsync(configPath, RandomHelper.RandomString());
            shouldBeConfigs.Add(configName);
        }

        var editedConfigFolder = GetEditedConfigFolder(options.Value.DATA_FOLDER);
        Directory.CreateDirectory(editedConfigFolder);
        for (var i = 0; i < RandomHelper.RandomInt(100); i++)
        {
            var configName = $"{RandomHelper.RandomString(50)}.cfg";
            var configPath = Path.Combine(editedConfigFolder, configName);
            await File.WriteAllTextAsync(configPath, RandomHelper.RandomString());
            shouldBeConfigs.Add(configName);
        }

        var duplicatedConfigName = $"{RandomHelper.RandomString()}.cfg";
        var duplicatedConfigPath = Path.Combine(editedConfigFolder, duplicatedConfigName);
        var duplicatedEditedConfigPath = Path.Combine(editedConfigFolder, duplicatedConfigName);
        await File.WriteAllTextAsync(duplicatedConfigPath, RandomHelper.RandomString());
        await File.WriteAllTextAsync(duplicatedEditedConfigPath, RandomHelper.RandomString());
        shouldBeConfigs.Add(duplicatedConfigName);

        var configEditorService = provider.GetRequiredService<ConfigEditorService>();

        // Act
        var existingConfigs = configEditorService.GetExistingConfigs();

        // Assert
        if (existingConfigs.IsError)
        {
            _outputHelper.WriteLine(existingConfigs.ErrorMessage());
            Assert.Fail();
        }

        Assert.Equal(shouldBeConfigs.Count, existingConfigs.Value.Count);
        foreach (var shouldBeConfig in shouldBeConfigs)
        {
            Assert.Contains(shouldBeConfig, existingConfigs.Value);
        }
    }


    [Fact]
    public async Task GetConfigFileTest()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();
        var configFolder = GetConfigFolder(options.Value.SERVER_FOLDER);
        var configName = $"{RandomHelper.RandomString()}.cfg";
        var configPath = Path.Combine(configFolder, configName);
        const string shouldBeConfigFileContent = """
                                                 game_mode 0
                                                 game_type 0

                                                 sv_cheats 1
                                                 bot_quota 99
                                                 bot_quota_mode normal
                                                 bot_kick
                                                 bot_stop 1

                                                 mp_limitteams 0
                                                 mp_autoteambalance 0

                                                 mp_respawn_on_death_t 1
                                                 mp_respawn_on_death_ct 1

                                                 mp_autokick false
                                                 mp_free_armor 2

                                                 mp_buy_anywhere 1
                                                 mp_maxmoney 100000
                                                 mp_startmoney 100000
                                                 mp_afterroundmoney 100000
                                                 mp_buytime 100000

                                                 sv_showimpacts 1
                                                 sv_showimpacts_time 10

                                                 sv_grenade_trajectory_prac_pipreview 1
                                                 sv_grenade_trajectory_prac_trailtime 8.000000
                                                 sv_grenade_trajectory_time_spectator 8.000000

                                                 sv_infinite_ammo 1
                                                 ammo_grenade_limit_total 5
                                                 mp_ct_default_grenades "weapon_incgrenade weapon_hegrenade weapon_smokegrenade weapon_flashbang weapon_decoy"
                                                 mp_t_default_grenades "weapon_incgrenade weapon_hegrenade weapon_smokegrenade weapon_flashbang weapon_dec

                                                 mp_death_drop_defuser 0
                                                 mp_death_drop_grenade 0
                                                 mp_death_drop_gun 0

                                                 mp_roundtime 60
                                                 mp_roundtime_defuse 60
                                                 mp_roundtime_hostage 60
                                                 mp_freezetime 0
                                                 mp_team_intro_time 0
                                                 mp_respawn_immunitytime 0
                                                 mp_warmup_pausetimer 1
                                                 mp_warmuptime 1
                                                 mp_restartgame 1
                                                 buddha true
                                                 mp_warmup_end
                                                 """;
        Directory.CreateDirectory(configFolder);
        await File.WriteAllTextAsync(configPath, shouldBeConfigFileContent);

        var configEditorService = provider.GetRequiredService<ConfigEditorService>();

        // Act
        var configContent = await configEditorService.GetConfigFile(configName);

        // Assert
        if (configContent.IsError)
        {
            _outputHelper.WriteLine(configContent.ErrorMessage());
            Assert.Fail();
        }

        Assert.Equal(shouldBeConfigFileContent, configContent);
    }

    [Fact]
    public async Task GetConfigFileTest_EditedConfig()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();

        var configFolder = GetConfigFolder(options.Value.SERVER_FOLDER);
        Directory.CreateDirectory(configFolder);
        var editedConfigFolder = GetEditedConfigFolder(options.Value.DATA_FOLDER);
        Directory.CreateDirectory(editedConfigFolder);

        var configName = $"{RandomHelper.RandomString()}.cfg";

        var configPath = Path.Combine(configFolder, configName);
        await File.WriteAllTextAsync(configPath, RandomHelper.RandomString(150));

        var shouldBeConfigFileContent = RandomHelper.RandomString(200);
        var editedConfigPath = Path.Combine(editedConfigFolder, configName);
        await File.WriteAllTextAsync(editedConfigPath, shouldBeConfigFileContent);

        var configEditorService = provider.GetRequiredService<ConfigEditorService>();

        // Act
        var configContent = await configEditorService.GetConfigFile(configName);

        // Assert
        if (configContent.IsError)
        {
            _outputHelper.WriteLine(configContent.ErrorMessage());
            Assert.Fail();
        }

        Assert.Equal(shouldBeConfigFileContent, configContent);
    }

    [Fact]
    public async Task SetConfigFileTest()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_outputHelper);
        await using var provider = applicationServices.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AppOptions>>();
        Directory.CreateDirectory(GetConfigFolder(options.Value.SERVER_FOLDER));

        var configEditorService = provider.GetRequiredService<ConfigEditorService>();

        // Act
        var configName = RandomHelper.RandomString();
        var shouldBeConfigContent = RandomHelper.RandomString(200);
        var setConfigFileResult = await configEditorService.SetConfigFile(configName, shouldBeConfigContent);

        // Assert
        if (setConfigFileResult.IsError)
        {
            Assert.Fail(setConfigFileResult.ErrorMessage());
        }

        var configFolder = GetConfigFolder(options.Value.SERVER_FOLDER);
        var configPath = Path.Combine(configFolder, configName);
        Assert.True(File.Exists(configPath));
        var configContent = await File.ReadAllTextAsync(configPath);
        Assert.Equal(shouldBeConfigContent, configContent);

        var editedConfigFolder = GetEditedConfigFolder(options.Value.DATA_FOLDER);
        var editedConfigPath = Path.Combine(editedConfigFolder, configName);
        Assert.True(File.Exists(editedConfigPath));
        var editedConfigContent = await File.ReadAllTextAsync(editedConfigPath);
        Assert.Equal(shouldBeConfigContent, editedConfigContent);
    }
}