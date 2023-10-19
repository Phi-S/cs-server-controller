using System.ComponentModel.DataAnnotations;

namespace DatabaseLib.Models;

public class ServerLog
{
    [Key] public long Id { get; set; }
    [Required] public required ServerStart ServerStart { get; set; }
    [Required] public required string Message { get; set; }
    [Required] public required DateTime CreatedAtUtc { get; set; }
}