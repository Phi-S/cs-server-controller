using Application.EventServiceFolder;
using Application.EventServiceFolder.EventArgs;
using Application.ServerServiceFolder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.ChatCommandFolder;

public class ChatCommandService : BackgroundService
{
    private readonly ILogger<ChatCommandService> _logger;
    private readonly ChatCommandsCache _chatCommandsCache;
    private readonly EventService _eventService;
    private readonly ServerService _serverService;

    public ChatCommandService(
        ILogger<ChatCommandService> logger,
        ChatCommandsCache chatCommandsCache,
        EventService eventService,
        ServerService serverService)
    {
        _logger = logger;
        _chatCommandsCache = chatCommandsCache;
        _eventService = eventService;
        _serverService = serverService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _chatCommandsCache.RefreshCache();
        _eventService.ChatMessage += OnChatMessageChatCommands;
    }

    private async void OnChatMessageChatCommands(object? sender, CustomEventArgChatMessage e)
    {
        try
        {
            var chatMessage = e.Message.ToLower().Trim();
            var chatCommandResult = await _chatCommandsCache.GetByChatMessage(chatMessage);
            if (chatCommandResult.IsError)
            {
                return;
            }

            var chatCommand = chatCommandResult.Value;
            _logger.LogInformation("Chat command \"{ChatMessage}\" is being processed", chatMessage);
            var result = await _serverService.ExecuteCommand(chatCommand.Command);
            if (result.IsError)
            {
                _logger.LogError("Failed to execute server command \"{Command}\" for chat message \"{ChatMessage}\"",
                    chatCommand.Command, chatMessage);
            }
            else
            {
                _logger.LogInformation("Server command \"{Command}\" executed for chat message \"{ChatMessage}\"",
                    chatCommand.Command, chatMessage);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Exception while trying to process chat command \"{ChatMessage}\"", e);
        }
    }
}