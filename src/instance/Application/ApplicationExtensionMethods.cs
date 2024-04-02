using Application.ChatCommandFolder;
using Application.ConfigEditorFolder;
using Application.EventServiceFolder;
using Application.PluginsFolder;
using Application.ServerServiceFolder;
using Application.ServerUpdateOrInstallServiceFolder;
using Application.StartParameterFolder;
using Application.StatusServiceFolder;
using Application.SystemLogFolder;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ApplicationExtensionMethods
{
    public static IServiceCollection AddApplication(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddInfrastructure();

        serviceCollection.AddHttpClient();

        serviceCollection.AddSingleton<EventService>();
        serviceCollection.AddSingleton<StatusService>();
        serviceCollection.AddSingleton<ServerUpdateOrInstallService>();
        serviceCollection.AddSingleton<ServerService>();
        serviceCollection.AddSingleton<ChatCommandsCache>();
        serviceCollection.AddSingleton<StartParameterService>();
        serviceCollection.AddSingleton<SystemLogService>();
        serviceCollection.AddSingleton<ConfigEditorService>();
        
        serviceCollection.AddSingleton<InstalledPluginsService>();
        serviceCollection.AddTransient<PluginInstallerService>();
        
        serviceCollection.AddHostedService<ChatCommandService>();

        serviceCollection.AddApplicationMediatR();
        return serviceCollection;
    }

    public static IServiceCollection AddApplicationMediatR(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ApplicationExtensionMethods).Assembly));
    }
}