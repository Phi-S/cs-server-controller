using InstanceApiServiceLib;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ServerInfoServiceLib;
using SharedModelsLib;
using web.Helper;

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
    protected string Hostname => ServerInfo?.Hostname ?? _defaultStartParameters.ServerHostname;

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
            var connectionString = GetConnectionString();
            Logger.LogInformation("Coping connection \"{ConnectionString}\" string to clipboard", connectionString);
            await JsRuntimeHelper.CopyToClipboard(JsRuntime, connectionString);
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

    protected async Task StopServer()
    {
        try
        {
            Logger.LogInformation("Stopping server");
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
            Logger.LogInformation("Starting server");
            // TODO: change default start parameters
            await InstanceApiService.Start(new StartParameters());
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to start server");
        }
    }

    protected void NavigateToServerLogsPage()
    {
        try
        {
            NavigationManager.NavigateTo("/server-logs");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to navigate to server logs page");
        }
    }

    protected async Task ChangeMap(string map)
    {
        try
        {
            await InstanceApiService.SendCommand($"changelevel {map}");
            Logger.LogInformation("Changing map to {Map}", map);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to change map");
        }
    }

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

    protected string LoadingMessage
    {
        get
        {
            if (ServerInfo is not null)
            {
                if (ServerInfo.ServerStarting)
                {
                    return "Starting server...";
                }

                if (ServerInfo.ServerUpdatingOrInstalling)
                {
                    return "Updating server...";
                }
            }

            return "";
        }
    }
}