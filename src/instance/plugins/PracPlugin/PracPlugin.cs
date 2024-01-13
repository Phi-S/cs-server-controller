using System.Globalization;
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using PracPlugin.Helper;
using PracPlugin.Services;

namespace PracPlugin;

public class TestPluginServiceCollection : IPluginServiceCollection<PracPlugin>
{
    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<BotService>();
        serviceCollection.AddSingleton<TimerService>();
        serviceCollection.AddSingleton<GrenadeService>();
        serviceCollection.AddSingleton<SpawnsService>();
    }
}

public class PracPlugin : BasePlugin
{
    private readonly ILogger<PracPlugin> _logger;
    private readonly BotService _botService;
    private readonly GrenadeService _grenadeService;
    private readonly SpawnsService _spawnsService;
    private readonly TimerService _timerService;

    public PracPlugin(
        ILogger<PracPlugin> logger,
        BotService botService,
        GrenadeService grenadeService,
        SpawnsService spawnsService,
        TimerService timerService)
    {
        _logger = logger;
        _botService = botService;
        _grenadeService = grenadeService;
        _spawnsService = spawnsService;
        _timerService = timerService;
    }

    public override string ModuleName => Assembly.GetExecutingAssembly().GetName().Name ??
                                         throw new NullReferenceException("AssemblyName");

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
        _grenadeService.RegisterEventHandler(this);
        _spawnsService.RegisterEventHandler(this);

        _logger.LogInformation("All event handler registered");
        Server.ExecuteCommand($"exec {ConfigName}");
        base.Load(hotReload);
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

    [ConsoleCommand("reload", "Executed the prac.cfg")]
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

    #region Bots

    [ConsoleCommand("cbot", "Places crouching bot on calling player position")]
    [CommandHelper(minArgs: 0, usage: "[CT/T] (optional)", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlaceCrouchingBotOnPlayerPosition(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            return;
        }

        if (command.ArgCount == 2)
        {
            var botTeam = command.ArgByIndex(1).ToLower();
            if (string.IsNullOrEmpty(botTeam) == false)
            {
                if (botTeam.Equals("t"))
                {
                    _botService.AddBot(player, CsTeam.Terrorist, true);
                }
                else if (botTeam.Equals("ct"))
                {
                    _botService.AddBot(player, CsTeam.CounterTerrorist, true);
                }
                else
                {
                    _logger.LogWarning("Cant add bot to team {BotTeam}", botTeam);
                }

                return;
            }
        }

        _botService.AddBot(player, CsTeam.None, true);
    }

    [ConsoleCommand("boost", "Places bot beneath calling player position")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlaceBotBeneathPlayerPosition(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
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
            return;
        }

        _botService.ClearBots(player);
    }

    #endregion

    #region Spawns

    [ConsoleCommand("spawn", "Teleport to specific spawn")]
    [CommandHelper(minArgs: 1, usage: "[Spawn]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void TeleportToSpawn(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            return;
        }

        if (command.ArgCount != 2)
        {
            return;
        }

        var spawnNumberString = command.ArgByIndex(1);
        if (int.TryParse(spawnNumberString, out var spawnNumber))
        {
            _spawnsService.TeleportToTeamSpawn(player, spawnNumber);
        }
    }

    [ConsoleCommand("tspawn", "Teleport to specific t spawn")]
    [CommandHelper(minArgs: 1, usage: "[Spawn]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void TeleportToTSpawn(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            return;
        }

        if (command.ArgCount != 2)
        {
            return;
        }

        var spawnNumberString = command.ArgByIndex(1);
        if (int.TryParse(spawnNumberString, out var spawnNumber))
        {
            _spawnsService.TeleportToTeamSpawn(player, spawnNumber, CsTeam.Terrorist);
        }
    }

    [ConsoleCommand("ctspawn", "Teleport to specific ct spawn")]
    [CommandHelper(minArgs: 1, usage: "[Spawn]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void TeleportToCtSpawn(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            return;
        }

        if (command.ArgCount != 2)
        {
            return;
        }

        var spawnNumberString = command.ArgByIndex(1);
        if (int.TryParse(spawnNumberString, out var spawnNumber))
        {
            _spawnsService.TeleportToTeamSpawn(player, spawnNumber, CsTeam.CounterTerrorist);
        }
    }

    #endregion

    #region Granades

    [ConsoleCommand("rethrow", "rethrows the last grenade")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void RethrowGrenade(CCSPlayerController? player, CommandInfo command)
    {
        Server.ExecuteCommand("sv_rethrow_last_grenade");
    }

    [ConsoleCommand("last", "Teleports player to his last thrown grenade position")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void TeleportToLastGrenade(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            return;
        }

        if (_grenadeService.TryGetLastThrownGrenade(player, out var lastThrownGrenadePosition))
        {
            player.PlayerPawn.Value?.Teleport(
                lastThrownGrenadePosition.Position,
                lastThrownGrenadePosition.Angle,
                new Vector(0, 0, 0)
            );
        }
    }

    [ConsoleCommand("ff", "Clears all grenades")]
    public void ClearAllGrenades(CCSPlayerController? player, CommandInfo command)
    {
        Server.ExecuteCommand("host_timescale 50");
        _timerService.AddTimer(20f, () => Server.ExecuteCommand("host_timescale 1"));
    }

    #endregion
}