namespace Shared.ApiModels;

public record UpdateOrInstallLogResponse(Guid UpdateOrInstallId, string Message, DateTime MessageReceivedAtUt);