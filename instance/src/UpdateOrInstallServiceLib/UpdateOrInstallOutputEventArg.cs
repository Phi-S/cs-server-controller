namespace UpdateOrInstallServiceLib;

public class UpdateOrInstallOutputEventArg(Guid updateOrInstallId, string message) : EventArgs
{
    public Guid UpdateOrInstallId { get; } = updateOrInstallId;
    public string Message { get; } = message;
}