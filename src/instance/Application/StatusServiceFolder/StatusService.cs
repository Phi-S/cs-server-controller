using Application.EventServiceFolder;
using Application.ServerHelperFolder;
using Domain;
using Microsoft.Extensions.Options;
using Shared.ApiModels;

namespace Application.StatusServiceFolder;

public sealed class StatusService
{
    private readonly IOptions<AppOptions> _options;
    private readonly EventService _eventService;

    public StatusService(IOptions<AppOptions> options, EventService eventService)
    {
        _options = options;
        _eventService = eventService;
        RegisterEventServiceHandler();
        ServerInstalled = ServerHelper.IsServerInstalled(options.Value.SERVER_FOLDER);
    }

    public event EventHandler? ServerStatusChanged;

    public ServerInfoResponse GetStatus()
    {
        var statusResponse = new ServerInfoResponse(
            ServerInstalled,
            ServerStartParameters?.ServerHostname,
            ServerStartParameters?.ServerPassword,
            CurrentMap,
            CurrentPlayerCount,
            ServerStartParameters?.MaxPlayer,
            _options.Value.IP_OR_DOMAIN,
            _options.Value.PORT,
            ServerStarting,
            ServerStarted,
            ServerStopping,
            ServerHibernating,
            ServerUpdatingOrInstalling,
            ServerPluginsUpdatingOrInstalling,
            DemoUploading,
            DateTime.UtcNow
        );
        return statusResponse;
    }

    #region register event service handler

    private void RegisterEventServiceHandler()
    {
        _eventService.StartingServer += (_, _) => ServerStarting = true;
        _eventService.StartingServerDone += (_, customEventArgStartingServerDone) =>
        {
            ServerStartParameters = customEventArgStartingServerDone.StartParameters;
            ServerStarting = false;
            ServerStarted = true;
        };
        _eventService.StartingServerFailed += (_, _) => { ServerStarting = false; };

        _eventService.StoppingServer += (_, _) => { ServerStopping = true; };
        _eventService.ServerExited += (_, _) =>
        {
            ServerStarting = false;
            ServerStarted = false;
            ServerHibernating = false;
            CurrentPlayerCount = 0;
            CurrentMap = "";
            ServerStartParameters = null;
            ServerStopping = false;
        };

        _eventService.UpdateOrInstallStarted += (_, _) => { ServerUpdatingOrInstalling = true; };
        _eventService.UpdateOrInstallDone += (_, _) =>
        {
            ServerUpdatingOrInstalling = false;
            ServerInstalled = ServerHelper.IsServerInstalled(_options.Value.SERVER_FOLDER);
        };
        _eventService.UpdateOrInstallCancelled += (_, _) =>
        {
            ServerUpdatingOrInstalling = false;
            ServerInstalled = ServerHelper.IsServerInstalled(_options.Value.SERVER_FOLDER);
        };
        _eventService.UpdateOrInstallFailed += (_, _) =>
        {
            ServerUpdatingOrInstalling = false;
            ServerInstalled = ServerHelper.IsServerInstalled(_options.Value.SERVER_FOLDER);
        };

        _eventService.PluginUpdateOrInstallStarted += (_, _) => { ServerPluginsUpdatingOrInstalling = true; };
        _eventService.PluginUpdateOrInstallDone += (_, _) => { ServerPluginsUpdatingOrInstalling = false; };
        _eventService.PluginUpdateOrInstallFailed += (_, _) => { ServerPluginsUpdatingOrInstalling = false; };

        _eventService.UploadDemoStarted += (_, _) => { DemoUploading = true; };
        _eventService.UploadDemoDone += (_, _) => { DemoUploading = false; };
        _eventService.UploadDemoFailed += (_, _) => { DemoUploading = false; };

        _eventService.HibernationStarted += (_, _) => { ServerHibernating = true; };
        _eventService.HibernationEnded += (_, _) => { ServerHibernating = false; };

        _eventService.MapChanged += (_, customEventArgMapChanged) => { CurrentMap = customEventArgMapChanged.MapName; };

        _eventService.PlayerConnected += (_, _) => { CurrentPlayerCount += 1; };
        _eventService.PlayerDisconnected += (_, _) => { CurrentPlayerCount -= 1; };
    }

    #endregion

    #region ServerInstalled

    private volatile bool _serverInstalled;
    private readonly object _serverInstalledLock = new();

    public bool ServerInstalled
    {
        get
        {
            lock (_serverInstalledLock)
            {
                return _serverInstalled;
            }
        }
        private set
        {
            lock (_serverInstalledLock)
            {
                _serverInstalled = value;
            }

            OnServerStatusChanged();
        }
    }

    #endregion

    #region ServerStartParameters

