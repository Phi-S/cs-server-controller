using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Shared;
using Web.BlazorExtensions;
using Web.Services;

namespace Web.Components.Pages;

public class ConfigsRazor : ComponentBase
{
    [Inject] private ILogger<ConfigsRazor> Logger { get; set; } = default!;
    [Inject] protected ToastService ToastService { get; set; } = default!;
    [Inject] private InstanceApiService InstanceApiService { get; set; } = default!;

    protected List<string> ConfigFiles = [];
    protected string? SelectedConfigFile;
    protected string? ConfigContent;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var configs = await InstanceApiService.Configs();
            if (configs.IsError)
            {
                Logger.LogError("Failed to get available config files. {Error}", configs.ErrorMessage());
                ToastService.Error($"Failed to get available config files. {configs.ErrorMessage()}");
                return;
            }

            ConfigFiles = configs.Value;
            await base.OnInitializedAsync();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception in OnInitializedAsync");
        }
    }

    protected async Task LoadConfigContent(string configFile)
    {
        try
        {
            var content = await InstanceApiService.ConfigsGetContent(configFile);
            if (content.IsError)
            {
                Logger.LogError("Failed to load config content. {Error}", content.ErrorMessage());
                ToastService.Error($"Failed to load config content. {content.ErrorMessage()}");
                return;
            }

            SelectedConfigFile = configFile;
            ConfigContent = content.Value;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception in LoadConfigContent");
        }
    }
    
    protected async Task ApplyConfigContent()
    {
        try
        {
            if (SelectedConfigFile is null)
            {
                Logger.LogError("Failed to apply config content. No Config selected");
                ToastService.Error("Failed to apply config content. No Config selected");
                return;
            }

            if (ConfigContent is null)
            {
                Logger.LogError("Failed to apply config content. Config content not found");
                ToastService.Error("Failed to apply config content. Config content not found");
                return;
            }
            
            var content = await InstanceApiService.ConfigsSetContent(SelectedConfigFile, ConfigContent);
            if (content.IsError)
            {
                Logger.LogError("Failed to apply config content. {Error}", content.ErrorMessage());
                ToastService.Error($"Failed to apply config content. {content.ErrorMessage()}");
            }
            else
            {
                ToastService.Info($"Config \"{SelectedConfigFile}\" updated");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception in ApplyConfigContent");
        }
    }
}