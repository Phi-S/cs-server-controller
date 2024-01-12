using CounterStrikeSharp.API.Modules.Utils;

namespace PracPlugin.Helper;

public static class ColorHelper
{
    public static string PlayerName(string playerName)
    {
        return $"\xA0{ChatColors.Blue}{playerName}{ChatColors.Default}";
    }

    public static string Value(string value)
    {
        return $"\xA0{ChatColors.Yellow}{value}{ChatColors.Default}";
    }
}