using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Database;

public static class UnitOfWorkHelper
{
    public static UnitOfWork GetUnitOfWork(this IServiceScope services)
    {
        return services.ServiceProvider.GetRequiredService<UnitOfWork>();
    }
}