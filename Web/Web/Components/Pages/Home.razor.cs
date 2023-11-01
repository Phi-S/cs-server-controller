using InstanceApiServiceLib;
using Microsoft.AspNetCore.Components;
using ServerInfoServiceLib;
using SharedModelsLib;

namespace web.Components.Pages;

public class HomeRazor : ComponentBase
{
    private static Guid? _currentUpdateOrInstallId;

    [Inject] private ILogger<HomeRazor> Logger { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;
    [Inject] private InstanceApiService InstanceApiService { get; set; } = default!;

    protected override void OnInitialized()
    {
        ServerInfoService.OnServerInfoChangedEvent += async (_, _) => await InvokeAsync(StateHasChanged);
    }

    protected async Task Start()
    {
        try
        {
            Logger.LogInformation("Starting server");
            await InstanceApiService.Start(new StartParameters());
            Logger.LogInformation("Server started");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to start server");
        }
    }

    protected async Task Stop()
    {
        try
        {
            Logger.LogInformation("Stopping server");
            await InstanceApiService.Stop();
            Logger.LogInformation("Server stopped");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to stop server");
        }
    }

    protected async Task StartUpdateOrInstall()
    {
        try
        {
            Logger.LogInformation("Starting server update or install");
            _currentUpdateOrInstallId = await InstanceApiService.StartUpdatingOrInstalling(null);
            Logger.LogInformation("Update or install started");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to update server");
        }
    }

    protected async Task CancelUpdateOrInstall()
    {
        try
        {
            Logger.LogInformation("Cancelling Update or install");
            if (_currentUpdateOrInstallId is null)
            {
                return;
            }

            await InstanceApiService.CancelUpdatingOrInstalling(_currentUpdateOrInstallId.Value);
            Logger.LogInformation("Update or install cancelled");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to cancel update");
        }
    }

    protected async Task ChangeMap(string map)
    {
        try
        {
            Logger.LogInformation("Changing map to {Map}", map);
            await InstanceApiService.SendCommand($"changelevel {map}");
            Logger.LogInformation("Map changed to {Map}", map);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to change map {Map}", map);
        }
    }

    protected async Task ExecuteConfig(string config)
    {
        try
        {
            Logger.LogInformation("Executing config {Config}", config);
            await InstanceApiService.SendCommand($"exec {config}");
            Logger.LogInformation("Config {Config} executed", config);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to execute config {Config}", config);
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

    protected bool DisabledWhenServerIsUpdatingOrInstalling
    {
        get
        {
            if (ServerInfoService.ServerInfo is null)
            {
                return false;
            }

            return ServerInfoService.ServerInfo.ServerUpdatingOrInstalling;
        }
    }

    protected bool DisabledWhenServerIsNotUpdatingOrInstalling
    {
        get
        {
            if (ServerInfoService.ServerInfo is null)
            {
                return false;
            }

            return ServerInfoService.ServerInfo.ServerUpdatingOrInstalling == false;
        }
    }
}