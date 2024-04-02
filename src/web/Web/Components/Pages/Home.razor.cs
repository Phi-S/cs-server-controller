using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Shared;
using Shared.ApiModels;
using Throw;
using Web.BlazorExtensions;
using Web.Helper;
using Web.Services;

namespace Web.Components.Pages;

public class HomeRazor : ComponentBase, IDisposable
{
    [Inject] private ILogger<HomeRazor> Logger { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] protected ToastService ToastService { get; set; } = default!;
    [Inject] protected PreloadService PreloadService { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;
    [Inject] private InstanceApiService InstanceApiService { get; set; } = default!;

    protected ServerInfoResponse? ServerInfo => ServerInfoService.ServerInfo.Get();

    protected int BrowserTimezoneOffset;
    private static Guid? _currentUpdateOrInstallId;
    protected string SendCommandBind = "";

    protected override void OnInitialized()
    {
        ServerInfoService.ServerInfo.OnChange += ServerInfoOnOnChange;
        ServerInfoService.AllLogs.OnChange += OnServerInfoOrLogsChangedEvent;
        _ = Load();
        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        BrowserTimezoneOffset = await JsRuntime.GetBrowserTimezoneOffset();
        await base.OnAfterRenderAsync(firstRender);
    }

    private void ServerInfoOnOnChange(ServerInfoResponse obj)
    {
        InvokeAsync(StateHasChanged);
    }

    private async void OnServerInfoOrLogsChangedEvent()
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

    private async Task Load()
    {
        PreloadService.Show();
        while (ServerInfo is null)
        {
            await Task.Delay(100);
        }

        PreloadService.Hide();
    }

    protected async Task Start()
    {
        try
        {
            var startResult = await InstanceApiService.ServerStart();
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
            var stopResult = await InstanceApiService.ServerStop();
            if (stopResult.IsError)
            {
                Logger.LogError("Failed to restart server. {Error}", stopResult.ErrorMessage());
                ToastService.Error($"Failed to restart server. {stopResult.ErrorMessage()}");
                return;
            }

            var startResult = await InstanceApiService.ServerStart();
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
            var stopResult = await InstanceApiService.ServerStop();
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

    protected async Task StartServerUpdateOrInstall()
    {
        try
        {
            var stopServer = await InstanceApiService.ServerStop();
            if (stopServer.IsError)
            {
                Logger.LogError("Start update or install server failed with error {Error}",
                    stopServer.ErrorMessage());
                ToastService.Error(
                    $"Failed to update server. {stopServer.ErrorMessage()}");
            }
            
            var startUpdateOrInstallResult = await InstanceApiService.ServerUpdateOrInstallStart();
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

    protected async Task CancelServerUpdateOrInstall()
    {
        try
        {
            _currentUpdateOrInstallId.ThrowIfNull();

            var cancelUpdatingOrInstalling =
                await InstanceApiService.ServerUpdateOrInstallCancel(_currentUpdateOrInstallId.Value);
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

    protected async Task ChangeMap(string map)
    {
        try
        {
            var mapChangeResult = await InstanceApiService.ServerSendCommand($"changelevel {map}");
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

            var sendCommand = await InstanceApiService.ServerSendCommand(SendCommandBind);
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
        ServerInfoService.ServerInfo.OnChange -= ServerInfoOnOnChange;
        ServerInfoService.AllLogs.OnChange -= OnServerInfoOrLogsChangedEvent;
    }
}