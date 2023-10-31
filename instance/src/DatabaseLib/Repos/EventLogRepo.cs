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
            TriggeredAtUtc = eventTriggeredAt,
            DataJson = eventDataJson,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<EventLog>> GetAllSince(DateTime since)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        return dbContext.EvenLogs.Where(log => log.TriggeredAtUtc >= since).ToList();
    }

    public async Task<List<EventLog>> GetAllSince(DateTime since, string eventName)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        return dbContext.EvenLogs.Where(log => log.TriggeredAtUtc >= since && log.Name.Equals(eventName))
            .ToList();
    }
}