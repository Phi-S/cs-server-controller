using System.Collections.Concurrent;
using ErrorOr;
using Infrastructure.Database;
using Infrastructure.Database.Models;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace Application.ChatCommandFolder;

public class ChatCommandsCache
{
    private readonly IServiceProvider _serviceProvider;

    public ChatCommandsCache(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private readonly SemaphoreSlim _chatCommandsLock = new(1, 1);
    private readonly ConcurrentBag<ChatCommandDbModel> _chatCommands = [];

    public async Task RefreshCache()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<UnitOfWork>();
        var chatCommandsFromDb = await unitOfWork.ChatCommandRepo.GetAll();
        try
        {
            await _chatCommandsLock.WaitAsync();
            _chatCommands.Clear();
            foreach (var chatCommand in chatCommandsFromDb)
            {
                _chatCommands.Add(chatCommand);
            }
        }
        finally
        {
            _chatCommandsLock.Release();
        }
    }

    public async Task<ErrorOr<ChatCommandDbModel>> GetByChatMessage(string chatMessage)
    {
        try
        {
            await _chatCommandsLock.WaitAsync();
            var chatCommand = _chatCommands.FirstOrDefault(c => c.ChatMessage.Equals(chatMessage));
            if (chatCommand is null)
            {
                return Errors.Fail($"Failed to find chat command with chat message \"{chatCommand}\"");
            }

            return chatCommand;
        }
        finally
        {
            _chatCommandsLock.Release();
        }
    }
}