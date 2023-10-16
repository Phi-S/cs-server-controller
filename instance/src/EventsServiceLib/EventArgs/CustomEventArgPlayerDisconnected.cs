namespace EventsServiceLib.EventArgs;

public class CustomEventArgPlayerDisconnected : CustomEventArg
{
    public string PlayerName { get; }
    public string DisconnectReason { get; }
    
    public CustomEventArgPlayerDisconnected(string eventName, string playerName, string disconnectReason) : base(eventName)
    {
        PlayerName = playerName;
        DisconnectReason = disconnectReason;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(PlayerName)}: {PlayerName}, {nameof(DisconnectReason)}: {DisconnectReason}";
    }
}