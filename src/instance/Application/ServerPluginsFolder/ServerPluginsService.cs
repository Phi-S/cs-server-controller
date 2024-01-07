using System.Formats.Tar;
using System.IO.Compression;
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

    private string CsgoFolder => Path.Combine(_options.Value.SERVER_FOLDER, "game", "csgo");
    private string AddonsFolder => Path.Combine(CsgoFolder, "addons");
    private const string MetamodUrl = "https://mms.alliedmods.net/mmsdrop/2.0/mmsource-2.0.0-git1278-linux.tar.gz";

    private const string CounterStrikeSharpUrl =
        "https://github.com/roflmuffin/CounterStrikeSharp/releases/download/v142/counterstrikesharp-with-runtime-build-142-linux-7b45a88.zip";

    public ServerPluginsService(ILogger<ServerPluginsService> logger, IOptions<AppOptions> options,
        HttpClient httpClient)
    {
        _logger = logger;
        _options = options;
        _httpClient = httpClient;
    }

    public async Task<ErrorOr<Success>> InstallBase()
    {
        _logger.LogInformation("Installing server plugins");
        if (Directory.Exists(AddonsFolder))
        {
            Directory.Delete(AddonsFolder, true);
        }

        Directory.CreateDirectory(AddonsFolder);

        var downloadMetamod = await DownloadMetamod();
        if (downloadMetamod.IsError)
        {
            _logger.LogError("Failed to install metamod. {Error}", downloadMetamod.ErrorMessage());
            return downloadMetamod.FirstError;
        }

        var downloadCounterStrikeSharp = await DownloadCounterStrikeSharp();
        if (downloadCounterStrikeSharp.IsError)
        {
            _logger.LogError("Failed to install CounterStrikeSharp. {Error}",
                downloadCounterStrikeSharp.ErrorMessage());
            return downloadCounterStrikeSharp.FirstError;
        }

        const string metamodEntry = "			Game csgo/addons/metamod";
        var gameinfoGiPath = Path.Combine(CsgoFolder, "gameinfo.gi");
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
        _logger.LogInformation("Server plugins installed");
        return Result.Success;
    }

    private async Task<ErrorOr<Success>> DownloadMetamod()
    {
        var downloadPath = Path.Combine(AddonsFolder, "metamod.tar.gz");
        var downLoadResult = await Download(MetamodUrl, downloadPath);
        if (downLoadResult.IsError)
        {
            return downLoadResult.FirstError;
        }

        var extractedFolderPath = Path.Combine(CsgoFolder);
        Directory.CreateDirectory(extractedFolderPath);

        await using var gzip = new GZipStream(File.OpenRead(downloadPath), CompressionMode.Decompress);
        using var unzippedStream = new MemoryStream();
        await gzip.CopyToAsync(unzippedStream);
        unzippedStream.Seek(0, SeekOrigin.Begin);

        await using var reader = new TarReader(unzippedStream);
        while (await reader.GetNextEntryAsync() is { } entry)
        {
            var entryPath = Path.Combine(
                extractedFolderPath,
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

        return Result.Success;
    }

    private async Task<ErrorOr<Success>> DownloadCounterStrikeSharp()
    {
        var downloadPath = Path.Combine(AddonsFolder, "counterstrikesharp-with-runtime.zip");
        var downLoadResult = await Download(CounterStrikeSharpUrl, Path.Combine(downloadPath));
        if (downLoadResult.IsError)
        {
            return downLoadResult.FirstError;
        }

        var extractedFolderPath = Path.Combine(CsgoFolder);
        Directory.CreateDirectory(extractedFolderPath);

        ZipFile.ExtractToDirectory(File.OpenRead(downloadPath), extractedFolderPath);
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
}