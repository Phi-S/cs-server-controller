using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using PracPlugin.Models;

namespace PracPlugin.Services;

public class SpawnsService : IBaseService
{
    private readonly ILogger<SpawnsService> _logger;

    public SpawnsService(ILogger<SpawnsService> logger)
    {
        _logger = logger;
    }

    public void Register(BasePlugin plugin)
    {
        plugin.RegisterEventHandler<EventMapTransition>(OnMapChange);
        _logger.LogInformation("SpawnsService events registered");

        plugin.AddCommand("spawn", "Teleport to specific spawn", CommandHandlerSpawn);
        _logger.LogInformation("SpawnsService commands registered");
    }

    #region Commands

    private void CommandHandlerSpawn(CCSPlayerController? player, CommandInfo commandinfo)
    {
        if (player is null)
        {
            return;
        }

        // spawn 1
        if (commandinfo.ArgCount == 2)
        {
            var spawnNumberString = commandinfo.ArgByIndex(1);
            if (int.TryParse(spawnNumberString, out var spawnNumber) == false)
            {
                PrintError();
                return;
            }

            TeleportToTeamSpawn(player, spawnNumber);
        }
        // spawn t/ct 1
        else if (commandinfo.ArgCount == 3)
        {
            var teamString = commandinfo.ArgByIndex(1);
            var spawnNumberString = commandinfo.ArgByIndex(2);
            if (int.TryParse(spawnNumberString, out var spawnNumber) == false)
            {
                PrintError();
                return;
            }

            if (teamString.ToLower().Trim().Equals("t"))
            {
                TeleportToTeamSpawn(player, spawnNumber, CsTeam.Terrorist);
            }
            else if (teamString.ToLower().Trim().Equals("ct"))
            {
                TeleportToTeamSpawn(player, spawnNumber, CsTeam.CounterTerrorist);
            }
            else
            {
                PrintError();
                return;
            }
        }
        else
        {
            PrintError();
            return;
        }

        return;

        void PrintError()
        {
            player.PrintToCenter(
                "Failed to teleport to spawn. Valid command format ([.spawn] [t/ct](optional) [spawn number]). Example: \".spawn t 1\", \".spawn 1\"");
        }
    }

    #endregion

    #region Events

    private HookResult OnMapChange(EventMapTransition @event, GameEventInfo info)
    {
        _mapSpawns = GetSpawnForCurrentMap();
        return HookResult.Continue;
    }

    #endregion

    private MapSpawnsModel? _mapSpawns;

    private MapSpawnsModel GetSpawnForCurrentMap()
    {
        var tSpawns = GetSpawns(CsTeam.Terrorist);
        var ctSpawn = GetSpawns(CsTeam.CounterTerrorist);
        return new MapSpawnsModel(Server.MapName, tSpawns, ctSpawn);
    }

    private List<PositionModel> GetSpawns(CsTeam team)
    {
        string teamString;
        if (team == CsTeam.Terrorist)
        {
            teamString = "terrorist";
        }
        else if (team == CsTeam.CounterTerrorist)
        {
            teamString = "counterterrorist";
        }
        else
        {
            return new List<PositionModel>();
        }

        var result = new List<PositionModel>();
        var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>($"info_player_{teamString}").ToList();
        Console.WriteLine($"Spawn: {spawns.Count()}");
        var minPrio = 1;
        foreach (var spawn in spawns)
        {
            if (spawn.IsValid && spawn.Enabled && spawn.Priority <= minPrio)
            {
                minPrio = spawn.Priority;
            }
        }

        foreach (var spawn in spawns)
        {
            if (spawn.IsValid && spawn.Enabled && spawn.Priority == minPrio)
            {
                result.Add(
                    new PositionModel(
                        spawn.CBodyComponent!.SceneNode!.AbsOrigin,
                        spawn.CBodyComponent.SceneNode.AbsRotation)
                );
            }
        }

        Console.WriteLine($"ResultSpawns: {result.Count}");
        return result;
    }

    private void TeleportToTeamSpawn(CCSPlayerController player, int spawnNumber, CsTeam team = CsTeam.None)
    {
        if (player.IsValid == false)
        {
            return;
        }

        if (spawnNumber <= 0)
        {
            return;
        }

        var targetTeam = team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist
            ? (CsTeam)player.TeamNum
            : team;

        _mapSpawns ??= GetSpawnForCurrentMap();

        List<PositionModel> spawns;
        if (targetTeam == CsTeam.Terrorist)
        {
            spawns = _mapSpawns.TSpawn;
        }
        else if (targetTeam == CsTeam.CounterTerrorist)
        {
            spawns = _mapSpawns.CtSpawn;
        }
        else
        {
            return;
        }

        if (spawns.Count < spawnNumber)
        {
            return;
        }

        var playerPawn = player.PlayerPawn;
        if (playerPawn.IsValid == false || playerPawn.Value is null)
        {
            return;
        }

        var spawn = spawns[spawnNumber - 1];
        playerPawn.Value.Teleport(
            spawn.Position,
            spawn.Angle,
            new Vector(0, 0, 0));
    }
}