using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Database.Models;

public class ChatCommandDbModel
{
    [Key] public long Id { get; set; }
    [Required] [StringLength(60)] public required string ChatMessage { get; set; }
    [Required] [StringLength(60)] public required string Command { get; set; }
    [Required] public required DateTime UpdatedUtc { get; set; }
    [Required] public required DateTime CreatedUtc { get; set; }
}