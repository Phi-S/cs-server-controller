namespace ExceptionsLib;

public class UpdateOrInstallFailedException : Exception
{
    public UpdateOrInstallFailedException(string? message) : base(message)
    {
    }
}