using Application.StartParameterFolder;
using Domain;
using Microsoft.Extensions.Options;
using Shared;
using Shared.ApiModels;
using TestHelper.RandomHelperFolder;
using TestHelper.TestLoggerFolder;
using TestHelper.UnitTestOutputFolder;
using Xunit.Abstractions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ApplicationTests;

public class StartParameterServiceTests
{
    private readonly ITestOutputHelper _outputHelper;

    public StartParameterServiceTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public void TestGet_JsonFileDoseNotExists()
    {
        // Arrange
        var unitTestFolder = UnitTestOutputHelper.GetNewUnitTestFolder(_outputHelper);
        var options = Options.Create(new AppOptions
        {
            APP_NAME = "TestAppName",
            IP_OR_DOMAIN = "TestIpOrDomain",
            PORT = "TestPort",
            STEAM_USERNAME = "TestSteamUsername",
            STEAM_PASSWORD = "TestSteamPassword",
            LOGIN_TOKEN = "TestLoginToken",
            DATA_FOLDER = unitTestFolder
        });

        var startParametersJsonPath = Path.Combine(unitTestFolder, "start-parameter.json");
        var startParameterService =
            new StartParameterService(new XunitLogger<StartParameterService>(_outputHelper), options);

        // Act
        Assert.False(File.Exists(startParametersJsonPath));
        var startParameters = startParameterService.Get();

        // Assert
        if (startParameters.IsError)
        {
            Assert.Fail(startParameters.ErrorMessage());
        }

        Assert.False(startParameters.IsError);
        Assert.True(File.Exists(startParametersJsonPath));
    }

