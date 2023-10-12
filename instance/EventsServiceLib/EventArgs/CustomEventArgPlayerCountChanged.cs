namespace EventsServiceLib.EventArgs;

public class CustomEventArgPlayerCountChanged : CustomEventArg
{
    public int PlayerCount { get; }

    public CustomEventArgPlayerCountChanged(string eventName, int playerCount) : base(eventName)
    {
        PlayerCount = playerCount;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(PlayerCount)}: {PlayerCount}";
    }
}