using System.Text.Json;
using SharedModelsLib.ApiModels;

namespace EventsServiceLib.EventArgs;

public class CustomEventArgStartingServerDone(Events eventName, StartParameters startParameters) : CustomEventArg(eventName)
{
    public StartParameters StartParameters { get; } = startParameters;

    public override string GetDataJson()
    {
        var data = new
        {
            StartParameters
        };
        return JsonSerializer.Serialize(data);
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(StartParameters)}: {StartParameters}";
    }
}