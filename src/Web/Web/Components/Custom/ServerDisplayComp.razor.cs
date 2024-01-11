using System.Net;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared;
using Shared.ApiModels;
using Throw;
using Web.BlazorExtensions;
using Web.Helper;
using Web.Services;

namespace Web.Components.Custom;

public class ServerDisplayCompRazor : ComponentBase, IDisposable
{
    [Inject] private ILogger<ServerDisplayCompRazor> Logger { get; set; } = default!;
    [Inject] protected ToastService ToastService { get; set; } = default!;
    [Inject] protected IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;
    [Inject] protected InstanceApiService InstanceApiService { get; set; } = default!;

    protected ServerInfoResponse? ServerInfo;
    protected string Hostname => ServerInfo?.Hostname ?? ServerInfoService.StartParameters.ServerHostname;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            ServerInfo = ServerInfoService.ServerInfo;
            var getStartParametersResult = await InstanceApiService.GetStartParameters();
            if (getStartParametersResult.IsError)
            {
                Logger.LogError("Failed to get start parameters. Using default start parameter. {Error}",
                    getStartParametersResult.ErrorMessage());
            }
            else
            {
                ServerInfoService.StartParameters = getStartParametersResult.Value;
            }

            ServerInfoService.OnServerInfoChangedEvent += OnServerInfoChangedEvent;
            ServerInfoService.OnStartParametersChangedEvent += ServerInfoServiceOnOnStartParametersChangedEvent;

            await base.OnInitializedAsync();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception while initialing the ServerDisplayComp");
        }
    }

    private async void ServerInfoServiceOnOnStartParametersChangedEvent(object? sender, EventArgs arg)
    {
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception in ServerInfoServiceOnOnStartParametersChangedEvent method");
        }
    }

    private async void OnServerInfoChangedEvent(object? sender, EventArgs arg)
    {
        try
        {
            ServerInfo = ServerInfoService.ServerInfo;
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception in OnServerInfoChangedEvent method");
        }
    }

    protected async Task ConnectToServer()
    {
        try
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

            await JsRuntime.InvokeVoidAsync("open", connectUrl, "");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception while executing ConnectToServer method");
        }
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
            ToastService.Error("Failed to copy server connection string");
            Logger.LogError(e, "Exception while executing CopyConnectStringToClipboard method");
        }
    }

    protected string GetBackgroundColor()
    {
        try
        {
            return ServerInfoService.ServerInfo is { ServerStarted: true }
                ? "bg-success"
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
            ToastService.Error("Failed to stop server");
            Logger.LogError(e, "Failed to stop server");
        }
    }

    protected async Task StartServer()
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
            ToastService.Error("Failed to start server");
            Logger.LogError(e, "Failed to start server");
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
            ToastService.Error("Failed to change map");
            Logger.LogError(e, "Failed to change map");
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

    public void Dispose()
    {
        ServerInfoService.OnServerInfoChangedEvent -= OnServerInfoChangedEvent;
        ServerInfoService.OnStartParametersChangedEvent -= ServerInfoServiceOnOnStartParametersChangedEvent;
    }
}