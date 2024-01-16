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

    public async Task<ServerStartDbModel> AddStart(string startParameters, DateTime startedAtUtc)
    {
        var serverStart = await _dbContext.ServerStarts.AddAsync(new ServerStartDbModel()
        {
            Id = Guid.NewGuid(),
            StartParameters = startParameters,
            StartedUtc = startedAtUtc,
            CreatedUtc = DateTime.UtcNow
        });
        return serverStart.Entity;
    }

    public async Task AddLog(Guid serverStartId, string message)
    {
        var serverStart = await _dbContext.ServerStarts.FirstAsync(start => start.Id == serverStartId);
        await _dbContext.ServerLogs.AddAsync(new ServerLogDbModel()
        {
            ServerStartDbModel = serverStart,
            Message = message,
            CreatedUtc = DateTime.UtcNow
        });
    }

    public Task<List<ServerLogDbModel>> GetLogsSince(DateTime since)
    {
        return Task.FromResult(_dbContext.ServerLogs.Where(log => log.CreatedUtc >= since)
            .Include(log => log.ServerStartDbModel)
            .ToList());
    }
}