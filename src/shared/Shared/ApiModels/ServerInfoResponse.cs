namespace Shared.ApiModels;

public record ServerInfoResponse(
    bool ServerInstalled,
    string? Hostname,
    string? ServerPassword,
    string? CurrentMap,
    int? CurrentPlayerCount,
    int? MaxPlayerCount,
    string IpOrDomain,
    string Port,
    bool ServerStarting,
    bool ServerStarted,
    bool ServerStopping,
    bool ServerHibernating,
    bool ServerUpdatingOrInstalling,
    bool DemoUploading,
    DateTime CreatedUtc
);