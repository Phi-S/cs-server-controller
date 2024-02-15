using BlazorBootstrap;
using ErrorOr;
using Microsoft.AspNetCore.Components;
using Shared;
using Shared.ApiModels;
using Web.BlazorExtensions;
using Web.Services;

namespace Web.Components.Pages;

public class SettingsRazor : ComponentBase, IDisposable
{
    [Inject] private ILogger<SettingsRazor> Logger { get; set; } = default!;
    [Inject] protected ToastService ToastService { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;
    [Inject] private InstanceApiService InstanceApiService { get; set; } = default!;

    protected StartParameters LocalStartParameters = new();

    protected override async Task OnInitializedAsync()
    {
        var startParameterResult = await InstanceApiService.GetStartParameters();
        if (startParameterResult.IsError)
        {
            throw new Exception($"Failed to get start parameters. {startParameterResult.ErrorMessage()}");
        }

        ServerInfoService.StartParameters.Set(startParameterResult.Value);
        LocalStartParameters = startParameterResult.Value;
        
        ServerInfoService.ServerInfo.OnChange += ServerInfoOnOnChange;
        ServerInfoService.StartParameters.OnChange += StartParametersOnOnChange;

        await base.OnInitializedAsync();
    }

    private async void StartParametersOnOnChange(StartParameters obj)
    {
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception in StartParametersOnOnChange");
        }
    }

    private async void ServerInfoOnOnChange(ServerInfoResponse obj)
    {
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception in ServerInfoOnOnChange");
        }
    }

    private async Task<ErrorOr<Success>> SaveStartParameters()
    {
        var setStartParametersResult = await InstanceApiService.SetStartParameters(LocalStartParameters);
        if (setStartParametersResult.IsError)
        {
            return setStartParametersResult.FirstError;
        }

        var startParameterResult = await InstanceApiService.GetStartParameters();
        if (startParameterResult.IsError)
        {
            return startParameterResult.FirstError;
        }

        ServerInfoService.StartParameters.Set(startParameterResult.Value);
        LocalStartParameters = startParameterResult.Value;
        await InvokeAsync(StateHasChanged);

        return Result.Success;
    }

    protected async Task OnSaveStartParametersButton()
    {
        try
        {
            var saveStartParameters = await SaveStartParameters();
            if (saveStartParameters.IsError)
            {
                Logger.LogError("Failed to save start settings. {Error}", saveStartParameters.ErrorMessage());
                ToastService.Error($"Failed to save start settings. {saveStartParameters.ErrorMessage()}");
                return;
            }

            ToastService.Info("Start settings saved");
        }
        catch (Exception e)
        {
            ToastService.Error("Failed to save start settings");
            Logger.LogError(e, "Error in SaveStartParameters");
        }
    }

    protected async Task SaveStartParametersAndRestartServer()
    {
        try
        {
            var saveStartParameters = await SaveStartParameters();
            if (saveStartParameters.IsError)
            {
                Logger.LogError("Failed to save start settings. {Error}", saveStartParameters.ErrorMessage());
                ToastService.Error($"Failed to save start settings. {saveStartParameters.ErrorMessage()}");
                return;
            }

            ToastService.Info("Start settings saved");

            var serverInfo = ServerInfoService.ServerInfo.Get();
            if (serverInfo is null)
            {
                Logger.LogError("Failed to restart server");
                ToastService.Error("Failed to restart server");
                return;
            }

            if (serverInfo.ServerStarted)
            {
                var stopResult = await InstanceApiService.ServerStop();
                if (stopResult.IsError)
                {
                    ToastService.Error($"Failed to restart server. {stopResult.ErrorMessage()}");
                    Logger.LogError("Failed to restart server. {Error}", stopResult.ErrorMessage());
                    return;
                }
            }

            var startResult = await InstanceApiService.ServerStart();
            if (startResult.IsError)
            {
                ToastService.Error($"Failed to restart server. {startResult.ErrorMessage()}");
                Logger.LogError("Failed to restart server. {Error}", startResult.ErrorMessage());
                return;
            }

            Logger.LogInformation("Start settings saved and server restarted");
            ToastService.Info("Start settings saved and server restarted");
        }
        catch (Exception e)
        {
            ToastService.Error("Failed to restart server");
            Logger.LogError(e, "Error in SaveStartParametersAndRestartServer");
        }
    }

    public void Dispose()
    {
        ServerInfoService.ServerInfo.OnChange -= ServerInfoOnOnChange;
        ServerInfoService.StartParameters.OnChange -= StartParametersOnOnChange;
    }
}