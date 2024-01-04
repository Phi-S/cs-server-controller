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

    public required DbSet<ServerStart> ServerStarts { get; set; }
    public required DbSet<ServerLog> ServerLogs { get; set; }
    public required DbSet<UpdateOrInstallStart> UpdateOrInstallStarts { get; set; }
    public required DbSet<UpdateOrInstallLog> UpdateOrInstallLogs { get; set; }
    public required DbSet<EventLog> EvenLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_options?.Value.DATABASE_PATH}");
        base.OnConfiguring(optionsBuilder);
    }
}