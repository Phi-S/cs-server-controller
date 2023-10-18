namespace DatabaseLib.Models;

public class EventLog
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required DateTime TriggeredAt { get; set; }
    public string? DataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}