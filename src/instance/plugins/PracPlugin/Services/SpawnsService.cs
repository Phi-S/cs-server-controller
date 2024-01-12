using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using PracPlugin.Models;

namespace PracPlugin.Services;

public class SpawnsService
{
    private readonly ILogger<SpawnsService> _logger;

    public SpawnsService(ILogger<SpawnsService> logger)
    {
        _logger = logger;
        _mapSpawns = GetSpawnForCurrentMap();
    }

    private MapSpawnsModel _mapSpawns;

    public void RegisterEventHandler(BasePlugin plugin)
    {
        plugin.RegisterEventHandler<EventMapTransition>(OnMapChange);
        _logger.LogInformation("Registered SpawnsService event handlers");
    }

    private HookResult OnMapChange(EventMapTransition @event, GameEventInfo info)
    {
        _mapSpawns = GetSpawnForCurrentMap();
        return HookResult.Continue;
    }

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
        var minPrio = 1;
        foreach (var spawn in spawns)
        {
            if (spawn.IsValid && spawn.Enabled && spawn.Priority < minPrio)
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

        return result;
    }

    public void TeleportToTeamSpawn(CCSPlayerController player, int spawnNumber, CsTeam team = CsTeam.None)
    {
        if (player.IsValid == false)
        {
            return;
        }

        if (spawnNumber <= 0)
        {
            return;
        }

        var targetTeam = (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist) ? (CsTeam)player.TeamNum : team;
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

        if (spawns.Count <= spawnNumber)
        {
            player.PrintToCenter(
                $"insufficient number of spawns found. spawns {spawns.Count} - {spawnNumber}");
            return;
        }

        var playerPawn = player.PlayerPawn;
        if (playerPawn.IsValid == false || playerPawn.Value is null)
        {
            return;
        }

        playerPawn.Value.Teleport(
            spawns[spawnNumber - 1].Position,
            spawns[spawnNumber - 1].Angle,
            new Vector(0, 0, 0));
    }
}