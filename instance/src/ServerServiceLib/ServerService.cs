using System.Diagnostics;
using AppOptionsLib;
using DatabaseLib.Models;
using DatabaseLib.Repos;
using EventsServiceLib;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResultLib;
using SharedModelsLib;
using StatusServiceLib;

namespace ServerServiceLib;

public partial class ServerService(
    ILogger<ServerService> logger,
    IOptions<AppOptions> options,
    StatusService statusService,
    EventService eventService,
    ServerRepo serverRepo)
{
    #region Const

    private const string SERVER_STARTED_MESSAGE = "#####_SERVER_STARTED";
    private const int SERVER_START_TIMEOUT_MS = 30_000;
    private const int SERVER_STOP_TIMEOUT_MS = 5_000;

    #endregion

    private volatile Process? _process;
    private readonly object _processLockObject = new();

    private readonly SemaphoreSlim _serverStartStopLock = new(1);

    private event EventHandler<ServerOutputEventArg>? ServerOutputEvent;


    public async Task<Result> Start(StartParameters startParameters)
    {
        Process? process = null;
        try
        {
            await _serverStartStopLock.WaitAsync();
            logger.LogInformation("Starting server");

            #region CheckIfServerIsReadyToStart

            var checkIfServerIsReadyToStart = CheckIfServerIsReadyToStart();
            if (checkIfServerIsReadyToStart.IsFailed)
            {
                logger.LogWarning(checkIfServerIsReadyToStart.Exception, "Server is not ready to start");
                eventService.OnStartingServerFailed();
                return Result.Fail(checkIfServerIsReadyToStart.Exception, "Server is not ready to start");
            }

            #endregion

            eventService.OnStartingServer();

            #region CopySteamclient

            var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var copySteamclient = CopySteamclient(homeFolder, options.Value.STEAMCMD_FOLDER);
            if (copySteamclient.IsFailed)
            {
                eventService.OnStartingServerFailed();
                logger.LogError(checkIfServerIsReadyToStart.Exception, "Failed to start server");
                return Result.Fail(copySteamclient.Exception, "Failed to start server");
            }

            #endregion


            #region StartServerProcess

            // ReSharper disable once StringLiteralTypo
            var executablePath = Path.Combine(options.Value.SERVER_FOLDER, "game", "bin", "linuxsteamrt64", "cs2");
            var startParameterString = startParameters.GetString(options.Value.PORT);

            var serverStart = await serverRepo.AddStart(startParameterString, DateTime.UtcNow);

            logger.LogInformation("Starting server with command: \"{StartCommand}\"",
                $"{executablePath} {startParameterString}");
            var startServerProcess = StartServerProcess(
                serverStart,
                executablePath,
                options.Value.SERVER_FOLDER,
                startParameterString
            );
            if (startServerProcess.IsFailed)
            {
                eventService.OnStartingServerFailed();
                logger.LogError(startServerProcess.Exception, "Failed to start server");
                return Result.Fail(startServerProcess.Exception, "Failed to start server");
            }

            #endregion

            #region WaitForServerToStart

            process = startServerProcess.Value;
            var waitForServerToStart = await WaitForServerToStart(process);
            if (waitForServerToStart.IsFailed)
            {
                eventService.OnStartingServerFailed();
                logger.LogError(waitForServerToStart.Exception, "Failed to start server");
                return Result.Fail(waitForServerToStart.Exception, "Failed to start server");
            }

            lock (_processLockObject)
            {
                _process = process;
            }

            logger.LogInformation("Server process started");

            #endregion

            StartOutputFlushBackgroundTask();
            logger.LogInformation("Background task to periodically write in StandardInput started");

            #region Maps

            if (string.IsNullOrWhiteSpace(startParameters.StartMap) == false)
            {
                eventService.OnMapChanged(startParameters.StartMap);
            }

            lock (_mapsLock)
            {
                GetAllMaps(options.Value.SERVER_FOLDER, true);
                logger.LogInformation("Available maps refreshed");
            }

            #endregion

            logger.LogInformation("Server started");
            eventService.OnStartingServerDone(startParameters);
            return Result.Ok();
        }
        catch (Exception e)
        {
            lock (_processLockObject)
            {
                process?.Kill();
                process?.Dispose();
            }

            eventService.OnStartingServerFailed();
            logger.LogError(e, "Server failed to start with exception");
            return Result.Fail(e);
        }
        finally
        {
            _serverStartStopLock.Release();
        }
    }

    private Result CheckIfServerIsReadyToStart()
    {
        if (statusService.ServerStarted)
        {
            return Result.Fail("Server already started");
        }

        if (statusService.ServerStarting)
        {
            return Result.Fail("Server already starting");
        }

        if (statusService.ServerStopping)
        {
            return Result.Fail("Server is stopping");
        }

        if (statusService.ServerUpdatingOrInstalling)
        {
            return Result.Fail("Server is updating");
        }

        return Result.Ok();
    }

    // ReSharper disable once CommentTypo
    /// <summary>
    /// https://developer.valvesoftware.com/wiki/Counter-Strike_2/Dedicated_Servers#steamservice.so_missing.2Ffailed_to_load
    /// fixes steamservice.so missing/failed to load
    /// This is a common issue with a fairly easy fix.
    /// The reason for this error is that SteamCMD doesnt place the file in the folder it should, as the games typically look for it there. So what you need to do is the following:
    /// Create a symlink (shortcut) for each of the files like this: (run each separately)
    /// ln -s /home/your_user/.local/share/Steam/steamcmd/linux64/steamclient.so /home/your_user/.steam/sdk64/
    /// ln -s /home/your_user/.local/share/Steam/steamcmd/linux32/steamclient.so /home/your_user/.steam/sdk32/
    ///</summary>
    /// <returns></returns>
    private Result CopySteamclient(string homeFolder, string steamcmdFolder)
    {
        const string steamclientSoName = "steamclient.so";
        var steamClientDestFolder = Path.Combine(homeFolder, ".steam", "sdk64");
        var steamClientDestPath = Path.Combine(steamClientDestFolder, steamclientSoName);

        var file = new FileInfo(steamClientDestPath);
        if (file.LinkTarget != null)
        {
            return Result.Ok();
        }

        var steamClientSrcPath = Path.Combine(steamcmdFolder, "linux64", steamclientSoName);
        if (File.Exists(steamClientSrcPath) == false)
        {
            return Result.Fail(
                $"Steam client not found at \"{steamClientSrcPath}\". Run UpdateOrInstall to install steamclient");
        }

        Directory.CreateDirectory(steamClientDestFolder);
        File.CreateSymbolicLink(steamClientDestPath, steamClientSrcPath);
        return Result.Ok();
    }

    private Result<Process> StartServerProcess(ServerStart serverStart, string executablePath, string serverFolder,
        string startParameter)
    {
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

        process.Exited += OnExited;
        process.ErrorDataReceived += OnProcessOnOutputDataReceived;
        process.OutputDataReceived += OnProcessOnOutputDataReceived;

        if (process.Start() == false)
        {
            process.Exited -= OnExited;
            process.ErrorDataReceived -= OnProcessOnOutputDataReceived;
            process.OutputDataReceived -= OnProcessOnOutputDataReceived;
            process.Close();
            process.Dispose();
            return Result<Process>.Fail("Failed to start server process");
        }

        ServerOutputEvent += ServerOutputLog;
        ServerOutputEvent += ServerOutputToDatabase;
        AddEventDetection();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;

        void OnProcessOnOutputDataReceived(object _, DataReceivedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.Data))
            {
                return;
            }

            ServerOutputEvent?.Invoke(this, new ServerOutputEventArg(serverStart, args.Data));
        }
    }


    private void ServerOutputLog(object? _, ServerOutputEventArg arg)
    {
        try
        {
            logger.LogInformation("Server: StartId: {StartID} | Output: {Output}", arg.ServerStart.Id, arg.Output);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while trying to add server output to database");
        }
    }

    private async void ServerOutputToDatabase(object? _, ServerOutputEventArg serverOutputEventArg)
    {
        try
        {
            await serverRepo.AddLog(serverOutputEventArg.ServerStart.Id, serverOutputEventArg.Output);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while trying to add server output to database");
        }
    }

    private void WriteLine(string text)
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

    private async Task<Result> WaitForServerToStart(Process process)
    {
        EventHandler<ServerOutputEventArg>? serverStartedHandler = null;
        try
        {
            var serverStarted = false;
            var hostActivated = false;
            serverStartedHandler = (_, serverOutputEventArg) =>
            {
                var message = serverOutputEventArg.Output;
                if (message.StartsWith("Host activate: Loading"))
                {
                    hostActivated = true;
                    process.StandardInput.WriteLine($"say {SERVER_STARTED_MESSAGE}");
                    return;
                }

                if (hostActivated == false)
                {
                    return;
                }

                if (message.Equals($"[All Chat][Console (0)]: {SERVER_STARTED_MESSAGE}") == false)
                {
                    return;
                }

                serverStarted = true;
            };


            ServerOutputEvent += serverStartedHandler;
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds <= SERVER_START_TIMEOUT_MS)
            {
                await Task.Delay(100);
                await process.StandardInput.WriteLineAsync(process.StandardInput.NewLine);

                if (serverStarted == false)
                {
                    continue;
                }

                break;
            }

            return serverStarted
                ? Result.Ok()
                : Result.Fail($"Timout while waiting for the server to start after {SERVER_START_TIMEOUT_MS} ms");
        }
        finally
        {
            ServerOutputEvent -= serverStartedHandler;
        }
    }


    private void OnExited(object? sender, EventArgs eventArgs)
    {
        try
        {
            logger.LogInformation("Server exited");

            lock (_processLockObject)
            {
                _process?.Dispose();
                _process = null;
                eventService.OnServerExited();
            }

            RemoveEventDetection();
            ServerOutputEvent -= ServerOutputLog;
            ServerOutputEvent -= ServerOutputToDatabase;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in OnExited");
        }
    }

    public async Task<Result> Stop()
    {
        if (IsServerStopped())
        {
            return Result.Ok();
        }

        eventService.OnStoppingServer();
        logger.LogInformation("Stopping server");
        var stopNormal = await StopNormal();
        if (stopNormal)
        {
            return Result.Ok();
        }

        logger.LogWarning("Force stopping the server because it failed to stop normally");
        var stopForce = await StopForce();
        if (stopForce)
        {
            logger.LogWarning("Server stopped forcefully");
            return Result.Ok("Server stopped forcefully");
        }

        logger.LogError("Server failed to stop normally in time and failed to force stop");
        return Result.Fail("Server failed to stop normally in time and failed to force stop");
    }

    private async Task<bool> StopNormal()
    {
        WriteLine("quit");

        var sw = Stopwatch.StartNew();
        while (true)
        {
            await Task.Delay(10);
            if (sw.ElapsedMilliseconds >= SERVER_STOP_TIMEOUT_MS)
            {
                return false;
            }

            if (IsServerStopped())
            {
                return true;
            }
        }
    }

    private async Task<bool> StopForce()
    {
        if (IsServerStopped())
        {
            return true;
        }

        lock (_processLockObject)
        {
            _process!.Kill();
            _process!.Close();
        }

        var waitForServerToStopTimeout = 5000;
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds <= waitForServerToStopTimeout)
        {
            await Task.Delay(10);
            if (IsServerStopped())
            {
                return true;
            }
        }

        return false;
    }

    private bool IsServerStopped()
    {
        lock (_processLockObject)
        {
            if (_process != null)
            {
                return false;
            }

            if (statusService.ServerStarted == false)
            {
                return true;
            }

            logger.LogCritical(
                "Server process is not set but server status still says the server is running. This should never happen");
            throw new Exception(
                "Server process is not set but server status still says the server is running. This should never happen");
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
                if (_process == null ||
                    statusService.ServerStarted == false ||
                    statusService.ServerStopping)
                {
                    break;
                }

                WriteLine(_process.StandardInput.NewLine);
            }
        });
    }
}