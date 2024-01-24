using Microsoft.AspNetCore.Components;
using Shared.ApiModels;
using Web.Services;

namespace Web.Components.Pages;

public class EventsRazor : ComponentBase, IDisposable
{
    [Inject] private ILogger<EventsRazor> Logger { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;

    protected IReadOnlyCollection<EventLogResponse> EventLogs => ServerInfoService.EventLogs.Get();

    protected override void OnInitialized()
    {
        try
        {
            ServerInfoService.EventLogs.OnChange += EventLogsOnOnChange;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception while initializing EventsPage");
        }
    }

    private async void EventLogsOnOnChange()
    {
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception in EventLogsOnOnChange");
        }
    }

    public void Dispose()
    {
        ServerInfoService.EventLogs.OnChange -= EventLogsOnOnChange;
    }
}