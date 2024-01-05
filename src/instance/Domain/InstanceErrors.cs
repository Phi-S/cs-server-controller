using ErrorOr;
using Shared;

namespace Domain;

public static class InstanceErrors
{
    public static Error ServerIsNotInstalled() => Errors.Fail("Server is not installed");

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