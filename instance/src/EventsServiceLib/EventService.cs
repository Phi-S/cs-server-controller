using DatabaseLib.Repos;
using EventsServiceLib.EventArgs;
using Microsoft.Extensions.Logging;
using SharedModelsLib.ApiModels;

namespace EventsServiceLib;

public enum Events
{
    STARTING_SERVER,
    STARTING_SERVER_DONE,
    STARTING_SERVER_FAILED,
    STOPPING_SERVER,
    SERVER_EXITED,

    UPDATE_OR_INSTALL_STARTED,
    UPDATE_OR_INSTALL_DONE,
    UPDATE_OR_INSTALL_CANCELLED,
    UPDATE_OR_INSTALL_FAILED,

    UPLOAD_DEMO_STARTED,
    UPLOAD_DEMO_DONE,
    UPLOAD_DEMO_FAILED,

    HIBERNATION_STARTED,
    HIBERNATION_ENDED,
    MAP_CHANGED,
    PLAYER_CONNECTED,
    PLAYER_DISCONNECTED,
    PLAYER_COUNT_CHANGED,
    CHAT_MESSAGE
}

public sealed class EventService
{
    private readonly ILogger<EventService> _logger;
    private readonly EventLogRepo _eventLogRepo;

    public EventService(ILogger<EventService> logger, EventLogRepo eventLogRepo)
    {
        _logger = logger;
        _eventLogRepo = eventLogRepo;

        AddHandlerToAllCommonEvents(LogEventTriggered);
        AddHandlerToAllCommonEvents(DatabaseEventTriggered);
    }

    private void AddHandlerToAllCommonEvents(EventHandler<CustomEventArg> handler)
    {
        StartingServer += handler;
        StartingServerDone += (sender, eventArg) => { handler(sender, eventArg); };
        StartingServerFailed += handler;
        StoppingServer += handler;
        ServerExited += handler;
        UpdateOrInstallStarted += (sender, eventArg) => { handler(sender, eventArg); };
        UpdateOrInstallDone += (sender, eventArg) => { handler(sender, eventArg); };
        UpdateOrInstallCancelled += (sender, eventArg) => { handler(sender, eventArg); };
        UpdateOrInstallFailed += (sender, eventArg) => { handler(sender, eventArg); };
        UploadDemoStarted += handler;
        UploadDemoDone += handler;
        UploadDemoFailed += handler;
        HibernationStarted += handler;
        HibernationEnded += handler;
        MapChanged += (sender, eventArg) => { handler(sender, eventArg); };
        PlayerConnected += (sender, eventArg) => { handler(sender, eventArg); };
        PlayerDisconnected += (sender, eventArg) => { handler(sender, eventArg); };
        PlayerCountChanged += (sender, eventArg) => { handler(sender, eventArg); };
        ChatMessage += (sender, eventArg) => { handler(sender, eventArg); };
    }

    private void LogEventTriggered(object? _, CustomEventArg arg)
    {
        _logger.LogInformation("Event \"{EventName}\" triggered. EventArg: \"{Arg}\"", arg.EventName, arg.ToString());
    }

