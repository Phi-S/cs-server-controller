using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using PracPlugin.Helper;
using PracPlugin.Models;

namespace PracPlugin.Services;

public class GrenadeService
{
    private readonly ILogger<GrenadeService> _logger;

    public GrenadeService(ILogger<GrenadeService> logger)
    {
        _logger = logger;
    }

    private readonly Dictionary<CCSPlayerController, PositionModel> _lastThrownGrenade = new();
    private readonly Dictionary<int, DateTime> _lastThrownSmoke = new();

    public void RegisterEventHandler(BasePlugin plugin)
    {
        plugin.RegisterEventHandler<EventSmokegrenadeDetonate>(OnSmokeDetonate);
        plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerDamage);
        plugin.RegisterEventHandler<EventPlayerBlind>(OnPlayerBlind);
        plugin.RegisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);
        plugin.RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        _logger.LogInformation("BotManager event handler registered");
    }

    public bool TryGetLastThrownGrenade(CCSPlayerController player, [MaybeNullWhen(false)] out PositionModel position)
    {
        if (_lastThrownGrenade.TryGetValue(player, out var positionTemp))
        {
            position = positionTemp;
            return true;
        }

        position = null;
        return false;
    }

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
        if (_lastThrownSmoke.TryGetValue(@event.Entityid, out var result))
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

    private HookResult OnGrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
    {
        if (@event.Userid.IsValid &&
            @event.Userid.PlayerPawn is { IsValid: true, Value.AbsOrigin: not null })
        {
            var lastThrownGrenadePlayerPosition = PositionModel.CopyFrom(
                @event.Userid.PlayerPawn.Value.AbsOrigin,
                @event.Userid.PlayerPawn.Value.EyeAngles);
            _lastThrownGrenade[@event.Userid] = lastThrownGrenadePlayerPosition;
        }
        else
        {
            _logger.LogWarning("Failed to add last grenade for {Player}", @event.Userid.PlayerName);
        }

        return HookResult.Continue;
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity is { IsValid: true, Entity: not null } && entity.Entity.DesignerName.EndsWith("_projectile"))
        {
            if (entity.Entity.DesignerName.Equals("smokegrenade_projectile"))
            {
                _lastThrownSmoke.Add((int)entity.Index, DateTime.UtcNow);
            }
        }
    }
}