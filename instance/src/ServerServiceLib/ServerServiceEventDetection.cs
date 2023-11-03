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
            }
            else if (output.Output.Equals("Server waking up from hibernation"))
            {
                eventService.OnHibernationEnded();
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while trying to detect Hibernation start");
        }
    }

    [GeneratedRegex("Changelevel to (.+)")]
    private static partial Regex MapChangeRegex();

    // Changelevel to de_nuke
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

            eventService.OnPlayerConnected(name, remote);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while trying to detect player connected");
        }
    }

    [GeneratedRegex(
        "Steam Net connection #(.+?) UDP steamid:(\\d+)@([0-9][0-9]?[0-9]?.[0-9][0-9]?[0-9]?.[0-9][0-9]?[0-9]?.[0-9][0-9]?[0-9]?:[0-9]{1,5}) closed by peer, reason (\\d{4}): (.+)")]
    private static partial Regex PlayerDisconnectRegex();

    // Steam Net connection #2892143414 UDP steamid:76561198154417260@10.10.20.10:49347 closed by peer, reason 1002: NETWORK_DISCONNECT_DISCONNECT_BY_USER
    public void NewOutputPlayerDisconnectDetection(object? _, ServerOutputEventArg output)
    {
        try
        {
            var match = PlayerDisconnectRegex().Match(output.Output);
            if (match.Groups.Count != 6)
            {
                return;
            }

            var connectionId = match.Groups[1].Value;
            var steamId64 = match.Groups[2].Value;
            var ipPort = match.Groups[3].Value;
            var disconnectReasonCode = match.Groups[4].Value;
            var disconnectReason = match.Groups[5].Value;

            eventService.OnPlayerDisconnected(connectionId, steamId64, ipPort, disconnectReasonCode, disconnectReason);
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

            eventService.OnChatMessage(chat, name, steamId3, message);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while trying to detect chat message");
        }
    }
}