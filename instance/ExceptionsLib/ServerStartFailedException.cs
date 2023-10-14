namespace ExceptionsLib;

public class ServerStartFailedException : Exception
{
    public ServerStartFailedException(string? message) : base(message)
    {
    }
}