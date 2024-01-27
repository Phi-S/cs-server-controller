using System.Collections.Immutable;
using System.Formats.Tar;
using System.IO.Compression;
using Application.EventServiceFolder;
using Application.Helpers;
using Application.ServerHelperFolder;
using Application.StatusServiceFolder;
using Application.SystemLogFolder;
using Domain;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace Application.ServerPluginsFolder;

public class ServerPluginsService
{
    private readonly ILogger<ServerPluginsService> _logger;
    private readonly IOptions<AppOptions> _options;
    private readonly HttpClient _httpClient;
    private readonly StatusService _statusService;
    private readonly EventService _eventService;
    private readonly SystemLogService _systemLogService;

    public ServerPluginsService(
        ILogger<ServerPluginsService> logger,
        IOptions<AppOptions> options,
        HttpClient httpClient,
        StatusService statusService,
        EventService eventService,
        SystemLogService systemLogService)
    {
        _logger = logger;
        _options = options;
        _httpClient = httpClient;
        _statusService = statusService;
        _eventService = eventService;
        _systemLogService = systemLogService;
    }

    private static readonly IImmutableList<string> AlwaysActivePlugins = ["EnableDisablePlugin"];

    private string CsgoFolder => Path.Combine(
        _options.Value.SERVER_FOLDER,
        "game",
        "csgo");

    #region Install

    public async Task<ErrorOr<Success>> UpdateOrInstall()
    {
        var updateOrInstall = ServerHelper.IsServerPluginBaseInstalled(_options.Value.SERVER_FOLDER);
        var updateOrInstallString = updateOrInstall
            ? "update"
            : "install";

        _logger.LogInformation("Starting server plugins {UpdateOrInstall}", updateOrInstallString);
        _systemLogService.LogHeader();
        _systemLogService.Log($"Starting server plugins {updateOrInstallString}");

        if (_statusService.ServerPluginsUpdatingOrInstalling)
        {
            _logger.LogError(
                "Failed to {UpdateOrInstall} server plugins. Another update or install process is already in progress",
                updateOrInstallString);
            _systemLogService.Log(
                $"Failed to {updateOrInstallString} server plugins. Another update or install process is already in progress");
            return Errors.Fail(
                $"Failed to {updateOrInstallString} server plugins. Another update or install process is already in progress");
        }

        if (_statusService.ServerStarted ||
            _statusService.ServerStarting ||
            _statusService.ServerStopping ||
            _statusService.ServerUpdatingOrInstalling)
        {
            _logger.LogError("Failed to {UpdateOrInstall} server plugins. Server is busy", updateOrInstallString);
            _systemLogService.Log($"Failed to {updateOrInstallString} server plugins. Server is busy");
            return Errors.Fail($"Failed to {updateOrInstallString} server plugins. Server is busy");
        }

        try
        {
            _eventService.OnPluginUpdateOrInstallStarted();
            var updatingOrInstallingString = updateOrInstall
                ? "Updating"
                : "Installing";

            _logger.LogInformation("{UpdatingOrInstalling} plugin base", updatingOrInstallingString);
            _systemLogService.Log($"{updatingOrInstallingString} plugin base");
            var installOrUpdateBase = await UpdateOrInstallBase();
            if (installOrUpdateBase.IsError)
            {
                _eventService.OnPluginUpdateOrInstallFailed();
                _logger.LogError(
                    "Server plugins {UpdateOrInstall} failed. Failed to {UpdateOrInstall-} plugin base. {Error}",
                    updateOrInstallString, updateOrInstallString, installOrUpdateBase.ErrorMessage());
                _systemLogService.Log(
                    $"Server plugins {updateOrInstallString} failed. Failed to {updateOrInstallString} plugin base.");
                return installOrUpdateBase.FirstError;
            }

            _logger.LogInformation("{UpdatingOrInstalling} plugins", updatingOrInstallingString);
            _systemLogService.Log($"{updatingOrInstallingString} plugins");
            var installOrUpdatePlugins = UpdateOrInstallPlugins();
            if (installOrUpdatePlugins.IsError)
            {
                _eventService.OnPluginUpdateOrInstallFailed();
                _logger.LogError(
                    "Server plugins {UpdateOrInstall} failed. Failed to {UpdateOrInstall-} plugins. {Error}",
                    updateOrInstallString, updateOrInstallString, installOrUpdatePlugins.ErrorMessage());
                _systemLogService.Log($"Server plugins {updateOrInstallString} failed. Failed to install plugins.");
                return installOrUpdateBase.FirstError;
            }

            _eventService.OnPluginUpdateOrInstallDone();
            _logger.LogInformation("Server plugins {UpdateOrInstall} completed", updateOrInstallString);
            _systemLogService.Log($"Server plugins {updateOrInstallString} completed");
            return Result.Success;
        }
        catch (Exception e)
        {
            _eventService.OnPluginUpdateOrInstallFailed();
            _logger.LogError(e, "Server plugins {UpdateOrInstall} failed with exception", updateOrInstallString);
            _systemLogService.Log($"Server plugins {updateOrInstallString} failed for unknown reasons");
            throw;
        }
    }

