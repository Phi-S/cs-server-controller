using System.Collections.Concurrent;
using Application.CQRS.Commands;
using Application.EventServiceFolder;
using Application.EventServiceFolder.EventArgs;
using Infrastructure.Database;
using Infrastructure.Database.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.ChatCommandFolder;

public class ChatCommandService : BackgroundService
{
    private readonly ILogger<ChatCommandService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly EventService _eventService;
    private readonly IMediator _mediator;

    public ChatCommandService(
        ILogger<ChatCommandService> logger,
        IServiceProvider serviceProvider,
        EventService eventService,
        IMediator mediator)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _eventService = eventService;
        _mediator = mediator;
    }

    private readonly SemaphoreSlim _chatCommandsLock = new(1, 1);
    private readonly ConcurrentBag<ChatCommand> _chatCommands = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await UpdateCommands();
        _eventService.StartingServerDone += async (_, _) => { await UpdateCommands(); };
        _eventService.ChatMessage += EventServiceOnChatMessage;
    }

    private async void EventServiceOnChatMessage(object? sender, CustomEventArgChatMessage e)
    {
        var chatMessage = e.Message.ToLower().Trim();
        _logger.LogInformation("Checking for chat message command with chat message: \"{ChatMessage}\"", chatMessage);

        await _chatCommandsLock.WaitAsync();
        try
        {
            foreach (var chatCommand in _chatCommands)
            {
                if (!chatMessage.Equals(chatCommand.ChatMessage.ToLower()))
                {
                    continue;
                }

                _logger.LogInformation("Chat command found {@ChatCommand}", chatCommand);
                var mediatorCommand = new SendCommandCommand(chatCommand.Command);
                var result = await _mediator.Send(mediatorCommand);
                if (result.IsError)
                {
                    _logger.LogError("Failed to execute chat command \"{Command}\" for chat message \"{ChatMessage}\"",
                        chatCommand.Command, chatMessage);
                }
                else
                {
                    _logger.LogInformation("chat command \"{Command}\" executed for chat message \"{ChatMessage}\"",
                        chatCommand.Command, chatMessage);
                }

                return;
            }
        }
        finally
        {
            _chatCommandsLock.Release();
        }
    }


    public async Task UpdateCommands()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.GetUnitOfWork();
        var allCommands = await unitOfWork.ChatCommandRepo.GetAll();

        try
        {
            await _chatCommandsLock.WaitAsync();
            _chatCommands.Clear();
            foreach (var command in allCommands)
            {
                _chatCommands.Add(command);
            }
        }
        finally
        {
            _chatCommandsLock.Release();
        }

        _logger.LogInformation("Available chat commands updated");
    }
}