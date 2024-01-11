using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using PracPlugin.Models;

namespace PracPlugin;

public class BotManager
{
    /// <summary>
    /// Dict of a bots Key = userid of bot
    /// </summary>
    private static readonly Dictionary<int, Dictionary<string, object>> SpawnedBots = new();

    /// <summary>
    /// Following code is heavily inspired by https://github.com/shobhit-pathak/MatchZy/blob/main/PracticeMode.cs
    /// </summary>
    /// <param name="player">player who added the bot</param>
    /// <param name="crouch">option if the added bot should crouch</param>
    public static void AddBot(CCSPlayerController player, bool crouch = false)
    {
        if (player.TeamNum == (byte)CsTeam.Terrorist)
        {
            Server.ExecuteCommand("bot_join_team T");
            Server.ExecuteCommand("bot_add_t");
        }
        else if (player.TeamNum == (byte)CsTeam.CounterTerrorist)
        {
            Server.ExecuteCommand("bot_join_team CT");
            Server.ExecuteCommand("bot_add_ct");
        }

        // Adding a small timer so that bot can be added in the world
        // Once bot is added, we teleport it to the requested position
        PracPlugin.Instance!.AddTimer(0.1f, () => SpawnBot(player, crouch));
        Server.ExecuteCommand("bot_stop 1");
        Server.ExecuteCommand("bot_freeze 1");
        Server.ExecuteCommand("bot_zombie 1");
    }

    /// <summary>
    /// Boost player onto bot
    /// </summary>
    /// <param name="player">player called the command</param>
    /// <param name="crouch">option if the added bot should crouch</param>
    public static void Boost(CCSPlayerController player, bool crouch = false)
    {
        AddBot(player, crouch);
        Console.WriteLine("Elevating player");
        PracPlugin.Instance!.AddTimer(0.2f, () => ElevatePlayer(player));
    }

    /// <summary>
    /// Remove closest bot to the player
    /// </summary>
    /// <param name="player">player called the command</param>
    public static void NoBot(CCSPlayerController player)
    {
        CCSPlayerController? closestBot = null;
        float distance = 0;
        foreach (var botDict in SpawnedBots.Values)
        {
            var botOwner = (CCSPlayerController)botDict["owner"];
            var bot = (CCSPlayerController)botDict["controller"];
            if (!bot.IsValid)
            {
                continue;
            }

            if (botOwner.UserId == player.UserId)
            {
                Console.WriteLine("found bot of player");
                if (closestBot == null)
                {
                    closestBot = bot;
                    distance = AbsolutDistance(botOwner, bot);
                }

                var tempDistance = AbsolutDistance(botOwner, bot);
                if (tempDistance < distance)
                {
                    distance = tempDistance;
                    closestBot = bot;
                }
            }
        }

        if (closestBot != null)
        {
            Console.WriteLine($"kickid {closestBot.UserId}");
            Server.ExecuteCommand($"kickid {closestBot.UserId}");
            SpawnedBots.Remove((int)closestBot.UserId!);
        }
    }

