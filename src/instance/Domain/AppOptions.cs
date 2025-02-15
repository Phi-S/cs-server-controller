﻿// ReSharper disable InconsistentNaming

using System.ComponentModel.DataAnnotations;

namespace Domain;

public class AppOptions
{
    public const string SECTION_NAME = "APP_OPTIONS";

    [Required] public required string APP_NAME { get; init; }
    [Required] public required string IP_OR_DOMAIN { get; init; }
    [Required] public required string PORT { get; init; }
    [Required] public required string STEAM_USERNAME { get; init; }
    [Required] public required string STEAM_PASSWORD { get; init; }


    public string? SEQ_URL { get; init; }
    public string DATA_FOLDER { get; init; } = "/data";
    public bool START_SERVER_ON_STARTUP { get; init; } = false;

    public string DATABASE_PATH => Path.Combine(DATA_FOLDER, "instance.db");
    public string SERVER_FOLDER => Path.Combine(DATA_FOLDER, "server");
    public string CSGO_FOLDER => Path.Combine(SERVER_FOLDER, "game", "csgo");
    public string STEAMCMD_FOLDER => Path.Combine(DATA_FOLDER, "steamcmd");

    public readonly string EXECUTING_FOLDER = Environment.CurrentDirectory;
}