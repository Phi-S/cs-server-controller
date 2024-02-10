using System.Collections.Concurrent;
using Application.EventServiceFolder;
using Application.EventServiceFolder.EventArgs;
using Application.ServerServiceFolder;
using Application.ServerUpdateOrInstallServiceFolder;
using Application.StatusServiceFolder;
using Application.SystemLogFolder;
using Microsoft.AspNetCore.SignalR;
using Shared.ApiModels;
using Shared.SignalR;

namespace Instance.SignalR;

public class SignalRUserService
{
    private readonly IHubContext<SignalRHub> _hubContext;
    private readonly ServerService _serverService;
    private readonly ServerUpdateOrInstallService _serverUpdateOrInstallService;
    private readonly EventService _eventService;
    private readonly StatusService _statusService;
    private readonly SystemLogService _systemLogService;

    public SignalRUserService(
        IHubContext<SignalRHub> hubContext,
        ServerService serverService,
        ServerUpdateOrInstallService serverUpdateOrInstallService,
        EventService eventService,
        StatusService statusService,
        SystemLogService systemLogService)
    {
        _hubContext = hubContext;
        _serverService = serverService;
        _serverUpdateOrInstallService = serverUpdateOrInstallService;
        _eventService = eventService;
        _statusService = statusService;
        _systemLogService = systemLogService;

        _systemLogService.OnSystemLogEvent += SystemLogServiceOnOnSystemLogEvent;
        _serverService.ServerOutputEvent += OnServerServiceOnServerOutputEvent;
        _serverUpdateOrInstallService.UpdateOrInstallOutput += OnServerUpdateOrInstallServiceOnServerUpdateOrInstallOutput;
        _eventService.OnEvent += EventServiceOnOnEvent;
        _statusService.ServerStatusChanged += StatusServiceOnServerStatusChanged;
    }

    private readonly object _connectionsLock = new();
    private readonly ConcurrentBag<string> _connections = [];

    #region EventHandlers

    private async void StatusServiceOnServerStatusChanged(object? sender, EventArgs e)
    {
        var status = _statusService.GetStatus();
        await _hubContext.Clients.All.SendServerInfo(status);
    }

    private async void SystemLogServiceOnOnSystemLogEvent(object? sender, SystemLogEventArgs arg)
    {
        var systemLog = new SystemLogResponse(arg.Message, DateTime.UtcNow);
        await _hubContext.Clients.All.SendSystemLog(systemLog);
    }

    private async void OnServerServiceOnServerOutputEvent(object? _, ServerOutputEventArg arg)
    {
        var serverLog = new ServerLogResponse(arg.ServerStartDbModel.Id, arg.Output, DateTime.UtcNow);
        await _hubContext.Clients.All.SendServerLog(serverLog);
    }

    private async void OnServerUpdateOrInstallServiceOnServerUpdateOrInstallOutput(object? _, ServerUpdateOrInstallOutputEventArg arg)
    {
        var response = new UpdateOrInstallLogResponse(arg.UpdateOrInstallId, arg.Message, DateTime.UtcNow);
        await _hubContext.Clients.All.SendUpdateOrInstallLog(response);
    }

    private async void EventServiceOnOnEvent(object? sender, CustomEventArg arg)
    {
        var eventResponse = new EventLogResponse(arg.EventName.ToString(), arg.TriggeredUtc);
        await _hubContext.Clients.All.SendEvent(eventResponse);
    }

    #endregion

    public void RegisterConnection(string connectionId)
    {
        lock (_connectionsLock)
        {
            _connections.Add(connectionId);
        }
    }

    public void RemoveConnection(string connectionId)
    {
        lock (_connectionsLock)
        {
            var tempConnection = _connections.Where(s => s.Equals(connectionId) == false);
            _connections.Clear();
            foreach (var connection in tempConnection)
            {
                _connections.Add(connection);
            }
        }
    }
}