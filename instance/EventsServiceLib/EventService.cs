using System.Text.RegularExpressions;
using EventsServiceLib.EventArgs;
using Microsoft.Extensions.Logging;

namespace EventsServiceLib;

public sealed class EventService
{
    private readonly ILogger<EventService> _logger;

    public EventService(ILogger<EventService> logger)
    {
        _logger = logger;

        AddHandlerToAllCommonEvents(LogEventTriggered);

        NewOutputRegisterHandlers();
        NewOutput += NewOutputHibernationDetection;
        NewOutput += NewOutputMapChangeDetection;
        NewOutput += NewOutputPlayerConnectDetection;
        NewOutput += NewOutputPlayerDisconnectDetection;
    }

    #region NewOutput

    public event EventHandler<string> NewOutput;

    private void NewOutputRegisterHandlers()
    {
        NewOutput += OnNewOutputPrintLog;
        NewOutput += OnNewOutputWriteToDatabase;
    }

    public void OnNewOutput(string? output)
    {
        if (!string.IsNullOrWhiteSpace(output))
        {
            NewOutput.Invoke(this, output);
        }
    }

    private void OnNewOutputPrintLog(object? _, string output)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger.LogInformation("CSGO: {InstanceLog}", output);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to print csgo output to the console");
        }
    }

    private async void OnNewOutputWriteToDatabase(object? sender, string output)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(output))
            {
                // TODO: create database
                //await ServerLogRepository.Add(output);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to add csgo output to the database");
        }
    }

    #region Detection

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
                OnHibernationStarted();

                _logger.LogInformation("Hibernation started");
            }
            else if (output.Equals("Server waking up from hibernation"))
            {
                OnHibernationEnded();

                _logger.LogInformation("Hibernation ended");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to detect Hibernation start");
        }
    }

    // *** Map Load: de_mirage: Map Group mg_activeL 08/25/2022 - 14:20:11: -------- Mapchange to de_mirage --------
    private void NewOutputMapChangeDetection(object? _, string output)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return;
            }

            const string regex = @"\*\*\* Map Load: (.+): Map Group";
            var matches = Regex.Match(output, regex);
            if (!matches.Success || matches.Groups.Count != 2) return;

            var newMap = matches.Groups[1].ToString().Trim();
            _logger.LogInformation("Map changed to {NewMap}", newMap);
            OnMapChanged(newMap);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to detect map change");
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

            _logger.LogWarning("New player connected. Player name: {PlayerName} | Player IP: {PlayerIp}", playerMame,
                ipPort);
            OnPlayerConnected(playerMame, ipPort);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to detect player connected");
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

            _logger.LogWarning("Player disconnected: {PlayerName} |{DisconnectReason}", playerName, disconnectReason);
            OnPlayerDisconnected(playerName, disconnectReason);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to detect player disconnect");
        }
    }

    #endregion

    #endregion

    public void AddHandlerToAllCommonEvents(EventHandler<CustomEventArg> handler)
    {
        StartingServer += handler;
        StartingServerDone += handler;
        StartingServerFailed += handler;
        StoppingServer += handler;
        ServerExited += handler;
        UpdateOrInstallStarted += handler;
        UpdateOrInstallDone += handler;
        UpdateOrInstallCancelled += handler;
        UpdateOrInstallFailed += handler;
        UploadDemoStarted += handler;
        UploadDemoDone += handler;
        UploadDemoFailed += handler;
        HibernationStarted += handler;
        HibernationEnded += handler;
        MapChanged += (sender, eventArg) => { handler(sender, eventArg); };
        PlayerConnected += (sender, eventArg) => { handler(sender, eventArg); };
        PlayerDisconnected += (sender, eventArg) => { handler(sender, eventArg); };
        PlayerCountChanged += (sender, eventArg) => { handler(sender, eventArg); };
    }

    private void LogEventTriggered(object? _, CustomEventArg arg)
    {
        _logger.LogInformation("Event \"{EventName}\" triggered. EventArg: \"{Arg}\"", arg.EventName, arg.ToString());
    }

    #region StartStop

    #region StartingServer

    public event EventHandler<CustomEventArg>? StartingServer;

    public void OnStartingServer()
    {
        StartingServer?.Invoke(this, new CustomEventArg(nameof(StartingServer)));
    }

    #endregion StartingServer

    #region StartingServerDone

    public event EventHandler<CustomEventArg>? StartingServerDone;

    public void OnStartingServerDone()
    {
        StartingServerDone?.Invoke(this, new CustomEventArg(nameof(StartingServerDone)));
    }

    #endregion StartingServerDone

    #region StartingServerFailed

    public event EventHandler<CustomEventArg>? StartingServerFailed;

    public void OnStartingServerFailed()
    {
        StartingServerFailed?.Invoke(this, new CustomEventArg(nameof(StartingServerFailed)));
    }

    #endregion StartingServerFailed


    #region StoppingServer

    public event EventHandler<CustomEventArg>? StoppingServer;

    public void OnStoppingServer()
    {
        StoppingServer?.Invoke(this, new CustomEventArg(nameof(StoppingServer)));
    }

    #endregion StoppingServer

    #region ServerExited

    public event EventHandler<CustomEventArg>? ServerExited;

    public void OnServerExited()
    {
        ServerExited?.Invoke(this, new CustomEventArg(nameof(ServerExited)));
    }

    #endregion ServerExited

    #endregion StartStop

    #region UpdateOrInstall

    #region UpdateOrInstallStarted

    public event EventHandler<CustomEventArg>? UpdateOrInstallStarted;

    public void OnUpdateOrInstallStarted()
    {
        UpdateOrInstallStarted?.Invoke(this, new CustomEventArg(nameof(UpdateOrInstallStarted)));
    }

    #endregion

    #region UpdateOrInstallDone

    public event EventHandler<CustomEventArg>? UpdateOrInstallDone;

    public void OnUpdateOrInstallDone()
    {
        UpdateOrInstallDone?.Invoke(this, new CustomEventArg(nameof(UpdateOrInstallDone)));
    }

    #endregion

    #region UpdateOrInstallCancelled

    public event EventHandler<CustomEventArg>? UpdateOrInstallCancelled;

    public void OnUpdateOrInstallCancelled()
    {
        UpdateOrInstallCancelled?.Invoke(this, new CustomEventArg(nameof(UpdateOrInstallCancelled)));
    }

    #endregion

    #region UpdateOrInstallFailed

    public event EventHandler<CustomEventArg>? UpdateOrInstallFailed;

    public void OnUpdateOrInstallFailed()
    {
        UpdateOrInstallFailed?.Invoke(this, new CustomEventArg(nameof(UpdateOrInstallFailed)));
    }

    #endregion

    #endregion UpdateOrInstall

    #region UploadDemo

    #region UploadDemoStarted

    public event EventHandler<CustomEventArg>? UploadDemoStarted;

    public void OnUploadDemoStarted(string demo)
    {
        UploadDemoStarted?.Invoke(this, new CustomEventArgDemoName(nameof(UploadDemoStarted), demo));
    }

    #endregion

    #region UploadDemoDone

    public event EventHandler<CustomEventArg>? UploadDemoDone;

    public void OnUploadDemoDone(string demo)
    {
        UploadDemoDone?.Invoke(this, new CustomEventArgDemoName(nameof(UploadDemoDone), demo));
    }

    #endregion

    #region UploadDemoFailed

    public event EventHandler<CustomEventArg>? UploadDemoFailed;

    public void OnUploadDemoFailed(string demo)
    {
        UploadDemoFailed?.Invoke(this, new CustomEventArgDemoName(nameof(UploadDemoFailed), demo));
    }

    #endregion

    #endregion UploadDemo

    #region Hibernation

    #region HibernationStarted

    public event EventHandler<CustomEventArg>? HibernationStarted;

    private void OnHibernationStarted()
    {
        HibernationStarted?.Invoke(this, new CustomEventArg(nameof(HibernationStarted)));
    }

    #endregion HibernationStarted

    #region HibernationEnded

    public event EventHandler<CustomEventArg>? HibernationEnded;

    private void OnHibernationEnded()
    {
        HibernationEnded?.Invoke(this, new CustomEventArg(nameof(HibernationEnded)));
    }

    #endregion HibernationEnded

    #endregion Hibernation

    #region MapChanged

    public event EventHandler<CustomEventArgMapChanged>? MapChanged;

    public void OnMapChanged(string mapName)
    {
        MapChanged?.Invoke(this, new CustomEventArgMapChanged(nameof(MapChanged), mapName));
    }

    #endregion MapChanged

    #region Player

    #region PlayerConnected

    public event EventHandler<CustomEventArgPlayerConnected>? PlayerConnected;

    protected void OnPlayerConnected(string playerName, string playerIp)
    {
        PlayerConnected?.Invoke(this, new CustomEventArgPlayerConnected(nameof(PlayerConnected), playerName, playerIp));
    }

    #endregion PlayerConnected

    #region PlayerDisconnected

    public event EventHandler<CustomEventArgPlayerDisconnected>? PlayerDisconnected;

    protected void OnPlayerDisconnected(string playerName, string disconnectReason)
    {
        PlayerDisconnected?.Invoke(this,
            new CustomEventArgPlayerDisconnected(nameof(PlayerDisconnected), playerName, disconnectReason));
    }

    #endregion PlayerConnected

    #region PlayerCountChanged

    public event EventHandler<CustomEventArgPlayerCountChanged>? PlayerCountChanged;

    public void OnPlayerCountChanged(int playerCount)
    {
        PlayerCountChanged?.Invoke(this, new CustomEventArgPlayerCountChanged(nameof(PlayerCountChanged), playerCount));
    }

    #endregion PlayerCountChanged

    #endregion Player
}