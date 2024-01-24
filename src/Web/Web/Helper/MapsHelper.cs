namespace Web.Helper;

public record Map(string MapName, string FriendlyMapName);

public static class MapsHelper
{
    public static readonly List<Map> Maps =
    [
        new Map("cs_italy", "Italy"),
        new Map("de_anubis", "Anubis"),
        new Map("de_inferno", "Inferno"),
        new Map("de_dust2", "Dust 2"),
        new Map("de_ancient", "Ancient"),
        new Map("de_mirage", "Mirage"),
        new Map("de_vertigo", "Vertigo"),
        new Map("de_overpass", "Overpass"),
        new Map("cs_office", "Office"),
        new Map("de_nuke", "Nuke")
    ];
}