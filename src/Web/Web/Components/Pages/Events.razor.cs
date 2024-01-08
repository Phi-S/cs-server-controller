using Microsoft.AspNetCore.Components;
using Web.Services;

namespace Web.Components.Pages;

public class EventsRazor : ComponentBase
{
    [Inject] private ILogger<EventsRazor> Logger { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;

    protected override void OnInitialized()
    {
        try
        {
            ServerInfoService.OnEventsChangedEvent += async (_, _) => { await InvokeAsync(StateHasChanged); };
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception while initializing EventsPage");
        }
    }
}