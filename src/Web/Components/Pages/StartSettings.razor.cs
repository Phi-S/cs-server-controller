using Microsoft.AspNetCore.Components;
using Throw;
using Web.Services;

namespace Web.Components.Pages;

public class StartSettingsRazor : ComponentBase
{
    [Inject] private ILogger<StartSettingsRazor> Logger { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;
    [Inject] private InstanceApiService InstanceApiService { get; set; } = default!;
    [Inject] private StartParametersJsonService StartParametersJsonService { get; set; } = default!;

    protected Shared.ApiModels.StartParameters? StartParameters;

    protected override void OnInitialized()
    {
        StartParameters = StartParametersJsonService.Get();
    }

    protected void SaveStartParameters()
    {
        try
        {
            if (StartParameters is null)
            {
                throw new NullReferenceException(nameof(StartParameters));
            }

            StartParametersJsonService.Overwrite(StartParameters);
            StartParameters = StartParametersJsonService.Get();
            InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error in SaveStartParameters");
        }
    }

    protected async Task SaveStartParametersAndRestartServer()
    {
        try
        {
            StartParameters.ThrowIfNull();
            ServerInfoService.ServerInfo.ThrowIfNull();

            StartParametersJsonService.Overwrite(StartParameters);
            var newStartParameters = StartParametersJsonService.Get();
            StartParameters = newStartParameters;
            await InvokeAsync(StateHasChanged);

            if (ServerInfoService.ServerInfo.ServerStarted)
            {
                await InstanceApiService.Stop();
            }

            await InstanceApiService.Start(newStartParameters);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error in SaveStartParametersAndRestartServer");
        }
    }
}