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

    [GeneratedRegex("""
                    "(.+)<(\d)><\[(.+)\]><(.+)>" (say_team|say) "(.+)"
                    """)]
    private static partial Regex ChatRegex();

    // L 01/08/2024 - 19:31:36: "PhiS :)<2><[U:1:84675937]><TERRORIST>" say "jasdkfjaskdj"
    // L 01/08/2024 - 19:32:46: "PhiS :)<2><[U:1:84675937]><CT>" say ".fasdfa"
    // L 01/08/2024 - 19:32:48: "PhiS :)<2><[U:1:84675937]><CT>" say_team "gfggrr"
    // L 01/08/2024 - 19:36:59: "PhiS > :) < --L<2><[U:1:84675937]><TERRORIST>" say_team "kjkoo"
    // L 01/08/2024 - 19:36:57: "PhiS > :) < --L<2><[U:1:84675937]><TERRORIST>" say "iklikdf"
    // L 01/08/2024 - 19:36:45: "PhiS > :) < --L<2><[U:1:84675937]><CT>" say "1fgg"
    // L 01/08/2024 - 19:36:48: "PhiS > :) < --L<2><[U:1:84675937]><CT>" say_team "ll"
    public void NewOutputAllChatDetection(object? _, ServerOutputEventArg output)
    {
        try
        {
            if (output.Output.StartsWith("L") == false)
            {
                return;
            }

            var match = ChatRegex().Match(output.Output);
            if (match.Groups.Count != 7)
            {
                return;
            }

            var playerName = match.Groups[1].Value;
            var userIdString = match.Groups[2].Value;
            var userId = int.Parse(userIdString);

            var steamId = match.Groups[3].Value;
            var teamString = match.Groups[4].Value;
            Team team;
            if (teamString.Equals("TERRORIST"))
            {
                team = Team.T;
            }
            else if (teamString.Equals("CT"))
            {
                team = Team.CT;
            }
            else
            {
                throw new Exception($"\"{teamString}\" is not a valid team");
            }


            var chatString = match.Groups[5].Value;
            Chat chat;
            if (chatString.Equals("say_team"))
            {
                chat = Chat.Team;
            }
            else if (chatString.Equals("say"))
            {
                chat = Chat.All;
            }
            else
            {
                throw new Exception($"\"{chatString}\" is not a valid chat");
            }

            var message = match.Groups[6].Value;

            _eventService.OnChatMessage(playerName, userId, steamId, team, chat, message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to detect chat message");
        }
    }
}