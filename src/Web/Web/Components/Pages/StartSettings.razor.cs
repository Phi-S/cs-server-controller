using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Shared;
using Throw;
using Web.BlazorExtensions;
using Web.Services;

namespace Web.Components.Pages;

public class StartSettingsRazor : ComponentBase
{
    [Inject] private ILogger<StartSettingsRazor> Logger { get; set; } = default!;
    [Inject] protected ToastService ToastService { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;
    [Inject] private InstanceApiService InstanceApiService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var startParameterResult = await InstanceApiService.GetStartParameters();
            if (startParameterResult.IsError)
            {
                Logger.LogError("Failed to get start parameters. {Error}", startParameterResult.ErrorMessage());
                throw new Exception($"Failed to get start parameters. {startParameterResult.ErrorMessage()}");
            }

            ServerInfoService.StartParameters = startParameterResult.Value;
            await base.OnInitializedAsync();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception in OnInitializedAsync");
            throw;
        }
    }

    protected async Task SaveStartParameters()
    {
        try
        {
            var setStartParametersResult =
                await InstanceApiService.SetStartParameters(ServerInfoService.StartParameters);
            if (setStartParametersResult.IsError)
            {
                Logger.LogError("Failed to save start settings. {Error}", setStartParametersResult.ErrorMessage());
                ToastService.Error($"Failed to save start settings. {setStartParametersResult.ErrorMessage()}");
                return;
            }

            var startParameterResult = await InstanceApiService.GetStartParameters();
            if (startParameterResult.IsError)
            {
                Logger.LogError("Failed to save start settings. {Error}", startParameterResult.ErrorMessage());
                ToastService.Error($"Failed to save start settings. {setStartParametersResult.ErrorMessage()}");
            }
            else
            {
                ServerInfoService.StartParameters = startParameterResult.Value;
                ToastService.Info("Start settings saved");
                await InvokeAsync(StateHasChanged);
            }
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
            ServerInfoService.ServerInfo.ThrowIfNull();

            await SaveStartParameters();

            if (ServerInfoService.ServerInfo.ServerStarted)
            {
                var stopResult = await InstanceApiService.Stop();
                if (stopResult.IsError)
                {
                    ToastService.Error($"Failed to restart server. {stopResult.ErrorMessage()}");
                    Logger.LogError("Failed to restart server. {Error}", stopResult.ErrorMessage());
                }
            }

            var startResult = await InstanceApiService.Start();
            if (startResult.IsError)
            {
                ToastService.Error($"Failed to restart server. {startResult.ErrorMessage()}");
                Logger.LogError("Failed to restart server. {Error}", startResult.ErrorMessage());
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
}