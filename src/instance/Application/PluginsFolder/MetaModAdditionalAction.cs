using ErrorOr;
using Shared;

namespace Application.PluginsFolder;

public static class MetaModAdditionalAction
{
    private const string MetamodEntry = "			Game csgo/addons/metamod";
    
    public static async Task<ErrorOr<Success>> AddMetamodEntryToGameinfoGi(string csgoFolder)
    {
        var gameinfoGiPath = Path.Combine(csgoFolder, "gameinfo.gi");
        if (File.Exists(gameinfoGiPath) == false)
        {
            return Errors.Fail($"Failed to find gameinfo.gi at \"{gameinfoGiPath}\"");
        }

        var fileLines = await File.ReadAllLinesAsync(gameinfoGiPath);
        if (fileLines.Any(l => l.Trim().Equals(MetamodEntry.Trim())))
        {
            return Result.Success;
        }

        await using var streamWriter = new StreamWriter(gameinfoGiPath);
        foreach (var fileLine in fileLines)
        {
            await streamWriter.WriteLineAsync(fileLine);
            if (fileLine.Trim().Equals("Game_LowViolence\tcsgo_lv // Perfect World content override"))
            {
                await streamWriter.WriteLineAsync(MetamodEntry);
            }
        }

        await streamWriter.FlushAsync();
        streamWriter.Close();

        return Result.Success;
    }

    private static async Task<ErrorOr<Success>> RemoveMetamodEntryFromGameinfoGi(string csgoFolder)
    {
        var gameinfoGiPath = Path.Combine(csgoFolder, "gameinfo.gi");
        if (File.Exists(gameinfoGiPath) == false)
        {
            return Errors.Fail($"Failed to find gameinfo.gi at \"{gameinfoGiPath}\"");
        }

        var fileLines = await File.ReadAllLinesAsync(gameinfoGiPath);
        if (fileLines.Any(l => l.Trim().Equals(MetamodEntry.Trim())) == false)
        {
            return Result.Success;
        }

        await using var streamWriter = new StreamWriter(gameinfoGiPath);
        foreach (var fileLine in fileLines)
        {
            if (fileLine.Trim().Equals(MetamodEntry.ToLower().Trim()))
            {
                continue;
            }

            await streamWriter.WriteLineAsync(fileLine);
        }

        await streamWriter.FlushAsync();
        streamWriter.Close();

        return Result.Success;
    }
}