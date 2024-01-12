using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PracPlugin.Services;

namespace PracPlugin;

public class TestPluginServiceCollection : IPluginServiceCollection<PracPlugin>
{
    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<BotService>();
        serviceCollection.AddSingleton<TimerService>();
    }
}

public class PracPlugin : BasePlugin
{
    private readonly ILogger<PracPlugin> _logger;

    public PracPlugin(ILogger<PracPlugin> logger, BotService botService)
    {
        _logger = logger;
        _botService = botService;
    }

    public override string ModuleName => Assembly.GetExecutingAssembly().GetName().Name ??
                                         throw new NullReferenceException("AssemblyName");

    public override string ModuleVersion => "1";

    private const string ConfigName = "prac.cfg";

    private readonly BotService _botService;

    public override void Load(bool hotReload)
    {
        var configSrcPath = Path.Combine(
            Server.GameDirectory,
            "csgo",
            "addons",
            "counterstrikesharp",
            "plugins",
            ModuleName,
            "Configs",
            ConfigName);

        if (File.Exists(configSrcPath) == false)
        {
            Console.WriteLine($"Failed to load {ModuleName}. \"prac.cfg\" not found");
            return;
        }

        var configDestPath = Path.Combine(
            Server.GameDirectory,
            "csgo",
            "cfg",
            ConfigName);

        File.Copy(configSrcPath, configDestPath, true);
        _botService.RegisterEventHandler(this);

        Server.ExecuteCommand($"exec {ConfigName}");
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
    [CommandHelper(minArgs: 0, usage: "[CT/T] (optional)", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlaceBotOnPlayerPosition(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            _logger.LogWarning("bot command can only be executed by clients");
            return;
        }

        if (command.ArgCount == 2)
        {
            var botTeam = command.ArgByIndex(1).ToLower();
            if (string.IsNullOrEmpty(botTeam) == false)
            {
                if (botTeam.Equals("t"))
                {
                    _botService.AddBot(player, CsTeam.Terrorist);
                }
                else if (botTeam.Equals("ct"))
                {
                    _botService.AddBot(player, CsTeam.CounterTerrorist);
                }
                else
                {
                    _logger.LogWarning("Cant add bot to team {BotTeam}", botTeam);
                }

                return;
            }
        }

        _botService.AddBot(player);
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

        _botService.AddBot(player, CsTeam.None, true);
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

        _botService.Boost(player);
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

        _botService.Boost(player, true);
    }

    [ConsoleCommand("nobot", "Removes the closest bot from calling player")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void RemoveClosestBotSpawnedByCallingPlayer(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            Console.WriteLine("!bot can only be executed by clients");
            return;
        }

        _botService.NoBot(player);
    }

    [ConsoleCommand("nobots", "Removes all bots spawn by the calling player")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void RemoveAllBotsSpawnedByCallingPlayer(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            Console.WriteLine("!bot can only be executed by clients");
            return;
        }

        _botService.ClearBots(player);
    }
}