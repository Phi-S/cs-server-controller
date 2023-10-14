// ReSharper disable InconsistentNaming

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace AppOptionsLib;

public class AppOptions
{
    public const string SECTION_NAME = "APP_OPTIONS";

    [Required] public required string APP_NAME { get; init; }
    [Required] public required string STEAM_USERNAME { get; init; }
    [Required] public required string STEAM_PASSWORD { get; init; }

    public string DATA_FOLDER => "/data";
    public string SERVER_FOLDER => Path.Combine(DATA_FOLDER, "server");
    public string STEAMCMD_FOLDER => Path.Combine(DATA_FOLDER, "steamcmd");
    public readonly string STEAMCMD_SH_NAME = "steamcmd.sh";
    public string STEAMCMD_SH_PATH => Path.Combine(STEAMCMD_FOLDER, STEAMCMD_SH_NAME);
    public readonly string STEAMCMD_PYTHON_SCRIPT_NAME = "steamcmd.py";
    public string STEAMCMD_PYTHON_SCRIPT_PATH => Path.Combine(STEAMCMD_FOLDER, STEAMCMD_PYTHON_SCRIPT_NAME);
}