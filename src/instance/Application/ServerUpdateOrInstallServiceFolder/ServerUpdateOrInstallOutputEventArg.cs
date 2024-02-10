namespace Application.ServerUpdateOrInstallServiceFolder;

public class ServerUpdateOrInstallOutputEventArg(Guid updateOrInstallId, string message) : EventArgs
{
    public Guid UpdateOrInstallId { get; } = updateOrInstallId;
    public string Message { get; } = message;
}