    #region Base

    public async Task<ErrorOr<Success>> UpdateOrInstallBase()
    {
        var addonsFolder = Path.Combine(CsgoFolder, "addons");
        Directory.CreateDirectory(addonsFolder);

        var installMetamod = await InstallMetamod(_httpClient, CsgoFolder);
        if (installMetamod.IsError)
        {
            return Errors.Fail($"Failed to install metamod. {installMetamod.ErrorMessage()}");
        }

        var installCounterStrikeSharp = await InstallCounterStrikeSharp(_httpClient, CsgoFolder);
        if (installCounterStrikeSharp.IsError)
        {
            return Errors.Fail($"Failed to install CounterStrikeSharp. {installCounterStrikeSharp.ErrorMessage()}");
        }

        _logger.LogInformation("CounterStrikeSharp installed");

        var addMetamodEntry = await AddMetamodEntryToGameinfoGi(CsgoFolder);
        if (addMetamodEntry.IsError)
        {
            return Errors.Fail($"Failed to add metamod entry to gameinfo.gi. {addMetamodEntry.ErrorMessage()}");
        }

        var configsFolder = Path.Combine(addonsFolder, "counterstrikesharp", "configs");
        CreateCoreCfg(configsFolder);
        return Result.Success;
    }

    public static async Task<ErrorOr<Success>> InstallMetamod(HttpClient httpClient, string csgoFolder)
    {
        var downloadTempFolder = FolderHelper.CreateNewTempFolder(csgoFolder);
        const string metamodUrl = "https://mms.alliedmods.net/mmsdrop/2.0/mmsource-2.0.0-git1280-linux.tar.gz";
        var downloadPath = Path.Combine(downloadTempFolder, "metamod.tar.gz");
        var downLoadResult = await Download(httpClient, metamodUrl, downloadPath);
        if (downLoadResult.IsError)
        {
            return downLoadResult.FirstError;
        }

        var extractionTempFolder = FolderHelper.CreateNewTempFolder(csgoFolder);
        await using (var gzip = new GZipStream(File.OpenRead(downloadPath), CompressionMode.Decompress))
        {
            using var unzippedStream = new MemoryStream();
            await gzip.CopyToAsync(unzippedStream);
            unzippedStream.Seek(0, SeekOrigin.Begin);

            await using var reader = new TarReader(unzippedStream);
            while (await reader.GetNextEntryAsync() is { } entry)
            {
                var entryPath = Path.Combine(
                    extractionTempFolder,
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

        var extractedAddonsFolder = Path.Combine(extractionTempFolder, "addons");
        if (Directory.Exists(extractedAddonsFolder) == false)
        {
            return Errors.Fail("Failed to extract metamod. No Addons folder found in extraction destination");
        }

        Directory.Delete(downloadTempFolder, true);
        var csgoAddonsFolder = Path.Combine(csgoFolder, "addons");
        var copyDirectory = FolderHelper.CopyDirectory(extractedAddonsFolder, csgoAddonsFolder);
        if (copyDirectory.IsError)
        {
            return copyDirectory.FirstError;
        }

        if (File.Exists(Path.Combine(csgoAddonsFolder, "metamod.vdf")) == false
            || File.Exists(Path.Combine(csgoAddonsFolder, "metamod_x64.vdf")) == false
            || Directory.Exists(Path.Combine(csgoAddonsFolder, "metamod")) == false
           )
        {
            return Errors.Fail($"Metamod installation not found at destination folder \"{csgoAddonsFolder}\"");
        }

        return Result.Success;
    }

    public static async Task<ErrorOr<Success>> InstallCounterStrikeSharp(HttpClient httpClient, string csgoFolder)
    {
        var downloadTempFolder = FolderHelper.CreateNewTempFolder(csgoFolder);
        const string counterStrikeSharpUrl =
            "https://github.com/roflmuffin/CounterStrikeSharp/releases/download/v159/counterstrikesharp-with-runtime-build-159-linux-5695c3f.zip";
        var downloadPath = Path.Combine(downloadTempFolder, "counterstrikesharp-with-runtime.zip");
        var downLoadResult = await Download(httpClient, counterStrikeSharpUrl, Path.Combine(downloadPath));
        if (downLoadResult.IsError)
        {
            return downLoadResult.FirstError;
        }

        var extractionTempFolder = FolderHelper.CreateNewTempFolder(csgoFolder);
        await using (var fileStream = File.OpenRead(downloadPath))
        {
            ZipFile.ExtractToDirectory(fileStream, extractionTempFolder);
        }

        var extractedAddonsFolder = Path.Combine(extractionTempFolder, "addons");
        if (Directory.Exists(extractionTempFolder) == false
            || File.Exists(Path.Combine(extractedAddonsFolder, "metamod", "counterstrikesharp.vdf")) == false
            || File.Exists(Path.Combine(extractedAddonsFolder, "counterstrikesharp", "api",
                "CounterStrikeSharp.API.dll")) == false)
        {
            return Errors.Fail("Extracted CounterStrikeSharp dose not exist");
        }

        Directory.Delete(downloadTempFolder, true);

        var csgoAddonsFolder = Path.Combine(csgoFolder, "addons");
        var copyDirectory = FolderHelper.CopyDirectory(extractedAddonsFolder, csgoAddonsFolder);
        if (copyDirectory.IsError)
        {
            return copyDirectory.FirstError;
        }

        if (Directory.Exists(csgoAddonsFolder) == false
            || File.Exists(Path.Combine(csgoAddonsFolder, "metamod", "counterstrikesharp.vdf")) == false
            || File.Exists(Path.Combine(csgoAddonsFolder, "counterstrikesharp", "api", "CounterStrikeSharp.API.dll")) ==
            false)
        {
            return Errors.Fail($"CounterStrikeSharp installation not found at destination {csgoAddonsFolder}");
        }

        return Result.Success;
    }

    public static async Task<ErrorOr<Success>> Download(HttpClient httpClient, string downloadUrl, string downloadPath)
    {
        await using var downloadStream = await httpClient.GetStreamAsync(downloadUrl);
        await using var fileStream = new FileStream(downloadPath, FileMode.OpenOrCreate);
        await downloadStream.CopyToAsync(fileStream);

        if (File.Exists(downloadPath))
        {
            return Result.Success;
        }

        return Errors.Fail($"Failed to download \"{downloadUrl}\"");
    }

    public static async Task<ErrorOr<Success>> AddMetamodEntryToGameinfoGi(string csgoFolder)
    {
        const string metamodEntry = "			Game csgo/addons/metamod";
        var gameinfoGiPath = Path.Combine(csgoFolder, "gameinfo.gi");
        if (File.Exists(gameinfoGiPath) == false)
        {
            return Errors.Fail($"Failed to find gameinfo.gi at \"{gameinfoGiPath}\"");
        }

        var fileLines = await File.ReadAllLinesAsync(gameinfoGiPath);
        if (fileLines.Any(l => l.Trim().Equals(metamodEntry.Trim())))
        {
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
        return Result.Success;
    }

    public static void CreateCoreCfg(string configsFolder)
    {
        var coreCfgJsonPath = Path.Combine(configsFolder, "core.json");
        const string json =
            """
            {
                "PublicChatTrigger": [ "." ],
                "SilentChatTrigger": [ "/" ],
                "FollowCS2ServerGuidelines": true,
                "PluginHotReloadEnabled": true,
                "ServerLanguage": "en"
            }
            """;

        File.WriteAllText(coreCfgJsonPath, json);
    }

    #endregion


    #region Plugins

    public ErrorOr<Success> UpdateOrInstallPlugins()
    {
        var pluginsSrc = Path.Combine(_options.Value.EXECUTING_FOLDER, nameof(ServerPluginsFolder), "plugins");
        var pluginsDest = Path.Combine(CsgoFolder, "addons", "counterstrikesharp", "plugins");
        var disabledPluginsDest = Path.Combine(pluginsDest, "disabled");

        if (Directory.Exists(pluginsSrc) == false)
        {
            return Errors.Fail($"Plugin source folder \"{pluginsSrc}\" dose not exist");
        }

        if (Directory.Exists(pluginsDest) == false
            || Directory.Exists(disabledPluginsDest) == false)
        {
            return Errors.Fail($"CounterStrikeSharp is not installed. Plugin folder \"{pluginsDest}\" dose not exist");
        }

        var pluginsToInstall = Directory
            .GetDirectories(pluginsSrc)
            .Select(p => Path.GetFileName(p))
            .ToList();

        if (pluginsToInstall.Count == 0 && AlwaysActivePlugins.Count == 0)
        {
            return Result.Success;
        }

        if (pluginsToInstall.Count == 0 && AlwaysActivePlugins.Count != 0)
        {
            return Errors.Fail(
                $"No plugins to install but those always active plugins are required: [{string.Join(",", AlwaysActivePlugins)}]");
        }

        _logger.LogInformation("Plugins to install: ({Count})[{Plugins}]", pluginsToInstall.Count,
            string.Join(",", pluginsToInstall));

        // Copy always active plugins
        foreach (var alwaysActivePlugin in AlwaysActivePlugins)
        {
            if (pluginsToInstall.Contains(alwaysActivePlugin) == false)
            {
                return Errors.Fail($"Always active plugin \"{alwaysActivePlugin}\" not found at \"{pluginsSrc}\"");
            }
        }

        foreach (var alwaysActivePlugin in AlwaysActivePlugins)
        {
            CopyPlugin(alwaysActivePlugin, pluginsSrc, pluginsDest);
            pluginsToInstall.Remove(alwaysActivePlugin);
            _logger.LogInformation("Always active plugin \"{AlwaysActivePlugin}\" installed", alwaysActivePlugin);
        }

        _logger.LogInformation(
            "All always active plugin installed. " +
            "Plugins left to install: ({Count})[{Plugins}]",
            pluginsToInstall.Count,
            string.Join(",", pluginsToInstall));

        var alreadyInstalledPlugins = Directory
            .GetDirectories(pluginsDest)
            .Select(Path.GetFileName)
            .ToArray();

        foreach (var alreadyInstalledPlugin in alreadyInstalledPlugins)
        {
            var pluginToInstall = pluginsToInstall.FirstOrDefault(p => p.Equals(alreadyInstalledPlugin));
            if (pluginToInstall is null)
            {
                continue;
            }

            CopyPlugin(pluginToInstall, pluginsSrc, pluginsDest);
            pluginsToInstall.Remove(pluginToInstall);
            _logger.LogInformation("Already installed plugin \"{AlreadyInstalledPlugin}\" updated", pluginToInstall);
        }

        var alreadyInstalledDisabledPlugins = Directory
            .GetDirectories(disabledPluginsDest)
            .Select(Path.GetFileName)
            .ToArray();

        foreach (var alreadyInstalledPlugin in alreadyInstalledDisabledPlugins)
        {
            var pluginToInstall = pluginsToInstall.FirstOrDefault(p => p.Equals(alreadyInstalledPlugin));
            if (pluginToInstall is null)
            {
                continue;
            }

            CopyPlugin(pluginToInstall, pluginsSrc, disabledPluginsDest);
            pluginsToInstall.Remove(pluginToInstall);
            _logger.LogInformation("Already installed plugin \"{AlreadyInstalledPlugin}\"(disabled) updated",
                pluginToInstall);
        }

        _logger.LogInformation(
            "Already installed plugins updated. " +
            "Plugins left to install: ({Count})[{Plugins}]",
            pluginsToInstall.Count,
            string.Join(",", pluginsToInstall));

        // Copy remaining plugins as disabled
        foreach (var plugin in pluginsToInstall)
        {
            CopyPlugin(plugin, pluginsSrc, disabledPluginsDest);
            _logger.LogInformation("New plugin installed: \"{NewPlugin}\"", plugin);
        }

        return Result.Success;

        void CopyPlugin(string pluginName, string pluginSrcFolder, string pluginDestFolder)
        {
            var pluginSrcPath = Path.Combine(pluginSrcFolder, pluginName);
            var pluginDestPath = Path.Combine(pluginDestFolder, pluginName);
            FolderHelper.CopyDirectory(pluginSrcPath, pluginDestPath);
        }
    }

    #endregion

    #endregion
}