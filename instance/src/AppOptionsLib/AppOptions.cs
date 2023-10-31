// ReSharper disable InconsistentNaming

using System.ComponentModel.DataAnnotations;

namespace AppOptionsLib;

public class AppOptions
{
    public const string SECTION_NAME = "APP_OPTIONS";

    [Required] public required string APP_NAME { get; init; }
    [Required] public required string IP_OR_DOMAIN { get; init; }
    [Required] public required string PORT { get; init; }
    [Required] public required string STEAM_USERNAME { get; init; }
    [Required] public required string STEAM_PASSWORD { get; init; }

    public string DATA_FOLDER { get; init; } = "/data";
    public string DATABASE_PATH => Path.Combine(DATA_FOLDER, "instance.db");
    public string SERVER_FOLDER => Path.Combine(DATA_FOLDER, "server");
    public string STEAMCMD_FOLDER => Path.Combine(DATA_FOLDER, "steamcmd");

    public string EXECUTING_FOLDER =
        Path.GetDirectoryName(Environment.ProcessPath) ?? throw new NullReferenceException(nameof(EXECUTING_FOLDER));
}