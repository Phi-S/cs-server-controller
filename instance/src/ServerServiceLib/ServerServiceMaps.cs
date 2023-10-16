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
            if (mapName.EndsWith(".bsp", StringComparison.InvariantCulture))
            {
                maps.Add(Path.GetFileNameWithoutExtension(mapName));
            }
        }

        if (refreshCache == false)
        {
            return maps;
        }


        lock (_maps)
        {
            _maps.Clear();
            _maps.AddRange(maps);
        }

        return maps;
    }
}