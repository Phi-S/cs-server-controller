using System.Formats.Tar;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Application.ServerServiceFolder;
using CustomCommandsPlugin;
using Domain;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;
using SharedPluginLib;

namespace Application.ServerPluginsFolder;

public class ServerPluginsService
{
    private readonly ILogger<ServerPluginsService> _logger;
    private readonly IOptions<AppOptions> _options;
    private readonly HttpClient _httpClient;
    private readonly ServerService _serverService;

    private const string MetamodUrl = "https://mms.alliedmods.net/mmsdrop/2.0/mmsource-2.0.0-git1278-linux.tar.gz";

    private const string CounterStrikeSharpUrl =
        "https://github.com/roflmuffin/CounterStrikeSharp/releases/download/v142/counterstrikesharp-with-runtime-build-142-linux-7b45a88.zip";

    public ServerPluginsService(
        ILogger<ServerPluginsService> logger,
        IOptions<AppOptions> options,
        HttpClient httpClient,
        ServerService serverService)
    {
        _logger = logger;
        _options = options;
        _httpClient = httpClient;
        _serverService = serverService;
    }

    public async Task<ErrorOr<PlayerPosition>> GetPlayerPosition(int userId)
    {
        var commandResponse = await _serverService.ExecuteCommand($"get_player_position {userId}");
        if (commandResponse.IsError)
        {
            return commandResponse.FirstError;
        }

        var pluginResponse = PluginResponse.GetFromJson(commandResponse.Value);
        if (pluginResponse is null)
        {
            return Errors.Fail("Failed deserialize plugin response");
        }

        if (pluginResponse.Success == false)
        {
            return Errors.Fail($"Command executed but failed with error {pluginResponse.DataJson}");
        }

        Console.WriteLine(pluginResponse);
        var tryGetData = pluginResponse.TryGetData(out PlayerPosition? playerPosition);
        if (tryGetData == false || playerPosition is null)
        {
            return Errors.Fail("Failed to get data from plugin response");
        }

        return playerPosition;
    }

    public async Task<ErrorOr<Success>> PlaceBotOnPlayerPosition(int userId, string side)
    {
        side = side.ToLower();
        if (side.Equals("t") == false && side.Equals("ct") == false)
        {
            return Errors.Fail("Bots can only be placed on ct or t side");
        }

        var playerPositionResult = await GetPlayerPosition(userId);
        if (playerPositionResult.IsError)
        {
            return playerPositionResult.FirstError;
        }

        var botAddResponse = await _serverService.ExecuteCommand($"bot_add {side}");
        if (botAddResponse.IsError)
        {
            return botAddResponse.FirstError;
        }

        var addedBotRegex = @"L (?:\d{2}\/\d{2}\/\d{4}) - (?:\d{2}:){3} ""(?:.+)<(\d{1,2})><BOT><>"" entered the game";
        var botAddMatch = Regex.Match(botAddResponse.Value, addedBotRegex);
        if (botAddMatch.Groups.Count != 2)
        {
            return Errors.Fail("Failed to get valid response from add bot command");
        }

        var playerPosition = playerPositionResult.Value;
        var userIdOfNewBot = botAddMatch.Groups[1].Value;
        var commandResponse = await _serverService.ExecuteCommand(
            $"move_player" +
            $" {userIdOfNewBot}" +
            $" {playerPosition.PositionX}" +
            $" {playerPosition.PositionY}" +
            $" {playerPosition.PositionZ}" +
            $" {playerPosition.AngleX}" +
            $" {playerPosition.AngleY}" +
            $" {playerPosition.AngleZ}");
        if (commandResponse.IsError)
        {
            return commandResponse.FirstError;
        }

        return Result.Success;
    }

    #region Install

    public async Task<ErrorOr<Success>> Install()
    {
        var installBaseResult = await InstallBase();
        if (installBaseResult.IsError)
        {
            return installBaseResult.FirstError;
        }

        var installPluginsResult = await InstallPlugins();
        if (installPluginsResult.IsError)
        {
            return installBaseResult.FirstError;
        }

        return Result.Success;
    }


