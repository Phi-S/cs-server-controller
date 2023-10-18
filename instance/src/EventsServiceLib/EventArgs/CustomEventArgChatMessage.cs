using System.Text.Json;

namespace EventsServiceLib.EventArgs;

public class CustomEventArgChatMessage(Events eventName, string chat, string playerName, string steamId3,
    string message) : CustomEventArg(eventName)
{
    public string Chat { get; } = chat;
    public string PlayerName { get; } = playerName;
    public string SteamId3 { get; } = steamId3;
    public string Message { get; } = message;

    public override string GetDataJson()
    {
        var data = new
        {
            Chat,
            PlayerName,
            SteamId3,
            Message
        };
        return JsonSerializer.Serialize(data);
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(Chat)}: {Chat}, {nameof(PlayerName)}: {PlayerName}, {nameof(SteamId3)}: {SteamId3}, {nameof(Message)}: {Message}";
    }
}