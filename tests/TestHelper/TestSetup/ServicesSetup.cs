using Application;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DockerContainerFolder;
using TestHelper.TestConfigurationFolder;
using TestHelper.TestLoggerFolder;
using Xunit.Abstractions;

namespace TestHelper.TestSetup;

public static class ServicesSetup
{
    public static async Task<IServiceCollection> GetApiInfrastructureCollection(ITestOutputHelper outputHelper)
    {
        var (id, containerName, connectionString) = await PostgresContainer.StartNew(outputHelper);
        var config = TestConfiguration.GetApiAppSettingsTest("DatabaseConnectionString", connectionString);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTestLogger(outputHelper);
        serviceCollection.AddSingleton(config);
        serviceCollection.AddInfrastructure();
        return serviceCollection;
    }

    public static async Task<IServiceCollection> GetApplicationCollection(ITestOutputHelper outputHelper)
    {
        var folderGuid = Guid.NewGuid();
        var config = TestConfiguration.GetApiAppSettingsTest(
            [
                new KeyValuePair<string, string?>("APP_OPTIONS:DATA_FOLDER",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "cs-controller-instance-unit-tests", folderGuid.ToString())),
                new KeyValuePair<string, string?>("APP_OPTIONS:APP_NAME", "cs-controller-instance-test"),
                new KeyValuePair<string, string?>("APP_OPTIONS:IP_OR_DOMAIN", "ip_or_domain"),
                new KeyValuePair<string, string?>("APP_OPTIONS:PORT", "port"),
                new KeyValuePair<string, string?>("APP_OPTIONS:STEAM_USERNAME", "steam_username"),
                new KeyValuePair<string, string?>("APP_OPTIONS:STEAM_PASSWORD", "steam_password"),
                new KeyValuePair<string, string?>("APP_OPTIONS:LOGIN_TOKEN", "login_token"),
            ]
        );
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTestLogger(outputHelper);
        serviceCollection.AddSingleton(config);
        serviceCollection.AddApplication();
        outputHelper.WriteLine($"Guid: {folderGuid}");
        return serviceCollection;
    }
}