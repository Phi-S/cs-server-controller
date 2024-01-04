// ReSharper disable InconsistentNaming

using System.ComponentModel.DataAnnotations;

namespace Web.Options;

public class AppOptions
{
    public const string SECTION_NAME = "APP_OPTIONS";

    [Required] public string APP_NAME { get; set; } = "cs-instance-web";
    [Required] public string INSTANCE_API_ENDPOINT { get; set; } = "http://127.0.0.1:11111";

    public string DATA_FOLDER { get; set; } = "/data";
    public string START_PARAMETERS_JSON_PATH => Path.Combine(DATA_FOLDER, "start-parameters.json");
    
    public string? SEQ_URL { get; set; }
}