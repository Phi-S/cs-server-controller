using Infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database.Repos;

public class ServerRepo
{
    private readonly InstanceDbContext _dbContext;

    public ServerRepo(InstanceDbContext instanceDbContext)
    {
        _dbContext = instanceDbContext;
    }
    
    public async Task<ServerStart> AddStart(string startParameters, DateTime startedAtUtc)
    {
        var serverStart = await _dbContext.ServerStarts.AddAsync(new ServerStart()
        {
            Id = Guid.NewGuid(),
            StartParameters = startParameters,
            StartedAtUtc = startedAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        });
        return serverStart.Entity;
    }

    public async Task AddLog(Guid serverStartId, string message)
    {
        var serverStart = await _dbContext.ServerStarts.FirstAsync(start => start.Id == serverStartId);
        await _dbContext.ServerLogs.AddAsync(new ServerLog()
        {
            ServerStart = serverStart,
            Message = message,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public Task<List<ServerLog>> GetLogsSince(DateTime since)
    {
        return Task.FromResult(_dbContext.ServerLogs.Where(log => log.CreatedAtUtc >= since)
            .Include(log => log.ServerStart)
            .ToList());
    }
}