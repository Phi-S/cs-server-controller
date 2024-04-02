using System.Text.Json;
using Domain;
using ErrorOr;
using Microsoft.Extensions.Options;
using Shared;
using Shared.ApiModels;

namespace Application.PluginsFolder;

public class InstalledPluginsService
{
    private readonly IOptions<AppOptions> _options;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private string InstalledVersionsJsonPath => Path.Combine(_options.Value.DATA_FOLDER, "installed-plugins.json");
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<InstalledPluginVersionsModel>? _cache;

    public InstalledPluginsService(IOptions<AppOptions> options)
    {
        _options = options;

        if (File.Exists(InstalledVersionsJsonPath) == false)
        {
            var json = JsonSerializer.Serialize(new List<InstalledPluginVersionsModel>(), _jsonSerializerOptions);
            File.WriteAllText(InstalledVersionsJsonPath, json);
        }
    }

    public async Task<ErrorOr<Success>> UpdateOrInstall(string name, string version)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Errors.Fail("Name is empty");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            return Errors.Fail("Version is empty");
        }

        try
        {
            await _lock.WaitAsync();

            name = name.ToLower().Trim();
            version = version.ToLower().Trim();

            var installedVersionsJson = await File.ReadAllTextAsync(InstalledVersionsJsonPath);
            var installedVersions =
                JsonSerializer.Deserialize<List<InstalledPluginVersionsModel>>(installedVersionsJson,
                    _jsonSerializerOptions);
            if (installedVersions is null)
            {
                return Errors.Fail("Failed to deserialize InstalledVersions.json");
            }

            var pluginEntry = installedVersions.FirstOrDefault(i => i.Name.Equals(name));
            if (pluginEntry is not null)
            {
                installedVersions.Remove(pluginEntry);
            }

            installedVersions.Add(new InstalledPluginVersionsModel(name, version));

            installedVersionsJson = JsonSerializer.Serialize(installedVersions, _jsonSerializerOptions);
            await File.WriteAllTextAsync(InstalledVersionsJsonPath, installedVersionsJson);

            _cache = installedVersions;
            return Result.Success;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ErrorOr<Success>> Uninstall(string name)
    {
        try
        {
            await _lock.WaitAsync();

            name = name.ToLower().Trim();

            var installedVersionsJson = await File.ReadAllTextAsync(InstalledVersionsJsonPath);
            var installedVersions =
                JsonSerializer.Deserialize<List<InstalledPluginVersionsModel>>(installedVersionsJson,
                    _jsonSerializerOptions);
            if (installedVersions is null)
            {
                return Errors.Fail("Failed to deserialize InstalledVersions.json");
            }

            var softwareEntry = installedVersions.FirstOrDefault(i => i.Name.Equals(name));
            if (softwareEntry is null)
            {
                return Errors.Fail($"No plugin with the name \"{name}\" installed");
            }

            installedVersions.Remove(softwareEntry);

            installedVersionsJson = JsonSerializer.Serialize(installedVersions, _jsonSerializerOptions);
            await File.WriteAllTextAsync(InstalledVersionsJsonPath, installedVersionsJson);

            _cache = installedVersions;
            return Result.Success;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ErrorOr<List<InstalledPluginVersionsModel>>> GetAll()
    {
        try
        {
            await _lock.WaitAsync();

            if (_cache is not null)
            {
                return _cache;
            }

            var installedVersionsJson = await File.ReadAllTextAsync(InstalledVersionsJsonPath);
            var installedVersions =
                JsonSerializer.Deserialize<List<InstalledPluginVersionsModel>>(installedVersionsJson,
                    _jsonSerializerOptions);
            if (installedVersions is null)
            {
                return Errors.Fail("Failed to deserialize InstalledVersions.json");
            }

            _cache = installedVersions;
            return installedVersions;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ErrorOr<bool>> IsInstalled(string name, string version)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        name = name.ToLower().Trim();
        version = version.ToLower().Trim();

        var getAll = await GetAll();
        if (getAll.IsError)
        {
            return getAll.Errors;
        }

        return getAll.Value
            .Exists(i => i.Name == name && i.Version == version);
    }

    public async Task<ErrorOr<bool>> IsInstalled(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        name = name.ToLower().Trim();

        var getAll = await GetAll();
        if (getAll.IsError)
        {
            return getAll.Errors;
        }

        return getAll.Value
            .Exists(i => i.Name == name);
    }
}