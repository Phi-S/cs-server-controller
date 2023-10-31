using InstanceApiServiceLib;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ServerInfoServiceLib;
using SharedModelsLib;

namespace web.Components.Custom;

public class ServerDisplayCompRazor : ComponentBase
{
    [Inject] private ILogger<ServerDisplayCompRazor> Logger { get; set; } = default!;
    [Inject] protected IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;
    [Inject] protected InstanceApiService InstanceApiService { get; set; } = default!;

    private readonly StartParameters _defaultStartParameters = new();
    protected InfoModel? ServerInfo => ServerInfoService.ServerInfo;
    protected string Hostname => ServerInfo?.Hostname ?? _defaultStartParameters.ServerName;

    protected string HostnameMdCol
    {
        get
        {
            if (ServerInfo is null || ServerInfo.ServerStarted == false)
            {
                return "col-md-7";
            }

            return "col-md-4";
        }
    }

    protected string ButtonsMdCol
    {
        get
        {
            if (ServerInfo is null || ServerInfo.ServerStarted == false)
            {
                return "col-md-5";
            }

            return "col-md-4";
        }
    }

    protected override void OnInitialized()
    {
        try
        {
            ServerInfoService.OnServerInfoChangedEvent += async (_, _) => await InvokeAsync(StateHasChanged);
            ServerInfoService.StartServerInfoBackgroundTask();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception while initialing the ServerDisplayComp");
        }
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
            throw;
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
            await InstanceApiService.Stop();
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
            // TODO: change default start parameters
            await InstanceApiService.Start(new StartParameters());
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to start server");
        }
    }
}