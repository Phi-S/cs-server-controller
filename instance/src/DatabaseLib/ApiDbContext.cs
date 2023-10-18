using DatabaseLib.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseLib;

public class ApiDbContext : DbContext
{
    public DbSet<ServerLog> ServerLogs { get; set; }
    public DbSet<UpdateOrInstallLog> UpdateOrInstallLogs { get; set; }
    public DbSet<EventLog> EvenLogs { get; set; }
}