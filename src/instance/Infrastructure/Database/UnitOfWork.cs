using Infrastructure.Database.Repos;

namespace Infrastructure.Database;

public class UnitOfWork
{
    protected readonly InstanceDbContext DbContext;
    public UpdateOrInstallRepo UpdateOrInstallRepo { get; }
    public ServerRepo ServerRepo { get; }
    public EventLogRepo EventLogRepo { get; }
    
    public UnitOfWork(InstanceDbContext dbContext)
    {
        DbContext = dbContext;
        UpdateOrInstallRepo = new UpdateOrInstallRepo(dbContext);
        ServerRepo = new ServerRepo(dbContext);
        EventLogRepo = new EventLogRepo(dbContext);
    }

    public async Task Save()
    {
        await DbContext.SaveChangesAsync();
    }
}