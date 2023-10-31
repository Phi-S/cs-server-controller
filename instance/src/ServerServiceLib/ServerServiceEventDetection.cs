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

    public void NewOutputHibernationDetection(object? _, ServerOutputEventArg output)
    {
        try
        {
            if (output.Output.Equals("Server is hibernating"))
            {
                eventService.OnHibernationStarted();
                logger.LogInformation("Hibernation started");
            }
            else if (output.Output.Equals("Server waking up from hibernation"))
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

    [GeneratedRegex("Host activate: Changelevel (.+)")]
    private static partial Regex MapChangeRegex();

    // Host activate: Changelevel (de_dust2)
    public void NewOutputMapChangeDetection(object? _, ServerOutputEventArg output)
    {
        try
        {
            var matches = MapChangeRegex().Match(output.Output);
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

    [GeneratedRegex(
        @"CNetworkGameServerBase::ConnectClient\(\sname='(.+?)',\sremote='([0-9][0-9]?[0-9]?.[0-9][0-9]?[0-9]?.[0-9][0-9]?[0-9]?.[0-9][0-9]?[0-9]?:[0-9]{1,5})'")]
    private static partial Regex PlayerConnectRegex();

    // CNetworkGameServerBase::ConnectClient( name='PhiS', remote='10.10.20.10:57143' )
    public void NewOutputPlayerConnectDetection(object? _, ServerOutputEventArg output)
    {
        try
        {
            if (output.Output.StartsWith("CNetworkGameServerBase::ConnectClient(") == false)
            {
                return;
            }

            var match = PlayerConnectRegex().Match(output.Output);
            if (match.Groups.Count != 3)
            {
                return;
            }

            var name = match.Groups[1].Value;
            var remote = match.Groups[2].Value;

            logger.LogWarning("New player connected. Player name: {PlayerName} | Player IP: {PlayerIp}", name,
                remote);
            eventService.OnPlayerConnected(name, remote);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while trying to detect player connected");
        }
    }

    [GeneratedRegex("Disconnect client '(.+?)'.+\\):\\s(.+)")]
    private static partial Regex PlayerDisconnectRegex();

    // TODO: test player discconent
    // Disconnect client 'PhiS' from server(59): NETWORK_DISCONNECT_EXITING
    public void NewOutputPlayerDisconnectDetection(object? _, ServerOutputEventArg output)
    {
        try
        {
            if (output.Output.StartsWith("SV: Disconnect client") == false)
            {
                return;
            }

            var match = PlayerDisconnectRegex().Match(output.Output);
            if (match.Groups.Count != 3)
            {
                return;
            }

            var name = match.Groups[1].Value;
            var disconnectReason = match.Groups[2].Value;

            logger.LogWarning("Player disconnected: {PlayerName} |{DisconnectReason}", name, disconnectReason);
            eventService.OnPlayerDisconnected(name, disconnectReason);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while trying to detect player disconnect");
        }
    }

    [GeneratedRegex(@"\[(.*?)\]\[(.*?)\s\((\d+?)\)]:\s(.+)")]
    private static partial Regex ChatRegex();

    // [All Chat][PhiS (194151532)]: ezz
    // [All Chat][PhiS (194151532)]: noob
    public void NewOutputAllChatDetection(object? _, ServerOutputEventArg output)
    {
        try
        {
            if (output.Output.StartsWith("[") == false)
            {
                return;
            }

            var match = ChatRegex().Match(output.Output);
            if (match.Groups.Count != 5)
            {
                return;
            }

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