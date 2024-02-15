using System.Text.RegularExpressions;
using Application.EventServiceFolder.EventArgs;
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

    [GeneratedRegex(@"(?:Client #(\d+) \"")(.+)(?:\"" connected @ )(.*):(.*)"
    )]
    private static partial Regex PlayerConnectRegex();

    public void NewOutputPlayerConnectDetection(object? _, ServerOutputEventArg output)
    {
        try
        {
            var outputString = output.Output.Trim();
            if (outputString.StartsWith("Client") == false)
            {
                return;
            }

            var match = PlayerConnectRegex().Match(outputString);
            if (match.Groups.Count != 5)
            {
                return;
            }

            var connectionId = match.Groups[1].Value;
            var username = match.Groups[2].Value;
            var ip = match.Groups[3].Value;
            var port = match.Groups[4].Value;

            _eventService.OnPlayerConnected(connectionId, username, ip, port);
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

    [GeneratedRegex(@"(\[All Chat\]|\[Allies Chat\])(?:\[(.+) \((\d+)\)\]:) (.+)")]
    private static partial Regex ChatRegex();

    public void NewOutputAllChatDetection(object? _, ServerOutputEventArg output)
    {
        try
        {
            var outputString = output.Output.Trim();
            if (outputString.StartsWith("[All") == false)
            {
                return;
            }

            var match = ChatRegex().Match(outputString);
            if (match.Groups.Count != 5)
            {
                return;
            }

            var chatString = match.Groups[1].Value;
            Chat chat;
            if (chatString.Equals("[Allies Chat]"))
            {
                chat = Chat.Team;
            }
            else if (chatString.Equals("[All Chat]"))
            {
                chat = Chat.All;
            }
            else
            {
                throw new Exception($"\"{chatString}\" is not a valid chat");
            }

            var username = match.Groups[2].Value;
            var steamId = match.Groups[3].Value;
            var message = match.Groups[4].Value;

            _eventService.OnChatMessage(chat, username, steamId, message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to detect chat message");
        }
    }
}