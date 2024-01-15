using System.Diagnostics;
using Application.EventServiceFolder;
using Application.StatusServiceFolder;
using Domain;
using ErrorOr;
using Infrastructure.Database;
using Infrastructure.Database.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;
using Shared.ApiModels;

namespace Application.ServerServiceFolder;

public partial class ServerService
{
    private readonly ILogger<ServerService> _logger;
    private readonly IOptions<AppOptions> _options;
    private readonly StatusService _statusService;
    private readonly EventService _eventService;
    private readonly IServiceProvider _services;

    public ServerService(ILogger<ServerService> logger,
        IOptions<AppOptions> options,
        StatusService statusService,
        EventService eventService,
        IServiceProvider services)
    {
        _logger = logger;
        _options = options;
        _statusService = statusService;
        _eventService = eventService;
        _services = services;
    }

    #region Const

    private const string ServerStartedMessage = "#####_SERVER_STARTED";
    private const int ServerStartTimeoutMs = 30_000;
    private const int ServerStopTimeoutMs = 15_000;

    #endregion

    private volatile Process? _process;
    private readonly object _processLockObject = new();

    private readonly SemaphoreSlim _serverStartStopLock = new(1);

    public event EventHandler<ServerOutputEventArg>? ServerOutputEvent;


    public async Task<ErrorOr<Success>> Start(StartParameters startParameters)
    {
        try
        {
            await _serverStartStopLock.WaitAsync();
            _logger.LogInformation("Starting server");

            #region CheckIfServerIsReadyToStart

            var checkIfServerIsReadyToStart = CheckIfServerIsReadyToStart();
            if (checkIfServerIsReadyToStart.IsError)
            {
                _eventService.OnStartingServer();
                _logger.LogWarning("Server is not ready to start. {Error}",
                    checkIfServerIsReadyToStart.ErrorMessage());
                _eventService.OnStartingServerFailed();
                return checkIfServerIsReadyToStart.FirstError;
            }

            _eventService.OnStartingServer();

            #endregion

            #region CopySteamclient

            _logger.LogInformation("Creating symbolic links for steam client");
            var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var copySteamclient = LinkSteamclient(homeFolder, _options.Value.STEAMCMD_FOLDER);
            if (copySteamclient.IsError)
            {
                _eventService.OnStartingServerFailed();
                _logger.LogError("Failed to start server. {Error}", copySteamclient.FirstError.Description);
                return copySteamclient.FirstError;
            }

            _logger.LogInformation("Steam client linked");

            #endregion

            #region StartServerProcess

            // ReSharper disable once StringLiteralTypo
            var executablePath = Path.Combine(_options.Value.SERVER_FOLDER, "game", "bin", "linuxsteamrt64", "cs2");
            var startParameterString =
                startParameters.GetAsCommandLineArgs(_options.Value.PORT, _options.Value.LOGIN_TOKEN);

            using var scope = _services.CreateScope();
            var unitOfWork = scope.GetUnitOfWork();
            var serverStart = await unitOfWork.ServerRepo.AddStart(startParameterString, DateTime.UtcNow);
            await unitOfWork.Save();
            _logger.LogInformation("Server start id: {ServerStartId}", serverStart.Id);
            _logger.LogInformation("Starting server with command: \"{StartCommand}\"",
                $"{executablePath} {startParameterString}");
            var startServerProcess = await StartServerProcess(
                serverStart,
                executablePath,
                startParameterString
            );
            if (startServerProcess.IsError)
            {
                _eventService.OnStartingServerFailed();
                _logger.LogError("Failed to start server. {Error}", startServerProcess.FirstError.Description);
                return startServerProcess.FirstError;
            }

            lock (_processLockObject)
            {
                _process = startServerProcess.Value;
            }

            _logger.LogInformation("Server process started");

            #endregion

            #region StartOutputFlushBackgroundTask

            _logger.LogInformation("Starting background task to periodically write in StandardInput");
            StartOutputFlushBackgroundTask();

            #endregion

            #region RefreshMaps

            _logger.LogInformation("Refreshing available maps");
            GetAllMaps(_options.Value.SERVER_FOLDER, true);

            #endregion

            _logger.LogInformation("Adding event detection");
            AddEventDetection();
            _eventService.OnStartingServerDone(startParameters);
            _eventService.OnMapChanged(startParameters.StartMap);
            _eventService.OnHibernationStarted();

            _logger.LogInformation("Server started");
            return Result.Success;
        }
        catch (Exception e)
        {
            _eventService.OnStartingServerFailed();
            _logger.LogError(e, "Server failed to start with exception");
            return Errors.Fail(description: $"Server failed to start with exception: {e}");
        }
        finally
        {
            _serverStartStopLock.Release();
        }
    }

