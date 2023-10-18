using System.Text.Json;

namespace EventsServiceLib.EventArgs;

public class CustomEventArgUpdateOrInstall(Events eventName, Guid id) : CustomEventArg(eventName)
{
    public Guid Id { get; } = id;

    public override string GetDataJson()
    {
        var data = new
        {
            Id
        };
        return JsonSerializer.Serialize(data);
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(Id)}: {Id}";
    }
}