namespace ResultLib;

public class ErrorMessageException : Exception
{
    public ErrorMessageException(string message) : base(message)
    {
    }

    public ErrorMessageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}