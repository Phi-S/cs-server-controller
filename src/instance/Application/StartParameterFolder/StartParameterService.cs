using System.Text.Json;
using Domain;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;
using Shared.ApiModels;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Application.StartParameterFolder;

public class StartParameterService
{
    private readonly ILogger<StartParameterService> _logger;
    private readonly IOptions<AppOptions> _options;

    public StartParameterService(ILogger<StartParameterService> logger, IOptions<AppOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    private string StartParameterJsonPath => Path.Combine(_options.Value.DATA_FOLDER, "start-parameter.json");
    private readonly object _startParameterLock = new();
    private StartParameters? _startParametersCache;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true
    };

    public ErrorOr<StartParameters> Get()
    {
        lock (_startParameterLock)
        {
            if (_startParametersCache is not null)
            {
                _logger.LogDebug("Returning cached start parameters");
                return _startParametersCache;
            }

            if (File.Exists(StartParameterJsonPath))
            {
                var json = File.ReadAllText(StartParameterJsonPath);
                var startParameters = JsonSerializer.Deserialize<StartParameters>(json, _jsonSerializerOptions);
                if (startParameters is null)
                {
                    return Errors.Fail("Failed to deserialize json file");
                }

                _startParametersCache = startParameters;
                _logger.LogDebug("Returning start parameters from json file");
                return startParameters;
            }
        }

        _logger.LogDebug("Creating new default start parameters");
        var newStartParameters = new StartParameters();
        Set(newStartParameters);
        return newStartParameters;
    }

    public void Set(StartParameters startParameters)
    {
        lock (_startParameterLock)
        {
            if (File.Exists(StartParameterJsonPath))
            {
                File.Delete(StartParameterJsonPath);
            }

            var json = JsonSerializer.Serialize(startParameters, _jsonSerializerOptions);
            File.WriteAllText(StartParameterJsonPath, json);
            _startParametersCache = startParameters;
            _logger.LogDebug("Start parameters file and cache updated");
        }
    }
}