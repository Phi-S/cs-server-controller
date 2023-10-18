namespace DatabaseLib.Models;

public class ServerLog
{
    public long Id { get; set; }
    public string Message { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}