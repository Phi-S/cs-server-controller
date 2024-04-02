namespace Shared.ApiModels;

public record PluginsResponseModel(
    string Name,
    string Url,
    string[] Versions,
    string? InstalledVersion
);