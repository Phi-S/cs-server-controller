using Domain;
using ErrorOr;

namespace Application.ServerServiceFolder;

public partial class ServerService
{
    private readonly Dictionary<string, string> _configs = new();
    private readonly object _configsLock = new();

    public ErrorOr<Dictionary<string, string>> GetAvailableConfigs(string serverFolder, bool refreshCache = false)
    {
        lock (_configsLock)
        {
            if (refreshCache == false && _configs.Count != 0)
            {
                return _configs;
            }
        }

        if (_statusService.ServerInstalled == false)
        {
            return InstanceErrors.ServerIsNotInstalled();
        }

        var cfgFolder = Path.Combine(serverFolder, "game", "csgo", "cfg");
        var configs = Directory.GetFiles(cfgFolder).Where(cfgPath => cfgPath.EndsWith("c.cfg"))
            .Select(Path.GetFileName);

        lock (_configsLock)
        {
            _configs.Clear();
            foreach (var config in configs)
            {
                if (config is null)
                {
                    continue;
                }

                var configPath = Path.Combine(cfgFolder, config);
                _configs.Add(config, configPath);
            }

            return _configs;
        }
    }
}