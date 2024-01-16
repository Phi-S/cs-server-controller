using Domain;
using Infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Database;

public class InstanceDbContext : DbContext
{
    private readonly IOptions<AppOptions>? _options;

    /// This constructor is needed for the Entity framework migration to work.
    /// Dont use it!!!!!!!!!!!!!!!!!!!
    public InstanceDbContext()
    {
    }

    public InstanceDbContext(IOptions<AppOptions> options)
    {
        _options = options;
    }

    public required DbSet<ServerStartDbModel> ServerStarts { get; set; }
    public required DbSet<SystemLogDbModel> SystemLogs { get; set; }
    public required DbSet<ServerLogDbModel> ServerLogs { get; set; }
    public required DbSet<UpdateOrInstallStartDbModel> UpdateOrInstallStarts { get; set; }
    public required DbSet<UpdateOrInstallLogModel> UpdateOrInstallLogs { get; set; }
    public required DbSet<EventLogDbModel> EvenLogs { get; set; }
    public required DbSet<ChatCommandDbModel> ChatCommands { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_options?.Value.DATABASE_PATH}");
        base.OnConfiguring(optionsBuilder);
    }
}