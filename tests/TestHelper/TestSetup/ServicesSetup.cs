using Application;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.TestConfigurationFolder;
using TestHelper.TestLoggerFolder;
using TestHelper.UnitTestOutputFolder;
using Xunit.Abstractions;

namespace TestHelper.TestSetup;

public static class ServicesSetup
{
    public static Task<IServiceCollection> GetApplicationCollection(ITestOutputHelper outputHelper)
    {
        var config = TestConfiguration.GetApiAppSettingsTest(
            [
                new KeyValuePair<string, string?>("APP_OPTIONS:DATA_FOLDER",
                    UnitTestOutputHelper.GetNewUnitTestFolder(outputHelper)),
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
        return Task.FromResult<IServiceCollection>(serviceCollection);
    }
}