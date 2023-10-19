using System.ComponentModel.DataAnnotations;

namespace DatabaseLib.Models;

public class EventLog
{
    [Key] public long Id { get; set; }
    [Required] public required string Name { get; set; }
    [Required] public required DateTime TriggeredAt { get; set; }
    public string? DataJson { get; set; }
    [Required] public required DateTime CreatedAtUtc { get; set; }
}