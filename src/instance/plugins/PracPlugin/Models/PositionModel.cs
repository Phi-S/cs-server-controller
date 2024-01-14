using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace PracPlugin.Models;

public record PositionModel(Vector Position, QAngle Angle)
{
    public static PositionModel CopyFrom(Vector position, QAngle angle)
    {
        var savedPosition = new Vector(position.X, position.Y, position.Z);
        var savedAngle = new QAngle(angle.X, angle.Y, angle.Z);
        return new PositionModel(savedPosition, savedAngle);
    }
};