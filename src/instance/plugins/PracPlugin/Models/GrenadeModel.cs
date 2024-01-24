using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using PracPlugin.Util;

namespace PracPlugin.Models;

public record GrenadeModel(
    CCSPlayerController Thrower,
    PositionModel ThrowerPosition,
    int GrenadeEntityId,
    string GrenadeName,
    PositionModel GrenadeThrowPosition,
    Vector GrenadeThrowVelocity
)
{
    public bool IsSmoke => GrenadeName.Equals(GrenadeNames.ProjectileSmoke);
    public bool IsMolotov => GrenadeName.Equals(GrenadeNames.ProjectileMolotov);
    public bool IsHE => GrenadeName.Equals(GrenadeNames.ProjectileHE);
    public bool IsFlashbang => GrenadeName.Equals(GrenadeNames.ProjectileFlashbang);
    public bool IsDecoy => GrenadeName.Equals(GrenadeNames.ProjectileDecoy);
};