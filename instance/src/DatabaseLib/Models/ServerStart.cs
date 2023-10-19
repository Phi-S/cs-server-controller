﻿using System.ComponentModel.DataAnnotations;

namespace DatabaseLib.Models;

public class ServerStart
{
    [Key] public Guid Id { get; set; }
    [Required] public required string StartParameters { get; set; }
    [Required] public required DateTime StartedAtUtc { get; set; }
    [Required] public required DateTime CreatedAtUtc { get; set; }
}