    private async void DatabaseEventTriggered(object? _, CustomEventArg arg)
    {
        try
        {
            await _eventLogRepo.Add(arg.EventName.ToString(), arg.TriggeredAtUtc, arg.GetDataJson());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to add event \"{Event}\" to database", arg);
        }
    }

    #region StartStop

    #region StartingServer

    public event EventHandler<CustomEventArg>? StartingServer;

    public void OnStartingServer()
    {
        StartingServer?.Invoke(this, new CustomEventArg(Events.STARTING_SERVER));
    }

    #endregion StartingServer

    #region StartingServerDone

    public event EventHandler<CustomEventArgStartingServerDone>? StartingServerDone;

    public void OnStartingServerDone(StartParameters startParameters)
    {
        StartingServerDone?.Invoke(this,
            new CustomEventArgStartingServerDone(Events.STARTING_SERVER_DONE, startParameters));
    }

    #endregion StartingServerDone

    #region StartingServerFailed

    public event EventHandler<CustomEventArg>? StartingServerFailed;

    public void OnStartingServerFailed()
    {
        StartingServerFailed?.Invoke(this, new CustomEventArg(Events.STARTING_SERVER_FAILED));
    }

    #endregion StartingServerFailed

    #region StoppingServer

    public event EventHandler<CustomEventArg>? StoppingServer;

    public void OnStoppingServer()
    {
        StoppingServer?.Invoke(this, new CustomEventArg(Events.STOPPING_SERVER));
    }

    #endregion StoppingServer

    #region ServerExited

    public event EventHandler<CustomEventArg>? ServerExited;

    public void OnServerExited()
    {
        ServerExited?.Invoke(this, new CustomEventArg(Events.SERVER_EXITED));
    }

    #endregion ServerExited

    #endregion StartStop

    #region UpdateOrInstall

    #region UpdateOrInstallStarted

    public event EventHandler<CustomEventArgUpdateOrInstall>? UpdateOrInstallStarted;

    public void OnUpdateOrInstallStarted(Guid id)
    {
        UpdateOrInstallStarted?.Invoke(this, new CustomEventArgUpdateOrInstall(Events.UPDATE_OR_INSTALL_STARTED, id));
    }

    #endregion

    #region UpdateOrInstallDone

    public event EventHandler<CustomEventArgUpdateOrInstall>? UpdateOrInstallDone;

    public void OnUpdateOrInstallDone(Guid id)
    {
        UpdateOrInstallDone?.Invoke(this, new CustomEventArgUpdateOrInstall(Events.UPDATE_OR_INSTALL_DONE, id));
    }

    #endregion

    #region UpdateOrInstallCancelled

    public event EventHandler<CustomEventArgUpdateOrInstall>? UpdateOrInstallCancelled;

    public void OnUpdateOrInstallCancelled(Guid id)
    {
        UpdateOrInstallCancelled?.Invoke(this,
            new CustomEventArgUpdateOrInstall(Events.UPDATE_OR_INSTALL_CANCELLED, id));
    }

    #endregion

    #region UpdateOrInstallFailed

    public event EventHandler<CustomEventArgUpdateOrInstall>? UpdateOrInstallFailed;

    public void OnUpdateOrInstallFailed(Guid id)
    {
        UpdateOrInstallFailed?.Invoke(this, new CustomEventArgUpdateOrInstall(Events.UPDATE_OR_INSTALL_FAILED, id));
    }

    #endregion

    #endregion UpdateOrInstall

    #region UploadDemo

    #region UploadDemoStarted

    public event EventHandler<CustomEventArg>? UploadDemoStarted;

    public void OnUploadDemoStarted(string demo)
    {
        UploadDemoStarted?.Invoke(this, new CustomEventArgDemoName(Events.UPLOAD_DEMO_STARTED, demo));
    }

    #endregion

    #region UploadDemoDone

    public event EventHandler<CustomEventArg>? UploadDemoDone;

    public void OnUploadDemoDone(string demo)
    {
        UploadDemoDone?.Invoke(this, new CustomEventArgDemoName(Events.UPLOAD_DEMO_DONE, demo));
    }

    #endregion

    #region UploadDemoFailed

    public event EventHandler<CustomEventArg>? UploadDemoFailed;

    public void OnUploadDemoFailed(string demo)
    {
        UploadDemoFailed?.Invoke(this, new CustomEventArgDemoName(Events.UPLOAD_DEMO_FAILED, demo));
    }

    #endregion

    #endregion UploadDemo

    #region Hibernation

    #region HibernationStarted

    public event EventHandler<CustomEventArg>? HibernationStarted;

    public void OnHibernationStarted()
    {
        HibernationStarted?.Invoke(this, new CustomEventArg(Events.HIBERNATION_STARTED));
    }

    #endregion HibernationStarted

    #region HibernationEnded

    public event EventHandler<CustomEventArg>? HibernationEnded;

    public void OnHibernationEnded()
    {
        HibernationEnded?.Invoke(this, new CustomEventArg(Events.HIBERNATION_ENDED));
    }

    #endregion HibernationEnded

    #endregion Hibernation

    #region MapChanged

    public event EventHandler<CustomEventArgMapChanged>? MapChanged;

    public void OnMapChanged(string mapName)
    {
        MapChanged?.Invoke(this, new CustomEventArgMapChanged(Events.MAP_CHANGED, mapName));
    }

    #endregion MapChanged

    #region Player

    #region PlayerConnected

    public event EventHandler<CustomEventArgPlayerConnected>? PlayerConnected;

    public void OnPlayerConnected(string playerName, string playerIp)
    {
        PlayerConnected?.Invoke(this, new CustomEventArgPlayerConnected(Events.PLAYER_CONNECTED, playerName, playerIp));
    }

    #endregion PlayerConnected

    #region PlayerDisconnected

    public event EventHandler<CustomEventArgPlayerDisconnected>? PlayerDisconnected;

    public void OnPlayerDisconnected(
        string connectionId,
        string steamId64,
        string ipPort,
        string disconnectReasonCode,
        string disconnectReason)
    {
        PlayerDisconnected?.Invoke(this,
            new CustomEventArgPlayerDisconnected(Events.PLAYER_DISCONNECTED,
                connectionId,
                steamId64,
                ipPort,
                disconnectReasonCode,
                disconnectReason));
    }

    #endregion PlayerConnected

    #region PlayerCountChanged

    public event EventHandler<CustomEventArgPlayerCountChanged>? PlayerCountChanged;

    public void OnPlayerCountChanged(int playerCount)
    {
        PlayerCountChanged?.Invoke(this,
            new CustomEventArgPlayerCountChanged(Events.PLAYER_COUNT_CHANGED, playerCount));
    }

    #endregion PlayerCountChanged

    #endregion Player

    #region Chat

    public event EventHandler<CustomEventArgChatMessage>? ChatMessage;

    public void OnChatMessage(string chat, string playerName, string steamId3, string message)
    {
        ChatMessage?.Invoke(this,
            new CustomEventArgChatMessage(Events.CHAT_MESSAGE, chat, playerName, steamId3, message));
    }

    #endregion
}