using CounterStrikeSharp.API.Modules.Commands;

namespace CustomCommandsPlugin;

public static class Helper
{
    public static float? ArgToFloat(this CommandInfo commandInfo, int argIndex)
    {
        var argAsString = commandInfo.ArgByIndex(argIndex);
        if (float.TryParse(argAsString, out var result))
        {
            return result;
        }

        return null;
    }
    
    public static int? ArgToInt(this CommandInfo commandInfo, int argIndex)
    {
        var argAsString = commandInfo.ArgByIndex(argIndex);
        if (int.TryParse(argAsString, out var result))
        {
            return result;
        }

        return null;
    }
}