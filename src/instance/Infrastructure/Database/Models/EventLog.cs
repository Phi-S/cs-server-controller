using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Database.Models;

public class EventLog
{
    [Key] public long Id { get; set; }
    [Required] public required string Name { get; set; }
    [Required] public required DateTime TriggeredAtUtc { get; set; }
    public string? DataJson { get; set; }
    [Required] public required DateTime CreatedAtUtc { get; set; }
}