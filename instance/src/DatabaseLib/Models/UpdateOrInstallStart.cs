using System.ComponentModel.DataAnnotations;

namespace DatabaseLib.Models;

public class UpdateOrInstallStart
{
    [Key] public Guid Id { get; set; }
    [Required] public required DateTime StartedAtUtc { get; set; }
    [Required] public required DateTime CreatedAtUtc { get; set; }
}