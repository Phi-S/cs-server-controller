using System.Text.Json;

namespace EventsServiceLib.EventArgs;

public class CustomEventArgPlayerConnected(Events eventName, string playerName, string playerIp) : CustomEventArg(eventName)
{
    public string PlayerName { get; } = playerName;
    public string PlayerIp { get; } = playerIp;

    public override string GetDataJson()
    {
        var data = new
        {
            PlayerName,
            PlayerIp
        };
        return JsonSerializer.Serialize(data);
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(PlayerName)}: {PlayerName}, {nameof(PlayerIp)}: {PlayerIp}";
    }
}