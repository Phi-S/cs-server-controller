namespace EventsServiceLib.EventArgs;

public class CustomEventArg : System.EventArgs
{
    public string EventName { get; }

    public CustomEventArg(string eventName)
    {
        EventName = eventName;
    }

    public override string ToString()
    {
        return $"{nameof(EventName)}: {EventName}";
    }
}