    private volatile StartParameters? _serverStartParameters;
    private readonly object _serverStartParametersLock = new();

    public StartParameters? ServerStartParameters
    {
        get
        {
            lock (_serverStartParametersLock)
            {
                return _serverStartParameters;
            }
        }
        private set
        {
            lock (_serverStartParametersLock)
            {
                _serverStartParameters = value;
            }

            OnServerStatusChanged();
        }
    }

    #endregion

    #region ServerStarted

    private volatile bool _serverStarted;
    private readonly object _serverStartedLock = new();

    public bool ServerStarted
    {
        get
        {
            lock (_serverStartedLock)
            {
                return _serverStarted;
            }
        }
        private set
        {
            lock (_serverStartedLock)
            {
                _serverStarted = value;
            }

            OnServerStatusChanged();
        }
    }

    #endregion

    #region ServerStarting

    private volatile bool _serverStarting;
    private readonly object _serverStartingLock = new();

    public bool ServerStarting
    {
        get
        {
            lock (_serverStartingLock)
            {
                return _serverStarting;
            }
        }

        private set
        {
            lock (_serverStartingLock)
            {
                _serverStarting = value;
            }

            OnServerStatusChanged();
        }
    }

    #endregion

    #region ServerStopping

    private volatile bool _serverStopping;
    private readonly object _serverStoppingLock = new();

    public bool ServerStopping
    {
        get
        {
            lock (_serverStoppingLock)
            {
                return _serverStopping;
            }
        }
        private set
        {
            lock (_serverStoppingLock)
            {
                _serverStopping = value;
            }

            OnServerStatusChanged();
        }
    }

    #endregion

    #region ServerUpdatingOrInstalling

    private volatile bool _serverUpdatingOrInstalling;
    private readonly object _serverUpdatingOrInstallingLock = new();

    public bool ServerUpdatingOrInstalling
    {
        get
        {
            lock (_serverUpdatingOrInstallingLock)
            {
                return _serverUpdatingOrInstalling;
            }
        }
        private set
        {
            lock (_serverUpdatingOrInstallingLock)
            {
                _serverUpdatingOrInstalling = value;
            }

            OnServerStatusChanged();
        }
    }

    #endregion

    #region ServerPluginsUpdatingOrInstalling

    private volatile bool _serverPluginsUpdatingOrInstalling;
    private readonly object _serverPluginsUpdatingOrInstallingLock = new();

    public bool ServerPluginsUpdatingOrInstalling
    {
        get
        {
            lock (_serverPluginsUpdatingOrInstallingLock)
            {
                return _serverPluginsUpdatingOrInstalling;
            }
        }
        private set
        {
            lock (_serverPluginsUpdatingOrInstallingLock)
            {
                _serverPluginsUpdatingOrInstalling = value;
            }

            OnServerStatusChanged();
        }
    }

    #endregion

    #region DemoUploading

    private volatile bool _demoUploading;
    private readonly object _demoUploadingLock = new();

    public bool DemoUploading
    {
        get
        {
            lock (_demoUploadingLock)
            {
                return _demoUploading;
            }
        }
        private set
        {
            lock (_demoUploadingLock)
            {
                _demoUploading = value;
            }

            OnServerStatusChanged();
        }
    }

    #endregion DemoUploading

    #region ServerHibernating

    private volatile bool _serverHibernating;
    private readonly object _serverHibernatingLock = new();

    public bool ServerHibernating
    {
        get
        {
            lock (_serverHibernatingLock)
            {
                return _serverHibernating;
            }
        }
        private set
        {
            lock (_serverHibernatingLock)
            {
                _serverHibernating = value;
            }

            OnServerStatusChanged();
        }
    }

    #endregion

    #region CurrentMap

    private string _currentMap = "";
    private readonly object _currentMapLock = new();

    public string CurrentMap
    {
        get
        {
            lock (_currentMapLock)
            {
                return _currentMap;
            }
        }
        private set
        {
            lock (_currentMapLock)
            {
                _currentMap = value;
            }

            OnServerStatusChanged();
        }
    }

    #endregion

    #region CurrentPlayerCount

    private int _currentPlayerCount;
    private readonly object _currentPlayerCountLock = new();

    public int CurrentPlayerCount
    {
        get
        {
            lock (_currentPlayerCountLock)
            {
                return _currentPlayerCount;
            }
        }
        private set
        {
            lock (_currentPlayerCountLock)
            {
                _currentPlayerCount = value;
            }

            _eventService.OnPlayerCountChanged(CurrentPlayerCount);
            OnServerStatusChanged();
        }
    }

    #endregion

    private void OnServerStatusChanged()
    {
        ServerStatusChanged?.Invoke(this, EventArgs.Empty);
    }
}