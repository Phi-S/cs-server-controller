using ErrorOr;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Shared;
using Shared.ApiModels;
using Shared.SignalR;
using Web.Options;

namespace Web.Services;

public record LogEntry(DateTime TimestampUtc, string Message);

public class ServerInfoService
{
    private readonly ILogger<ServerInfoService> _logger;
    private readonly IOptions<AppOptions> _options;
    private readonly InstanceApiService _instanceApiService;

    public ServerInfoService(
        ILogger<ServerInfoService> logger,
        IOptions<AppOptions> options,
        InstanceApiService instanceApiService)
    {
        _logger = logger;
        _options = options;
        _instanceApiService = instanceApiService;
    }

    public async Task<ErrorOr<Success>> StartSignalRConnection()
    {
        try
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(new Uri($"{_options.Value.INSTANCE_API_ENDPOINT}/hub"))
                .WithKeepAliveInterval(TimeSpan.FromSeconds(1))
                .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(10)])
                .Build();
            await connection.StartAsync();
            var addSignalrRHandler = await AddSignalrRHandler(connection);
            if (addSignalrRHandler.IsError)
            {
                return addSignalrRHandler.FirstError;
            }

            connection.Reconnected += async _ =>
            {
                _logger.LogInformation("Reconnected to signalr hub");
                var addSignalrRHandlerReconnected = await AddSignalrRHandler(connection);
                if (addSignalrRHandlerReconnected.IsError)
                {
                    _logger.LogError("Failed to add signalRHandlers after reconnect. {Error}",
                        addSignalrRHandlerReconnected.ErrorMessage());
                    return;
                }

                _logger.LogInformation("Successfully reconnected");
            };

            return Result.Success;
        }
        catch (Exception e)
        {
            return Errors.Fail($"Exception: {e.Message}");
        }
    }

    private async Task<ErrorOr<Success>> AddSignalrRHandler(HubConnection connection)
    {
        var last24Hours = DateTime.UtcNow.Subtract(TimeSpan.FromHours(24));
        var serverInfo = await _instanceApiService.Info();
        if (serverInfo.IsError)
        {
            return serverInfo.FirstError;
        }

        var serverLogs = await _instanceApiService.LogsServer(last24Hours);
        if (serverLogs.IsError)
        {
            return serverLogs.FirstError;
        }

        var events = await _instanceApiService.LogsEvents(last24Hours);
        if (events.IsError)
        {
            return events.FirstError;
        }

        var updateOrInstallLogs = await _instanceApiService.LogsUpdateOrInstall(last24Hours);
        if (updateOrInstallLogs.IsError)
        {
            return updateOrInstallLogs.FirstError;
        }

        var maps = await _instanceApiService.Maps();
        if (maps.IsError)
        {
            return maps.FirstError;
        }

        ServerInfo = serverInfo.Value;
        Events = events.Value;
        ServerLogs = serverLogs.Value;
        UpdateOrInstallLogs = updateOrInstallLogs.Value;
        var allLogs = serverLogs.Value.Select(l => new LogEntry(l.MessageReceivedAtUt, l.Message)).ToList();
        allLogs.AddRange(updateOrInstallLogs.Value.Select(l => new LogEntry(l.MessageReceivedAtUt, l.Message)));
        AllLogs = allLogs;
        Maps = maps.Value;

        connection.Remove(SignalRMethods.ServerStatusMethod);
        connection.Remove(SignalRMethods.ServerLogMethod);
        connection.Remove(SignalRMethods.EventMethod);
        connection.Remove(SignalRMethods.UpdateOrInstallLogMethod);

        connection.OnServerStatus(async response =>
        {
            // If the server first started or the server went from offline to online; refresh the available maps
            if (ServerInfo is null ||
                (ServerInfo.ServerStarted == false && response.ServerStarted))
            {
                var mapsOnServerStart = await _instanceApiService.Maps();
                if (mapsOnServerStart.IsError)
                {
                    _logger.LogError("Failed to get maps after server start. {Error}",
                        mapsOnServerStart.ErrorMessage());
                }

                Maps = mapsOnServerStart.Value;
            }

            ServerInfo = response;
        });


        connection.OnEvent(response =>
        {
            Events ??= [];
            Events.Add(response);
            OnEventsChangedEvent?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        });

        connection.OnServerLog(response =>
        {
            ServerLogs ??= [];
            ServerLogs.Add(response);
            AllLogs.Add(new LogEntry(response.MessageReceivedAtUt, response.Message));
            OnAllLogsChangedEvent?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        });

        connection.OnUpdateOrInstallLog(response =>
        {
            UpdateOrInstallLogs ??= [];
            UpdateOrInstallLogs.Add(response);
            AllLogs.Add(new LogEntry(response.MessageReceivedAtUt, response.Message));
            OnAllLogsChangedEvent?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        });

        return Result.Success;
    }


    #region ServerInfo

    private readonly object _serverInfoLock = new();
    private ServerInfoResponse? _serverInfo;
    public event EventHandler? OnServerInfoChangedEvent;

    public ServerInfoResponse? ServerInfo
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
                OnServerInfoChangedEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool IsServerBusy
    {
        get
        {
            var serverInfo = ServerInfo;
            if (serverInfo is null)
            {
                return false;
            }

            if (serverInfo.ServerStarting ||
                serverInfo.ServerStopping ||
                serverInfo.ServerUpdatingOrInstalling ||
                serverInfo.ServerPluginsUpdatingOrInstalling ||
                serverInfo.DemoUploading)
            {
                return true;
            }

            return false;
        }
    }

    #endregion

    #region Events

    private readonly object _eventsLock = new();
    private List<EventLogResponse>? _events;
    public event EventHandler? OnEventsChangedEvent;

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
                    OnEventsChangedEvent?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    #endregion

    #region AllLogs

    private readonly object _allLogsLock = new();
    private List<LogEntry> _allLogs = [];
    public event EventHandler? OnAllLogsChangedEvent;

    public List<LogEntry> AllLogs
    {
        get
        {
            lock (_allLogsLock)
            {
                return _allLogs;
            }
        }
        private set
        {
            lock (_allLogsLock)
            {
                _allLogs = value;
                OnAllLogsChangedEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    #endregion

    #region ServerLogs

    private readonly object _serverLogsLock = new();
    private List<ServerLogResponse>? _serverLogs;
    public event EventHandler? OnServerLogsChangedEvent;

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
            lock (_serverLogsLock)
            {
                _serverLogs = value is null
                    ? value
                    : value.OrderByDescending(l => l.MessageReceivedAtUt).ToList();
                OnServerLogsChangedEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    #endregion

    #region UpdateOrInstallLogs

    private readonly object _updateOrInstallLogsLock = new();
    private List<UpdateOrInstallLogResponse>? _updateOrInstallLogs;
    public event EventHandler? OnUpdateOrInstallLogsChangedEvent;

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
                    OnUpdateOrInstallLogsChangedEvent?.Invoke(this, EventArgs.Empty);
                }
            }
        }
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

    #region StartParameters

    private StartParameters _startParameters = new();
    private readonly object _startParametersLock = new();
    public event EventHandler? OnStartParametersChangedEvent;

    public StartParameters StartParameters
    {
        get
        {
            lock (_startParametersLock)
            {
                return _startParameters;
            }
        }
        set
        {
            lock (_startParametersLock)
            {
                _startParameters = value;
                OnStartParametersChangedEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    #endregion
}