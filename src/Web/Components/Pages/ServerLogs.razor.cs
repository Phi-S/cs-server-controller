using Microsoft.AspNetCore.Components;
using Web.Services;

namespace Web.Components.Pages;

public record LogEntry(DateTime Timestamp, string Message);

public class ServerLogsRazor : ComponentBase
{
    [Inject] private ILogger<ServerLogsRazor> Logger { get; set; } = default!;
    [Inject] private ServerInfoService ServerInfoService { get; set; } = default!;

    protected List<LogEntry> ServerLogs => GetLogs();

    protected override void OnInitialized()
    {
        try
        {
            ServerInfoService.OnServerLogsChangedEvent += async (_, _) => await InvokeAsync(StateHasChanged);
            //ServerInfoService.StartServerLogsBackgroundTask();
            ServerInfoService.OnUpdateOrInstallLogsChangedEvent += async (_, _) => await InvokeAsync(StateHasChanged);
            //ServerInfoService.StartUpdateOrInstallLogsBackgroundTask();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception while initializing ServerLogs page");
        }
    }

    private List<LogEntry> GetLogs()
    {
        var logs = new List<LogEntry>();

        var serverLogs = ServerInfoService.ServerLogs
            ?.Select(serverLog => new LogEntry(serverLog.MessageReceivedAtUt, serverLog.Message)).ToList();
        if (serverLogs is not null)
        {
            logs.AddRange(serverLogs);
        }

        var updateOrInstallLogs =
            ServerInfoService.UpdateOrInstallLogs?.Select(log => new LogEntry(log.MessageReceivedAtUt, log.Message));
        if (updateOrInstallLogs is not null)
        {
            logs.AddRange(updateOrInstallLogs);
        }

        return logs.OrderByDescending(entry => entry.Timestamp).ToList();
    }
}