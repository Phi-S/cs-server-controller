using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Shared;
using Shared.ApiModels;
using Web.BlazorExtensions;
using Web.Services;

namespace Web.Components.Pages;

public class ChatCommandRazor : ComponentBase
{
    [Inject] private ILogger<ServerLogsRazor> Logger { get; set; } = default!;
    [Inject] protected ToastService ToastService { get; set; } = default!;
    [Inject] private InstanceApiService InstanceApiService { get; set; } = default!;

    protected ConfirmDialog DeleteConfirmDialogRef = default!;
    protected List<ChatCommandResponse> ChatCommandsList = [];
    protected bool ShowNewRow;
    protected string NewChatCommand = "";
    protected string NewServerCommand = "";

    protected override async Task OnInitializedAsync()
    {
        await RefreshChatCommands();
        await base.OnInitializedAsync();
    }

    private async Task RefreshChatCommands()
    {
        var chatCommands = await InstanceApiService.ChatCommands();
        if (chatCommands.IsError)
        {
            Logger.LogError("Failed to get chat commands {Error}", chatCommands.ErrorMessage());
            throw new Exception($"Failed to get chat commands {chatCommands.ErrorMessage()}");
        }

        ChatCommandsList = chatCommands.Value;
        await InvokeAsync(StateHasChanged);
    }

    protected async Task New()
    {
        NewChatCommand = "";
        NewServerCommand = "";
        ShowNewRow = !ShowNewRow;
        await InvokeAsync(StateHasChanged);
    }

    protected async Task SaveNew()
    {
        var addResult = await InstanceApiService.NewChatCommand(NewChatCommand, NewServerCommand);
        if (addResult.IsError)
        {
            Logger.LogError("Failed to create new chat command \"{ChatCommand}\" {Error}", NewChatCommand,
                addResult.ErrorMessage());
            ToastService.Error($"Failed to create new chat command \"{NewChatCommand}\". {addResult.ErrorMessage()}");
        }

        Logger.LogInformation("New chat command created \"{ChatCommand}\"", NewChatCommand);
        ToastService.Info($"New chat command created \"{NewChatCommand}\"");
        await RefreshChatCommands();
        NewChatCommand = "";
        NewServerCommand = "";
        ShowNewRow = false;
        await InvokeAsync(StateHasChanged);
    }

    protected async Task Delete(string chatCommand)
    {
        var confirm = await DeleteConfirmDialogRef.Show($"Delete \"{chatCommand}\"",
            $"Are you sure you want to delete the chat command \"{chatCommand}\"?");
        if (confirm == false)
        {
            return;
        }

        var deleteResult = await InstanceApiService.DeleteChatCommand(chatCommand);
        if (deleteResult.IsError)
        {
            Logger.LogError("Failed to delete chat command \"{ChatCommand}\" {Error}", chatCommand,
                deleteResult.ErrorMessage());
            ToastService.Error($"Failed to delete chat command \"{chatCommand}\". {deleteResult.ErrorMessage()}");
        }

        Logger.LogInformation("Chat command \"{ChatCommand}\" deleted", chatCommand);
        ToastService.Info($"Chat command \"{chatCommand}\" deleted");
        await RefreshChatCommands();
    }
}