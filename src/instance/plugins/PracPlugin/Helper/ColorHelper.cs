using CounterStrikeSharp.API.Modules.Utils;

namespace PracPlugin.Helper;

public static class ColorHelper
{
    public static string PlayerName(string playerName)
    {
        return $"{ChatColors.Blue}{playerName}{ChatColors.Default}";
    }

    public static string Value(string value)
    {
        return $"{ChatColors.Yellow}{value}{ChatColors.Default}";
    }
}