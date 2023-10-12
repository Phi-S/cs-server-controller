using System.Diagnostics;
using AppOptionsLib;
using EventsServiceLib;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StatusServiceLib;

namespace ServerServiceLib;

public partial class ServerService(
    ILogger<ServerService> logger,
    IOptions<AppOptions> options,
    StatusService statusService,
    EventService eventService)
{
    #region Const

    private const string SERVER_STARTED_MESSAGE = "#####_SERVER_STARTED";
    public const int RESPONSE_TIMEOUT_MS = 15_000;
    private const int SERVER_START_TIMEOUT_MS = 30_000;
    private const int SERVER_STOP_TIMEOUT_MS = 5_000;

    #endregion

    private volatile Process? _process;
    private readonly object _processLockObject = new();
    private event EventHandler<string>? ServerOutputEvent;


    public async Task Start(StartParameters startParameters)
    {
        Process? process = null;
        try
        {
            CheckIfServerIsReadyToStart();
            eventService.OnStartingServer();
            logger.LogInformation("Starting server");

            process = StartServerProcess(options.Value.SERVER_FOLDER,
                startParameters.GetString());
            await WaitForServerToStart(process);
            await Task.Delay(2000);

            if (statusService.ServerStarted)
            {
                logger.LogInformation("Server started");
                lock (_processLockObject)
                {
                    _process = process;
                }

                StartOutputFlushBackgroundTask();
                logger.LogInformation("Background task to periodically write in StandardInput started");
                
                eventService.StartingServerDone += (_, _) =>
                {
                    lock (_mapsLock)
                    {
                        _maps = GetAllMaps(options.Value.SERVER_FOLDER, true);
                    }
                };
                eventService.StartingServerDone += (_, _) =>
                {
                    if (!string.IsNullOrWhiteSpace(startParameters.StartMap))
                    {
                        eventService.OnMapChanged(startParameters.StartMap);
                    }
                };
            }
            else
            {
                throw new Exception("Server failed to start");
            }
        }
        catch
        {
            lock (_processLockObject)
            {
                process?.Kill();
                process?.Dispose();
            }

            eventService.OnStartingServerFailed();
            throw;
        }
    }

    private void CheckIfServerIsReadyToStart()
    {
        if (statusService.ServerStarted)
            throw new Exception("Server already started");

        if (statusService.ServerStarting)
            throw new Exception("Server already starting");

        if (statusService.ServerStopping)
            throw new Exception("Server is still stopping");

        if (statusService.ServerUpdatingOrInstalling)
            throw new Exception("Server is updating.");
    }

    private Process StartServerProcess(string serverFolder, string startParameter)
    {
        var executablePath = Path.Combine(serverFolder, "game", "bin", "linuxsteamrt64", "cs2");

        logger.LogInformation("Starting server with command: \"{StartCommand}\"",
            $"{executablePath} {startParameter}");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = serverFolder,
                FileName = executablePath,
                Arguments = startParameter,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true
            },
            EnableRaisingEvents = true
        };

        eventService.NewOutput += ServerOutputEvent;
        process.Exited += OnExited;
        process.ErrorDataReceived += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.Data))
            {
                return;
            }
            ServerOutputEvent?.Invoke(this,args.Data);
            //eventService.OnNewOutput(args.Data);
        };
        process.OutputDataReceived += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.Data))
            {
                return;
            }
            ServerOutputEvent?.Invoke(this,args.Data);
            //eventService.OnNewOutput(args.Data);
        };

        if (!process.Start())
        {
            throw new Exception("Failed to start server process");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    public void WriteLine(string text)
    {
        try
        {
            if (!statusService.ServerStarted || _process == null) return;

            lock (_processLockObject)
            {
                _process.StandardInput.WriteLine(text);
            }
        }
        catch (Exception e)
        {
            using (logger.BeginScope(new Dictionary<string, object> {{nameof(text), text}}))
            {
                logger.LogError(e, "Exception");
            }
        }
    }

    private async Task WaitForServerToStart(Process process)
    {
        var watchForServerStartedMessage = false;

        var onNewOutputServerStarted = new EventHandler<string>((_, output) =>
        {
            if (watchForServerStartedMessage)
            {
                if (!output.Trim().Equals($"Console: {SERVER_STARTED_MESSAGE}")) return;
                eventService.OnStartingServerDone();
            }
            else
            {
                if (!output.Trim().Equals($"---- Host_NewGame ----")) return;
                watchForServerStartedMessage = true;
                process.StandardInput.WriteAsync($"say {SERVER_STARTED_MESSAGE}{process.StandardInput.NewLine}");
            }
        });

        try
        {
            eventService.NewOutput += onNewOutputServerStarted;

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds <= SERVER_START_TIMEOUT_MS)
            {
                await Task.Delay(100);
                await process.StandardInput.WriteAsync(process.StandardInput.NewLine);

                if (!statusService.ServerStarted) continue;
                break;
            }
        }
        catch (Exception e)
        {
            throw new Exception("Exception while waiting for the the server to start", e);
        }
        finally
        {
            eventService.NewOutput -= onNewOutputServerStarted;
        }
    }


    private void OnExited(object? sender, EventArgs eventArgs)
    {
        logger.LogWarning("Server exited");

        lock (_processLockObject)
        {
            _process?.Dispose();

            _process = null;
            eventService.OnServerExited();
        }

        eventService.NewOutput -= ServerOutputEvent;
    }

    public void Stop()
    {
        try
        {
            if (statusService.ServerStopping) return;
            if (_process == null && !statusService.ServerStarted) return;

            eventService.OnStoppingServer();
            WriteLine("quit");

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds <= SERVER_STOP_TIMEOUT_MS)
            {
                if (_process != null) continue;
                logger.LogInformation("Server stopped");
                break;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to stop server");
        }
        finally
        {
            if (_process != null)
            {
                logger.LogWarning("Force stopping server");
                lock (_processLockObject)
                {
                    _process.Kill();
                    _process.Close();
                    _process.Dispose();
                    _process = null;
                }
            }
            
            eventService.NewOutput -= ServerOutputEvent;
        }
    }

    //Starting background task to periodically write in StandardInput,
    //so the output always gets the newest data.
    private void StartOutputFlushBackgroundTask()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(10);
                if (_process == null || !statusService.ServerStarted || statusService.ServerStopping) break;
                WriteLine(_process.StandardInput.NewLine);
            }
        });
    }
}