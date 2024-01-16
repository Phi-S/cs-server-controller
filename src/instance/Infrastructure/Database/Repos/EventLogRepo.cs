using Infrastructure.Database.Models;

namespace Infrastructure.Database.Repos;

public class EventLogRepo
{
    private readonly InstanceDbContext _dbContext;

    public EventLogRepo(InstanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task Add(string eventName, DateTime eventTriggeredAt, string? eventDataJson)
    {
        await _dbContext.EvenLogs.AddAsync(new EventLogDbModel()
        {
            Name = eventName,
            TriggeredUtc = eventTriggeredAt,
            DataJson = eventDataJson,
            CreatedUtc = DateTime.UtcNow
        });
    }

    public Task<List<EventLogDbModel>> GetLogsSince(DateTime since)
    {
        return Task.FromResult(_dbContext.EvenLogs.Where(log => log.TriggeredUtc >= since).ToList());
    }

    public Task<List<EventLogDbModel>> GetLogsSince(DateTime since, string eventName)
    {
        return Task.FromResult(_dbContext.EvenLogs.Where(log => log.TriggeredUtc >= since && log.Name.Equals(eventName))
            .ToList());
    }
}