namespace Shared.ApiModels;

public record ServerLogResponse(Guid StartId, string Message, DateTime MessageReceivedAtUt);