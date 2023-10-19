namespace ExceptionsLib;

public class ServerNotStartedException : Exception
{
    public ServerNotStartedException(string? message) : base(message)
    {
    }
}