using Microsoft.AspNetCore.Components;
using Shared.ApiModels;
using Web.Services;

namespace Web.Components.Pages;

public class EventsRazor : ComponentBase
{
    [Inject] private ILogger<ServerLogsRazor> Logger { get; set; } = default!;
    [Inject] private ServerInfoService ServerInfoService { get; set; } = default!;

    protected List<EventLogResponse>? EventLogs => ServerInfoService.Events;

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