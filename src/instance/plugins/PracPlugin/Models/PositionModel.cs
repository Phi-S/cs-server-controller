using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using ErrorOr;
using PracPlugin.ErrorsExtension;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace PracPlugin.Models;

public record PositionModel(Vector Position, QAngle Angle)
{
    public static ErrorOr<PositionModel> CopyFrom(CCSPlayerPawn? playerPawn)
    {
        if (playerPawn is null || playerPawn.IsValid == false)
        {
            return Errors.Fail("Player pawn not valid");
        }

        var playerPosition = playerPawn.AbsOrigin;
        var playerAngle = playerPawn.EyeAngles;

        if (playerPosition is null)
        {
            return Errors.Fail("Failed to get valid player pawn position");
        }

        return CopyFrom(playerPosition, playerAngle);
    }

    public static ErrorOr<PositionModel> CopyFrom(Vector? position, QAngle? angle)
    {
        if (position is null)
        {
            return Errors.Fail("Position is not valid");
        }

        if (angle is null)
        {
            return Errors.Fail("Angle is not valid");
        }

        var savedPosition = new Vector(position.X, position.Y, position.Z);
        var savedAngle = new QAngle(angle.X, angle.Y, angle.Z);
        return new PositionModel(savedPosition, savedAngle);
    }
};