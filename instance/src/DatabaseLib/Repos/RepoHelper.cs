using Microsoft.Extensions.DependencyInjection;

namespace DatabaseLib.Repos;

public static class RepoHelper
{
    public static InstanceDbContext New(IServiceProvider serviceProvider) =>
        serviceProvider.CreateScope().ServiceProvider.GetRequiredService<InstanceDbContext>();
}