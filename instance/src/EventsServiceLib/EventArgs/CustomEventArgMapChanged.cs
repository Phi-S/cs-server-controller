namespace EventsServiceLib.EventArgs;

public class CustomEventArgMapChanged : CustomEventArg
{
    public string MapName { get; }

    public CustomEventArgMapChanged(string eventName, string mapName) : base(eventName)
    {
        MapName = mapName;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(MapName)}: {MapName}";
    }
}