using InstanceApiServiceLib;
using Microsoft.Extensions.Logging;
using SharedModelsLib;

namespace ServerInfoServiceLib;

public class ServerInfoService(ILogger<ServerInfoService> logger, InstanceApiService instanceApiService)
{
    public InfoModel? ServerInfo
    {
        get
        {
            lock (_responseModelStatusLock)
            {
                return _responseModelStatus;
            }
        }
        set
        {
            lock (_responseModelStatusLock)
            {
                if (_responseModelStatus is not null && _responseModelStatus.Equals(value))
                {
                    return;
                }

                _responseModelStatus = value;
                OnServerStatusChangedEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private readonly object _responseModelStatusLock = new();
    private InfoModel? _responseModelStatus;
    private event EventHandler? OnServerStatusChangedEvent;

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private volatile bool _statusBackgroundTaskRunning;

    public void StartStatusBackgroundTask()
    {
        ServerInfo = new InfoModel(
            "affenhaus",
            "server pw 123",
            "kek.at",
            90,
            45,
            "server.at",
            "27011",
            false,
            true,
            false,
            false,
            false,
            false);

        if (_statusBackgroundTaskRunning)
        {
            return;
        }

        if (_cancellationTokenSource.TryReset() == false)
        {
            throw new Exception("Failed to reset cancellation token");
        }

        Task.Run(async () =>
        {
            try
            {
                _statusBackgroundTaskRunning = true;

                while (_cancellationTokenSource.IsCancellationRequested == false)
                {
                    try
                    {
                        ServerInfo = await instanceApiService.Info();
                        await Task.Delay(1000);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "exception is status background task");
                    }
                }
            }
            finally
            {
                _statusBackgroundTaskRunning = false;
            }
        });
    }

    public void StopServerStatusBackgroundTask()
    {
        _cancellationTokenSource.Cancel();
        ServerInfo = null;
    }
}