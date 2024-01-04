using System.Text.Json;

namespace Application.EventServiceFolder.EventArgs;

public class CustomEventArgPlayerCountChanged(Events eventName, int playerCount) : CustomEventArg(eventName)
{
    public int PlayerCount { get; } = playerCount;

    public override string GetDataJson()
    {
        var data = new
        {
            PlayerCount
        };
        return JsonSerializer.Serialize(data);
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(PlayerCount)}: {PlayerCount}";
    }
}