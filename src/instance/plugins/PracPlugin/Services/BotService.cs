using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PracPlugin.Models;

namespace PracPlugin.Services;

public class BotService : BackgroundService
{
    private readonly ILogger<BotService> _logger;
    private readonly PracPlugin _plugin;
    private readonly TimerService _timerService;

    public BotService(
        ILogger<BotService> logger,
        PracPlugin plugin,
        TimerService timerService)
    {
        _logger = logger;
        _plugin = plugin;
        _timerService = timerService;
    }


    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        _logger.LogInformation("BotService event handler registered");

        _plugin.AddCommand("bot", "Places standing bot on calling player position", CommandHandlerBot);
        _plugin.AddCommand("cbot", "Places crouching bot on calling player position", CommandHandlerCBot);
        _plugin.AddCommand("boost", "Places bot beneath calling player position", CommandHandlerBoost);
        _plugin.AddCommand("cboost", "Places crouching bot beneath calling player position", CommandHandlerCBoost);
        _plugin.AddCommand("nobot", "Removes the closest bot from calling player", CommandHandlerNoBot);
        _plugin.AddCommand("nobots", "Removes all bots spawn by the calling player", CommandHandlerNoBots);
        _logger.LogInformation("BotService commands registered");

        return Task.CompletedTask;
    }

    #region Commands

    private void CommandHandlerNoBots(CCSPlayerController? player, CommandInfo commandinfo)
    {
        if (player is null)
        {
            return;
        }

        ClearBots(player);
    }

    private void CommandHandlerNoBot(CCSPlayerController? player, CommandInfo commandinfo)
    {
        if (player is null)
        {
            return;
        }

        NoBot(player);
    }

    private void CommandHandlerCBoost(CCSPlayerController? player, CommandInfo commandinfo)
    {
        if (player is null)
        {
            return;
        }

        Boost(player, true);
    }

    private void CommandHandlerBoost(CCSPlayerController? player, CommandInfo commandinfo)
    {
        if (player is null)
        {
            return;
        }

        Boost(player);
    }

    private void CommandHandlerCBot(CCSPlayerController? player, CommandInfo commandinfo)
    {
        if (player is null)
        {
            return;
        }

        if (commandinfo.ArgCount == 2)
        {
            var botTeam = commandinfo.ArgByIndex(1).ToLower();
            if (string.IsNullOrEmpty(botTeam) == false)
            {
                if (botTeam.Equals("t"))
                {
                    AddBot(player, CsTeam.Terrorist, true);
                }
                else if (botTeam.Equals("ct"))
                {
                    AddBot(player, CsTeam.CounterTerrorist, true);
                }
                else
                {
                    _logger.LogWarning("Cant add bot to team {BotTeam}", botTeam);
                }

                return;
            }
        }

        AddBot(player, CsTeam.None, true);
    }

    private void CommandHandlerBot(CCSPlayerController? player, CommandInfo commandinfo)
    {
        if (player is null)
        {
            return;
        }

        if (commandinfo.ArgCount == 2)
        {
            var botTeam = commandinfo.ArgByIndex(1).ToLower();
            if (string.IsNullOrEmpty(botTeam) == false)
            {
                if (botTeam.Equals("t"))
                {
                    AddBot(player, CsTeam.Terrorist);
                }
                else if (botTeam.Equals("ct"))
                {
                    AddBot(player, CsTeam.CounterTerrorist);
                }
                else
                {
                    _logger.LogWarning("Cant add bot to team {BotTeam}", botTeam);
                }

                return;
            }
        }

        AddBot(player);
    }

    #endregion

    #region Events

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;

        // Respawing a bot where it was actually spawned during practice session
        if (player.IsValid && player is { IsBot: true, UserId: not null })
        {
            if (_spawnedBots.Get(player.UserId.Value, out var spawnedBotInfo))
            {
                var playerPawn = player.PlayerPawn.Value;
                if (playerPawn is null)
                {
                    throw new Exception("Player pawn not found");
                }

                var bot = playerPawn.Bot!;
                var movementService =
                    new CCSPlayer_MovementServices(playerPawn.MovementServices!.Handle);
                playerPawn.Teleport(
                    spawnedBotInfo.Position.Position,
                    spawnedBotInfo.Position.Angle,
                    new Vector(0, 0, 0));
                if (spawnedBotInfo.Crouch)
                {
                    playerPawn.Flags |= (uint)PlayerFlags.FL_DUCKING;
                    _timerService.AddTimer(0.1f, () => movementService.DuckAmount = 1);
                    _timerService.AddTimer(0.2f, () => bot.IsCrouching = true);
                }
            }
        }

        return HookResult.Continue;
    }

    #endregion

    private readonly ThreadSaveDictionary<int, BotInfoModel> _spawnedBots = new();


    /// <summary>
    /// Following code is heavily inspired by https://github.com/shobhit-pathak/MatchZy/blob/main/PracticeMode.cs
    /// </summary>
    /// <param name="player">player who added the bot</param>
    /// <param name="team">team(CT/T) the bot will join. If none. The bot will join the opposite team of the player</param>
    /// <param name="crouch">option if the added bot should crouch</param>
    private void AddBot(CCSPlayerController player, CsTeam team = CsTeam.None, bool crouch = false)
    {
        // If no valid team is given,
        // use the opposite of the player team to spawn the  (CT player == T bot / T player == CT bot)
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            if (player.TeamNum == (byte)CsTeam.Terrorist)
            {
                team = CsTeam.CounterTerrorist;
            }
            else if (player.TeamNum == (byte)CsTeam.CounterTerrorist)
            {
                team = CsTeam.Terrorist;
            }
        }

        if (team == CsTeam.Terrorist)
        {
            Server.ExecuteCommand("bot_join_team T");
            Server.ExecuteCommand("bot_add_t");
        }
        else if (team == CsTeam.CounterTerrorist)
        {
            Server.ExecuteCommand("bot_join_team CT");
            Server.ExecuteCommand("bot_add_ct");
        }
        else
        {
            _logger.LogWarning("Bots cant be added to {Tem} team", team.ToString());
            return;
        }

        // Adding a small timer so that bot can be added in the world
        // Once bot is added, we teleport it to the requested position
        _timerService.AddTimer(0.1f, () => SpawnBot(player, team, crouch));
        Server.ExecuteCommand("bot_stop 1");
        Server.ExecuteCommand("bot_freeze 1");
        Server.ExecuteCommand("bot_zombie 1");
    }

    private void SpawnBot(CCSPlayerController botOwner, CsTeam team = CsTeam.None, bool crouch = false)
    {
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            _logger.LogWarning("Cant spawn bot as {Team}", team.ToString());
            return;
        }

        var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
        var unusedBotFound = false;
        foreach (var tempPlayer in playerEntities)
        {
            if (tempPlayer.IsBot == false)
            {
                continue;
            }

            if (tempPlayer.UserId.HasValue == false)
            {
                continue;
            }

            if (tempPlayer.TeamNum != (byte)team)
            {
                continue;
            }

            if (_spawnedBots.ContainsKey(tempPlayer.UserId.Value) == false && unusedBotFound)
            {
                // Kicking the unused bot. We have to do this because bot_add_t/bot_add_ct may add multiple bots but we need only 1, so we kick the remaining unused ones
                _logger.LogInformation("Kicking unused bot {BotName}", tempPlayer.PlayerName);
                Server.ExecuteCommand($"bot_kick {tempPlayer.PlayerName}");
                continue;
            }

            if (_spawnedBots.ContainsKey(tempPlayer.UserId.Value))
            {
                continue;
            }

            var botOwnerPawn = botOwner.PlayerPawn.Value;
            if (botOwnerPawn is null)
            {
                throw new Exception("Bot owner pawn not found");
            }

            var tempPlayerPawn = tempPlayer.PlayerPawn.Value;
            if (tempPlayerPawn is null)
            {
                throw new Exception("Bot pawn not found");
            }

            var pos = new Vector(
                botOwnerPawn.CBodyComponent?.SceneNode?.AbsOrigin.X,
                botOwnerPawn.CBodyComponent?.SceneNode?.AbsOrigin.Y,
                botOwnerPawn.CBodyComponent?.SceneNode?.AbsOrigin.Z);
            var eyes = new QAngle(
                botOwnerPawn.EyeAngles.X,
                botOwnerPawn.EyeAngles.Y,
                botOwnerPawn.EyeAngles.Z);
            PositionModel botOwnerPosition = new(pos, eyes);

            // Add key-value pairs to the inner dictionary
            var spawnedBotInfo = new BotInfoModel(
                tempPlayer,
                botOwnerPosition,
                botOwner,
                crouch);
            _spawnedBots.AddOrUpdate(tempPlayer.UserId.Value, spawnedBotInfo);

            var movementService =
                new CCSPlayer_MovementServices(tempPlayerPawn.MovementServices!.Handle);
            var bot = tempPlayerPawn.Bot!;
            tempPlayerPawn.Teleport(
                botOwnerPosition.Position,
                botOwnerPosition.Angle,
                new Vector(0, 0, 0));
            if (spawnedBotInfo.Crouch)
            {
                _timerService.AddTimer(0.1f, () => movementService.DuckAmount = 1);
                _timerService.AddTimer(0.2f, () => bot.IsCrouching = true);
            }

            unusedBotFound = true;
        }

        if (unusedBotFound == false)
        {
            Server.PrintToChatAll($"Cannot add bots, the team is full! Use .nobots to remove the current bots.");
        }
    }

    /// <summary>
    /// Boost player onto bot
    /// </summary>
    /// <param name="player">player called the command</param>
    /// <param name="crouch">option if the added bot should crouch</param>
    private void Boost(CCSPlayerController player, bool crouch = false)
    {
        AddBot(player, CsTeam.None, crouch);
        _timerService.AddTimer(0.2f, () => ElevatePlayer(player));
    }

    /// <summary>
    /// Remove closest bot to the player
    /// </summary>
    /// <param name="player">player called the command</param>
    private void NoBot(CCSPlayerController player)
    {
        CCSPlayerController? closestBot = null;
        float distance = 0;
        foreach (var botDict in _spawnedBots.Values)
        {
            var botOwner = botDict.Owner;
            var bot = botDict.Controller;
            if (bot.IsValid == false)
            {
                continue;
            }

            if (botOwner.UserId != player.UserId)
            {
                continue;
            }

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

        if (closestBot != null)
        {
            Server.ExecuteCommand($"kickid {closestBot.UserId}");
            _spawnedBots.Remove((int)closestBot.UserId!);
            _logger.LogInformation("Closest bot to {Player} removed", player.PlayerName);
        }
    }

    /// <summary>
    /// Calculate difference in coordinates between a player and a bot
    /// </summary>
    /// <param name="player">player</param>
    /// <param name="bot">bot</param>
    /// <returns>absolut distance x+y</returns>
    private float AbsolutDistance(CCSPlayerController player, CCSPlayerController bot)
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

        return distanceX + distanceY + distanceZ;
    }

    /// <summary>
    /// Remove all bots of the player
    /// </summary>
    /// <param name="player"></param>
    private void ClearBots(CCSPlayerController player)
    {
        foreach (var spawnedBotInfo in _spawnedBots.Values)
        {
            if (spawnedBotInfo.Owner.UserId == player.UserId)
            {
                var bot = spawnedBotInfo.Controller;
                Server.ExecuteCommand($"kickid {bot.UserId}");
                _spawnedBots.Remove(bot.UserId!.Value);
            }
        }
    }

    private void ElevatePlayer(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn is null)
        {
            throw new Exception("Player pawn not found");
        }

        playerPawn.Teleport(
            new Vector(
                playerPawn.CBodyComponent!.SceneNode!.AbsOrigin.X,
                playerPawn.CBodyComponent!.SceneNode!.AbsOrigin.Y,
                playerPawn.CBodyComponent!.SceneNode!.AbsOrigin.Z + 80.0f),
            playerPawn.EyeAngles,
            new Vector(0, 0, 0));
    }
}