namespace ExceptionsLib;

public enum ServerBusyAction
{
    STARTING,
    STOPPING,
    STARTED,
    UPDATING_OR_INSTALLING,
    EXECUTING_COMMAND
}

public class ServerIsBusyException : Exception
{
    public ServerIsBusyException(ServerBusyAction serverBusyAction) : base(
        $"Server is busy {serverBusyAction.ToString()}")
    {
    }

    public ServerIsBusyException(string? message) : base(message)
    {
    }
}