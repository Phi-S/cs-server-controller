namespace DatabaseLib.Models;

public class UpdateOrInstallLog
{
    public long Id { get; set; }
    public string Message { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}