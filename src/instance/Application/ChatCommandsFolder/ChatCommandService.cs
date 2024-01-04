using Application.EventServiceFolder;
using Application.EventServiceFolder.EventArgs;
using Application.ServerServiceFolder;

namespace Application.ChatCommandsFolder;

public class ChatCommandService
{
    private readonly EventService _eventService;
    private readonly ServerService _serverService;

    public ChatCommandService(EventService eventService, ServerService serverService)
    {
        _eventService = eventService;
        _serverService = serverService;

        eventService.ChatMessage += EventServiceOnChatMessage;
    }

    private readonly object _commandBindsLock = new();
    private readonly Dictionary<string, string> _commandBinds = new();

    public void AddNewCommand(string chatMessage, string command)
    {
        lock (_commandBindsLock)
        {
            _commandBinds.Add(chatMessage, command);
        }
    }

    private string? CommandForChatMessage(string chatMessage)
    {
        lock (_commandBindsLock)
        {
            if (_commandBinds.TryGetValue(chatMessage.Trim(), out var command))
            {
                return command;
            }
        }

        return null;
    }

    private async void EventServiceOnChatMessage(object? sender, CustomEventArgChatMessage e)
    {
        var commandForChatMessage = CommandForChatMessage(e.Message);
        if (commandForChatMessage is null)
        {
            return;
        }

        await _serverService.ExecuteCommand(commandForChatMessage);
    }
}