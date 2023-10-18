using DatabaseLib.Models;

namespace DatabaseLib.Repos;

public class EventLogRepo(ApiDbContext dbContext)
{
    public async Task Add(string eventName, DateTime eventTriggeredAt, string? eventDataJson)
    {
        await dbContext.EvenLogs.AddAsync(new EventLog()
        {
            Name = eventName,
            TriggeredAt = eventTriggeredAt,
            DataJson = eventDataJson,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    public Task<List<EventLog>> GetLast(int amount = 50)
    {
        return Task.FromResult(dbContext.EvenLogs.TakeLast(amount).ToList());
    }

    public Task<List<EventLog>> GetAllSince(DateTime since)
    {
        return Task.FromResult(dbContext.EvenLogs.Where(log => log.TriggeredAt > since).ToList());
    }

    public Task<List<EventLog>> GetAllSince(DateTime since, string eventName)
    {
        return Task.FromResult(dbContext.EvenLogs.Where(log => log.TriggeredAt > since && log.Name.Equals(eventName))
            .ToList());
    }
}