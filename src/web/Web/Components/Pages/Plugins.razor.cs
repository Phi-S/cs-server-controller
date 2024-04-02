using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
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

    protected List<PluginsResponseModel>? Plugins;
    protected readonly ConcurrentDictionary<string, string> SelectedVersion = [];

    protected override async Task OnInitializedAsync()
    {
        await GetPlugins();
        await base.OnInitializedAsync();
    }

    private async Task GetPlugins()
    {
        var installedVersions = await InstanceApiService.Plugins();
        if (installedVersions.IsError)
        {
            Logger.LogWarning("Failed to get installed versions. {Error}", installedVersions.ErrorMessage());
        }
        else
        {
            Plugins = installedVersions.Value;
            foreach (var plugin in Plugins)
            {
                SelectedVersion[plugin.Name] = plugin.Versions.Last();
            }
        }
    }

    private bool IsInstalled(string name, [MaybeNullWhen(false)] out string version)
    {
        var plugin = Plugins?.FirstOrDefault(v => v.Name.Equals(name.ToLower().Trim()));
        if (plugin is not null && string.IsNullOrWhiteSpace(plugin.InstalledVersion) == false)
        {
            version = plugin.InstalledVersion;
            return true;
        }

        version = null;
        return false;
    }

    protected string GetUpdateOrInstallString(PluginsResponseModel plugin)
    {
        return plugin.InstalledVersion is null ? "Install" : "Update";
    }

    protected async Task UpdateOrInstall(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Logger.LogError("Failed to install plugin. Plugin name not valid");
            ToastService.Error("Failed to install plugin. Plugin name not valid");
            return;
        }

        var plugin = Plugins?.FirstOrDefault(p => p.Name.ToLower().Trim() == name.ToLower().Trim());
        if (plugin is null)
        {
            Logger.LogError("Failed to install plugin. Plugin {Name} not found", name);
            ToastService.Error($"Failed to install plugin. Plugin {name} not found");
            return;
        }

        var selectedVersion = SelectedVersion[name];
        if (string.IsNullOrWhiteSpace(selectedVersion) ||
            plugin.Versions.Contains(selectedVersion, StringComparer.InvariantCultureIgnoreCase) == false)
        {
            Logger.LogError("Failed to install plugin. Version is not valid");
            ToastService.Error($"Failed to install plugin. Version is not valid");
            return;
        }

        var updateOrInstall = await InstanceApiService.PluginUpdateOrInstall(name, selectedVersion);
        if (updateOrInstall.IsError)
        {
            Logger.LogError("Failed to install plugin. {Error}", updateOrInstall.ErrorMessage());
            ToastService.Error($"Failed to install plugin. {updateOrInstall.ErrorMessage()}");
            return;
        }

        Logger.LogInformation("Plugin {Name} {Version} installed", name, selectedVersion);
        ToastService.Info($"Plugin {name} {selectedVersion} installed");
        await GetPlugins();
    }
}