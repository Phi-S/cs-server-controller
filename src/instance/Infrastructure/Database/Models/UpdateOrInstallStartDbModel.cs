using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Database.Models;

public class UpdateOrInstallStartDbModel
{
    [Key] public Guid Id { get; set; }
    [Required] public required DateTime StartedUtc { get; set; }
    [Required] public required DateTime CreatedUtc { get; set; }
}