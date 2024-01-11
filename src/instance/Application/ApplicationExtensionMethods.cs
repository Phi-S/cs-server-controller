using Application.ChatCommandFolder;
using Application.EventServiceFolder;
using Application.ServerPluginsFolder;
using Application.ServerServiceFolder;
using Application.StartParameterFolder;
using Application.StatusServiceFolder;
using Application.UpdateOrInstallServiceFolder;
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
        serviceCollection.AddSingleton<UpdateOrInstallService>();
        serviceCollection.AddSingleton<ServerService>();
        serviceCollection.AddSingleton<ChatCommandsCache>();
        serviceCollection.AddSingleton<ServerPluginsService>();
        serviceCollection.AddSingleton<StartParameterService>();

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