namespace ExceptionsLib;

public class ServerCommandExecutionFailedException : Exception
{
    public ServerCommandExecutionFailedException(string? message) : base(message)
    {
    }
}