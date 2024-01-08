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
    string playerName,
    int userId,
    string steamId,
    Team team,
    Chat chat,
    string message
) : CustomEventArg(eventName)
{
    public string PlayerName { get; } = playerName;
    public int UserId { get; } = userId;
    public string SteamId { get; } = steamId;
    public Team Team { get; } = team;
    public Chat Chat { get; } = chat;
    public string Message { get; } = message;

    public override string GetDataJson()
    {
        var data = new
        {
            PlayerName,
            UserId,
            SteamId,
            Team,
            Chat,
            Message
        };
        return JsonSerializer.Serialize(data);
    }

    public override string ToString()
    {
        return
            $"{base.ToString()}, {nameof(PlayerName)}: {PlayerName}, {nameof(UserId)}: {UserId}, {nameof(SteamId)}: {SteamId}, {nameof(Team)}: {Team}, {nameof(Chat)}: {Chat}, {nameof(Message)}: {Message}";
    }
}