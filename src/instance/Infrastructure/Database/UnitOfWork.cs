using Infrastructure.Database.Repos;

namespace Infrastructure.Database;

public class UnitOfWork
{
    protected readonly InstanceDbContext DbContext;
    public SystemLogRepo SystemLogRepo { get; }
    public UpdateOrInstallRepo UpdateOrInstallRepo { get; }
    public ServerRepo ServerRepo { get; }
    public EventLogRepo EventLogRepo { get; }
    public ChatCommandRepo ChatCommandRepo { get; }
    
    public UnitOfWork(InstanceDbContext dbContext)
    {
        DbContext = dbContext;
        SystemLogRepo = new SystemLogRepo(dbContext);
        UpdateOrInstallRepo = new UpdateOrInstallRepo(dbContext);
        ServerRepo = new ServerRepo(dbContext);
        EventLogRepo = new EventLogRepo(dbContext);
        ChatCommandRepo = new ChatCommandRepo(dbContext);
    }

    public async Task Save()
    {
        await DbContext.SaveChangesAsync();
    }
}