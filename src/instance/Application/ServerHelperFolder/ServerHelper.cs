using ByteSizeLib;

namespace Application.ServerHelperFolder;

public static class ServerHelper
{
    private static long GetDirectorySize(DirectoryInfo directory)
    {
        // Add file sizes.
        var fis = directory.GetFiles();
        var size = fis.Sum(fi => fi.Length);
        // Add subdirectory sizes.
        var dis = directory.GetDirectories();
        size += dis.Sum(GetDirectorySize);

        return size;
    }

    public static bool IsServerInstalled(string serverFolder, int shouldBeFolderSizeInGiB = 30)
    {
        var directory = new DirectoryInfo(serverFolder);
        if (directory.Exists == false)
        {
            return false;
        }

        var size = GetDirectorySize(directory);
        var bytes = ByteSize.FromBytes(size);
        if (bytes.GibiBytes < shouldBeFolderSizeInGiB)
        {
            return false;
        }

        var csgoFolderPath = Path.Combine(serverFolder, "game", "csgo");
        if (Directory.Exists(csgoFolderPath) == false)
        {
            return false;
        }

        #region paks

        var pak01000Path = Path.Combine(csgoFolderPath, "pak01_000.vpk");
        if (File.Exists(pak01000Path) == false)
        {
            return false;
        }

        var pak01001Path = Path.Combine(csgoFolderPath, "pak01_001.vpk");
        if (File.Exists(pak01001Path) == false)
        {
            return false;
        }

        var pak01002Path = Path.Combine(csgoFolderPath, "pak01_002.vpk");
        if (File.Exists(pak01002Path) == false)
        {
            return false;
        }

        var pak01050Path = Path.Combine(csgoFolderPath, "pak01_050.vpk");
        if (File.Exists(pak01050Path) == false)
        {
            return false;
        }

        #endregion

        #region cfgs

        var cfgFolderPath = Path.Combine(csgoFolderPath, "cfg");
        if (Directory.Exists(cfgFolderPath) == false)
        {
            return false;
        }

        var serverCfgPath = Path.Combine(cfgFolderPath, "server.cfg");
        if (File.Exists(serverCfgPath) == false)
        {
            return false;
        }

        var gamemodeCompetitiveCfgPath = Path.Combine(cfgFolderPath, "gamemode_competitive.cfg");
        if (File.Exists(gamemodeCompetitiveCfgPath) == false)
        {
            return false;
        }

        var gamemodeDeathmatchCfgPath = Path.Combine(cfgFolderPath, "gamemode_deathmatch.cfg");
        if (File.Exists(gamemodeDeathmatchCfgPath) == false)
        {
            return false;
        }

        #endregion

        return true;
    }
}