    private ErrorOr<Success> CheckIfServerIsReadyToStart()
    {
        if (_statusService.ServerStarted)
        {
            return Errors.Fail("Server already started");
        }

        if (_statusService.ServerStarting)
        {
            return Errors.Fail("Server already starting");
        }

        if (_statusService.ServerStopping)
        {
            return Errors.Fail("Server is stopping");
        }

        if (_statusService.ServerUpdatingOrInstalling)
        {
            return Errors.Fail("Server is updating");
        }

        return Result.Success;
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
    private ErrorOr<Success> LinkSteamclient(string homeFolder, string steamcmdFolder)
    {
        const string steamclientSoName = "steamclient.so";
        var steamClientSrcPath = Path.Combine(steamcmdFolder, "linux64", steamclientSoName);
        var steamClientDestFolder = Path.Combine(homeFolder, ".steam", "sdk64");
        var steamClientDestPath = Path.Combine(steamClientDestFolder, steamclientSoName);

        if (File.Exists(steamClientSrcPath) == false)
        {
            return Errors.Fail(description:
                $"Steam client not found at \"{steamClientSrcPath}\". Run UpdateOrInstall to install steamclient");
        }

        var file = new FileInfo(steamClientDestPath);
        if (file.LinkTarget != null && file.LinkTarget.Equals(steamClientSrcPath))
        {
            return Result.Success;
        }

        Directory.CreateDirectory(steamClientDestFolder);
        File.CreateSymbolicLink(steamClientDestPath, steamClientSrcPath);
        return Result.Success;
    }

    private async Task<ErrorOr<Process>> StartServerProcess(
        ServerStart serverStart,
        string executablePath,
        string startParameter)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
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
            return Errors.Fail(description: "Failed to start server process");
        }

