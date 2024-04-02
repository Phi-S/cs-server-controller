using System.Formats.Tar;
using System.IO.Compression;
using Application.EventServiceFolder;
using Domain;
using ErrorOr;
using Microsoft.Extensions.Options;
using Shared;
using Shared.ApiModels;

namespace Application.PluginsFolder;

public class PluginInstallerService
{
    private readonly IOptions<AppOptions> _options;
    private readonly EventService _eventService;
    private readonly InstalledPluginsService _installedPluginsService;
    private readonly HttpClient _httpClient;

    private readonly List<PluginModel> _plugins;
    private readonly SemaphoreSlim _lock = new(1);

    public PluginInstallerService(IOptions<AppOptions> options,
        EventService eventService,
        InstalledPluginsService installedPluginsService,
        HttpClient httpClient)
    {
        _options = options;
        _eventService = eventService;
        _installedPluginsService = installedPluginsService;
        _httpClient = httpClient;

        var metamod =
            new PluginModel("Metamod:Source", "https://www.sourcemm.net",
            [
                new PluginVersion(
                    "2.0.0-git1282",
                    "https://mms.alliedmods.net/mmsdrop/2.0/mmsource-2.0.0-git1282-linux.tar.gz",
                    "",
                    null,
                    async () => await MetaModAdditionalAction.AddMetamodEntryToGameinfoGi(_options.Value.CSGO_FOLDER))
            ]);

        var counterStrikeSharp =
            new PluginModel("CounterStrikeSharp", "https://github.com/roflmuffin/CounterStrikeSharp",
                [
                    new PluginVersion("v202",
                        "https://github.com/roflmuffin/CounterStrikeSharp/releases/download/v202/counterstrikesharp-with-runtime-build-202-linux-696ecad.zip",
                        "",
                        [new PluginDependency(metamod, "2.0.0-git1282")]
                    ),
                    new PluginVersion("v203",
                        "https://github.com/roflmuffin/CounterStrikeSharp/releases/download/v203/counterstrikesharp-with-runtime-build-203-linux-211516c.zip",
                        "",
                        [new PluginDependency(metamod, "2.0.0-git1282")])
                ]
            );

        var cs2PracticeMode = new PluginModel("Cs2PracticeMode", "https://github.com/Phi-S/cs2-practice-mode",
            [
                new PluginVersion("0.0.1",
                    "https://github.com/Phi-S/cs2-practice-mode/releases/download/0.0.1/cs2-practice-mode-linux-0.0.1.tar.gz",
                    "/addons/counterstrikesharp/plugins",
                    [new PluginDependency(counterStrikeSharp, "v202")]
                ),
                new PluginVersion("0.0.2",
                    "https://github.com/Phi-S/cs2-practice-mode/releases/download/0.0.2/cs2-practice-mode-linux-0.0.2.tar.gz",
                    "/addons/counterstrikesharp/plugins",
                    [new PluginDependency(counterStrikeSharp, "v202")])
            ]
        );

        _plugins = [metamod, counterStrikeSharp, cs2PracticeMode];
    }

    public async Task<ErrorOr<List<PluginsResponseModel>>> GetPlugins()
    {
        var result = new List<PluginsResponseModel>();

        var installedPlugins = await _installedPluginsService.GetAll();
        if (installedPlugins.IsError)
        {
            return installedPlugins.Errors;
        }

        foreach (var plugin in _plugins)
        {
            var versions = new List<string>();
            string? installedVersion = null;
            foreach (var pluginVersion in plugin.Versions)
            {
                versions.Add(pluginVersion.Version);
                var isInstalled = await _installedPluginsService.IsInstalled(plugin.Name, pluginVersion.Version);
                if (isInstalled.IsError)
                {
                    return isInstalled.Errors;
                }

                if (isInstalled.Value)
                {
                    installedVersion = pluginVersion.Version;
                }
            }

            var pluginResponseModel =
                new PluginsResponseModel(
                    plugin.Name,
                    plugin.Url,
                    versions.ToArray(),
                    installedVersion);
            result.Add(pluginResponseModel);
        }

        return result;
    }

    public async Task<ErrorOr<Success>> UpdateOrInstall(string pluginName, string versionToInstallOrUpdate)
    {
        var plugin = _plugins.FirstOrDefault(p => p.Name.ToLower().Trim() == pluginName.ToLower().Trim());
        if (plugin is null)
        {
            return Errors.Fail($"Plugin \"{pluginName}\" not found");
        }

        return await UpdateOrInstall(plugin, versionToInstallOrUpdate);
    }

