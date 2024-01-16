using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Database.Models;

public class SystemLogDbModel
{
    [Key] public long Id { get; set; }
    [Required] public required string Message { get; set; }
    [Required] public required DateTime CreatedUtc { get; set; }
}