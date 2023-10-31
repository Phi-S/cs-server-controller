using DatabaseLib.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseLib.Repos;

public class UpdateOrInstallRepo(IServiceProvider serviceProvider)
{
    public async Task<UpdateOrInstallStart> AddStart(DateTime startedAtUtc)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        var addAsync = await dbContext.UpdateOrInstallStarts.AddAsync(new UpdateOrInstallStart()
        {
            Id = Guid.NewGuid(),
            StartedAtUtc = startedAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        return addAsync.Entity;
    }

    public async Task AddLog(Guid updateOrInstallStartId, string message)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        var updateOrInstallStart =
            await dbContext.UpdateOrInstallStarts.FirstAsync(start => start.Id == updateOrInstallStartId);
        await dbContext.UpdateOrInstallLogs.AddAsync(new UpdateOrInstallLog()
        {
            UpdateOrInstallStart = updateOrInstallStart,
            Message = message,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<UpdateOrInstallLog>> GetSince(DateTime logsSince)
    {
        await using var dbContext = RepoHelper.New(serviceProvider);
        return dbContext.UpdateOrInstallLogs.Where(log => log.CreatedAtUtc >= logsSince)
            .Include(log => log.UpdateOrInstallStart)
            .ToList();
    }
}