    private async Task<ErrorOr<Success>> UpdateOrInstall(PluginModel pluginModel, string versionToInstallOrUpdate)
    {
        if (string.IsNullOrWhiteSpace(versionToInstallOrUpdate))
        {
            return Errors.Fail("No version specified");
        }

        var isInstalled = await _installedPluginsService.IsInstalled(pluginModel.Name, versionToInstallOrUpdate);
        if (isInstalled.IsError)
        {
            return isInstalled.Errors;
        }

        if (isInstalled.Value)
        {
            return Result.Success;
        }


        var version = pluginModel.Versions.FirstOrDefault(v =>
            v.Version.Equals(versionToInstallOrUpdate, StringComparison.InvariantCultureIgnoreCase));

        if (version is null)
        {
            return Errors.Fail($"Version {versionToInstallOrUpdate} is not available");
        }

        // Install all dependencies
        if (version.PluginDependencies is not null)
        {
            foreach (var pluginDependency in version.PluginDependencies)
            {
                var updateOrInstallDependency =
                    await UpdateOrInstall(pluginDependency.Plugin, pluginDependency.Version);
                if (updateOrInstallDependency.IsError)
                {
                    return updateOrInstallDependency.Errors;
                }
            }
        }

        try
        {
            await _lock.WaitAsync();
            _eventService.OnPluginUpdateOrInstallStarted();
            var tempFolder = Directory.CreateTempSubdirectory();

            #region Download

            var downloadFolder = Path.Combine(tempFolder.FullName, "download");
            Directory.CreateDirectory(downloadFolder);

            var download = await DownloadUrl(version.DownloadUrl, downloadFolder);
            if (download.IsError)
            {
                _eventService.OnPluginUpdateOrInstallFailed();
                return download.Errors;
            }

            var downloadedPath = download.Value;

            #endregion

            #region Extract

            var extractedFolder = Path.Combine(tempFolder.FullName, "extracted");
            Directory.CreateDirectory(extractedFolder);

            ErrorOr<Success> extract;
            if (downloadedPath.EndsWith(".zip"))
            {
                extract = await ExtractZip(downloadedPath, extractedFolder);
            }
            else if (downloadedPath.EndsWith(".tar.gz"))
            {
                extract = await ExtractTarGz(downloadedPath, extractedFolder);
            }
            else
            {
                _eventService.OnPluginUpdateOrInstallFailed();
                return Errors.Fail("Only zip and tar.gz files are supported");
            }

            if (extract.IsError)
            {
                _eventService.OnPluginUpdateOrInstallFailed();
                return extract.Errors;
            }

            #endregion

            #region Copy

            var destinationFolder = version.DestinationFolder
                .Replace("/", Path.DirectorySeparatorChar.ToString())
                .Replace("\\", Path.DirectorySeparatorChar.ToString());

            if (destinationFolder.StartsWith(Path.DirectorySeparatorChar))
            {
                destinationFolder = destinationFolder[1..];
            }

            destinationFolder = Path.Combine(_options.Value.CSGO_FOLDER, destinationFolder);

            var copyDirectory = CopyDirectory(extractedFolder, destinationFolder);
            if (copyDirectory.IsError)
            {
                _eventService.OnPluginUpdateOrInstallFailed();
                return copyDirectory.Errors;
            }

            #endregion

            if (version.AdditionalAction is not null)
            {
                var additionalAction = await version.AdditionalAction.Invoke();
                if (additionalAction.IsError)
                {
                    _eventService.OnPluginUpdateOrInstallFailed();
                    return additionalAction.FirstError;
                }
            }

            var addInstalledPluginsEntry =
                await _installedPluginsService.UpdateOrInstall(pluginModel.Name, versionToInstallOrUpdate);
            if (addInstalledPluginsEntry.IsError)
            {
                _eventService.OnPluginUpdateOrInstallFailed();
                return addInstalledPluginsEntry.Errors;
            }

            _eventService.OnPluginUpdateOrInstallDone();
            return Result.Success;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<ErrorOr<string>> DownloadUrl(string downloadUrl, string destinationFolder)
    {
        if (string.IsNullOrWhiteSpace(destinationFolder) || Directory.Exists(destinationFolder) == false)
        {
            return Errors.Fail($"Destination folder \"{destinationFolder}\" is not valid");
        }

        var uri = new Uri(downloadUrl);
        var filename = Path.GetFileName(uri.LocalPath);
        var destinationPath = Path.Combine(destinationFolder, filename);

        await using var downloadStream = await _httpClient.GetStreamAsync(downloadUrl);
        await using var fileStream = new FileStream(destinationPath, FileMode.OpenOrCreate);
        await downloadStream.CopyToAsync(fileStream);

        if (File.Exists(destinationPath))
        {
            return destinationPath;
        }

        return Errors.Fail($"Failed to download \"{downloadUrl}\"");
    }

    private static async Task<ErrorOr<Success>> ExtractZip(string srcPath, string destFolder)
    {
        await using var fileStream = File.OpenRead(srcPath);
        ZipFile.ExtractToDirectory(fileStream, destFolder);
        return Result.Success;
    }

    private static async Task<ErrorOr<Success>> ExtractTarGz(string srcPath, string destFolder)
    {
        await using (var gzip = new GZipStream(File.OpenRead(srcPath), CompressionMode.Decompress))
        {
            using var unzippedStream = new MemoryStream();
            await gzip.CopyToAsync(unzippedStream);
            unzippedStream.Seek(0, SeekOrigin.Begin);

            await using var reader = new TarReader(unzippedStream);
            while (await reader.GetNextEntryAsync() is { } entry)
            {
                var entryPath = Path.Combine(
                    destFolder,
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

        return Result.Success;
    }

    private static ErrorOr<Success> CopyDirectory(string sourceDir, string destinationDir, bool overwrite = true)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (dir.Exists == false)
        {
            return Errors.Fail($"Source directory not found: {dir.FullName}");
        }

        Directory.CreateDirectory(destinationDir);

        var dirs = dir.GetDirectories();
        var files = dir.GetFiles();

        foreach (var file in files)
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, overwrite);
        }

        foreach (var subDir in dirs)
        {
            var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }

        return Result.Success;
    }
}