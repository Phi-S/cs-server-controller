using System.Text.Json;
using Domain;
using ErrorOr;
using Microsoft.Extensions.Options;
using Shared;
using Shared.ApiModels;

namespace Application.InstalledVersionsFolder;

public class InstalledVersionsService
{
    private readonly IOptions<AppOptions> _options;

    public InstalledVersionsService(IOptions<AppOptions> options)
    {
        _options = options;
    }

    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private string InstalledVersionsJsonPath => Path.Combine(_options.Value.DATA_FOLDER, "installed-versions.json");
    private readonly SemaphoreSlim _updateLock = new(1, 1);

    public async Task<ErrorOr<Success>> UpdateOrInstall(string name, string version)
    {
        try
        {
            await _updateLock.WaitAsync();

            name = name.ToLower().Trim();
            version = version.ToLower().Trim();
            if (File.Exists(InstalledVersionsJsonPath) == false)
            {
                var installedVersions = new List<InstalledVersionsModel>()
                {
                    new(name, version, null, DateTime.UtcNow)
                };

                var json = JsonSerializer.Serialize(installedVersions, _jsonSerializerOptions);
                await File.WriteAllTextAsync(InstalledVersionsJsonPath, json);
            }
            else
            {
                var installedVersionsJson = await File.ReadAllTextAsync(InstalledVersionsJsonPath);
                var installedVersions =
                    JsonSerializer.Deserialize<List<InstalledVersionsModel>>(installedVersionsJson,
                        _jsonSerializerOptions);
                if (installedVersions is null)
                {
                    return Errors.Fail("Failed to deserialize InstalledVersions.json");
                }

                var softwareEntry = installedVersions.FirstOrDefault(i => i.Name.Equals(name));
                if (softwareEntry is null)
                {
                    installedVersions.Add(
                        new InstalledVersionsModel(name, version, null, DateTime.UtcNow)
                    );
                }
                else
                {
                    installedVersions.Remove(softwareEntry);
                    installedVersions.Add(softwareEntry with { Version = version, UpdatedUtc = DateTime.UtcNow });
                }

                installedVersionsJson = JsonSerializer.Serialize(installedVersions, _jsonSerializerOptions);
                await File.WriteAllTextAsync(InstalledVersionsJsonPath, installedVersionsJson);
            }

            return Result.Success;
        }
        finally
        {
            _updateLock.Release();
        }
    }

    public async Task<ErrorOr<List<InstalledVersionsModel>>> GetAll()
    {
        try
        {
            await _updateLock.WaitAsync();
            if (File.Exists(InstalledVersionsJsonPath) == false)
            {
                return Errors.Fail("InstalledVersionsJson dose not exist");
            }

            var installedVersionsJson = await File.ReadAllTextAsync(InstalledVersionsJsonPath);
            var installedVersions =
                JsonSerializer.Deserialize<List<InstalledVersionsModel>>(installedVersionsJson,
                    _jsonSerializerOptions);
            if (installedVersions is null)
            {
                return Errors.Fail("Failed to deserialize InstalledVersions.json");
            }

            return installedVersions;
        }
        finally
        {
            _updateLock.Release();
        }
    }
}