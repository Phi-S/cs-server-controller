using Microsoft.AspNetCore.Components;
using ServerInfoServiceLib;
using SharedModelsLib.ApiModels;

namespace web.Components.Pages;

public class EventsRazor : ComponentBase
{
    [Inject] private ILogger<ServerLogsRazor> Logger { get; set; } = default!;
    [Inject] private ServerInfoService ServerInfoService { get; set; } = default!;

    protected List<EventLogResponse> EventLogs => ServerInfoService.Events ?? new List<EventLogResponse>();

    protected override void OnInitialized()
    {
        try
        {
            ServerInfoService.OnEventsChangedEvent += async (_, _) => { await InvokeAsync(StateHasChanged); };
            ServerInfoService.StartEventsBackgroundTask();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception while initializing EventsPage");
        }
    }
}