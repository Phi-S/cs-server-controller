using System.Text.RegularExpressions;
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
        ServerOutputEvent += NewOutputAllChatDetection;
    }

    private void RemoveEventDetection()
    {
        ServerOutputEvent -= NewOutputHibernationDetection;
        ServerOutputEvent -= NewOutputMapChangeDetection;
        ServerOutputEvent -= NewOutputPlayerConnectDetection;
        ServerOutputEvent -= NewOutputPlayerDisconnectDetection;
        ServerOutputEvent -= NewOutputAllChatDetection;
    }

    public void NewOutputHibernationDetection(object? _, string output)
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
    public void NewOutputMapChangeDetection(object? _, string output)
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

    // CNetworkGameServerBase::ConnectClient( name='PhiS', remote='10.10.20.10:57143' )
    public void NewOutputPlayerConnectDetection(object? _, string output)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return;
            }

            if (output.StartsWith("CNetworkGameServerBase::ConnectClient(") == false)
            {
                return;
            }

            const string nameRegex = "name='(.+?)'";
            var nameMatch = Regex.Match(output, nameRegex);
            if (nameMatch.Groups.Count != 2)
            {
                return;
            }

            var name = nameMatch.Groups[1].Value;

            const string remoteRegex =
                "remote='([0-9][0-9]?[0-9]?.[0-9][0-9]?[0-9]?.[0-9][0-9]?[0-9]?.[0-9][0-9]?[0-9]?:[0-9]{1,5})'";
            var remoteMatch = Regex.Match(output, remoteRegex);
            if (remoteMatch.Groups.Count != 2)
            {
                return;
            }

            var remote = remoteMatch.Groups[1].Value;


            logger.LogWarning("New player connected. Player name: {PlayerName} | Player IP: {PlayerIp}", name,
                remote);
            eventService.OnPlayerConnected(name, remote);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while trying to detect player connected");
        }
    }

    // Disconnect client 'PhiS' from server(59): NETWORK_DISCONNECT_EXITING
    public void NewOutputPlayerDisconnectDetection(object? _, string output)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return;
            }

            if (output.StartsWith("Disconnect client") == false)
            {
                return;
            }

            const string nameRegex = "client '(.+?)'";
            var nameMatch = Regex.Match(output, nameRegex);
            if (nameMatch.Groups.Count != 2)
            {
                return;
            }

            var name = nameMatch.Groups[1].Value;

            const string disconnectReasonRegex = ":\\s(.+?)$";
            var disconnectReasonMatch = Regex.Match(output, disconnectReasonRegex);
            if (nameMatch.Groups.Count != 2)
            {
                return;
            }

            var disconnectReason = disconnectReasonMatch.Groups[1].Value;

            logger.LogWarning("Player disconnected: {PlayerName} |{DisconnectReason}", name, disconnectReason);
            eventService.OnPlayerDisconnected(name, disconnectReason);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while trying to detect player disconnect");
        }
    }

    // [All Chat][PhiS (194151532)]: ezz
    // [All Chat][PhiS (194151532)]: noob
    public void NewOutputAllChatDetection(object? _, string output)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return;
            }

            const string regex = @"\[(.*?)\]\[(.*?)\s\((\d+?)\)]:\s(.+)";
            var match = Regex.Match(output, regex);
            var chat = match.Groups[1].Value;
            var name = match.Groups[2].Value;
            var steamId3 = match.Groups[3].Value;
            var message = match.Groups[4].Value;

            logger.LogWarning("{Chat} message from {Name}/{SteamId3}: {Message}", chat, name, steamId3, message);
            eventService.OnChatMessage(chat, name, steamId3, message);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while trying to detect chat message");
        }
    }
}