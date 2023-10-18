﻿namespace EventsServiceLib.EventArgs;

public class CustomEventArg(Events eventName) : System.EventArgs
{
    public Events EventName { get; } = eventName;
    public DateTime TriggeredAtUtc { get; } = DateTime.UtcNow;

    public virtual string? GetDataJson()
    {
        return null;
    }

    public override string ToString()
    {
        return $"{nameof(EventName)}: {EventName}, {nameof(TriggeredAtUtc)}: {TriggeredAtUtc}";
    }
}