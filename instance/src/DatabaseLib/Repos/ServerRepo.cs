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

    public async Task<ServerStart> GetLastStart()
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        var latestStartedAt = await dbContext.ServerStarts.MaxAsync(serverStart => serverStart.StartedAtUtc);
        return await dbContext.ServerStarts.FirstAsync(start => start.StartedAtUtc == latestStartedAt);
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

    public async Task<List<ServerLog>> GetLastLogs(ServerStart serverStart, int amount)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        return dbContext.ServerLogs.Where(log => log.ServerStart.Id == serverStart.Id).Take(amount)
            .OrderByDescending(log => log.CreatedAtUtc).ToList();
    }

    public async Task<List<ServerLog>> GetAllLogs(ServerStart serverStart)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        return dbContext.ServerLogs.Where(log => log.ServerStart.Id == serverStart.Id).ToList();
    }

    public async Task<List<ServerLog>> GetSince(DateTime since)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        return dbContext.ServerLogs.Where(log => log.ServerStart.CreatedAtUtc > since).ToList();
    }
}