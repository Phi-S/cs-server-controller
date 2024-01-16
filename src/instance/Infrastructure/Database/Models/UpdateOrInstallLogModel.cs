using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Database.Models;

public class UpdateOrInstallLogModel
{
    [Key] public long Id { get; set; }
    [Required] public required UpdateOrInstallStartDbModel UpdateOrInstallStartDbModel { get; set; }
    [Required] public required string Message { get; set; }
    [Required] public required DateTime CreatedUtc { get; set; }
}