        ServerOutputEvent += ServerOutputLog;
        ServerOutputEvent += ServerOutputToDatabase;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var waitForServerToStart = await WaitForServerToStart(process);
        return waitForServerToStart.IsError
            ? waitForServerToStart.FirstError
            : process;

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
            using (_logger.BeginScope(new Dictionary<string, object> { ["StartId"] = arg.ServerStart.Id }))
            {
                _logger.LogInformation("CS: {Output}", arg.Output);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to log server output");
        }
    }

    private async void ServerOutputToDatabase(object? _, ServerOutputEventArg serverOutputEventArg)
    {
        try
        {
            using var scope = _services.CreateScope();
            var unitOfWork = scope.GetUnitOfWork();
            await unitOfWork.ServerRepo.AddLog(serverOutputEventArg.ServerStart.Id, serverOutputEventArg.Output);
            await unitOfWork.Save();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while trying to add server output to database");
        }
    }

    private ErrorOr<Success> WriteLine(string text)
    {
        try
        {
            {
                if (_statusService.ServerStarted == false || _process is null)
                {
                    return Errors.Fail(description: $"Failed to write line \"{text}\". Server is not started");
                }

                lock (_processLockObject)
                {
                    _process.StandardInput.WriteLine(text);
                }
            }

            return Result.Success;
        }
        catch (Exception e)
        {
            return Errors.Fail(description: $"Failed to write line \"{text}\" with exception: {e}");
        }
    }

    private async Task<ErrorOr<Success>> WaitForServerToStart(Process process)
    {
        DataReceivedEventHandler? serverStartedHandler = null;
        try
        {
            var serverStarted = false;
            var hostActivated = false;

            serverStartedHandler = (_, args) =>
            {
                try
                {
                    var message = args.Data;
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        return;
                    }

                    if (message.StartsWith("Host activate: Loading"))
                    {
                        hostActivated = true;
                        process.StandardInput.WriteLine($"say {ServerStartedMessage}");
                        return;
                    }

                    if (hostActivated == false)
                    {
                        return;
                    }

                    if (message.Equals($"[All Chat][Console (0)]: {ServerStartedMessage}") == false)
                    {
                        return;
                    }

                    serverStarted = true;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception in serverStartedHandler");
                }
            };

            process.OutputDataReceived += serverStartedHandler;
            process.ErrorDataReceived += serverStartedHandler;

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds <= ServerStartTimeoutMs)
            {
                await Task.Delay(100);
                await process.StandardInput.WriteLineAsync(process.StandardInput.NewLine);

                if (serverStarted == false)
                {
                    continue;
                }

                return Result.Success;
            }

            return Errors.Fail($"Timout while waiting for the server to start after {ServerStartTimeoutMs} ms");
        }
        finally
        {
            process.OutputDataReceived -= serverStartedHandler;
            process.ErrorDataReceived -= serverStartedHandler;
        }
    }


    private void OnExited(object? sender, EventArgs eventArgs)
    {
        try
        {
            _logger.LogInformation("Server exited");

            lock (_processLockObject)
            {
                _process?.Dispose();
                _process = null;
                _eventService.OnServerExited();
            }

            RemoveEventDetection();
            ServerOutputEvent -= ServerOutputLog;
            ServerOutputEvent -= ServerOutputToDatabase;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in OnExited");
        }
    }

    public async Task<ErrorOr<Success>> Stop()
    {
        if (IsServerStopped())
        {
            return Result.Success;
        }

        _eventService.OnStoppingServer();
        _logger.LogInformation("Stopping server");
        var stopNormal = await StopNormal();
        if (stopNormal)
        {
            return Result.Success;
        }

        _logger.LogWarning("Force stopping the server because it failed to stop normally");
        var stopForce = await StopForce();
        if (stopForce)
        {
            _logger.LogWarning("Server stopped forcefully");
            if (_statusService.ServerStarted)
            {
                _eventService.OnServerExited();
            }
            return Result.Success;
        }

        _logger.LogError("Server failed to stop normally in time and failed to force stop");
        return Errors.Fail(description: "Server failed to stop normally in time and failed to force stop");
    }

    private async Task<bool> StopNormal()
    {
        var writeLine = WriteLine("quit");
        if (writeLine.IsError)
        {
            return false;
        }

        var sw = Stopwatch.StartNew();
        while (true)
        {
            await Task.Delay(10);
            if (sw.ElapsedMilliseconds >= ServerStopTimeoutMs)
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
            _process?.Kill(true);
            _process?.Close();
        }

        var waitForServerToStopTimeout = 5000;
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds <= waitForServerToStopTimeout)
        {
            await Task.Delay(50);
            if (IsServerStopped())
            {
                return true;
            }
        }

        lock (_processLockObject)
        {
            if (_process is not null)
            {
                if (_process.HasExited)
                {
                    _process = null;
                    return true;
                }
            }
            else
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

            if (_statusService.ServerStarted == false)
            {
                return true;
            }

            _logger.LogCritical(
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
            try
            {
                var retries = 0;
                _logger.LogInformation("OutputFlushBackgroundTask started");
                while (true)
                {
                    try
                    {
                        if (retries == 5)
                        {
                            break;
                        }

                        await Task.Delay(50);
                        lock (_processLockObject)
                        {
                            if (_process is null)
                            {
                                _logger.LogWarning("OutputFlushBackgroundTask break; process is not set");
                                break;
                            }
                        }

                        if (_statusService is { ServerStarted: false, ServerStarting: false })
                        {
                            _logger.LogWarning("OutputFlushBackgroundTask break; Server is not started");
                            break;
                        }

                        lock (_processLockObject)
                        {
                            _process.StandardInput.Write(_process.StandardInput.NewLine);
                        }

                        retries = 0;
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Exception in OutputFlushBackgroundTask. Restarting...");
                        retries++;
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in OutputFlushBackgroundTask");
            }

            _logger.LogInformation("OutputFlushBackgroundTask exited");
        });
    }
}