using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Database.Models;

public class UpdateOrInstallLog
{
    [Key] public long Id { get; set; }
    [Required] public required UpdateOrInstallStart UpdateOrInstallStart { get; set; }
    [Required] public required string Message { get; set; }
    [Required] public required DateTime CreatedAtUtc { get; set; }
}