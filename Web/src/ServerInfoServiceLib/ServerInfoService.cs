using InstanceApiServiceLib;
using Microsoft.Extensions.Logging;
using SharedModelsLib.ApiModels;

namespace ServerInfoServiceLib;

public class ServerInfoService(ILogger<ServerInfoService> logger, InstanceApiService instanceApiService)
{
    #region ServerInfo

    private readonly object _serverInfoLock = new();
    private InfoModel? _serverInfo;
    public event EventHandler? OnServerInfoChangedEvent;

    public CancellationTokenSource? ServerInfoBackgroundTaskCancellationTokenSource;
    private volatile bool _serverInfoBackgroundTaskRunning;

    public InfoModel? ServerInfo
    {
        get
        {
            lock (_serverInfoLock)
            {
                return _serverInfo;
            }
        }
        private set
        {
            lock (_serverInfoLock)
            {
                _serverInfo = value;
            }

            OnServerInfoChangedEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public void StartServerInfoBackgroundTask()
    {
        if (_serverInfoBackgroundTaskRunning)
        {
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("ServerInfoBackgroundTask started");
                _serverInfoBackgroundTaskRunning = true;
                ServerInfoBackgroundTaskCancellationTokenSource = new CancellationTokenSource();

                while (ServerInfoBackgroundTaskCancellationTokenSource.IsCancellationRequested == false)
                {
                    try
                    {
                        var newServerInfo = await instanceApiService.Info();
                        if (ServerInfo is not null && ServerInfo.Equals(newServerInfo))
                        {
                            continue;
                        }

                        // If the server first started or the server went from offline to online; refresh the available maps
                        if (ServerInfo is null || (ServerInfo.ServerStarted == false && newServerInfo.ServerStarted))
                        {
                            Maps = await instanceApiService.Maps();
                            Configs = await instanceApiService.Configs();
                        }

                        ServerInfo = newServerInfo;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "exception is ServerInfoBackgroundTask");
                    }
                    finally
                    {
                        await Task.Delay(1000, ServerInfoBackgroundTaskCancellationTokenSource.Token);
                    }
                }
            }
            finally
            {
                _serverInfoBackgroundTaskRunning = false;
                logger.LogInformation("ServerInfoBackgroundTask stopped");
            }
        });
    }

    #endregion

    #region Events

    private readonly object _eventsLock = new();
    private List<EventLogResponse>? _events;
    public event EventHandler? OnEventsChangedEvent;

    private volatile bool _eventsBackgroundTaskRunning;
    public CancellationTokenSource? EventBackgroundTaskCancellationTokenSource;

    public List<EventLogResponse>? Events
    {
        get
        {
            lock (_eventsLock)
            {
                return _events;
            }
        }
        private set
        {
            {
                lock (_eventsLock)
                {
                    _events = value;
                }

                OnEventsChangedEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void StartEventsBackgroundTask()
    {
        if (_eventsBackgroundTaskRunning)
        {
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("EventsBackgroundTask started");
                _eventsBackgroundTaskRunning = true;
                EventBackgroundTaskCancellationTokenSource = new CancellationTokenSource();
                var lastEventsPullTimestamp = DateTime.UtcNow.Subtract(TimeSpan.FromHours(24));
                while (EventBackgroundTaskCancellationTokenSource.IsCancellationRequested == false)
                {
                    try
                    {
                        var events = await instanceApiService.LogsEvents(lastEventsPullTimestamp);
                        lastEventsPullTimestamp = DateTime.UtcNow;
                        var currentEvents = Events;
                        if (currentEvents == null)
                        {
                            Events = events.OrderByDescending(e => e.TriggeredAtUtc).ToList();
                            continue;
                        }

                        var newEvents = new List<EventLogResponse>();
                        foreach (var e in events)
                        {
                            if (currentEvents.Any(currentEvent => currentEvent.Equals(e)) == false)
                            {
                                newEvents.Add(e);
                            }
                        }

                        if (newEvents.Count != 0 == false)
                        {
                            continue;
                        }

                        currentEvents.AddRange(newEvents);
                        currentEvents = currentEvents.OrderByDescending(e => e.TriggeredAtUtc).ToList();
                        Events = currentEvents;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "exception in EventsBackgroundTask");
                    }
                    finally
                    {
                        await Task.Delay(1000, EventBackgroundTaskCancellationTokenSource.Token);
                    }
                }
            }
            finally
            {
                _eventsBackgroundTaskRunning = false;
                logger.LogInformation("EventsBackgroundTask started");
            }
        });
    }

    #endregion

    #region ServerLogs

    private readonly object _serverLogsLock = new();
    private List<ServerLogResponse>? _serverLogs;
    public event EventHandler? OnServerLogsChangedEvent;

    private volatile bool _serverLogsBackgroundTaskRunning;
    public CancellationTokenSource? ServerLogsBackgroundTaskCancellationTokenSource;

    public List<ServerLogResponse>? ServerLogs
    {
        get
        {
            lock (_serverLogsLock)
            {
                return _serverLogs;
            }
        }
        private set
        {
            {
                lock (_serverLogsLock)
                {
                    _serverLogs = value;
                }

                OnServerLogsChangedEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void StartServerLogsBackgroundTask()
    {
        if (_serverLogsBackgroundTaskRunning)
        {
            return;
        }


        Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("ServerLogsBackgroundTask started");
                _serverLogsBackgroundTaskRunning = true;
                ServerLogsBackgroundTaskCancellationTokenSource = new CancellationTokenSource();
                var lastServerLogsPullTimestamp = DateTime.UtcNow.Subtract(TimeSpan.FromHours(24));
                while (ServerLogsBackgroundTaskCancellationTokenSource.IsCancellationRequested == false)
                {
                    try
                    {
                        var newLogsFromInstance = await instanceApiService.LogsServer(lastServerLogsPullTimestamp);
                        lastServerLogsPullTimestamp = DateTime.UtcNow;

                        if (newLogsFromInstance.Count == 0)
                        {
                            continue;
                        }

                        var currentLogs = ServerLogs;
                        if (currentLogs == null)
                        {
                            ServerLogs = newLogsFromInstance;
                            continue;
                        }

                        var newServerLogs = new List<ServerLogResponse>();
                        foreach (var log in newLogsFromInstance)
                        {
                            if (currentLogs.Any(currentLog => currentLog.Equals(log)) == false)
                            {
                                newServerLogs.Add(log);
                            }
                        }

                        currentLogs.AddRange(newServerLogs);
                        currentLogs = currentLogs.OrderByDescending(e => e.MessageReceivedAtUt).ToList();
                        ServerLogs = currentLogs;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "exception in ServerLogsBackgroundTask");
                    }
                    finally
                    {
                        await Task.Delay(1000, ServerLogsBackgroundTaskCancellationTokenSource.Token);
                    }
                }
            }
            finally
            {
                _serverLogsBackgroundTaskRunning = false;
                logger.LogInformation("ServerLogsBackgroundTask stopped");
            }
        });
    }

    #endregion

    #region UpdateOrInstallLogs

    private readonly object _updateOrInstallLogsLock = new();
    private List<UpdateOrInstallLogResponse>? _updateOrInstallLogs;
    public event EventHandler? OnUpdateOrInstallLogsChangedEvent;


    private volatile bool _updateOrInstallLogsBackgroundTaskRunning;
    public CancellationTokenSource? UpdateOrInstallLogsBackgroundTaskCancellationTokenSource;

    public List<UpdateOrInstallLogResponse>? UpdateOrInstallLogs
    {
        get
        {
            lock (_updateOrInstallLogsLock)
            {
                return _updateOrInstallLogs;
            }
        }
        private set
        {
            {
                lock (_updateOrInstallLogsLock)
                {
                    _updateOrInstallLogs = value;
                }

                OnUpdateOrInstallLogsChangedEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void StartUpdateOrInstallLogsBackgroundTask()
    {
        if (_updateOrInstallLogsBackgroundTaskRunning)
        {
            return;
        }


        Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("UpdateOrInstallLogsBackgroundTask started");
                _updateOrInstallLogsBackgroundTaskRunning = true;
                UpdateOrInstallLogsBackgroundTaskCancellationTokenSource = new CancellationTokenSource();
                var lastLogsPullTimestamp = DateTime.UtcNow.Subtract(TimeSpan.FromHours(24));
                while (UpdateOrInstallLogsBackgroundTaskCancellationTokenSource.IsCancellationRequested == false)
                {
                    try
                    {
                        var logs = await instanceApiService.LogsUpdateOrInstall(lastLogsPullTimestamp);
                        lastLogsPullTimestamp = DateTime.UtcNow;
                        var currentLogs = UpdateOrInstallLogs;
                        if (currentLogs == null)
                        {
                            UpdateOrInstallLogs = logs;
                            continue;
                        }

                        var newServerLogs = new List<UpdateOrInstallLogResponse>();
                        foreach (var e in logs)
                        {
                            if (currentLogs.Any(currentEvent => currentEvent.Equals(e)) == false)
                            {
                                newServerLogs.Add(e);
                            }
                        }

                        currentLogs.AddRange(newServerLogs);
                        currentLogs = currentLogs.OrderByDescending(e => e.MessageReceivedAtUt).ToList();
                        UpdateOrInstallLogs = currentLogs;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "exception in UpdateOrInstallLogsBackgroundTask");
                    }
                    finally
                    {
                        await Task.Delay(1000, UpdateOrInstallLogsBackgroundTaskCancellationTokenSource.Token);
                    }
                }
            }
            finally
            {
                _updateOrInstallLogsBackgroundTaskRunning = false;
                logger.LogInformation("UpdateOrInstallLogsBackgroundTask stopped");
            }
        });
    }

    #endregion

    #region Maps

    private readonly List<string> _maps = new();
    private readonly object _mapsLock = new();

    public List<string> Maps
    {
        get
        {
            lock (_mapsLock)
            {
                return _maps;
            }
        }
        private set
        {
            if (value.Count <= 0) return;
            lock (_mapsLock)
            {
                _maps.Clear();
                _maps.AddRange(value);
            }
        }
    }

    #endregion

    #region Configs

    private readonly List<string> _configs = new();
    private readonly object _configsLock = new();

    public List<string> Configs
    {
        get
        {
            lock (_configsLock)
            {
                return _configs;
            }
        }
        private set
        {
            if (value.Count <= 0) return;
            lock (_configsLock)
            {
                _configs.Clear();
                _configs.AddRange(value);
            }
        }
    }

    #endregion
}