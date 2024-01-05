namespace Application.StatusServiceFolder;

public record PlayerInfoModel(
    string ConnectionId,
    string SteamId64,
    string IpPort,
    string DisconnectReasonCode,
    string DisconnectReason);