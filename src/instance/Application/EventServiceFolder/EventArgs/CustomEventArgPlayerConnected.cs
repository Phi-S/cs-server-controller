using System.Text.Json;

namespace Application.EventServiceFolder.EventArgs;

public class CustomEventArgPlayerConnected(
    Events eventName,
    string connectionId,
    string username,
    string ip,
    string port)
    : CustomEventArg(eventName)
{
    public string ConnectionId { get; } = connectionId;
    public string Username { get; set; } = username;
    public string Ip { get; } = ip;
    public string Port { get; } = port;

    public override string GetDataJson()
    {
        var data = new
        {
            ConnectionId,
            Username,
            Ip,
            Port
        };
        return JsonSerializer.Serialize(data);
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(ConnectionId)}: {ConnectionId}, {nameof(Username)}: {Username}, {nameof(Ip)}: {Ip}, {nameof(Port)}: {Port}";
    }
}