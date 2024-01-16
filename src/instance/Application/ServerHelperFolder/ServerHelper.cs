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
    
    public static bool IsServerPluginBaseInstalled(string serverFolder)
    {
        var csgoFolder = Path.Combine(
            serverFolder,
            "game",
            "csgo");
        
        var addonsFolder = Path.Combine(csgoFolder, "addons");
        if (Directory.Exists(addonsFolder) == false)
        {
            return false;
        }

        var metamodVdfFile = Path.Combine(addonsFolder, "metamod.vdf");
        if (File.Exists(metamodVdfFile) == false)
        {
            return false;
        }

        var metamodVdfx64File = Path.Combine(addonsFolder, "metamod_x64.vdf");
        if (File.Exists(metamodVdfx64File) == false)
        {
            return false;
        }

        var metamodFolder = Path.Combine(addonsFolder, "metamod");
        if (Directory.Exists(metamodFolder) == false)
        {
            return false;
        }

        var counterstrikesharpFolder = Path.Combine(addonsFolder, "counterstrikesharp");
        if (Directory.Exists(counterstrikesharpFolder) == false)
        {
            return false;
        }

        var counterstrikesharpDllFile = Path.Combine(counterstrikesharpFolder, "api", "CounterStrikeSharp.API.dll");
        if (File.Exists(counterstrikesharpDllFile) == false)
        {
            return false;
        }

        var counterstrikesharpSoFile =
            Path.Combine(counterstrikesharpFolder, "bin", "linuxsteamrt64", "counterstrikesharp.so");
        if (File.Exists(counterstrikesharpSoFile) == false)
        {
            return false;
        }
        
        var gameinfoGiPath = Path.Combine(csgoFolder, "gameinfo.gi");
        var fileLines = File.ReadAllLines(gameinfoGiPath);
        if (fileLines.Any(l => l.Trim().Equals("Game csgo/addons/metamod")) == false)
        {
            return false;
        }

        return true;
    }
    
    
}