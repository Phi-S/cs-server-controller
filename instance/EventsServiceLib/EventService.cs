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
    }

    #region NewOutput

    public event EventHandler<string>? NewOutput;

    private void NewOutputRegisterHandlers()
    {
        NewOutput += OnNewOutputPrintLog;
        NewOutput += OnNewOutputWriteToDatabase;
    }

    public void OnNewOutput(string? output)
    {
        if (!string.IsNullOrWhiteSpace(output))
        {
            NewOutput?.Invoke(this, output);
        }
    }

    private void OnNewOutputPrintLog(object? _, string output)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger.LogInformation("CS: {InstanceLog}", output);
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

    #endregion

    private void AddHandlerToAllCommonEvents(EventHandler<CustomEventArg> handler)
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

    public void OnHibernationStarted()
    {
        HibernationStarted?.Invoke(this, new CustomEventArg(nameof(HibernationStarted)));
    }

    #endregion HibernationStarted

    #region HibernationEnded

    public event EventHandler<CustomEventArg>? HibernationEnded;

    public void OnHibernationEnded()
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

    public void OnPlayerConnected(string playerName, string playerIp)
    {
        PlayerConnected?.Invoke(this, new CustomEventArgPlayerConnected(nameof(PlayerConnected), playerName, playerIp));
    }

    #endregion PlayerConnected

    #region PlayerDisconnected

    public event EventHandler<CustomEventArgPlayerDisconnected>? PlayerDisconnected;

    public void OnPlayerDisconnected(string playerName, string disconnectReason)
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