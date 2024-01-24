using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using ErrorOr;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PracPlugin.ErrorsExtension;
using PracPlugin.Helper;
using PracPlugin.Models;

namespace PracPlugin.Services;

public class GrenadeService : BackgroundService
{
    private readonly ILogger<GrenadeService> _logger;
    private readonly PracPlugin _plugin;
    private readonly TimerService _timerService;

    public GrenadeService(
        ILogger<GrenadeService> logger,
        PracPlugin plugin,
        TimerService timerService
    )
    {
        _logger = logger;
        _plugin = plugin;
        _timerService = timerService;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _plugin.RegisterEventHandler<EventSmokegrenadeDetonate>(OnSmokeDetonate);
        _plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerDamage);
        _plugin.RegisterEventHandler<EventPlayerBlind>(OnPlayerBlind);
        _plugin.RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        _plugin.RegisterListener<Listeners.OnTick>(OnTick);
        _logger.LogInformation("GrenadeService event handler registered");

        _plugin.AddCommand("rethrow", "rethrows the last grenade", CommandHandlerRethrow);
        _plugin.AddCommand("last", "Teleports player to his last thrown grenade position", CommandHandlerLast);
        _plugin.AddCommand("ff", "Clears all grenades", CommandHandlerFf);
        _logger.LogInformation("GrenadeService commands registered");

