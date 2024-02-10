namespace Shared.ApiModels;

public record ServerInfoResponse(
    bool ServerInstalled,
    bool CounterStrikeSharpInstalled,
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
    bool ServerPluginsUpdatingOrInstalling,
    bool DemoUploading,
    DateTime CreatedUtc
);