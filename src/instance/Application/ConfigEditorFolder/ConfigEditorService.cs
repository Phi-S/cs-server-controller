using Application.EventServiceFolder;
using Domain;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace Application.ConfigEditorFolder;

public class ConfigEditorService
{
    private readonly ILogger<ConfigEditorService> _logger;
    private readonly IOptions<AppOptions> _options;
    private readonly EventService _eventService;

    public ConfigEditorService(ILogger<ConfigEditorService> logger, IOptions<AppOptions> options,
        EventService eventService)
    {
        _logger = logger;
        _options = options;
        _eventService = eventService;
        _eventService.StartingServerDone += (_, _) =>
        {
            try
            {
                ApplyEditedConfigs();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception in OnStartingServerDone ConfigEditorService");
            }
        };
    }

    private string ConfigFolder => Path.Combine(_options.Value.SERVER_FOLDER, "game", "csgo", "cfg");
    private string EditedConfigFolder => Path.Combine(_options.Value.DATA_FOLDER, "edited-config");


    public ErrorOr<List<string>> GetExistingConfigs()
    {
        var configFilePaths = new List<string>();
        if (Directory.Exists(EditedConfigFolder))
        {
            var editedConfigFiles = Directory.GetFiles(EditedConfigFolder);
            configFilePaths.AddRange(editedConfigFiles);
        }

        if (Directory.Exists(ConfigFolder) == false)
        {
            return Errors.Fail($"Config folder \"{ConfigFolder}\" dose not exist");
        }

        var configFiles = Directory.GetFiles(ConfigFolder);
        configFilePaths.AddRange(configFiles);

        var result = new List<string>();
        foreach (var configFilePath in configFilePaths)
        {
            var configName = Path.GetFileName(configFilePath);
            if (string.IsNullOrWhiteSpace(configName))
            {
                continue;
            }

            result.Add(configName);
        }

        return result.Distinct().ToList();
    }

    public async Task<ErrorOr<string>> GetConfigFile(string fileName)
    {
        if (Directory.Exists(EditedConfigFolder))
        {
            var editedConfigFilePath = Path.Combine(EditedConfigFolder, fileName);
            if (File.Exists(editedConfigFilePath))
            {
                return await File.ReadAllTextAsync(editedConfigFilePath);
            }
        }

        var configFilePath = Path.Combine(ConfigFolder, fileName);
        if (File.Exists(configFilePath) == false)
        {
            return Errors.Fail("File not found");
        }

        return await File.ReadAllTextAsync(configFilePath);
    }

    public async Task<ErrorOr<Success>> SetConfigFile(string fileName, string content)
    {
        if (Directory.Exists(EditedConfigFolder) == false)
        {
            Directory.CreateDirectory(EditedConfigFolder);
        }

        if (Directory.Exists(ConfigFolder) == false)
        {
            return Errors.Fail($"Config folder \"{ConfigFolder}\" dose not exist");
        }

        var editedConfigFilePath = Path.Combine(EditedConfigFolder, fileName);
        await File.WriteAllTextAsync(editedConfigFilePath, content);

        var configFileDestPath = Path.Combine(ConfigFolder, fileName);
        File.Copy(editedConfigFilePath, configFileDestPath, true);

        _logger.LogInformation("Config \"{ConfigFileName}\" updated", fileName);
        return Result.Success;
    }

    private void ApplyEditedConfigs()
    {
        if (Directory.Exists(EditedConfigFolder) == false)
        {
            return;
        }

        var editedConfigFilePaths = Directory.GetFiles(EditedConfigFolder);
        foreach (var editedConfigFilePath in editedConfigFilePaths)
        {
            if (editedConfigFilePath.EndsWith(".cfg") == false)
            {
                continue;
            }

            var configDestinationPath = Path.Combine(ConfigFolder, editedConfigFilePath);
            File.Copy(editedConfigFilePath, configDestinationPath, true);
        }
    }
}