    [Fact]
    public void TestGet_JsonFileExists()
    {
        // Arrange
        var unitTestFolder = UnitTestOutputHelper.GetNewUnitTestFolder(_outputHelper);
        var options = Options.Create(new AppOptions
        {
            APP_NAME = "TestAppName",
            IP_OR_DOMAIN = "TestIpOrDomain",
            PORT = "TestPort",
            STEAM_USERNAME = "TestSteamUsername",
            STEAM_PASSWORD = "TestSteamPassword",
            LOGIN_TOKEN = "TestLoginToken",
            DATA_FOLDER = unitTestFolder
        });

        var startParametersJsonPath = Path.Combine(unitTestFolder, "start-parameter.json");
        var newStartParameters = new StartParameters(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            RandomHelper.RandomInt(),
            Guid.NewGuid().ToString(),
            RandomHelper.RandomInt(),
            RandomHelper.RandomInt(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString());
        var json = JsonSerializer.Serialize(newStartParameters);
        File.WriteAllText(startParametersJsonPath, json);

        var startParameterService =
            new StartParameterService(new XunitLogger<StartParameterService>(_outputHelper), options);

        // Act
        Assert.True(File.Exists(startParametersJsonPath));
        var startParameters = startParameterService.Get();

        // Assert
        if (startParameters.IsError)
        {
            Assert.Fail(startParameters.ErrorMessage());
        }

        Assert.False(startParameters.IsError);
        Assert.True(File.Exists(startParametersJsonPath));
        var startParametersFromFile = startParameters.Value;
        Assert.Equal(newStartParameters.ServerHostname, startParametersFromFile.ServerHostname);
        Assert.Equal(newStartParameters.ServerPassword, startParametersFromFile.ServerPassword);
        Assert.Equal(newStartParameters.MaxPlayer, startParametersFromFile.MaxPlayer);
        Assert.Equal(newStartParameters.StartMap, startParametersFromFile.StartMap);
        Assert.Equal(newStartParameters.GameMode, startParametersFromFile.GameMode);
        Assert.Equal(newStartParameters.GameType, startParametersFromFile.GameType);
        Assert.Equal(newStartParameters.LoginToken, startParametersFromFile.LoginToken);
        Assert.Equal(newStartParameters.AdditionalStartParameters, startParametersFromFile.AdditionalStartParameters);
    }

    [Fact]
    public void TestSet_JsonFileDoseNotExist()
    {
        // Arrange
        var unitTestFolder = UnitTestOutputHelper.GetNewUnitTestFolder(_outputHelper);
        var options = Options.Create(new AppOptions
        {
            APP_NAME = "TestAppName",
            IP_OR_DOMAIN = "TestIpOrDomain",
            PORT = "TestPort",
            STEAM_USERNAME = "TestSteamUsername",
            STEAM_PASSWORD = "TestSteamPassword",
            LOGIN_TOKEN = "TestLoginToken",
            DATA_FOLDER = unitTestFolder
        });

        var startParametersJsonPath = Path.Combine(unitTestFolder, "start-parameter.json");
        var newStartParameters = new StartParameters(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            RandomHelper.RandomInt(),
            Guid.NewGuid().ToString(),
            RandomHelper.RandomInt(),
            RandomHelper.RandomInt(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString());
        var startParameterService =
            new StartParameterService(new XunitLogger<StartParameterService>(_outputHelper), options);

        // Act
        Assert.False(File.Exists(startParametersJsonPath));
        startParameterService.Set(newStartParameters);

        // Assert
        Assert.True(File.Exists(startParametersJsonPath));
        var json = File.ReadAllText(startParametersJsonPath);
        var startParametersFromFile = JsonSerializer.Deserialize<StartParameters>(json);
        Assert.NotNull(startParametersFromFile);
        Assert.Equal(newStartParameters.ServerHostname, startParametersFromFile.ServerHostname);
        Assert.Equal(newStartParameters.ServerPassword, startParametersFromFile.ServerPassword);
        Assert.Equal(newStartParameters.MaxPlayer, startParametersFromFile.MaxPlayer);
        Assert.Equal(newStartParameters.StartMap, startParametersFromFile.StartMap);
        Assert.Equal(newStartParameters.GameMode, startParametersFromFile.GameMode);
        Assert.Equal(newStartParameters.GameType, startParametersFromFile.GameType);
        Assert.Equal(newStartParameters.LoginToken, startParametersFromFile.LoginToken);
        Assert.Equal(newStartParameters.AdditionalStartParameters, startParametersFromFile.AdditionalStartParameters);
    }

    [Fact]
    public void TestSet_JsonFileExists()
    {
        // Arrange
        var unitTestFolder = UnitTestOutputHelper.GetNewUnitTestFolder(_outputHelper);
        var options = Options.Create(new AppOptions
        {
            APP_NAME = "TestAppName",
            IP_OR_DOMAIN = "TestIpOrDomain",
            PORT = "TestPort",
            STEAM_USERNAME = "TestSteamUsername",
            STEAM_PASSWORD = "TestSteamPassword",
            LOGIN_TOKEN = "TestLoginToken",
            DATA_FOLDER = unitTestFolder
        });

        var startParametersJsonPath = Path.Combine(unitTestFolder, "start-parameter.json");
        var newStartParameters = new StartParameters(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            RandomHelper.RandomInt(),
            Guid.NewGuid().ToString(),
            RandomHelper.RandomInt(),
            RandomHelper.RandomInt(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString());
        var newStartParametersJson = JsonSerializer.Serialize(newStartParameters);
        File.WriteAllText(startParametersJsonPath, newStartParametersJson);

        var startParameterService =
            new StartParameterService(new XunitLogger<StartParameterService>(_outputHelper), options);

        // Act
        Assert.True(File.Exists(startParametersJsonPath));
        startParameterService.Set(newStartParameters);

        // Assert
        Assert.True(File.Exists(startParametersJsonPath));
        var json = File.ReadAllText(startParametersJsonPath);
        var startParametersFromFile = JsonSerializer.Deserialize<StartParameters>(json);
        Assert.NotNull(startParametersFromFile);
        Assert.Equal(newStartParameters.ServerHostname, startParametersFromFile.ServerHostname);
        Assert.Equal(newStartParameters.ServerPassword, startParametersFromFile.ServerPassword);
        Assert.Equal(newStartParameters.MaxPlayer, startParametersFromFile.MaxPlayer);
        Assert.Equal(newStartParameters.StartMap, startParametersFromFile.StartMap);
        Assert.Equal(newStartParameters.GameMode, startParametersFromFile.GameMode);
        Assert.Equal(newStartParameters.GameType, startParametersFromFile.GameType);
        Assert.Equal(newStartParameters.LoginToken, startParametersFromFile.LoginToken);
        Assert.Equal(newStartParameters.AdditionalStartParameters, startParametersFromFile.AdditionalStartParameters);
    }
}