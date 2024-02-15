using System.Text.Json;

namespace Application.EventServiceFolder.EventArgs;

public enum Chat
{
    Team,
    All
}

public enum Team
{
    // ReSharper disable once InconsistentNaming
    CT,
    T
}

public class CustomEventArgChatMessage(
    Events eventName,
    Chat chat,
    string playerName,
    string steamId,
    string message
) : CustomEventArg(eventName)
{
    public Chat Chat { get; } = chat;
    public string PlayerName { get; } = playerName;
    public string SteamId { get; set; } = steamId;
    public string Message { get; } = message;

    public override string GetDataJson()
    {
        var data = new
        {
            Chat,
            PlayerName,
            SteamId,
            Message
        };
        return JsonSerializer.Serialize(data);
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(Chat)}: {Chat}, {nameof(PlayerName)}: {PlayerName}, {nameof(SteamId)}: {SteamId}, {nameof(Message)}: {Message}";
    }
}