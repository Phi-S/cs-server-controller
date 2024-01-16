using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Database.Models;

public class ServerStartDbModel
{
    [Key] public Guid Id { get; set; }
    [Required] public required string StartParameters { get; set; }
    [Required] public required DateTime StartedUtc { get; set; }
    [Required] public required DateTime CreatedUtc { get; set; }
}