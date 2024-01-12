using CounterStrikeSharp.API.Core;

namespace PracPlugin.Models;

public record BotInfoModel(CCSPlayerController Controller, PositionModel Position, CCSPlayerController Owner, bool Crouch);