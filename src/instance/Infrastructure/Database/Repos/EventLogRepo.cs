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
        await _dbContext.EvenLogs.AddAsync(new EventLog()
        {
            Name = eventName,
            TriggeredAtUtc = eventTriggeredAt,
            DataJson = eventDataJson,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public Task<List<EventLog>> GetAllSince(DateTime since)
    {
        return Task.FromResult(_dbContext.EvenLogs.Where(log => log.TriggeredAtUtc >= since).ToList());
    }

    public Task<List<EventLog>> GetAllSince(DateTime since, string eventName)
    {
        return Task.FromResult(_dbContext.EvenLogs.Where(log => log.TriggeredAtUtc >= since && log.Name.Equals(eventName))
            .ToList());
    }
}