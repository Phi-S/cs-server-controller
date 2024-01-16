using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared;
using Throw;
using Web.BlazorExtensions;
using Web.Services;

namespace Web.Components.Pages;

public class HomeRazor : ComponentBase, IDisposable
{
    [Inject] private ILogger<HomeRazor> Logger { get; set; } = default!;
    [Inject] protected ToastService ToastService { get; set; } = default!;
    [Inject] protected PreloadService PreloadService { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;
    [Inject] private InstanceApiService InstanceApiService { get; set; } = default!;

    private static Guid? _currentUpdateOrInstallId;
    protected string SendCommandBind = "";
    private readonly object _logsLock = new();

    protected override void OnInitialized()
    {
        ServerInfoService.OnServerInfoChangedEvent += OnServerInfoOrLogsChangedEvent;
        ServerInfoService.OnAllLogsChangedEvent += OnServerInfoOrLogsChangedEvent;
    }

    private async void OnServerInfoOrLogsChangedEvent(object? o, EventArgs eventArgs)
    {
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error in OnServerInfoOrLogsChangedEvent Method");
        }
    }

    protected List<LogEntry> GetLogs()
    {
        lock (_logsLock)
        {
            var allLogs = ServerInfoService.AllLogs.ToArray();
            return allLogs.OrderByDescending(l => l.TimestampUtc).Take(2000).ToList();
        }
    }

    protected async Task Start()
    {
        try
        {
            var startResult = await InstanceApiService.Start();
            if (startResult.IsError)
            {
                Logger.LogError("Failed to start server. {Error}", startResult.ErrorMessage());
                ToastService.Error($"Failed to start server. {startResult.ErrorMessage()}");
            }
            else
            {
                Logger.LogInformation("Server started");
                ToastService.Info("Server started");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to start server");
            ToastService.Error("Failed to start server");
        }
    }

    protected async Task Restart()
    {
        try
        {
            var stopResult = await InstanceApiService.Stop();
            if (stopResult.IsError)
            {
                Logger.LogError("Failed to restart server. {Error}", stopResult.ErrorMessage());
                ToastService.Error($"Failed to restart server. {stopResult.ErrorMessage()}");
                return;
            }

            var startResult = await InstanceApiService.Start();
            if (startResult.IsError)
            {
                Logger.LogError("Failed to restart server. {Error}", startResult.ErrorMessage());
                ToastService.Error($"Failed to restart server. {startResult.ErrorMessage()}");
                return;
            }

            Logger.LogInformation("Server restarted");
            ToastService.Info("Server restarted");
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to restart server");
            ToastService.Error("Failed to restart server");
        }
    }

    protected async Task Stop()
    {
        try
        {
            var stopResult = await InstanceApiService.Stop();
            if (stopResult.IsError)
            {
                Logger.LogError("Failed to stop server. {Error}", stopResult.ErrorMessage());
                ToastService.Error($"Failed to stop server. {stopResult.ErrorMessage()}");
            }
            else
            {
                Logger.LogInformation("Server stopped");
                ToastService.Info("Server stopped");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to stop server");
            ToastService.Error($"Failed to stop server");
        }
    }

    protected async Task StartUpdateOrInstall()
    {
        try
        {
            var startUpdateOrInstallResult = await InstanceApiService.StartUpdatingOrInstalling();
            if (startUpdateOrInstallResult.IsError)
            {
                Logger.LogError("Start update or install server failed with error {Error}",
                    startUpdateOrInstallResult.ErrorMessage());
                ToastService.Error(
                    $"Failed to update server. {startUpdateOrInstallResult.ErrorMessage()}");
            }
            else
            {
                _currentUpdateOrInstallId = startUpdateOrInstallResult.Value;
                Logger.LogInformation("Update or install started");
                ToastService.Info("Server update started");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to update server");
            ToastService.Error("Failed to update server");
        }
    }

    protected async Task CancelUpdateOrInstall()
    {
        try
        {
            _currentUpdateOrInstallId.ThrowIfNull();

            var cancelUpdatingOrInstalling =
                await InstanceApiService.CancelUpdatingOrInstalling(_currentUpdateOrInstallId.Value);
            if (cancelUpdatingOrInstalling.IsError)
            {
                Logger.LogError("Failed to cancel server update. {Error}",
                    cancelUpdatingOrInstalling.ErrorMessage());
                ToastService.Error(
                    $"Failed to cancel server update. {cancelUpdatingOrInstalling.ErrorMessage()}");
            }
            else
            {
                Logger.LogInformation("Server update cancelled");
                ToastService.Info("Server update cancelled");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to cancel server update");
            ToastService.Error("Failed to cancel server update");
        }
    }

    protected async Task UpdateOrInstallPlugins()
    {
        try
        {
            var updateOrInstallPlugins = await InstanceApiService.UpdateOrInstallPlugins();
            if (updateOrInstallPlugins.IsError)
            {
                Logger.LogError("Start update plugins failed with error {Error}",
                    updateOrInstallPlugins.ErrorMessage());
                ToastService.Error(
                    $"Failed to update plugins. {updateOrInstallPlugins.ErrorMessage()}");
            }
            else
            {
                Logger.LogInformation("Plugins updated");
                ToastService.Info("Plugins updated");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to update plugins");
            ToastService.Error("Failed to update plugins");
        }
    }

    protected async Task ChangeMap(string map)
    {
        try
        {
            var mapChangeResult = await InstanceApiService.SendCommand($"changelevel {map}");
            if (mapChangeResult.IsError)
            {
                Logger.LogError("Failed to change map to {Map}. {Error}", map, mapChangeResult.ErrorMessage());
                ToastService.Error($"Failed to change map to {map}. {mapChangeResult.ErrorMessage()}");
            }
            else
            {
                Logger.LogInformation("Map changed to {Map}", map);
                ToastService.Info($"Map changed to {map}");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to change map to {Map}", map);
            ToastService.Error($"Failed to change map to {map}");
        }
    }

    protected async Task SendCommand()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SendCommandBind))
            {
                return;
            }

            var sendCommand = await InstanceApiService.SendCommand(SendCommandBind);
            if (sendCommand.IsError)
            {
                Logger.LogError("Failed to execute command \"{Command}\". {Error}", SendCommandBind,
                    sendCommand.ErrorMessage());
                ToastService.Error(sendCommand.ErrorMessage());
            }
            else
            {
                Logger.LogInformation("Command \"{Command}\" executed", SendCommandBind);
                ToastService.Info($"Command \"{SendCommandBind}\" executed");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to execute command \"{Command}\"", SendCommandBind);
            ToastService.Error($"Failed to execute command \"{SendCommandBind}\"");
        }
    }

    protected async Task OnEnter(KeyboardEventArgs args)
    {
        if (args.Key.Equals("Enter"))
        {
            await SendCommand();
        }
    }

    public void Dispose()
    {
        ServerInfoService.OnServerInfoChangedEvent -= OnServerInfoOrLogsChangedEvent;
        ServerInfoService.OnAllLogsChangedEvent -= OnServerInfoOrLogsChangedEvent;
    }
}