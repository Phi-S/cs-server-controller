using System.Text.Json;

namespace Application.EventServiceFolder.EventArgs;

public class CustomEventArgPlayerConnected(Events eventName, string connectionId, string steamId, string ipPort)
    : CustomEventArg(eventName)
{
    public string ConnectionId { get; } = connectionId;
    public string SteamId { get; } = steamId;
    public string IpPort { get; } = ipPort;

    public override string GetDataJson()
    {
        var data = new
        {
            ConnectionId,
            SteamId,
            IpPort
        };
        return JsonSerializer.Serialize(data);
    }

    public override string ToString()
    {
        return
            $"{base.ToString()}, {nameof(ConnectionId)}: {ConnectionId}, {nameof(SteamId)}: {SteamId}, {nameof(IpPort)}: {IpPort}";
    }
}