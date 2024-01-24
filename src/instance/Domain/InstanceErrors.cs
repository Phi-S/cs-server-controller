using ErrorOr;

namespace Domain;

public static class InstanceErrors
{
    public enum ServerBusyTypes
    {
        Starting,
        Stopping,
        Started,
        UpdatingOrInstalling,
        PluginsUpdatingOrInstalling,
        ExecutingCommand
    }

    public static Error ServerIsBusy(ServerBusyTypes serverBusyTypes)
    {
        return Error.Failure($"ServerIsBusy_{serverBusyTypes.ToString()}", $"Server is busy {serverBusyTypes}");
    }
}