namespace SharedModelsLib.ApiModels;

public record ServerLogResponse(Guid StartId, string Message, DateTime MessageReceivedAtUt);