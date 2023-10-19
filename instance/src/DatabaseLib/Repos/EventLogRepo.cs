using DatabaseLib.Models;

namespace DatabaseLib.Repos;

public class EventLogRepo(IServiceProvider serviceProvider)
{
    public async Task Add(string eventName, DateTime eventTriggeredAt, string? eventDataJson)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        await dbContext.EvenLogs.AddAsync(new EventLog()
        {
            Name = eventName,
            TriggeredAt = eventTriggeredAt,
            DataJson = eventDataJson,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<EventLog>> GetLast(int amount = 50)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        return dbContext.EvenLogs.TakeLast(amount).ToList();
    }

    public async Task<List<EventLog>> GetAllSince(DateTime since)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        return dbContext.EvenLogs.Where(log => log.TriggeredAt >= since).ToList();
    }

    public async Task<List<EventLog>> GetAllSince(DateTime since, string eventName)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        return dbContext.EvenLogs.Where(log => log.TriggeredAt >= since && log.Name.Equals(eventName))
            .ToList();
    }
}