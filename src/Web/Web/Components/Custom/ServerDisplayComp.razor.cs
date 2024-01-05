using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared.ApiModels;
using Throw;
using Web.Helper;
using Web.Services;

namespace Web.Components.Custom;

public class ServerDisplayCompRazor : ComponentBase
{
    [Inject] private ILogger<ServerDisplayCompRazor> Logger { get; set; } = default!;
    [Inject] protected IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;
    [Inject] protected InstanceApiService InstanceApiService { get; set; } = default!;

    [Inject] private StartParametersJsonService StartParametersJsonService { get; set; } = default!;

    private static readonly StartParameters DefaultStartParameters = new();
    protected ServerStatusResponse? ServerInfo;
    protected string Hostname => ServerInfo?.Hostname ?? DefaultStartParameters.ServerHostname;

    protected override void OnInitialized()
    {
        try
        {
            ServerInfo = ServerInfoService.ServerInfo;
            ServerInfoService.OnServerInfoChangedEvent += async (_, _) =>
            {
                ServerInfo = ServerInfoService.ServerInfo;
                await InvokeAsync(StateHasChanged);
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception while initialing the ServerDisplayComp");
        }
    }

    protected async Task ConnectToServer()
    {
        ServerInfo.ThrowIfNull();
        
        var dnsResolve = await Dns.GetHostEntryAsync(ServerInfo.IpOrDomain);
        var serverIp = dnsResolve.AddressList.First().MapToIPv4().ToString();
        if (ServerInfo.IpOrDomain.Equals("localhost"))
        {
            serverIp = "127.0.0.1";
        }

        var connectUrl = $"steam://connect/{serverIp}:{ServerInfo.Port}";
        if (string.IsNullOrWhiteSpace(ServerInfo.ServerPassword) == false)
        {
            connectUrl += $"/{ServerInfo.ServerPassword}";
        }

        Console.WriteLine(connectUrl);
        await JsRuntime.InvokeVoidAsync("open", connectUrl, "");
    }

    protected async Task CopyConnectStringToClipboard()
    {
        try
        {
            ServerInfo.ThrowIfNull();
            var passwordString = ServerInfo.ServerPassword is null ? "" : $"; password {ServerInfo.ServerPassword}";
            var connectionString = $"connect {ServerInfo.IpOrDomain}:{ServerInfo.Port}{passwordString}";
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
            return ServerInfoService.ServerInfo is { ServerStarted: true }
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
            var startParameters = StartParametersJsonService.Get();
            await InstanceApiService.Start(startParameters);
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