namespace EventsServiceLib.EventArgs;

public class CustomEventArgPlayerConnected : CustomEventArg
{
    public string PlayerName { get; }
    public string PlayerIp { get; }

    public CustomEventArgPlayerConnected(string eventName, string playerName, string playerIp) : base(eventName)
    {
        PlayerName = playerName;
        PlayerIp = playerIp;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(PlayerName)}: {PlayerName}, {nameof(PlayerIp)}: {PlayerIp}";
    }
}