    public async Task<ErrorOr<Success>> InstallBase()
    {
        _logger.LogInformation("Installing server plugins");

        var csgoFolder = Path.Combine(_options.Value.SERVER_FOLDER, "game", "csgo");
        var addonsFolder = Path.Combine(csgoFolder, "addons");

        if (Directory.Exists(addonsFolder))
        {
            Directory.Delete(addonsFolder, true);
        }

        Directory.CreateDirectory(addonsFolder);

        var downloadMetamod = await DownloadMetamod(csgoFolder);
        if (downloadMetamod.IsError)
        {
            _logger.LogError("Failed to install metamod. {Error}", downloadMetamod.ErrorMessage());
            return downloadMetamod.FirstError;
        }

        var downloadCounterStrikeSharp = await DownloadCounterStrikeSharp(csgoFolder);
        if (downloadCounterStrikeSharp.IsError)
        {
            _logger.LogError("Failed to install CounterStrikeSharp. {Error}",
                downloadCounterStrikeSharp.ErrorMessage());
            return downloadCounterStrikeSharp.FirstError;
        }


        var addMetamodEntry = await AddMetamodEntryToGameinfoGi(csgoFolder);
        if (addMetamodEntry.IsError)
        {
            _logger.LogError("Failed to install CounterStrikeSharp. {Error}",
                addMetamodEntry.ErrorMessage());
            return addMetamodEntry.FirstError;
        }

        _logger.LogInformation("Server plugins installed");
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> DownloadMetamod(string csgoFolder)
    {
        var downloadPath = Path.Combine(csgoFolder, "addons", "metamod.tar.gz");
        var downLoadResult = await Download(MetamodUrl, downloadPath);
        if (downLoadResult.IsError)
        {
            return downLoadResult.FirstError;
        }

        Directory.CreateDirectory(csgoFolder);
        await using (var gzip = new GZipStream(File.OpenRead(downloadPath), CompressionMode.Decompress))
        {
            using var unzippedStream = new MemoryStream();
            await gzip.CopyToAsync(unzippedStream);
            unzippedStream.Seek(0, SeekOrigin.Begin);


            await using var reader = new TarReader(unzippedStream);
            while (await reader.GetNextEntryAsync() is { } entry)
            {
                var entryPath = Path.Combine(
                    csgoFolder,
                    entry.Name.Replace("/", Path.DirectorySeparatorChar.ToString()
                    ));

                if (entry.EntryType == TarEntryType.Directory)
                {
                    Directory.CreateDirectory(entryPath);
                }
                else
                {
                    await entry.ExtractToFileAsync(entryPath, true);
                }
            }
        }

        File.Delete(downloadPath);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> DownloadCounterStrikeSharp(string csgoFolder)
    {
        var downloadPath = Path.Combine(csgoFolder, "addons", "counterstrikesharp-with-runtime.zip");
        var downLoadResult = await Download(CounterStrikeSharpUrl, Path.Combine(downloadPath));
        if (downLoadResult.IsError)
        {
            return downLoadResult.FirstError;
        }

        Directory.CreateDirectory(csgoFolder);

        await using (var fileStream = File.OpenRead(downloadPath))
        {
            ZipFile.ExtractToDirectory(fileStream, csgoFolder);
        }

        File.Delete(downloadPath);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> Download(string downloadUrl, string downloadPath)
    {
        await using var downloadStream = await _httpClient.GetStreamAsync(downloadUrl);
        await using var fileStream = new FileStream(downloadPath, FileMode.OpenOrCreate);
        await downloadStream.CopyToAsync(fileStream);

        if (File.Exists(downloadPath))
        {
            return Result.Success;
        }

        return Errors.Fail($"Failed to download \"{downloadUrl}\"");
    }

    public async Task<ErrorOr<Success>> AddMetamodEntryToGameinfoGi(string csgoFolder)
    {
        const string metamodEntry = "			Game csgo/addons/metamod";
        var gameinfoGiPath = Path.Combine(csgoFolder, "gameinfo.gi");
        if (File.Exists(gameinfoGiPath) == false)
        {
            _logger.LogInformation("Failed to find gameinfo.gi at \"{GameinfoGiPath}\"", gameinfoGiPath);
            return Errors.Fail($"Failed to find gameinfo.gi at \"{gameinfoGiPath}\"");
        }

        var fileLines = await File.ReadAllLinesAsync(gameinfoGiPath);
        if (fileLines.Any(l => l.Trim().Equals(metamodEntry.Trim())))
        {
            _logger.LogInformation("gameinfo.gi already contains the metamod entry");
            return Result.Success;
        }

        await using var streamWriter = new StreamWriter(gameinfoGiPath);
        foreach (var fileLine in fileLines)
        {
            await streamWriter.WriteLineAsync(fileLine);
            if (fileLine.Trim().Equals("Game_LowViolence\tcsgo_lv // Perfect World content override"))
            {
                await streamWriter.WriteLineAsync(metamodEntry);
            }
        }

        await streamWriter.FlushAsync();
        streamWriter.Close();
        _logger.LogInformation("Added metamod entry to gameinfo.gi");
        return Result.Success;
    }

    public Task<ErrorOr<Success>> InstallPlugins()
    {
        var pluginsSrc = Path.Combine(_options.Value.EXECUTING_FOLDER, "ServerPluginsFolder", "plugins");
        var pluginsDest = Path.Combine(_options.Value.SERVER_FOLDER, "game", "csgo", "addons", "counterstrikesharp",
            "plugins");

        Directory.Delete(pluginsDest);

        foreach (var dirPath in Directory.GetDirectories(pluginsSrc, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(pluginsSrc, pluginsDest));
        }

        foreach (var newPath in Directory.GetFiles(pluginsSrc, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(pluginsSrc, pluginsDest), true);
        }

        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    #endregion
}