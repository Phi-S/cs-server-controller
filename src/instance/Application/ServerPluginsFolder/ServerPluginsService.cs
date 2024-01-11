using System.Collections.Immutable;
using System.Formats.Tar;
using System.IO.Compression;
using Domain;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace Application.ServerPluginsFolder;

using PluginAlias = (string plugin, bool disabled);

public class ServerPluginsService
{
    private readonly ILogger<ServerPluginsService> _logger;
    private readonly IOptions<AppOptions> _options;
    private readonly HttpClient _httpClient;

    private static readonly IImmutableList<string> AlwaysActivePlugins = ["EnableDisablePlugin"];
    private const string MetamodUrl = "https://mms.alliedmods.net/mmsdrop/2.0/mmsource-2.0.0-git1278-linux.tar.gz";

    private const string CounterStrikeSharpUrl =
        "https://github.com/roflmuffin/CounterStrikeSharp/releases/download/v142/counterstrikesharp-with-runtime-build-142-linux-7b45a88.zip";

    private string PluginsFolder => Path.Combine(
        _options.Value.SERVER_FOLDER,
        "game",
        "csgo",
        "addons",
        "counterstrikesharp",
        "plugins");

    public ServerPluginsService(
        ILogger<ServerPluginsService> logger,
        IOptions<AppOptions> options,
        HttpClient httpClient)
    {
        _logger = logger;
        _options = options;
        _httpClient = httpClient;
    }

    #region Install

    public async Task<ErrorOr<Success>> Install()
    {
        var installBaseResult = await InstallBase();
        if (installBaseResult.IsError)
        {
            return installBaseResult.FirstError;
        }

        var installPluginsResult = InstallPlugins();
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

    public ErrorOr<Success> InstallPlugins()
    {
        var pluginsSrc = Path.Combine(_options.Value.EXECUTING_FOLDER, "ServerPluginsFolder", "plugins");
        var pluginsDest = Path.Combine(_options.Value.SERVER_FOLDER, "game", "csgo", "addons", "counterstrikesharp",
            "plugins");
        var disabledPluginsDest = Path.Combine(pluginsDest, "disabled");

        if (Directory.Exists(pluginsDest))
        {
            Directory.Delete(pluginsDest, true);
        }

        Directory.CreateDirectory(pluginsDest);
        Directory.CreateDirectory(disabledPluginsDest);

        var allAvailablePlugins = Directory.GetDirectories(pluginsSrc).Select(Path.GetFileName).ToArray();

        // Copy always active plugins
        foreach (var alwaysActivePlugin in AlwaysActivePlugins)
        {
            if (allAvailablePlugins.Contains(alwaysActivePlugin) == false)
            {
                return Errors.Fail($"Always active plugin \"{alwaysActivePlugin}\" not found at \"{pluginsSrc}\"");
            }
        }

        foreach (var alwaysActivePlugin in AlwaysActivePlugins)
        {
            var alwaysActivePluginSrcPath = Path.Combine(pluginsSrc, alwaysActivePlugin);
            var alwaysActivePluginDestPath = Path.Combine(pluginsDest, alwaysActivePlugin);
            CopyDirectory(alwaysActivePluginSrcPath, alwaysActivePluginDestPath);
        }

        // Copy remaining plugins as disabled
        foreach (var plugin in allAvailablePlugins)
        {
            if (string.IsNullOrWhiteSpace(plugin))
            {
                continue;
            }

            // Skip already copied (Always active plugins)
            if (AlwaysActivePlugins.Contains(plugin))
            {
                continue;
            }

            var pluginSrcPath = Path.Combine(pluginsSrc, plugin);
            var pluginDestPath = Path.Combine(disabledPluginsDest, plugin);
            CopyDirectory(pluginSrcPath, pluginDestPath);
        }

        return Result.Success;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        var dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // Copy subDirectories
        foreach (var subDir in dirs)
        {
            var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }

    #endregion

    public List<PluginAlias> GetInstalledPlugins()
    {
        if (Directory.Exists(PluginsFolder) == false)
        {
            return [];
        }

        var result = new List<PluginAlias>();

        var pluginFolders = Directory.GetDirectories(PluginsFolder);
        foreach (var pluginFolder in pluginFolders)
        {
            var pluginName = Path.GetFileName(PluginsFolder);
            if (pluginName.Equals("disabled"))
            {
                continue;
            }

            result.Add((pluginName, false));
        }


        var disabledPluginFolder = Path.Combine(PluginsFolder, "disabled");
        var disabledPluginsFolders = Directory.GetDirectories(disabledPluginFolder);
        foreach (var pluginFolder in disabledPluginsFolders)
        {
            var pluginName = Path.GetFileName(pluginFolder);
            result.Add((pluginName, true));
        }

        return result;
    }
}