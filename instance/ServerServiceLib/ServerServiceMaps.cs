namespace ServerServiceLib;

public partial class ServerService
{
    private volatile List<string> _maps = new();
    private readonly object _mapsLock = new();

    private List<string> GetAllMaps(string serverFolder, bool refreshCache = false)
    {
        if (refreshCache == false && _maps.Count != 0)
        {
            return _maps.ToList();
        }

        var mapFolderPath = Path.Combine(serverFolder, "game", "csgo", "maps");
        var maps = new List<string>();

        foreach (var mapPath in Directory.GetFiles(mapFolderPath))
        {
            var mapName = Path.GetFileName(mapPath);
            if (mapName.EndsWith(".bsp", StringComparison.InvariantCulture))
            {
                maps.Add(Path.GetFileNameWithoutExtension(mapName));
            }
        }

        return maps;
    }
}