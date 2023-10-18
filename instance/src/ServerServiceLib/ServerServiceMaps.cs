namespace ServerServiceLib;

public partial class ServerService
{
    private readonly List<string> _maps = new();
    private readonly object _mapsLock = new();

    public List<string> GetAllMaps(string serverFolder, bool refreshCache = false)
    {
        lock (_maps)
        {
            if (refreshCache == false && _maps.Count != 0)
            {
                return _maps.ToList();
            }
        }

        var mapFolderPath = Path.Combine(serverFolder, "game", "csgo", "maps");
        var maps = new List<string>();

        foreach (var mapPath in Directory.GetFiles(mapFolderPath))
        {
            var mapName = Path.GetFileName(mapPath);
            if (mapName.EndsWith(".vpk", StringComparison.InvariantCulture) == false)
            {
                continue;
            }

            if (mapName.Count(c => c == '_') > 1)
            {
                continue;
            }

            if (mapName.Equals("graphics_settings.vpk"))
            {
                continue;
            }

            if (mapName.Equals("lobby_mapveto.vpk"))
            {
                continue;
            }

            maps.Add(Path.GetFileNameWithoutExtension(mapName));
        }

        lock (_maps)
        {
            _maps.Clear();
            _maps.AddRange(maps);
        }

        return maps;
    }
}