using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Shared;
using Shared.ApiModels;
using Web.BlazorExtensions;
using Web.Services;

namespace Web.Components.Pages;

public class PluginsRazor : ComponentBase
{
    [Inject] private ILogger<PluginsRazor> Logger { get; set; } = default!;
    [Inject] protected ToastService ToastService { get; set; } = default!;
    [Inject] protected ServerInfoService ServerInfoService { get; set; } = default!;
    [Inject] private InstanceApiService InstanceApiService { get; set; } = default!;


    protected List<InstalledVersionsModel>? InstalledVersions;

    protected override async Task OnInitializedAsync()
    {
        var installedVersions = await InstanceApiService.InstalledVersions();
        if (installedVersions.IsError)
        {
            Logger.LogWarning("Failed to get installed versions. {Error}", installedVersions.ErrorMessage());
        }
        else
        {
            InstalledVersions = installedVersions.Value;
        }

        await base.OnInitializedAsync();
    }

    protected string GetCounterStrikeSharpVersion()
    {
        var counterStrikeSharpVersion = InstalledVersions?.FirstOrDefault(v => v.Name.Equals("counterstrikesharp"));
        return counterStrikeSharpVersion is null ? "" : $"current version: {counterStrikeSharpVersion.Version}";
    }

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