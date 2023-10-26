// ReSharper disable InconsistentNaming

namespace AppOptionsLib;

public class AppOptions
{
    public const string SECTION_NAME = "APP_OPTIONS";

    public string APP_NAME { get; set; } = "cs-instance-web";
    public string INSTANCE_API_ENDPOINT { get; set; } = "http://127.0.0.1:11111";
}