using AppOptionsLib;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DatabaseLib;

public static class DatabaseExtensionMethods
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddDbContext<ApiDbContext>((provider, builder) =>
        {
            var options = provider.GetRequiredService<IOptions<AppOptions>>();
            builder.UseSqlite($"Data Source={options.Value.DATABASE_PATH}");
        });
        return serviceCollection;
    }

    public static async Task CreateAndMigrateDatabase(this IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<ApiDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}