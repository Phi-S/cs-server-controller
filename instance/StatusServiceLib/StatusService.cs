using EventsServiceLib;

namespace StatusServiceLib;

public sealed class StatusService
{
    public record ResponseModelStatus(
        bool ServerStarting,
        bool ServerStarted,
        bool ServerStopping,
        bool ServerHibernating,
        string CurrentMap,
        int CurrentPlayerCount,
        bool ServerUpdatingOrInstalling,
        bool DemoUploading
    );
    
    private readonly EventService _eventService;

    public StatusService(EventService eventService)
    {
        _eventService = eventService;

        RegisterEventServiceHandler();
    }

    public ResponseModelStatus GetStatus()
    {
        return new ResponseModelStatus(
            ServerStarting,
            ServerStarted,
            ServerStopping,
            ServerHibernating,
            CurrentMap,
            CurrentPlayerCount,
            ServerUpdatingOrInstalling,
            DemoUploading
        );
    }


    #region register event service handler

    private void RegisterEventServiceHandler()
    {
        _eventService.StartingServer += (_, _) => ServerStarting = true;
        _eventService.StartingServerDone += (_, _) =>
        {
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
            ServerStopping = false;
        };

        _eventService.UpdateOrInstallStarted += (_, _) => { ServerUpdatingOrInstalling = true; };
        _eventService.UpdateOrInstallDone += (_, _) => { ServerUpdatingOrInstalling = false; };
        _eventService.UpdateOrInstallCancelled += (_, _) => { ServerUpdatingOrInstalling = false; };
        _eventService.UpdateOrInstallFailed += (_, _) => { ServerUpdatingOrInstalling = false; };

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
        }
    }

    #endregion
}