        return Task.CompletedTask;
    }

    // used for rethrow
    private readonly ThreadSaveDictionary<CCSPlayerController, GrenadeModel> _lastThrownGrenade = new();

    // used for smoke fly time
    private readonly ThreadSaveDictionary<int, DateTime> _lastThrownSmoke = new();

    /// <summary>
    /// List of plugin re-thrown grenades
    /// </summary>
    private readonly List<CBaseCSGrenadeProjectile?> _selfThrownGrenade = new();

    #region Events

    private HookResult OnPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
    {
        var blindDuration = Math.Round(@event.BlindDuration, 2);
        Server.PrintToChatAll(
            $"{ColorHelper.PlayerName(@event.Attacker.PlayerName)} blinded {ColorHelper.PlayerName(@event.Userid.PlayerName)} for " +
            $"{ColorHelper.Value(blindDuration.ToString(CultureInfo.InvariantCulture))}seconds");
        return HookResult.Continue;
    }

    private HookResult OnSmokeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info)
    {
        if (_lastThrownSmoke.Get(@event.Entityid, out var result))
        {
            var throwDuration = Math.Round((DateTime.UtcNow - result).TotalSeconds, 2)
                .ToString(CultureInfo.InvariantCulture);
            Server.PrintToChatAll(
                $"Smoke thrown by " +
                $"{ColorHelper.PlayerName(@event.Userid.PlayerName)} " +
                $"took " +
                $"{ColorHelper.Value(throwDuration)}seconds " +
                $"to detonate");
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDamage(EventPlayerHurt @event, GameEventInfo info)
    {
        if (@event.Weapon.Equals("hegrenade"))
        {
            Server.PrintToChatAll(
                $"{ColorHelper.PlayerName(@event.Attacker.PlayerName)} damaged {ColorHelper.PlayerName(@event.Userid.PlayerName)} " +
                $"for {ColorHelper.Value(@event.DmgHealth.ToString())}HP");
        }

        return HookResult.Continue;
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity.IsValid == false || entity.Entity is null)
        {
            return;
        }

        var designerName = entity.DesignerName;
        if (designerName.EndsWith("_projectile") == false)
        {
            return;
        }

        if (designerName.Equals("smokegrenade_projectile"))
        {
            _lastThrownSmoke.AddOrUpdate((int)entity.Index, DateTime.UtcNow);
        }

        Server.NextFrame(() =>
        {
            var grenade = new CBaseCSGrenadeProjectile(entity.Handle);
            var throwerHandle = grenade.Thrower.Value?.Controller.Value?.Handle;
            if (throwerHandle is null)
            {
                return;
            }

            var thrower = new CCSPlayerController(throwerHandle.Value);
            var throwerPosition = PositionModel.CopyFrom(thrower.PlayerPawn.Value);
            if (throwerPosition.IsError)
            {
                // TODO: display error
                return;
            }

            var grenadeThrowPosition = PositionModel.CopyFrom(grenade.InitialPosition, throwerPosition.Value.Angle);
            if (grenadeThrowPosition.IsError)
            {
                // TODO: display error
                return;
            }

            var grenadeThrowVelocity = new Vector(
                grenade.InitialVelocity.X,
                grenade.InitialVelocity.Y,
                grenade.InitialVelocity.Z
            );

            var grenadeModel = new GrenadeModel(
                thrower,
                throwerPosition.Value,
                (int)entity.Index,
                designerName,
                grenadeThrowPosition.Value,
                grenadeThrowVelocity);
            _lastThrownGrenade.AddOrUpdate(thrower, grenadeModel);
        });
    }

    private void OnTick()
    {
        /*
        for (int i = 0; i < SelfThrownGrenade.Count; i++)
        {
            CBaseCSGrenadeProjectile? projectile = SelfThrownGrenade[i];
            if (projectile == null || !projectile.IsValid)
            {
                SelfThrownGrenade.RemoveAt(i);
                i--;
                continue;
            }

            //Smoke projectiles are somewhat special since they need some extra manipulation
            if (projectile.IsSmokeGrenade)
            {
                var cSmoke = new CSmokeGrenadeProjectile(projectile.Handle);
                if (cSmoke.AbsVelocity.X == 0.0f && cSmoke.AbsVelocity.Y == 0.0f && cSmoke.AbsVelocity.Z == 0.0f)
                {
                    Server.PrintToChatAll($"smoke stopped moving...");
                    Server.PrintToChatAll($"cSmoke.DidSmokeEffect: {cSmoke.DidSmokeEffect}");
                    Server.PrintToChatAll($"cSmoke.DetonateTime: {cSmoke.DetonateTime}");
                    Server.PrintToChatAll($"cSmoke.DetonationRecorded: {cSmoke.DetonationRecorded}");
                    Server.PrintToChatAll($"cSmoke.SmokeDetonationPos: {cSmoke.SmokeDetonationPos}");
                    Server.PrintToChatAll($"cSmoke.AbsOrigin: {cSmoke.AbsOrigin}");
                    Server.PrintToChatAll($"cSmoke.AbsRotation: {cSmoke.AbsRotation}");
                    Server.PrintToChatAll($"cSmoke.AbsOrigin: {cSmoke.AbsVelocity}");


                    cSmoke.EffectEntity.Value?.DispatchSpawn();
                    cSmoke.EffectEntity.Value?.Teleport(cSmoke.AbsOrigin ?? new Vector(0, 0, 0),
                        cSmoke.AbsRotation ?? new QAngle(0, 0, 0), cSmoke.AbsVelocity);

                    Server.PrintToChatAll("=======================================");
                    Server.PrintToChatAll($"cSmoke.DidSmokeEffect: {cSmoke.DidSmokeEffect}");
                    Server.PrintToChatAll($"cSmoke.DetonateTime: {cSmoke.DetonateTime}");
                    Server.PrintToChatAll($"cSmoke.DetonationRecorded: {cSmoke.DetonationRecorded}");
                    Server.PrintToChatAll($"cSmoke.SmokeDetonationPos: {cSmoke.SmokeDetonationPos}");
                    SelfThrownGrenade.RemoveAt(i);
                    i--;
                    continue;
                }
            }
            else
            {
                //Non Smoke projectiles like HE, Flash or Molotov can be removed, does not need extra attention
                SelfThrownGrenade.RemoveAt(i);
                i--;
            }
        }*/
    }

    #endregion

    #region Commands

    private void CommandHandlerRethrow(CCSPlayerController? player, CommandInfo commandinfo)
    {
        if (player is null)
        {
            return;
        }

        if (TryGetLastThrownGrenade(player, out var lastThrownGrenade) == false)
        {
            return;
        }

        var spawnGrenade = SpawnGrenade(lastThrownGrenade);
        if (spawnGrenade.IsError)
        {
            player.PrintToCenter($"Failed to rethrow grenade. {spawnGrenade.FirstError.Description}");
        }
    }

    private void CommandHandlerLast(CCSPlayerController? player, CommandInfo commandinfo)
    {
        if (player is null)
        {
            return;
        }

        if (TryGetLastThrownGrenade(player, out var lastThrownGrenadePosition))
        {
            player.PlayerPawn.Value?.Teleport(
                lastThrownGrenadePosition.ThrowerPosition.Position,
                lastThrownGrenadePosition.ThrowerPosition.Angle,
                new Vector(0, 0, 0)
            );
        }
    }

    private void CommandHandlerFf(CCSPlayerController? player, CommandInfo commandinfo)
    {
        Server.ExecuteCommand("host_timescale 50");
        _timerService.AddTimer(20f, () => Server.ExecuteCommand("host_timescale 1"));
    }

    #endregion


    private bool TryGetLastThrownGrenade(CCSPlayerController player, [MaybeNullWhen(false)] out GrenadeModel position)
    {
        if (_lastThrownGrenade.Get(player, out var positionTemp))
        {
            position = positionTemp;
            return true;
        }

        position = null;
        return false;
    }

    private ErrorOr<Success> SpawnGrenade(GrenadeModel grenade)
    {
        CBaseCSGrenadeProjectile? grenadeEntity;
        if (grenade.IsSmoke)
        {
            Server.ExecuteCommand("sv_rethrow_last_grenade");
            return Result.Success;

            grenadeEntity = Utilities.CreateEntityByName<CSmokeGrenadeProjectile>(grenade.GrenadeName);
            if (grenadeEntity is not null)
            {
                grenadeEntity.IsSmokeGrenade = true;
            }
        }
        else if (grenade.IsMolotov)
        {
            grenadeEntity = Utilities.CreateEntityByName<CMolotovProjectile>(grenade.GrenadeName);
        }
        else if (grenade.IsHE)
        {
            grenadeEntity = Utilities.CreateEntityByName<CHEGrenadeProjectile>(grenade.GrenadeName);
        }
        else if (grenade.IsFlashbang)
        {
            grenadeEntity = Utilities.CreateEntityByName<CFlashbangProjectile>(grenade.GrenadeName);
        }
        else if (grenade.IsDecoy)
        {
            grenadeEntity = Utilities.CreateEntityByName<CDecoyProjectile>(grenade.GrenadeName);
        }
        else
        {
            return Errors.Fail($"\"{grenade.GrenadeName}\" is not a valid grenade to spawn");
        }

        if (grenadeEntity is null)
        {
            return Errors.Fail("Failed to spawn grenade");
        }

        grenadeEntity.Elasticity = 0.33f;
        grenadeEntity.IsLive = false;
        grenadeEntity.DmgRadius = 350.0f;
        grenadeEntity.Damage = 99.0f;
        grenadeEntity.InitialPosition.X = grenade.GrenadeThrowPosition.Position.X;
        grenadeEntity.InitialPosition.Y = grenade.GrenadeThrowPosition.Position.Y;
        grenadeEntity.InitialPosition.Z = grenade.GrenadeThrowPosition.Position.Z;
        grenadeEntity.InitialVelocity.X = grenade.GrenadeThrowVelocity.X;
        grenadeEntity.InitialVelocity.Y = grenade.GrenadeThrowVelocity.Y;
        grenadeEntity.InitialVelocity.Z = grenade.GrenadeThrowVelocity.Z;
        grenadeEntity.Teleport(
            grenade.GrenadeThrowPosition.Position,
            grenade.GrenadeThrowPosition.Angle,
            grenade.GrenadeThrowVelocity
        );

        grenadeEntity.DispatchSpawn();

        grenadeEntity.Globalname = "custom";
        grenadeEntity.AcceptInput("FireUser1", grenade.Thrower, grenade.Thrower, "");
        grenadeEntity.AcceptInput("InitializeSpawnFromWorld", null, null, "");
        grenadeEntity.TeamNum = grenade.Thrower.TeamNum;
        grenadeEntity.Thrower.Raw = grenade.Thrower.PlayerPawn.Raw;
        grenadeEntity.OriginalThrower.Raw = grenade.Thrower.PlayerPawn.Raw;
        grenadeEntity.OwnerEntity.Raw = grenade.Thrower.PlayerPawn.Raw;

        _selfThrownGrenade.Add(grenadeEntity);
        return Result.Success;
    }
}