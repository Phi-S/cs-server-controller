namespace SharedModelsLib.ApiModels;

public record UpdateOrInstallLogResponse(Guid UpdateOrInstallId, string Message, DateTime MessageReceivedAtUt);