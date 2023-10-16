using System.Text.RegularExpressions;
using EventsServiceLib;
using Microsoft.Extensions.Logging;

namespace ServerServiceLib;

public partial class ServerService
{
    private void AddEventDetection()
    {
        ServerOutputEvent += NewOutputHibernationDetection;
        ServerOutputEvent += NewOutputMapChangeDetection;
        ServerOutputEvent += NewOutputPlayerConnectDetection;
        ServerOutputEvent += NewOutputPlayerDisconnectDetection;
    }

    private void NewOutputHibernationDetection(object? _, string output)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return;
            }

            output = output.Trim();

            if (output.Equals("Server is hibernating"))
            {
                eventService.OnHibernationStarted();

                logger.LogInformation("Hibernation started");
            }
            else if (output.Equals("Server waking up from hibernation"))
            {
                eventService.OnHibernationEnded();

                logger.LogInformation("Hibernation ended");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while trying to detect Hibernation start");
        }
    }

    // Host activate: Changelevel (de_dust2)
    private void NewOutputMapChangeDetection(object? _, string output)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return;
            }

            output = output.Trim();

            const string regex = @"Host activate: Changelevel (.+)";
            var matches = Regex.Match(output, regex);
            if (!matches.Success || matches.Groups.Count != 2) return;

            var newMap = matches.Groups[1].ToString()
                .Trim()
                .Replace("(", "")
                .Replace(")", "");
            logger.LogInformation("Map changed to {NewMap}", newMap);
            eventService.OnMapChanged(newMap);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while trying to detect map change");
        }
    }

    // Client "PhiS" connected (10.10.1.20:27005).
    private void NewOutputPlayerConnectDetection(object? _, string output)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return;
            }

            const string regex =
                @"Client ""(.+)"" connected \(([0-9][0-9]?[0-9]?.[0-9][0-9]?[0-9]?.[0-9][0-9]?[0-9]?.[0-9][0-9]?[0-9]?:[0-9]{1,5})\).";
            var matches = Regex.Match(output, regex);
            if (!matches.Success || matches.Groups.Count != 3) return;

            var playerMame = matches.Groups[1].ToString().Trim();
            var ipPort = matches.Groups[2].ToString().Trim();

            logger.LogWarning("New player connected. Player name: {PlayerName} | Player IP: {PlayerIp}", playerMame,
                ipPort);
            eventService.OnPlayerConnected(playerMame, ipPort);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while trying to detect player connected");
        }
    }

    // Dropped PhiS from server: Disconnect
    private void NewOutputPlayerDisconnectDetection(object? _, string output)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return;
            }

            const string regex = "Dropped (.+) from server: ([a-zA-Z0-9]+)";
            var matches = Regex.Match(output, regex);
            if (!matches.Success || matches.Groups.Count != 3) return;

            var playerName = matches.Groups[1].ToString().Trim();
            var disconnectReason = matches.Groups[2].ToString().Trim();

            logger.LogWarning("Player disconnected: {PlayerName} |{DisconnectReason}", playerName, disconnectReason);
            eventService.OnPlayerDisconnected(playerName, disconnectReason);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while trying to detect player disconnect");
        }
    }
}