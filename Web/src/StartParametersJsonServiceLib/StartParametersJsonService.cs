using System.Text.Json;
using AppOptionsLib;
using Microsoft.Extensions.Options;
using SharedModelsLib.ApiModels;

namespace StartParametersJsonServiceLib;

public class StartParametersJsonService(IOptions<AppOptions> options)
{
    public void Overwrite(StartParameters startParameters)
    {
        var json = JsonSerializer.Serialize(startParameters);
        File.WriteAllText(options.Value.START_PARAMETERS_JSON_PATH, json);
    }

    /// <summary>
    /// If the <c>start-parameters.json</c> file dose not exists. A new one with the default values will be created and returned.
    /// </summary>
    /// <returns>The deserialized <c>StartParameters</c> from the <c>start-parameters.json</c> file</returns>
    /// <exception cref="Exception"></exception>
    public StartParameters Get()
    {
        var startParametersJsonPath = options.Value.START_PARAMETERS_JSON_PATH;
        if (File.Exists(startParametersJsonPath) == false)
        {
            Overwrite(new StartParameters());
        }

        var json = File.ReadAllText(startParametersJsonPath);
        return JsonSerializer.Deserialize<StartParameters>(json) ??
               throw new Exception("Failed to Deserialize StartParameters from file");
    }
}