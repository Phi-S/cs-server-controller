using Application.EventServiceFolder.EventArgs;
using Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.ApiModels;

namespace Application.EventServiceFolder;

public enum Events
{
    StartingServer,
    StartingServerDone,
    StartingServerFailed,
    StoppingServer,
    ServerExited,

    UpdateOrInstallStarted,
    UpdateOrInstallDone,
    UpdateOrInstallCancelled,
    UpdateOrInstallFailed,

    UploadDemoStarted,
    UploadDemoDone,
    UploadDemoFailed,

    HibernationStarted,
    HibernationEnded,
    MapChanged,
    PlayerConnected,
    PlayerDisconnected,
    PlayerCountChanged,
    ChatMessage
}

public sealed class EventService
{
    private readonly ILogger<EventService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public EventService(ILogger<EventService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        AddHandlerToAllCommonEvents(LogEventTriggered);
        AddHandlerToAllCommonEvents(DatabaseEventTriggered);
        AddHandlerToAllCommonEvents((sender, arg) => { OnEvent?.Invoke(sender, arg); });
    }

    public event EventHandler<CustomEventArg>? OnEvent;

    public void AddHandlerToAllCommonEvents(EventHandler<CustomEventArg> handler)
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
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.GetUnitOfWork();
            await unitOfWork.EventLogRepo.Add(arg.EventName.ToString(), arg.TriggeredAtUtc, arg.GetDataJson());
            await unitOfWork.Save();
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
        StartingServer?.Invoke(this, new CustomEventArg(Events.StartingServer));
    }

    #endregion StartingServer

    #region StartingServerDone

    public event EventHandler<CustomEventArgStartingServerDone>? StartingServerDone;

    public void OnStartingServerDone(StartParameters startParameters)
    {
        StartingServerDone?.Invoke(this,
            new CustomEventArgStartingServerDone(Events.StartingServerDone, startParameters));
    }

    #endregion StartingServerDone

    #region StartingServerFailed

    public event EventHandler<CustomEventArg>? StartingServerFailed;

    public void OnStartingServerFailed()
    {
        StartingServerFailed?.Invoke(this, new CustomEventArg(Events.StartingServerFailed));
    }

    #endregion StartingServerFailed

    #region StoppingServer

    public event EventHandler<CustomEventArg>? StoppingServer;

    public void OnStoppingServer()
    {
        StoppingServer?.Invoke(this, new CustomEventArg(Events.StoppingServer));
    }

    #endregion StoppingServer

    #region ServerExited

    public event EventHandler<CustomEventArg>? ServerExited;

    public void OnServerExited()
    {
        ServerExited?.Invoke(this, new CustomEventArg(Events.ServerExited));
    }

    #endregion ServerExited

    #endregion StartStop

    #region UpdateOrInstall

    #region UpdateOrInstallStarted

    public event EventHandler<CustomEventArgUpdateOrInstall>? UpdateOrInstallStarted;

    public void OnUpdateOrInstallStarted(Guid id)
    {
        UpdateOrInstallStarted?.Invoke(this, new CustomEventArgUpdateOrInstall(Events.UpdateOrInstallStarted, id));
    }

    #endregion

    #region UpdateOrInstallDone

    public event EventHandler<CustomEventArgUpdateOrInstall>? UpdateOrInstallDone;

    public void OnUpdateOrInstallDone(Guid id)
    {
        UpdateOrInstallDone?.Invoke(this, new CustomEventArgUpdateOrInstall(Events.UpdateOrInstallDone, id));
    }

    #endregion

    #region UpdateOrInstallCancelled

    public event EventHandler<CustomEventArgUpdateOrInstall>? UpdateOrInstallCancelled;

    public void OnUpdateOrInstallCancelled(Guid id)
    {
        UpdateOrInstallCancelled?.Invoke(this,
            new CustomEventArgUpdateOrInstall(Events.UpdateOrInstallCancelled, id));
    }

    #endregion

    #region UpdateOrInstallFailed

    public event EventHandler<CustomEventArgUpdateOrInstall>? UpdateOrInstallFailed;

    public void OnUpdateOrInstallFailed(Guid id)
    {
        UpdateOrInstallFailed?.Invoke(this, new CustomEventArgUpdateOrInstall(Events.UpdateOrInstallFailed, id));
    }

    #endregion

    #endregion UpdateOrInstall

    #region UploadDemo

    #region UploadDemoStarted

    public event EventHandler<CustomEventArg>? UploadDemoStarted;

    public void OnUploadDemoStarted(string demo)
    {
        UploadDemoStarted?.Invoke(this, new CustomEventArgDemoName(Events.UploadDemoStarted, demo));
    }

    #endregion

    #region UploadDemoDone

    public event EventHandler<CustomEventArg>? UploadDemoDone;

    public void OnUploadDemoDone(string demo)
    {
        UploadDemoDone?.Invoke(this, new CustomEventArgDemoName(Events.UploadDemoDone, demo));
    }

    #endregion

    #region UploadDemoFailed

    public event EventHandler<CustomEventArg>? UploadDemoFailed;

    public void OnUploadDemoFailed(string demo)
    {
        UploadDemoFailed?.Invoke(this, new CustomEventArgDemoName(Events.UploadDemoFailed, demo));
    }

    #endregion

    #endregion UploadDemo

    #region Hibernation

    #region HibernationStarted

    public event EventHandler<CustomEventArg>? HibernationStarted;

    public void OnHibernationStarted()
    {
        HibernationStarted?.Invoke(this, new CustomEventArg(Events.HibernationStarted));
    }

    #endregion HibernationStarted

    #region HibernationEnded

    public event EventHandler<CustomEventArg>? HibernationEnded;

    public void OnHibernationEnded()
    {
        HibernationEnded?.Invoke(this, new CustomEventArg(Events.HibernationEnded));
    }

    #endregion HibernationEnded

    #endregion Hibernation

    #region MapChanged

    public event EventHandler<CustomEventArgMapChanged>? MapChanged;

    public void OnMapChanged(string mapName)
    {
        MapChanged?.Invoke(this, new CustomEventArgMapChanged(Events.MapChanged, mapName));
    }

    #endregion MapChanged

    #region Player

    #region PlayerConnected

    public event EventHandler<CustomEventArgPlayerConnected>? PlayerConnected;

    public void OnPlayerConnected(string playerName, string playerIp)
    {
        PlayerConnected?.Invoke(this, new CustomEventArgPlayerConnected(Events.PlayerConnected, playerName, playerIp));
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
            new CustomEventArgPlayerDisconnected(Events.PlayerDisconnected,
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
            new CustomEventArgPlayerCountChanged(Events.PlayerCountChanged, playerCount));
    }

    #endregion PlayerCountChanged

    #endregion Player

    #region Chat

    public event EventHandler<CustomEventArgChatMessage>? ChatMessage;

    public void OnChatMessage(string chat, string playerName, string steamId3, string message)
    {
        ChatMessage?.Invoke(this,
            new CustomEventArgChatMessage(Events.ChatMessage, chat, playerName, steamId3, message));
    }

    #endregion
}