using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Application.ServerServiceFolder;

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
                _eventService.OnHibernationStarted();
            }
            else if (output.Output.Equals("Server waking up from hibernation"))
            {
                _eventService.OnHibernationEnded();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to detect Hibernation start");
        }
    }

    [GeneratedRegex(@"Host activate: Changelevel \((.+)\)")]
    private static partial Regex MapChangeRegex();

    // Changelevel to de_nukeh
    public void NewOutputMapChangeDetection(object? _, ServerOutputEventArg output)
    {
        try
        {
            var matches = MapChangeRegex().Match(output.Output);
            if (matches.Success == false || matches.Groups.Count != 2)
            {
                return;
            }

            var newMap = matches.Groups[1]
                .ToString()
                .Trim();
            _eventService.OnMapChanged(newMap);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to detect map change");
        }
    }

    [GeneratedRegex(
        @"Accepting Steam Net connection #(\d+) UDP steamid:(\d+)@((?:\d{1,3}[.|:]){4}(?:[\d]{5}))"
    )]
    private static partial Regex PlayerConnectRegex();

    // Accepting Steam Net connection #3000669907 UDP steamid:76561198044941665@172.17.0.1:54196
    public void NewOutputPlayerConnectDetection(object? _, ServerOutputEventArg output)
    {
        try
        {
            if (output.Output.StartsWith("Accepting Steam Net connection") == false)
            {
                return;
            }

            var match = PlayerConnectRegex().Match(output.Output);
            if (match.Groups.Count != 4)
            {
                return;
            }

            var connectionId = match.Groups[1].Value;
            var stemId = match.Groups[2].Value;
            var ipPort = match.Groups[3].Value;

            _eventService.OnPlayerConnected(connectionId, stemId, ipPort);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to detect player connected");
        }
    }

    [GeneratedRegex(
        @"\[#(\d+) UDP steamid:(\d+)@((?:\d{1,3}[.|:]){4}(?:[\d]{0,5}))\] closed by (?:app|peer)(?:, entering linger state)? \((\d+)\):? (.+)"
    )]
    private static partial Regex PlayerDisconnectRegex();

    // [#3000669907 UDP steamid:76561198044941665@172.17.0.1:54196] closed by app, entering linger state (2158) NETWORK_DISCONNECT_KICKED_IDLE
    // [#2292360458 UDP steamid:76561198044941665@172.17.0.1:33160] closed by app, entering linger state (2039) NETWORK_DISCONNECT_KICKED
    // [#2330728174 UDP steamid:76561198154417260@10.10.20.10:50819] closed by peer (1059): NETWORK_DISCONNECT_EXITING
    // [#1570318589 UDP steamid:76561198158337634@95.91.227.226:1103] closed by peer (1002): NETWORK_DISCONNECT_DISCONNECT_BY_USER

    public void NewOutputPlayerDisconnectDetection(object? _, ServerOutputEventArg output)
    {
        try
        {
            if (output.Output.Contains("UDP steamid") == false)
            {
                return;
            }

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

            _eventService.OnPlayerDisconnected(connectionId, steamId64, ipPort, disconnectReasonCode, disconnectReason);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to detect player disconnect");
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

            _eventService.OnChatMessage(chat, name, steamId3, message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to detect chat message");
        }
    }
}