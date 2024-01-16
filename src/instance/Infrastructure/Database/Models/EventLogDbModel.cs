using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Database.Models;

public class EventLogDbModel
{
    [Key] public long Id { get; set; }
    [Required] public required string Name { get; set; }
    [Required] public required DateTime TriggeredUtc { get; set; }
    public string? DataJson { get; set; }
    [Required] public required DateTime CreatedUtc { get; set; }
}