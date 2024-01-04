using System.Text.Json;
using Shared.ApiModels;

namespace Application.EventServiceFolder.EventArgs;

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