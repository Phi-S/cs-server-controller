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

namespace Application.CounterStrikeSharpUpdateOrInstallFolder;

public class CounterStrikeSharpUpdateOrInstallService
{
    private readonly ILogger<CounterStrikeSharpUpdateOrInstallService> _logger;
    private readonly IOptions<AppOptions> _options;
    private readonly HttpClient _httpClient;
    private readonly StatusService _statusService;
    private readonly EventService _eventService;
    private readonly SystemLogService _systemLogService;

    public CounterStrikeSharpUpdateOrInstallService(
        ILogger<CounterStrikeSharpUpdateOrInstallService> logger,
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

    private string CsgoFolder => Path.Combine(
        _options.Value.SERVER_FOLDER,
        "game",
        "csgo");

    private ErrorOr<Success> IsServerReadyToUpdateOrInstall()
    {
        if (_statusService.ServerPluginsUpdatingOrInstalling)
        {
            return Errors.Fail("Another update or install process is already in progress");
        }

        if (_statusService.ServerStarted)
        {
            return Errors.Fail("Server is running");
        }

        if (_statusService.ServerStarting)
        {
            return Errors.Fail("Server is starting");
        }

        if (_statusService.ServerStopping)
        {
            return Errors.Fail("Server is stopping");
        }

        if (_statusService.ServerUpdatingOrInstalling)
        {
            return Errors.Fail("Server update or install is in progress");
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> StartUpdateOrInstall()
    {
        try
        {
            var shouldUpdateOrInstall = ServerHelper.IsServerPluginBaseInstalled(_options.Value.SERVER_FOLDER);
            var updateOrInstallString = shouldUpdateOrInstall
                ? "update"
                : "install";

            var isServerReadyToUpdateOrInstall = IsServerReadyToUpdateOrInstall();
            if (isServerReadyToUpdateOrInstall.IsError)
            {
                return Errors.Fail($"Server is not ready to {updateOrInstallString} Metamod and CounterStrikeSharp. " +
                                   $"{isServerReadyToUpdateOrInstall.ErrorMessage()}");
            }

            var updateOrInstall = await UpdateOrInstall(shouldUpdateOrInstall);
            if (updateOrInstall.IsError)
            {
                _eventService.OnPluginUpdateOrInstallFailed();
                _logger.LogError("Metamod and CounterStrikeSharp {UpdateOrInstall} failed. {Error}",
                    updateOrInstallString, updateOrInstall.ErrorMessage());
                _systemLogService.Log(
                    $"Metamod and CounterStrikeSharp {updateOrInstallString} failed. {updateOrInstall.ErrorMessage()}");
            }

            return updateOrInstall;
        }
        catch (Exception e)
        {
            _eventService.OnPluginUpdateOrInstallFailed();
            _logger.LogError(e, $"Metamod and CounterStrikeSharp update or install failed with exception");
            _systemLogService.Log($"Metamod and CounterStrikeSharp update or install failed");
            throw;
        }
    }

    private async Task<ErrorOr<Success>> UpdateOrInstall(bool shouldUpdateOrInstall)
    {
        var updateOrInstallString = shouldUpdateOrInstall ? "update" : "install";
        var updatingOrInstallingString = shouldUpdateOrInstall ? "Updating" : "Installing";

        _eventService.OnPluginUpdateOrInstallStarted();
        _logger.LogInformation("Starting to {UpdateOrInstall} Metamod and CounterStrikeSharp",
            updateOrInstallString);
        _systemLogService.LogHeader();
        _systemLogService.Log($"Starting to {updateOrInstallString} Metamod and CounterStrikeSharp");

        #region CreateAddonsFolder

        var addonsFolder = Path.Combine(CsgoFolder, "addons");
        Directory.CreateDirectory(addonsFolder);

        #endregion

        #region Metamod

        _logger.LogInformation("{UpdatingOrInstalling} Metamod", updatingOrInstallingString);
        _systemLogService.Log($"{updatingOrInstallingString} Metamod");
        var installMetamod = await InstallMetamod(_httpClient, CsgoFolder);
        if (installMetamod.IsError)
        {
            return Errors.Fail($"Metamod {updateOrInstallString} failed. {installMetamod.ErrorMessage()}");
        }

        _logger.LogInformation("Metamod {UpdateOrInstall} successful", updateOrInstallString);
        _systemLogService.Log($"Metamod {updateOrInstallString} successful");

        #endregion

        #region CounterStrikeSharp

        _logger.LogInformation("{UpdatingOrInstalling} CounterStrikeSharp", updatingOrInstallingString);
        _systemLogService.Log($"{updatingOrInstallingString} CounterStrikeSharp");
        var installCounterStrikeSharp = await InstallCounterStrikeSharp(_httpClient, CsgoFolder);
        if (installCounterStrikeSharp.IsError)
        {
            return Errors.Fail(
                $"Failed to {updateOrInstallString} CounterStrikeSharp. {installCounterStrikeSharp.ErrorMessage()}");
        }

        _logger.LogInformation("CounterStrikeSharp {UpdateOrInstall} successful", updateOrInstallString);
        _systemLogService.Log($"CounterStrikeSharp {updateOrInstallString} successful");

        #endregion

        #region Gameinfo.gi

        _logger.LogInformation("Adding Metamod entry to Gameinfo.gi");
        _systemLogService.Log("Adding Metamod entry to Gameinfo.gi");
        var addMetamodEntry = await AddMetamodEntryToGameinfoGi(CsgoFolder);
        if (addMetamodEntry.IsError)
        {
            return Errors.Fail($"Failed to add metamod entry to gameinfo.gi. {addMetamodEntry.ErrorMessage()}");
        }

        #endregion

        var configsFolder = Path.Combine(addonsFolder, "counterstrikesharp", "configs");
        CreateCoreCfg(configsFolder);

        _eventService.OnPluginUpdateOrInstallDone();
        _logger.LogInformation("Metamod and CounterStrikeSharp {UpdateOrInstall} completed", updateOrInstallString);
        _systemLogService.Log($"Metamod and CounterStrikeSharp {updateOrInstallString} completed");
        return Result.Success;
    }

    #region Base

    public static async Task<ErrorOr<Success>> InstallMetamod(HttpClient httpClient, string csgoFolder)
    {
        var downloadTempFolder = FolderHelper.CreateNewTempFolder(csgoFolder);
        const string metamodUrl = "https://mms.alliedmods.net/mmsdrop/2.0/mmsource-2.0.0-git1282-linux.tar.gz";
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
            "https://github.com/roflmuffin/CounterStrikeSharp/releases/download/v164/counterstrikesharp-with-runtime-build-164-linux-8967c40.zip";
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
                "PublicChatTrigger": [ ".", "!" ],
                "SilentChatTrigger": [ "/" ],
                "FollowCS2ServerGuidelines": true,
                "PluginHotReloadEnabled": true,
                "ServerLanguage": "en"
            }
            """;

        File.WriteAllText(coreCfgJsonPath, json);
    }

    #endregion
}