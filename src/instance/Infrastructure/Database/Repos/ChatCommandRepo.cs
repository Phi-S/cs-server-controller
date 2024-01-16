using ErrorOr;
using Infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Infrastructure.Database.Repos;

public class ChatCommandRepo
{
    private readonly InstanceDbContext _dbContext;

    public ChatCommandRepo(InstanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ChatMessageExist(string chatMessage)
    {
        var cleanedChatMessage = chatMessage.ToLower().Trim();
        return await _dbContext.ChatCommands.AnyAsync(command => command.ChatMessage.Equals(cleanedChatMessage));
    }

    public async Task<ErrorOr<Created>> Add(string chatMessage, string command)
    {
        if (await ChatMessageExist(chatMessage))
        {
            return Errors.Fail($"Chat message \"{chatMessage}\" is already exists");
        }

        var cleanedChatMessage = chatMessage.ToLower().Trim();
        await _dbContext.ChatCommands.AddAsync(new ChatCommandDbModel
        {
            ChatMessage = cleanedChatMessage,
            Command = command,
            UpdatedUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow
        });
        return Result.Created;
    }

    public async Task<ErrorOr<Deleted>> Delete(string chatMessage)
    {
        var cleanedChatMessage = chatMessage.ToLower().Trim();
        var chatCommandToDelete =
            await _dbContext.ChatCommands.FirstOrDefaultAsync(command =>
                command.ChatMessage.Equals(cleanedChatMessage));
        if (chatCommandToDelete is null)
        {
            return Errors.Fail($"Chat command with chat message \"{cleanedChatMessage}\" dose not exist");
        }

        _dbContext.ChatCommands.Remove(chatCommandToDelete);
        return Result.Deleted;
    }

    public Task<List<ChatCommandDbModel>> GetAll()
    {
        return Task.FromResult(_dbContext.ChatCommands.ToList());
    }
}