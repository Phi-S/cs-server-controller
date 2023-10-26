using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ServerInfoServiceLib;
using SharedModelsLib;


namespace web.Components.Custom;

public class ServerDisplayCompRazor : ComponentBase, IDisposable
{
    [Inject] private ILogger<ServerDisplayCompRazor> Logger { get; set; } = default!;
    [Inject] protected IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;

    protected InfoModel? ServerInfo => ServerInfoService.ServerInfo;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            ServerInfoService.StartStatusBackgroundTask();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception while executing OnInitializedAsync method");
        }

        await base.OnInitializedAsync();
    }


    protected string GetConnectionString()
    {
        if (ServerInfo is null)
        {
            return "";
        }

        var passwordString = ServerInfo.ServerPassword is null ? "" : $"password {ServerInfo.ServerPassword}";
        return $"connect {ServerInfo.IpOrDomain}:{ServerInfo.Port} {passwordString}";
    }

    protected async Task CopyConnectStringToClipboard()
    {
        try
        {
            // TODO: get ip/domain from status command??
            /*

            await JsRuntimeHelper.CopyToClipboard(JsRuntime,
                $"connect {ServerInfoModel.IpOrDomain}:{ServerInfoModel.Port}; password {ServerInfoModel.ServerPassword};");
                */
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception while executing CopyConnectStringToClipboard method");
        }
    }

    protected string GetBackgroundColor()
    {
        try
        {
            return ServerInfoService.ServerInfo is {ServerStarted: true}
                ? "bg-info"
                : "bg-warning";
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to get background color");
        }

        return "background-color: red;";
    }

    protected async Task UpdateOrInstall()
    {
        try
        {
            /*
            using (ServerInfoModel.Busy)
            {
                StatusMessageService.TriggerInfo(Username,
                    $"Started updating the server \"{ServerInfoModel.Hostname}\"");
                using (Logger.BeginScope(ComponentParameterModel.LogParameter))
                {
                    Logger.LogInformation("{Username} started updating the server \"{ServerHostname}\"",
                        Username,
                        ServerInfoModel.Hostname);
                }

                await ServerBackendService.HubServerApi.UpdateOrInstall(ServerInfoModel.ServerId);

                await Task.Delay(ServerBackendService.REFRESH_SERVER_ONLINE_STATUS_INTERVAL_IN_MS * 2);
            }
            */
        }
        catch (Exception e)
        {
            await InvokeAsync(StateHasChanged);
            Logger.LogError(e, "Failed to update server");
        }
    }

    protected async Task OpenChangeMapComponent()
    {
        try
        {
            /*
            ChangeMapCompRef.ThrowIfNull();

            await ChangeMapCompRef.Open();
            ChangeMapCompRef.ModalTitle = ServerInfoModel.Hostname;
            */
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to open change password component");
        }
    }

    protected async Task StopServer()
    {
        try
        {
            /*
            using (ServerInfoModel.Busy)
            {
                StatusMessageService.TriggerInfo(ComponentParameterModel.Username,
                    $"Stopping server \"{ServerInfoModel.Hostname}\"");
                using (Logger.BeginScope(ComponentParameterModel.LogParameter))
                {
                    Logger.LogInformation("Stopping server \"{Hostname}\"", ServerInfoModel.Hostname);
                }

                await ServerBackendService.HubServerApi.Stop(ServerInfoModel.ServerId);
            }*/
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to stop server");
        }
    }

    protected async Task StartServer()
    {
        try
        {
            /*
            using (ServerInfoModel.Busy)
            {
                StatusMessageService.TriggerInfo(ComponentParameterModel.Username,
                    $"Starting server \"{ServerInfoModel.Hostname}\"");
                using (Logger.BeginScope(ComponentParameterModel.LogParameter))
                {
                    Logger.LogInformation("Starting server \"{Hostname}\"", ServerInfoModel.Hostname);
                }

                await ServerBackendService.HubServerApi.Assign(ServerInfoModel.ServerId);
                await ServerBackendService.HubServerApi.Start(ServerInfoModel.ServerId);
            }
            */
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to start server");
        }
    }

    public void Dispose()
    {
        ServerInfoService.StopServerStatusBackgroundTask();
    }
}