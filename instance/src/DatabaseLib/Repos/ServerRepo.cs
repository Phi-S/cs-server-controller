using DatabaseLib.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseLib.Repos;

public class ServerRepo(IServiceProvider serviceProvider)
{
    public async Task<ServerStart> AddStart(string startParameters, DateTime startedAtUtc)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        var serverStart = await dbContext.ServerStarts.AddAsync(new ServerStart()
        {
            Id = Guid.NewGuid(),
            StartParameters = startParameters,
            StartedAtUtc = startedAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        return serverStart.Entity;
    }

    public async Task AddLog(Guid serverStartId, string message)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        var serverStart = await dbContext.ServerStarts.FirstAsync(start => start.Id == serverStartId);
        await dbContext.ServerLogs.AddAsync(new ServerLog()
        {
            ServerStart = serverStart,
            Message = message,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<ServerLog>> GetSince(DateTime since)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        return dbContext.ServerLogs.Where(log => log.CreatedAtUtc >= since)
            .Include(log => log.ServerStart)
            .ToList();
    }
}