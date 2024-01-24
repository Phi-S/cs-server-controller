using Infrastructure.Database.Models;

namespace Infrastructure.Database.Repos;

public class SystemLogRepo
{
    private readonly InstanceDbContext _dbContext;

    public SystemLogRepo(InstanceDbContext instanceDbContext)
    {
        _dbContext = instanceDbContext;
    }

    public async Task AddLog(DateTime createdUtc ,string message)
    {
        await _dbContext.SystemLogs.AddAsync(new SystemLogDbModel()
        {
            Message = message,
            CreatedUtc = createdUtc
        });
    }

    public Task<List<SystemLogDbModel>> GetLogsSince(DateTime since)
    {
        return Task.FromResult(_dbContext.SystemLogs.Where(log => log.CreatedUtc >= since)
            .ToList());
    }
}