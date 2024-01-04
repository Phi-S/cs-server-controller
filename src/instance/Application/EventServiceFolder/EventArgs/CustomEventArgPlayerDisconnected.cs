using System.Text.Json;

namespace Application.EventServiceFolder.EventArgs;

public class CustomEventArgPlayerDisconnected(
        Events eventName,
        string connectionId,
        string steamId64,
        string ipPort,
        string disconnectReasonCode,
        string disconnectReason)
    : CustomEventArg(eventName)
{
    public string ConnectionId { get; } = connectionId;
    public string SteamId64 { get; } = steamId64;
    public string IpPort { get; } = ipPort;
    public string DisconnectReasonCode { get; } = disconnectReasonCode;
    public string DisconnectReason { get; } = disconnectReason;

    public override string GetDataJson()
    {
        var data = new
        {
            ConnectionId,
            SteamId64,
            IpPort,
            DisconnectReasonCode,
            DisconnectReason
        };
        return JsonSerializer.Serialize(data);
    }

    public override string ToString()
    {
        return
            $"{base.ToString()}, {nameof(ConnectionId)}: {ConnectionId}, {nameof(SteamId64)}: {SteamId64}, {nameof(IpPort)}: {IpPort}, {nameof(DisconnectReasonCode)}: {DisconnectReasonCode}, {nameof(DisconnectReason)}: {DisconnectReason}";
    }
}