    /// <summary>
    /// Calculate difference in coordinates between a player and a bot
    /// </summary>
    /// <param name="player">player</param>
    /// <param name="bot">bot</param>
    /// <returns>absolut distance x+y</returns>
    private static float AbsolutDistance(CCSPlayerController player, CCSPlayerController bot)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn is null)
        {
            throw new Exception("Player pawn not found");
        }

        var botPawn = bot.PlayerPawn.Value;
        if (botPawn is null)
        {
            throw new Exception("Bot pawn not found");
        }


        var playerPos = playerPawn.CBodyComponent!.SceneNode!.AbsOrigin;
        var botPos = botPawn.CBodyComponent!.SceneNode!.AbsOrigin;
        var distanceX = playerPos.X - botPos.X;
        var distanceY = playerPos.Y - botPos.Y;
        var distanceZ = playerPos.Z - botPos.Z;
        if (distanceX < 0)
        {
            distanceX *= -1;
        }

        if (distanceY < 0)
        {
            distanceY *= -1;
        }

        if (distanceZ < 0)
        {
            distanceZ *= -1;
        }

        Console.WriteLine($"calculating distance {distanceX + distanceY + distanceZ}");
        return distanceX + distanceY + distanceZ;
    }

    /// <summary>
    /// Remove all bots of the player
    /// </summary>
    /// <param name="player"></param>
    public static void ClearBots(CCSPlayerController player)
    {
        foreach (var botDict in SpawnedBots.Values)
        {
            var botOwner = (CCSPlayerController)botDict["owner"];
            var bot = (CCSPlayerController)botDict["controller"];
            if (botOwner.UserId == player.UserId)
            {
                Server.ExecuteCommand($"kickid {bot.UserId}");
                SpawnedBots.Remove((int)bot.UserId!);
            }
        }
    }


    private static void ElevatePlayer(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn is null)
        {
            throw new Exception("Player pawn not found");
        }

        playerPawn.Teleport(
            new Vector(playerPawn.CBodyComponent!.SceneNode!.AbsOrigin.X,
                playerPawn.CBodyComponent!.SceneNode!.AbsOrigin.Y,
                playerPawn.CBodyComponent!.SceneNode!.AbsOrigin.Z + 80.0f),
            playerPawn.EyeAngles, new Vector(0, 0, 0));
        Console.WriteLine(
            $"boosting player: {playerPawn.CBodyComponent!.SceneNode!.AbsOrigin.X} - {playerPawn.CBodyComponent!.SceneNode!.AbsOrigin.Y} - {playerPawn.CBodyComponent!.SceneNode!.AbsOrigin.Z + 80.0f}");
    }

    private static void SpawnBot(CCSPlayerController botOwner, bool crouch = false)
    {
        var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
        var unusedBotFound = false;
        foreach (var tempPlayer in playerEntities)
        {
            if (!tempPlayer.IsBot) continue;
            if (tempPlayer.UserId.HasValue)
            {
                if (!SpawnedBots.ContainsKey(tempPlayer.UserId.Value) && unusedBotFound)
                {
                    Console.WriteLine(
                        $"UNUSED BOT FOUND: {tempPlayer.UserId.Value} EXECUTING: kickid {tempPlayer.UserId.Value}");
                    // Kicking the unused bot. We have to do this because bot_add_t/bot_add_ct may add multiple bots but we need only 1, so we kick the remaining unused ones
                    Server.ExecuteCommand($"kickid {tempPlayer.UserId.Value}");
                    continue;
                }

                if (SpawnedBots.ContainsKey(tempPlayer.UserId.Value))
                {
                    continue;
                }
                else
                {
                    SpawnedBots[tempPlayer.UserId.Value] = new Dictionary<string, object>();
                }

                var botOwnerPawn = botOwner.PlayerPawn.Value;
                if (botOwnerPawn is null)
                {
                    throw new Exception("Bot owner pawn not found");
                }

                var tempPlayerPawn = tempPlayer.PlayerPawn.Value;
                if (tempPlayerPawn is null)
                {
                    throw new Exception("Player pawn not found");
                }

                var pos = new Vector(botOwnerPawn.CBodyComponent?.SceneNode?.AbsOrigin.X,
                    botOwnerPawn.CBodyComponent?.SceneNode?.AbsOrigin.Y,
                    botOwnerPawn.CBodyComponent?.SceneNode?.AbsOrigin.Z);
                var eyes = new QAngle(botOwnerPawn.EyeAngles.X, botOwnerPawn.EyeAngles.Y,
                    botOwnerPawn.EyeAngles.Z);
                PositionModel botOwnerPosition = new(pos, eyes);
                // Add key-value pairs to the inner dictionary
                SpawnedBots[tempPlayer.UserId.Value]["controller"] = tempPlayer;
                SpawnedBots[tempPlayer.UserId.Value]["position"] = botOwnerPosition;
                SpawnedBots[tempPlayer.UserId.Value]["owner"] = botOwner;
                SpawnedBots[tempPlayer.UserId.Value]["crouchstate"] = crouch;
                var movementService =
                    new CCSPlayer_MovementServices(tempPlayerPawn.MovementServices!.Handle);
                var bot = tempPlayerPawn.Bot!;
                tempPlayerPawn.Teleport(botOwnerPosition.Position, botOwnerPosition.Angle,
                    new Vector(0, 0, 0));
                if ((bool)SpawnedBots[tempPlayer.UserId.Value]["crouchstate"])
                {
                    PracPlugin.Instance!.AddTimer(0.1f, () => movementService.DuckAmount = 1);
                    PracPlugin.Instance.AddTimer(0.2f, () => bot.IsCrouching = true);
                }

                unusedBotFound = true;
            }
        }

        if (!unusedBotFound)
        {
            Server.PrintToChatAll($"Cannot add bots, the team is full! Use .nobots to remove the current bots.");
        }
    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        Console.WriteLine("----OnPlayerSpawn BotManager---");
        var player = @event.Userid;

        // Respawing a bot where it was actually spawned during practice session
        if (player.IsValid && player.IsBot && player.UserId.HasValue)
        {
            if (SpawnedBots.ContainsKey(player.UserId.Value))
            {
                if (SpawnedBots[player.UserId.Value]["position"] is PositionModel botPosition)
                {
                    var playerPawn = player.PlayerPawn.Value;
                    if (playerPawn is null)
                    {
                        throw new Exception("Player pawn not found");
                    }

                    var bot = playerPawn.Bot!;
                    var movementService =
                        new CCSPlayer_MovementServices(playerPawn.MovementServices!.Handle);
                    playerPawn.Teleport(botPosition.Position, botPosition.Angle,
                        new Vector(0, 0, 0));
                    if ((bool)SpawnedBots[player.UserId.Value]["crouchstate"])
                        playerPawn.Flags |= (uint)PlayerFlags.FL_DUCKING;
                    if ((bool)SpawnedBots[player.UserId.Value]["crouchstate"])
                    {
                        PracPlugin.Instance!.AddTimer(0.1f, () => movementService.DuckAmount = 1);
                        PracPlugin.Instance.AddTimer(0.2f, () => bot.IsCrouching = true);
                    }
                }
            }
        }

        return HookResult.Continue;
    }
}