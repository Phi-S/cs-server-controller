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

    public async Task<UpdateOrInstallStartDbModel> AddStart(DateTime startedAtUtc)
    {
        var addAsync = await _dbContext.UpdateOrInstallStarts.AddAsync(new UpdateOrInstallStartDbModel()
        {
            Id = Guid.NewGuid(),
            StartedUtc = startedAtUtc,
            CreatedUtc = DateTime.UtcNow
        });
        return addAsync.Entity;
    }

    public async Task AddLog(Guid updateOrInstallStartId, string message)
    {
        var updateOrInstallStart =
            await _dbContext.UpdateOrInstallStarts.FirstAsync(start => start.Id == updateOrInstallStartId);
        await _dbContext.UpdateOrInstallLogs.AddAsync(new UpdateOrInstallLogModel()
        {
            UpdateOrInstallStartDbModel = updateOrInstallStart,
            Message = message,
            CreatedUtc = DateTime.UtcNow
        });
    }

    public Task<List<UpdateOrInstallLogModel>> GetLogsSince(DateTime logsSince)
    {
        return Task.FromResult(_dbContext.UpdateOrInstallLogs.Where(log => log.CreatedUtc >= logsSince)
            .Include(log => log.UpdateOrInstallStartDbModel)
            .ToList());
    }
}