using DatabaseLib.Repos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DatabaseLib;

public static class DatabaseExtensionMethods
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddDbContext<InstanceDbContext>();
        serviceCollection.AddSingleton<EventLogRepo>();
        serviceCollection.AddSingleton<ServerRepo>();
        serviceCollection.AddSingleton<UpdateOrInstallRepo>();
        return serviceCollection;
    }

    public static async Task CreateAndMigrateDatabase(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InstanceDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}