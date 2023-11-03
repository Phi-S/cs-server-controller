namespace SharedModelsLib.ApiModels;

public record InfoModel(
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
    bool DemoUploading
)
{
    public virtual bool Equals(InfoModel? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Hostname, other.Hostname, StringComparison.CurrentCulture) &&
               string.Equals(ServerPassword, other.ServerPassword, StringComparison.CurrentCulture) &&
               string.Equals(CurrentMap, other.CurrentMap, StringComparison.CurrentCulture) &&
               CurrentPlayerCount == other.CurrentPlayerCount && MaxPlayerCount == other.MaxPlayerCount &&
               string.Equals(IpOrDomain, other.IpOrDomain, StringComparison.CurrentCulture) &&
               string.Equals(Port, other.Port, StringComparison.CurrentCulture) &&
               ServerStarting == other.ServerStarting && ServerStarted == other.ServerStarted &&
               ServerStopping == other.ServerStopping && ServerHibernating == other.ServerHibernating &&
               ServerUpdatingOrInstalling == other.ServerUpdatingOrInstalling && DemoUploading == other.DemoUploading;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (Hostname != null ? StringComparer.CurrentCulture.GetHashCode(Hostname) : 0);
            hashCode = (hashCode * 397) ^
                       (ServerPassword != null ? StringComparer.CurrentCulture.GetHashCode(ServerPassword) : 0);
            hashCode = (hashCode * 397) ^
                       (CurrentMap != null ? StringComparer.CurrentCulture.GetHashCode(CurrentMap) : 0);
            hashCode = (hashCode * 397) ^ CurrentPlayerCount.GetHashCode();
            hashCode = (hashCode * 397) ^ MaxPlayerCount.GetHashCode();
            hashCode = (hashCode * 397) ^ StringComparer.CurrentCulture.GetHashCode(IpOrDomain);
            hashCode = (hashCode * 397) ^ StringComparer.CurrentCulture.GetHashCode(Port);
            hashCode = (hashCode * 397) ^ ServerStarting.GetHashCode();
            hashCode = (hashCode * 397) ^ ServerStarted.GetHashCode();
            hashCode = (hashCode * 397) ^ ServerStopping.GetHashCode();
            hashCode = (hashCode * 397) ^ ServerHibernating.GetHashCode();
            hashCode = (hashCode * 397) ^ ServerUpdatingOrInstalling.GetHashCode();
            hashCode = (hashCode * 397) ^ DemoUploading.GetHashCode();
            return hashCode;
        }
    }
}