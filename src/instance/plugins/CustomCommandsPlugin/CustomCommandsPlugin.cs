using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using SharedPluginLib;

namespace CustomCommandsPlugin;

public class CustomCommandsPlugin : BasePlugin
{
    public override string ModuleName => "Custom commands plugin";
    public override string ModuleVersion => "0.0.1";

    [GameEventHandler(HookMode.Post)]
    public HookResult OnPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
    {
        Server.PrintToChatAll($"{@event.Userid.PlayerName} blinded for {@event.BlindDuration}");
        return HookResult.Continue;
    }

    [ConsoleCommand("send_server_message", "Send server message")]
    [CommandHelper(minArgs: 1, usage: "[Message]", whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void SendServerMessage(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null)
        {
            command.ReplyToCommand(PluginResponse.GetFailedJson("Players can not use this command"));
            return;
        }

        var message = command.ArgByIndex(1);
        if (string.IsNullOrWhiteSpace(message))
        {
            command.ReplyToCommand(PluginResponse.GetFailedJson("Message is empty"));
            return;
        }

        Server.PrintToChatAll(message);
    }

    [ConsoleCommand("get_player_position",
        "Gets the current position of the specified player. Can only be called by the server")]
    [CommandHelper(minArgs: 1, usage: "[user slot]", whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void GetPlayerPosition(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null)
        {
            command.ReplyToCommand(PluginResponse.GetFailedJson("Players can not use this command"));
            return;
        }

        var userSlotString = command.ArgByIndex(1);
        var userSlotParseResult = int.TryParse(userSlotString, out var userSlot);
        if (userSlotParseResult == false)
        {
            command.ReplyToCommand($"User slot {userSlotString} is not valid");
            return;
        }

        var allPlayers = Utilities.GetPlayers();
        var playerToGetPositionOf = allPlayers.FirstOrDefault(p => p.Slot == userSlot);
        if (playerToGetPositionOf is null)
        {
            command.ReplyToCommand($"No Player found for the user slot {userSlot}");
            return;
        }

        var playerPawn = playerToGetPositionOf.PlayerPawn.Value;
        if (playerPawn is null)
        {
            command.ReplyToCommand("Player pawn not found");
            return;
        }

        var position = playerPawn.AbsOrigin;
        if (position is null)
        {
            command.ReplyToCommand("Player position not found");
            return;
        }

        var angle = playerPawn.EyeAngles;
        var playerPosition = new PlayerPosition(
            userSlot,
            playerToGetPositionOf.PlayerName,
            position.X,
            position.Y,
            position.Z,
            angle.X,
            angle.Y,
            angle.Z
        );

        command.ReplyToCommand(PluginResponse.GetSuccessJson(playerPosition));
    }

    [ConsoleCommand("move_player",
        "Adds an static bot at the specified position. Can only be called by the server")]
    [CommandHelper(minArgs: 1, usage: "[User slot] [Position (X Y Z)] [Angle (X Y Z)]",
        whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void PlaceStaticBotOnPosition(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null)
        {
            command.ReplyToCommand(PluginResponse.GetFailedJson("Players can not use this command"));
            return;
        }

        var userSlot = command.ArgToInt(1);
        if (userSlot is null)
        {
            command.ReplyToCommand($"user slot {userSlot} is not valid");
            return;
        }

        var allPlayers = Utilities.GetPlayers();
        var playerToGetPositionOf = allPlayers.FirstOrDefault(p => p.Slot == userSlot);
        if (playerToGetPositionOf is null || playerToGetPositionOf.IsValid == false)
        {
            command.ReplyToCommand($"No Player found for the user slot {userSlot}");
            return;
        }

        var playerPawn = playerToGetPositionOf.PlayerPawn.Value;
        if (playerPawn is null || playerPawn.IsValid == false)
        {
            command.ReplyToCommand("Player pawn not found");
            return;
        }

        var positionX = command.ArgToFloat(2);
        var positionY = command.ArgToFloat(3);
        var positionZ = command.ArgToFloat(4);
        if (positionX is null || positionY is null || positionZ is null)
        {
            command.ReplyToCommand($"Position is not valid");
            return;
        }

        var angleX = command.ArgToFloat(5);
        var angleY = command.ArgToFloat(6);
        var angleZ = command.ArgToFloat(7);
        if (angleX is null || angleY is null || angleZ is null)
        {
            command.ReplyToCommand($"Angle is not valid");
            return;
        }

        playerPawn.Teleport(
            new Vector(positionX, positionY, positionZ),
            new QAngle(angleX, angleY, angleZ),
            new Vector(0, 0, 0)
        );
    }
}