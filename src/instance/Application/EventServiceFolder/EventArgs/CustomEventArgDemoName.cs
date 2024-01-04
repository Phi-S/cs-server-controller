using System.Text.Json;

namespace Application.EventServiceFolder.EventArgs;

public class CustomEventArgDemoName(Events eventName, string demoName) : CustomEventArg(eventName)
{
    public readonly string DemoName = demoName;

    public override string GetDataJson()
    {
        var data = new
        {
            DemoName
        };
        return JsonSerializer.Serialize(data);
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(DemoName)}: {DemoName}";
    }
}