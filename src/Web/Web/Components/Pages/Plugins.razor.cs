using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Shared;
using Web.BlazorExtensions;
using Web.Services;

namespace Web.Components.Pages;

public class PluginsRazor : ComponentBase
{
    [Inject] private ILogger<PluginsRazor> Logger { get; set; } = default!;
    [Inject] protected ToastService ToastService { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;
    [Inject] private InstanceApiService InstanceApiService { get; set; } = default!;

    protected string CounterStrikeSharpUpdateOrInstall()
    {
        var serverInfo = ServerInfoService.ServerInfo.Get();
        if (serverInfo is null)
        {
            return "Install";
        }

        return serverInfo.CounterStrikeSharpInstalled ? "Update" : "Install";
    }

    protected async Task UpdateOrInstallCounterStrikeSharp()
    {
        try
        {
            ToastService.Info($"Starting CounterStrikeSharp {CounterStrikeSharpUpdateOrInstall()}");
            var updateOrInstallPlugins = await InstanceApiService.CounterstrikeSharpUpdateOrInstall();
            if (updateOrInstallPlugins.IsError)
            {
                ToastService.Error(
                    $"Failed to {CounterStrikeSharpUpdateOrInstall()} CounterStrikeSharp. {updateOrInstallPlugins.ErrorMessage()}");
            }
            else
            {
                ToastService.Info($"CounterStrikeSharp {CounterStrikeSharpUpdateOrInstall()} successful");
            }

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception in UpdateOrInstallCounterStrikeSharp");
        }
    }
}