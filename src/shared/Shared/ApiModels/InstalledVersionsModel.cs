namespace Shared.ApiModels;

public record InstalledVersionsModel(
    string Name,
    string Version,
    DateTime? UpdatedUtc,
    DateTime InstalledUtc);