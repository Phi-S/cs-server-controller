namespace EventsServiceLib.EventArgs;

public class CustomEventArgDemoName : CustomEventArg
{
    public readonly string DemoName;

    public CustomEventArgDemoName(string eventName, string demoName) : base(eventName)
    {
        DemoName = demoName;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(DemoName)}: {DemoName}";
    }
}