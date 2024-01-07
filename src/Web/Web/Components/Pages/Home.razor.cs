using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared;
using Throw;
using Web.BlazorExtensions;
using Web.Services;

namespace Web.Components.Pages;

public class HomeRazor : ComponentBase
{
    [Inject] protected ToastService ToastService { get; set; } = default!;
    [Inject] private ILogger<HomeRazor> Logger { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;
    [Inject] private InstanceApiService InstanceApiService { get; set; } = default!;
    [Inject] private StartParametersJsonService StartParametersJsonService { get; set; } = default!;

    private static Guid? _currentUpdateOrInstallId;
    protected string SendCommandBind = "";

    protected override void OnInitialized()
    {
        ServerInfoService.OnServerInfoChangedEvent += async (_, _) => await InvokeAsync(StateHasChanged);
        ServerInfoService.OnAllLogsChangedEvent += async (_, _) => { await InvokeAsync(StateHasChanged); };
    }

    protected async Task Start()
    {
        try
        {
            var startParameters = StartParametersJsonService.Get();
            var startResult = await InstanceApiService.Start(startParameters);
            if (startResult.IsError)
            {
                Logger.LogError("Failed to start server. {Error}", startResult.ErrorMessage());
                ToastService.Error($"Failed to start server. {startResult.ErrorMessage()}");
            }

            ToastService.Info("Server started");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to start server");
            ToastService.Error($"Failed to start server");
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

            var startParameters = StartParametersJsonService.Get();
            var startResult = await InstanceApiService.Start(startParameters);
            if (startResult.IsError)
            {
                Logger.LogError("Failed to restart server. {Error}", startResult.ErrorMessage());
                ToastService.Error($"Failed to restart server. {startResult.ErrorMessage()}");
                return;
            }

            ToastService.Info("Server restarted");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to restart server");
            ToastService.Error($"Failed to restart server");
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
            var startUpdateOrInstallResult = await InstanceApiService.StartUpdatingOrInstalling(null);
            if (startUpdateOrInstallResult.IsError)
            {
                Logger.LogError("Start update or install server failed with error {Error}",
                    startUpdateOrInstallResult.ErrorMessage());
                ToastService.Error(
                    $"Failed to update server. {startUpdateOrInstallResult.ErrorMessage()}");
                return;
            }

            _currentUpdateOrInstallId = startUpdateOrInstallResult.Value;
            Logger.LogInformation("Update or install started");
            ToastService.Info("Server update started");
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
                return;
            }

            Logger.LogInformation("Server update cancelled");
            ToastService.Info("Server update cancelled");
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
            var mapChangeResult = await InstanceApiService.SendCommand($"changelevel {map}");
            if (mapChangeResult.IsError)
            {
                Logger.LogError("Failed to change map to {Map}. {Error}", map, mapChangeResult.ErrorMessage());
                ToastService.Error($"Failed to change map to {map}. {mapChangeResult.ErrorMessage()}");
            }

            Logger.LogInformation("Map changed to {Map}", map);
            ToastService.Info($"Map changed to {map}");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to change map to {Map}", map);
            ToastService.Error($"Failed to change map to {map}");
        }
    }

    protected async Task ExecuteConfig(string config)
    {
        try
        {
            var sendCommand = await InstanceApiService.SendCommand($"exec {config}");
            if (sendCommand.IsError)
            {
                Logger.LogError("Failed to execute config \"{Config}\". {Error}", config, sendCommand.ErrorMessage());
                ToastService.Error($"Failed to execute config \"{config}\". {sendCommand.ErrorMessage()}");
            }

            Logger.LogInformation("Config \"{Config}\" executed", config);
            ToastService.Info($"Config \"{config}\" executed");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to execute config \"{Config}\"", config);
            ToastService.Error($"Failed to execute config \"{config}\"");
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
                return;
            }

            Logger.LogInformation("Command \"{Command}\" executed", SendCommandBind);
            ToastService.Info($"Command \"{SendCommandBind}\" executed");
            SendCommandBind = "";
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


    protected bool DisabledWhenServerIsOffline
    {
        get
        {
            if (ServerInfoService.ServerInfo is null)
            {
                return false;
            }

            return ServerInfoService.ServerInfo.ServerStarted == false;
        }
    }
}