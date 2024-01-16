namespace Application.SystemLogFolder;

public class SystemLogEventArgs : EventArgs
{
    public DateTime CreatedUtc { get; }
    public string Message { get; }

    public SystemLogEventArgs(DateTime createdUtc, string message)
    {
        CreatedUtc = createdUtc;
        Message = message;
    }
}