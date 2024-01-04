using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Shared.ApiModels;
using Shared.SignalR;
using Web.Options;

namespace Web.Services;

public class ServerInfoService
{
    private readonly IOptions<AppOptions> _options;
    private readonly InstanceApiService _instanceApiService;

    public ServerInfoService(
        IOptions<AppOptions> options,
        InstanceApiService instanceApiService)
    {
        _options = options;
        _instanceApiService = instanceApiService;
    }

    public async Task StartSignalRConnection()
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri($"{_options.Value.INSTANCE_API_ENDPOINT}/hub"))
            .WithKeepAliveInterval(TimeSpan.FromSeconds(1))
            .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(10)])
            .Build();
        await connection.StartAsync();
        await AddSignalrRHandler(connection);
        connection.Reconnected += async _ => { await AddSignalrRHandler(connection); };
    }

    private async Task AddSignalrRHandler(HubConnection connection)
    {
        var last24Hours = DateTime.UtcNow.Subtract(TimeSpan.FromHours(24));
        ServerInfo = await _instanceApiService.Info();
        ServerLogs = (await _instanceApiService.LogsServer(last24Hours)).OrderByDescending(l => l.MessageReceivedAtUt)
            .ToList();
        Events = (await _instanceApiService.LogsEvents(last24Hours)).OrderByDescending(l => l.TriggeredAtUtc).ToList();
        UpdateOrInstallLogs = (await _instanceApiService.LogsUpdateOrInstall(last24Hours))
            .OrderByDescending(l => l.MessageReceivedAtUt).ToList();
        Maps = await _instanceApiService.Maps();
        Configs = await _instanceApiService.Configs();

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
                Maps = await _instanceApiService.Maps();
                Configs = await _instanceApiService.Configs();
            }

            ServerInfo = response;
        });

        connection.OnServerLog(response =>
        {
            ServerLogs ??= [];
            ServerLogs.Add(response);
            ServerLogs =
                ServerLogs.OrderByDescending(l => l.MessageReceivedAtUt).ToList();
            return Task.CompletedTask;
        });

        connection.OnEvent(response =>
        {
            Events ??= [];
            Events.Add(response);
            Events = Events.OrderByDescending(l => l.TriggeredAtUtc).ToList();
            return Task.CompletedTask;
        });

        connection.OnUpdateOrInstallLog(response =>
        {
            UpdateOrInstallLogs ??= [];
            UpdateOrInstallLogs.Add(response);
            UpdateOrInstallLogs = UpdateOrInstallLogs
                .OrderByDescending(l => l.MessageReceivedAtUt).ToList();
            return Task.CompletedTask;
        });
    }


    #region ServerInfo

    private readonly object _serverInfoLock = new();
    private ServerStatusResponse? _serverInfo;
    public event EventHandler? OnServerInfoChangedEvent;

    public ServerStatusResponse? ServerInfo
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
                }

                OnEventsChangedEvent?.Invoke(this, EventArgs.Empty);
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
            {
                lock (_serverLogsLock)
                {
                    _serverLogs = value;
                }

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
                }

                OnUpdateOrInstallLogsChangedEvent?.Invoke(this, EventArgs.Empty);
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