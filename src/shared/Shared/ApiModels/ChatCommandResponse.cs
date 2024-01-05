namespace Shared.ApiModels;

public record ChatCommandResponse(long CommandId, string ChatMessage, string ServerCommand);