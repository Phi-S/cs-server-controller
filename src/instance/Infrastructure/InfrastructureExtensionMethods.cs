using Domain;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class InfrastructureExtensionMethods
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection serviceCollection)
    {
        using (var serviceProvider = serviceCollection.BuildServiceProvider())
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            serviceCollection.AddOptions<AppOptions>()
                .Bind(configuration.GetSection(AppOptions.SECTION_NAME))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }

        using (var serviceProvider = serviceCollection.BuildServiceProvider())
        {
            var options = serviceProvider.GetRequiredService<IOptions<AppOptions>>();
            Directory.CreateDirectory(options.Value.DATA_FOLDER);
        }

        #region Database

        serviceCollection.AddDbContext<InstanceDbContext>();

        // Migrate database
        using (var serviceProvider = serviceCollection.BuildServiceProvider())
        {
            using (var dbContext = serviceProvider.GetRequiredService<InstanceDbContext>())
            {
                dbContext.Database.Migrate();
            }
        }

        serviceCollection.AddScoped<UnitOfWork>();

        #endregion

        return serviceCollection;
    }
}