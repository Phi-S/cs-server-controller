﻿using ErrorOr;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Shared;
using Shared.ApiModels;
using Shared.SignalR;
using Web.Misc;
using Web.Options;

namespace Web.Services;

public record LogEntry(DateTime TimestampUtc, string Message, bool Highlight = false);

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

    private readonly SemaphoreSlim _signalRConnectionLock = new(1, 1);
    private HubConnection? _signalRConnection;
    public event Action? OnSignalRConnectionStateChanged;

    public async Task<bool> IsSignalRConnected()
    {
        try
        {
            await _signalRConnectionLock.WaitAsync();
            return _signalRConnection?.State == HubConnectionState.Connected;
        }
        finally
        {
            _signalRConnectionLock.Release();
        }
    }

    private class RetryPolicyLoop : IRetryPolicy
    {
        private const int ReconnectionWaitSeconds = 1;

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return TimeSpan.FromSeconds(ReconnectionWaitSeconds);
        }
    }

    public async Task<ErrorOr<Success>> StartSignalRConnection()
    {
        try
        {
            await _signalRConnectionLock.WaitAsync();
            if (_signalRConnection is not null)
            {
                await _signalRConnection.StopAsync();
                await _signalRConnection.DisposeAsync();
            }

            _signalRConnection = new HubConnectionBuilder()
                .WithUrl(new Uri($"{_options.Value.INSTANCE_API_ENDPOINT}/hub"))
                .WithKeepAliveInterval(TimeSpan.FromSeconds(1))
                .WithAutomaticReconnect(new RetryPolicyLoop())
                .Build();

            await _signalRConnection.StartAsync();

            var addSignalrRHandler = await AddSignalrRHandler(_signalRConnection);
            if (addSignalrRHandler.IsError)
            {
                return addSignalrRHandler.FirstError;
            }

            _signalRConnection.Reconnecting += exception =>
            {
                OnSignalRConnectionStateChanged?.Invoke();
                _logger.LogError(exception, "SignalR connection Reconnecting");
                return Task.CompletedTask;
            };

            _signalRConnection.Closed += exception =>
            {
                OnSignalRConnectionStateChanged?.Invoke();
                _logger.LogError(exception, "SignalR connection closed");
                return Task.CompletedTask;
            };

            _signalRConnection.Reconnected += async _ =>
            {
                OnSignalRConnectionStateChanged?.Invoke();
                _logger.LogInformation("Reconnected to signalr hub");
                var addSignalrRHandlerReconnected = await AddSignalrRHandler(_signalRConnection);
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
            _logger.LogError(e, "Exception");
            return Errors.Fail($"Exception in StartSignalRConnection. {e}");
        }
        finally
        {
            _signalRConnectionLock.Release();
        }
    }

    private async Task<ErrorOr<Success>> AddSignalrRHandler(HubConnection connection)
    {
        if (connection.State != HubConnectionState.Connected)
        {
            return Errors.Fail("not Hub connection");
        }

        var serverInfo = await _instanceApiService.Info();
        if (serverInfo.IsError)
        {
            return Errors.Fail($"Failed to get server infos {serverInfo.ErrorMessage()}");
        }

        var startParameters = await _instanceApiService.GetStartParameters();
        if (startParameters.IsError)
        {
            return Errors.Fail($"Failed to get start parameters {startParameters.ErrorMessage()}");
        }

        var last24Hours = DateTime.UtcNow.Subtract(TimeSpan.FromHours(24));
        var systemLogs = await _instanceApiService.SystemLogs(last24Hours);
        if (systemLogs.IsError)
        {
            return Errors.Fail($"Failed to get system logs {systemLogs.ErrorMessage()}");
        }

        var serverLogs = await _instanceApiService.ServerLogs(last24Hours);
        if (serverLogs.IsError)
        {
            return Errors.Fail($"Failed to get server logs {serverLogs.ErrorMessage()}");
        }

        var events = await _instanceApiService.EventsLogs(last24Hours);
        if (events.IsError)
        {
            return Errors.Fail($"Failed to get events {events.ErrorMessage()}");
        }

        var updateOrInstallLogs = await _instanceApiService.ServerUpdateOrInstallLogs(last24Hours);
        if (updateOrInstallLogs.IsError)
        {
            return Errors.Fail($"Failed to get update or install logs {updateOrInstallLogs.ErrorMessage()}");
        }

        ServerInfo.Set(serverInfo.Value);
        StartParameters.Set(startParameters.Value);

        SystemLogs.Set(systemLogs.Value);
        EventLogs.Set(events.Value);
        ServerLogs.Set(serverLogs.Value);
        UpdateOrInstallLogs.Set(updateOrInstallLogs.Value);
        var allLogs = serverLogs.Value.Select(l => new LogEntry(l.MessageReceivedAtUt, l.Message)).ToList();
        allLogs.AddRange(updateOrInstallLogs.Value.Select(l => new LogEntry(l.MessageReceivedAtUt, l.Message)));
        allLogs.AddRange(systemLogs.Value.Select(l => new LogEntry(l.CreatedUtc, l.Message, true)));
        AllLogs.Set(allLogs);

        connection.Remove(SignalRMethods.ServerInfoMethod);
        connection.Remove(SignalRMethods.SystemLogMethod);
        connection.Remove(SignalRMethods.ServerLogMethod);
        connection.Remove(SignalRMethods.EventMethod);
        connection.Remove(SignalRMethods.UpdateOrInstallLogMethod);

        connection.OnServerInfo(response =>
        {
            try
            {
                ServerInfo.Set(response);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in OnServerInfo");
            }

            return Task.CompletedTask;
        });


        connection.OnEvent(response =>
        {
            try
            {
                EventLogs.Add(response);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in OnEvent");
            }

            return Task.CompletedTask;
        });

        connection.OnSystemLog(response =>
        {
            try
            {
                SystemLogs.Add(response);
                AllLogs.Add(new LogEntry(response.CreatedUtc, response.Message, true));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in OnSystemLog");
            }

            return Task.CompletedTask;
        });

        connection.OnServerLog(response =>
        {
            try
            {
                ServerLogs.Add(response);
                AllLogs.Add(new LogEntry(response.MessageReceivedAtUt, response.Message));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in OnServerLog");
            }

            return Task.CompletedTask;
        });

        connection.OnUpdateOrInstallLog(response =>
        {
            try
            {
                UpdateOrInstallLogs.Add(response);
                AllLogs.Add(new LogEntry(response.MessageReceivedAtUt, response.Message));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in OnUpdateOrInstallLog");
            }

            return Task.CompletedTask;
        });

        return Result.Success;
    }


    #region ServerInfo

    public readonly ThreadSaveData<ServerInfoResponse> ServerInfo = new();

    public bool IsServerBusy
    {
        get
        {
            var serverInfo = ServerInfo.Get();
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

    public readonly ThreadSaveList<SystemLogResponse> SystemLogs = new();
    public readonly ThreadSaveList<EventLogResponse> EventLogs = new();
    public readonly ThreadSaveList<ServerLogResponse> ServerLogs = new();
    public readonly ThreadSaveList<UpdateOrInstallLogResponse> UpdateOrInstallLogs = new();
    public readonly ThreadSaveList<LogEntry> AllLogs = new();
    public readonly ThreadSaveData<StartParameters> StartParameters = new();
}