using ErrorOr;

namespace Domain;

public static class Errors
{
    public static string ErrorMessage<TValue>(this ErrorOr<TValue> error)
    {
        return error.FirstError.Description;
    }
    public static Error Fail(string description = "A failure has occurred.")
    {
        return Error.Failure("General.Failure", description);
    }

    public static Error ServerIsNotInstalled() => Fail("Server is not installed");

    public enum ServerBusyTypes
    {
        Starting,
        Stopping,
        Started,
        UpdatingOrInstalling,
        ExecutingCommand
    }

    public static Error ServerIsBusy(ServerBusyTypes serverBusyTypes)
    {
        return Error.Failure($"ServerIsBusy_{serverBusyTypes.ToString()}", $"Server is busy {serverBusyTypes}");
    }
}