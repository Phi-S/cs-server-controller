using Infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database.Repos;

public class UpdateOrInstallRepo
{
    private readonly InstanceDbContext _dbContext;

    public UpdateOrInstallRepo(InstanceDbContext instanceDbContext)
    {
        _dbContext = instanceDbContext;
    }

    public async Task<UpdateOrInstallStart> AddStart(DateTime startedAtUtc)
    {
        var addAsync = await _dbContext.UpdateOrInstallStarts.AddAsync(new UpdateOrInstallStart()
        {
            Id = Guid.NewGuid(),
            StartedAtUtc = startedAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        });
        return addAsync.Entity;
    }

    public async Task AddLog(Guid updateOrInstallStartId, string message)
    {
        var updateOrInstallStart =
            await _dbContext.UpdateOrInstallStarts.FirstAsync(start => start.Id == updateOrInstallStartId);
        await _dbContext.UpdateOrInstallLogs.AddAsync(new UpdateOrInstallLog()
        {
            UpdateOrInstallStart = updateOrInstallStart,
            Message = message,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public Task<List<UpdateOrInstallLog>> GetSince(DateTime logsSince)
    {
        return Task.FromResult(_dbContext.UpdateOrInstallLogs.Where(log => log.CreatedAtUtc >= logsSince)
            .Include(log => log.UpdateOrInstallStart)
            .ToList());
    }
}