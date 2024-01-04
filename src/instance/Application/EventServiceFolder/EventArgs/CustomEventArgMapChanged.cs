using System.Text.Json;

namespace Application.EventServiceFolder.EventArgs;

public class CustomEventArgMapChanged(Events eventName, string mapName) : CustomEventArg(eventName)
{
    public string MapName { get; } = mapName;

    public override string GetDataJson()
    {
        var data = new
        {
            MapName
        };
        return JsonSerializer.Serialize(data);
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(MapName)}: {MapName}";
    }
}