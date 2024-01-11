using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace PracPlugin;

public class PracPlugin : BasePlugin
{
    public static PracPlugin? Instance { get; private set; }

    public override string ModuleName => Assembly.GetExecutingAssembly().GetName().Name ??
                                         throw new NullReferenceException("Assemblyname");

    public override string ModuleVersion => "1";

    private const string ConfigName = "prac.cfg";

    public override void Load(bool hotReload)
    {
        var configSrcPath = Path.Combine(
            Server.GameDirectory,
            "csgo",
            "addons",
            "counterstrikesharp",
            "plugins",
            nameof(PracPlugin),
            ConfigName);

        if (File.Exists(configSrcPath) == false)
        {
            Console.WriteLine($"Failed to laod {ModuleName}. \"prac.cfg\" not found");
        }

        var configDestPath = Path.Combine(
            Server.GameDirectory,
            "csgo",
            "cfg",
            ConfigName);

        File.Copy(configSrcPath, configDestPath, true);

        Server.ExecuteCommand($"exec {ConfigName}");
        Instance = this;
        base.Load(hotReload);
    }

    [GameEventHandler]
    public HookResult OnPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
    {
        Server.PrintToChatAll($"{@event.Userid.PlayerName} blinded for {@event.BlindDuration} seconds");
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectedFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        // Execute prac config after first player joins
        var playerCount = Utilities.GetPlayers().Count(p => p.IsBot == false);
        if (playerCount == 1)
        {
            Server.ExecuteCommand($"exec {ConfigName}");
        }

        return HookResult.Continue;
    }

    [ConsoleCommand("prac", "Executed the prac.cfg")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnPracCommand(CCSPlayerController? player, CommandInfo command)
    {
        Server.ExecuteCommand($"exec {ConfigName}");
    }

    [ConsoleCommand("bot", "Places standing bot on calling player position")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlaceBotOnPlayerPosition(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            Console.WriteLine("!bot can only be executed by clients");
            return;
        }

        BotManager.AddBot(player);
    }

    [ConsoleCommand("cbot", "Places crouching bot on calling player position")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlaceCrouchingBotOnPlayerPosition(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            Console.WriteLine("!bot can only be executed by clients");
            return;
        }

        BotManager.AddBot(player, true);
    }

    [ConsoleCommand("boost", "Places bot beneath calling player position")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlaceBotBeneathPlayerPosition(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            Console.WriteLine("!bot can only be executed by clients");
            return;
        }

        BotManager.Boost(player);
    }

    [ConsoleCommand("cboost", "Places crouching bot beneath calling player position")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlaceCrouchingBotBeneathPlayerPosition(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            Console.WriteLine("!bot can only be executed by clients");
            return;
        }

        BotManager.Boost(player, true);
    }

    [ConsoleCommand("clear", "Removes all bots spawn by the calling player")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void RemoveAllBotsSpawnedByCallingPlayer(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            Console.WriteLine("!bot can only be executed by clients");
            return;
        }

        BotManager.NoBot(player);
    }
}