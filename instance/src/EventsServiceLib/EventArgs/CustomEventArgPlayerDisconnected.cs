using System.Text.Json;

namespace EventsServiceLib.EventArgs;

public class CustomEventArgPlayerDisconnected(Events eventName, string playerName, string disconnectReason)
    : CustomEventArg(eventName)
{
    public string PlayerName { get; } = playerName;
    public string DisconnectReason { get; } = disconnectReason;

    public override string GetDataJson()
    {
        var data = new
        {
            PlayerName,
            DisconnectReason
        };
        return JsonSerializer.Serialize(data);
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(PlayerName)}: {PlayerName}, {nameof(DisconnectReason)}: {DisconnectReason}";
    }
}