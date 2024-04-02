using ErrorOr;

namespace Application.PluginsFolder;

public record PluginDependency(PluginModel Plugin, string Version);

/// <summary>
/// 
/// </summary>
/// <param name="Version"></param>
/// <param name="DownloadUrl"></param>
/// <param name="DestinationFolder">The folder in with to copy the downloaded files. Base is the csgo directory. Example: \addons\counterstrikesharp\plugins</param>
public record PluginVersion(
    string Version,
    string DownloadUrl,
    string DestinationFolder,
    PluginDependency[]? PluginDependencies,
    Func<Task<ErrorOr<Success>>>? AdditionalAction = null);

public record PluginModel(
    string Name,
    string Url,
    PluginVersion[] Versions);