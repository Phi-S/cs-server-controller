using Microsoft.AspNetCore.Components;
using Web.Services;

namespace Web.Components.Pages;

public class EventsRazor : ComponentBase, IDisposable
{
    [Inject] private ILogger<EventsRazor> Logger { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;

    protected override void OnInitialized()
    {
        try
        {
            ServerInfoService.OnEventsChangedEvent += OnServerInfoServiceOnOnEventsChangedEvent;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception while initializing EventsPage");
        }
    }

    private async void OnServerInfoServiceOnOnEventsChangedEvent(object? o, EventArgs eventArgs)
    {
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        ServerInfoService.OnEventsChangedEvent -= OnServerInfoServiceOnOnEventsChangedEvent;
    }
}