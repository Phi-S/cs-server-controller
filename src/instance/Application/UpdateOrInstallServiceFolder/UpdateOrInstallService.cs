using System.Text;
using Application.EventServiceFolder;
using Application.ServerPluginsFolder;
using Application.StatusServiceFolder;
using CliWrap;
using CliWrap.Buffered;
using Domain;
using ErrorOr;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Infrastructure.Database;
using Infrastructure.Database.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace Application.UpdateOrInstallServiceFolder;

public class UpdateOrInstallService
{
    private readonly ILogger<UpdateOrInstallService> _logger;
    private readonly IOptions<AppOptions> _options;
    private readonly StatusService _statusService;
    private readonly EventService _eventService;
    private readonly HttpClient _httpClient;
    private readonly ServerPluginsService _serverPluginsService;
    private readonly IServiceProvider _services;

    public UpdateOrInstallService(ILogger<UpdateOrInstallService> logger,
        IOptions<AppOptions> options,
        StatusService statusService,
        EventService eventService,
        HttpClient httpClient,
        ServerPluginsService serverPluginsService,
        IServiceProvider services
    )
    {
        _logger = logger;
        _options = options;
        _statusService = statusService;
        _eventService = eventService;
        _httpClient = httpClient;
        _serverPluginsService = serverPluginsService;
        _services = services;
    }

    #region Properties

    public event EventHandler<UpdateOrInstallOutputEventArg>? UpdateOrInstallOutput;
    private volatile CancellationTokenSource _cancellationTokenSource = new();
    private readonly SemaphoreSlim _updateOrInstallLock = new(1);
    private readonly object _idLock = new();
    private UpdateOrInstallStart? _updateOrInstallStart;

    private UpdateOrInstallStart? UpdateOrInstallStart
    {
        get
        {
            lock (_idLock)
            {
                return _updateOrInstallStart;
            }
        }
        set
        {
            lock (_idLock)
            {
                _updateOrInstallStart = value;
            }
        }
    }

    #endregion


    public async Task<ErrorOr<Guid>> StartUpdateOrInstall(Func<Task>? afterUpdateOrInstallSuccessfulAction = null)
    {
        try
        {
            await _updateOrInstallLock.WaitAsync();
            _logger.LogInformation("Starting updating or install the server");
            if (_statusService.ServerUpdatingOrInstalling)
            {
                _logger.LogWarning(
                    "Failed to start updating or installing. Another update or install process is still running");
                return Errors.Fail(
                    "Failed to start updating or installing. Another update or install process is still running");
            }

            if (_statusService.ServerStarting)
            {
                _logger.LogWarning(
                    "Failed to start updating or installing. Server is starting");
                return Errors.Fail(
                    "Failed to start updating or installing. Server is starting");
            }

            if (_statusService.ServerStopping)
            {
                _logger.LogWarning(
                    "Failed to start updating or installing. Server is stopping");
                return Errors.Fail(
                    "Failed to start updating or installing. Server is stopping");
            }

            if (_statusService.ServerStarted)
            {
                _logger.LogWarning(
                    "Failed to start updating or installing. Server is started");
                return Errors.Fail(
                    "Failed to start updating or installing. Server is started");
            }

            using var scope = _services.CreateScope();
            var unitOfWork = scope.GetUnitOfWork();
            var updateOrInstallStart = await unitOfWork.UpdateOrInstallRepo.AddStart(DateTime.UtcNow);
            UpdateOrInstallStart = updateOrInstallStart;

            _ = UpdateOrInstallServer(
                updateOrInstallStart.Id,
                _options.Value,
                afterUpdateOrInstallSuccessfulAction);

            await unitOfWork.Save();
            return updateOrInstallStart.Id;
        }
        finally
        {
            _updateOrInstallLock.Release();
        }
    }

    private async Task UpdateOrInstallServer(
        Guid id,
        AppOptions options,
        Func<Task>? afterUpdateOrInstallSuccessfulAction)
    {
        try
        {
            _eventService.OnUpdateOrInstallStarted(id);
            _cancellationTokenSource = new CancellationTokenSource();
            _logger.LogInformation("UpdateOrInstallId: {UpdateOrInstallId}", id);

            #region install steamcmd

            const string steamcmdShName = "steamcmd.sh";
            const string pythonScriptName = "steamcmd.py";

            _logger.LogInformation("Installing steamcmd");
            if (CheckIfSteamcmdIsInstalled(
                    options.STEAMCMD_FOLDER,
                    steamcmdShName,
                    pythonScriptName) == false)
            {
                await InstallSteamcmd(options.STEAMCMD_FOLDER,
                    steamcmdShName,
                    pythonScriptName);
                _logger.LogInformation("steamcmd installed successfully");
            }
            else
            {
                _logger.LogInformation("steamcmd already installed");
            }

            var pythonScriptSrcPath =
                Path.Combine(options.EXECUTING_FOLDER, "UpdateOrInstallServiceFolder", pythonScriptName);
            var pythonScriptDestPath = Path.Combine(options.STEAMCMD_FOLDER, pythonScriptName);

            _logger.LogInformation("Linking python script {SrcFile} > {DestFile}",
                pythonScriptSrcPath, pythonScriptDestPath);
            File.Delete(pythonScriptDestPath);
            File.CreateSymbolicLink(pythonScriptDestPath, pythonScriptSrcPath);

            #endregion

            #region executing update or install process

            _logger.LogInformation("Starting the update or install process");
            var executeUpdateOrInstallProcess = await ExecuteUpdateOrInstallProcess(
                id,
                options.STEAMCMD_FOLDER,
                steamcmdShName,
                pythonScriptName,
                options.SERVER_FOLDER,
                options.STEAM_USERNAME,
                options.STEAM_PASSWORD
            );
            if (executeUpdateOrInstallProcess.IsError)
            {
                _logger.LogError("Failed to update or install server. {Error}",
                    executeUpdateOrInstallProcess.FirstError.Description);
                _eventService.OnUpdateOrInstallFailed(id);
                return;
            }

            #endregion

            var installPluginsResult = await _serverPluginsService.InstallOrUpdate();
            if (installPluginsResult.IsError)
            {
                _logger.LogError("Failed to install server plugins. {Error}",
                    installPluginsResult.FirstError.Description);
                _eventService.OnUpdateOrInstallFailed(id);
            }

            if (afterUpdateOrInstallSuccessfulAction != null)
            {
                await afterUpdateOrInstallSuccessfulAction.Invoke();
            }

            _logger.LogInformation("Done updating or installing server");
            _eventService.OnUpdateOrInstallDone(id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update or install server");
            _eventService.OnUpdateOrInstallFailed(id);
        }
        finally
        {
            UpdateOrInstallStart = null;
        }
    }

    public ErrorOr<Success> CancelUpdate(Guid id)
    {
        if (id.Equals(UpdateOrInstallStart?.Id) == false)
        {
            _logger.LogWarning(
                "Failed to cancel update or install. Ids dont match. IdToCancel: {CancelId} CurrentId: {CurrentId}",
                id, UpdateOrInstallStart?.Id);
            return Errors.Fail(
                $"Failed to cancel update or install. Ids dont match. IdToCancel: {id} CurrentId: {UpdateOrInstallStart?.Id}");
        }

        _cancellationTokenSource.Cancel();
        _eventService.OnUpdateOrInstallCancelled(id);
        return Result.Success;
    }

    private async Task<ErrorOr<Success>> ExecuteUpdateOrInstallProcess(
        Guid updateOrInstallId,
        string steamcmdFolder,
        string steamcmdShName,
        string pythonScriptName,
        string serverFolder,
        string steamUsername,
        string steamPassword)
    {
        try
        {
            UpdateOrInstallOutput += OnUpdateOrInstallOutputLog;
            UpdateOrInstallOutput += OnUpdateOrInstallOutputToDatabase;

            var pythonScriptPath = Path.Combine(steamcmdFolder, pythonScriptName);
            var steamcmdShPath = Path.Combine(steamcmdFolder, steamcmdShName);
            var command =
                $"{pythonScriptPath} " +
                $"\"" +
                $"{steamcmdShPath} " +
                $"+force_install_dir {serverFolder} " +
                $"+login {steamUsername} {steamPassword} " +
                $"+app_update 730 " +
                $"validate " +
                $"+quit" +
                $"\"";

            _logger.LogDebug("Steamcmd command: {Command}", command);
            const string successMessage = "Success! App '730' fully installed.";

            var successMessageReceived = false;
            var cli = await Cli.Wrap("python3")
                .WithArguments(command)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(output =>
                {
                    UpdateOrInstallOutput?.Invoke(this, new UpdateOrInstallOutputEventArg(updateOrInstallId, output));
                    if (string.IsNullOrWhiteSpace(output) || output.Trim().Equals(successMessage) == false)
                    {
                        return;
                    }

                    successMessageReceived = true;
                }))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(output =>
                {
                    UpdateOrInstallOutput?.Invoke(this,
                        new UpdateOrInstallOutputEventArg(updateOrInstallId, output));
                }))
                .ExecuteAsync(_cancellationTokenSource.Token);

            if (cli.ExitCode is not 0)
            {
                return Errors.Fail($"Failed with exit code {cli.ExitCode}");
            }

            if (successMessageReceived == false)
            {
                return Errors.Fail("Failed to receive success message");
            }
        }
        finally
        {
            UpdateOrInstallOutput -= OnUpdateOrInstallOutputLog;
            UpdateOrInstallOutput -= OnUpdateOrInstallOutputToDatabase;
        }

        return Result.Success;
    }

    #region streamcmd

    private bool CheckIfSteamcmdIsInstalled(
        string steamCmdFolder,
        string steamcmdShName,
        string steamcmdPythonScriptName)
    {
        var steamcmdShPath = Path.Combine(steamCmdFolder, steamcmdShName);
        var steamcmdPythonScriptPath = Path.Combine(steamCmdFolder, steamcmdPythonScriptName);

        return Directory.Exists(steamCmdFolder)
               && File.Exists(steamcmdShPath)
               && File.Exists(steamcmdPythonScriptPath);
    }

    private async Task DownloadAndUnpackSteamcmd(string steamCmdFolder)
    {
        var steamcmdTarGzPath = Path.Combine(steamCmdFolder, "steamcmd_linux.tar.gz");
        const string downloadUrl = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz";

        await using (var downloadStream = await _httpClient.GetStreamAsync(downloadUrl))
        await using (var fw = File.Create(steamcmdTarGzPath))
        {
            await downloadStream.CopyToAsync(fw);
        }

        await using (var fs = File.OpenRead(steamcmdTarGzPath))
        await using (var gzipFs = new GZipInputStream(fs))
        using (var tarArchive = TarArchive.CreateInputTarArchive(gzipFs, Encoding.Default))
        {
            tarArchive.ExtractContents(steamCmdFolder);
        }

        File.Delete(steamcmdTarGzPath);
    }

    /// <summary>
    /// Will install or reinstall steamcmd. If steamcmd already exists, it will be deleted and reinstalled.
    /// </summary>
    /// <exception cref="Exception"></exception>
    private async Task InstallSteamcmd(
        string steamcmdFolder,
        string steamcmdShFileName,
        string steamcmdPythonScriptName)
    {
        if (Directory.Exists(steamcmdFolder))
        {
            Directory.Delete(steamcmdFolder, true);
        }

        Directory.CreateDirectory(steamcmdFolder);

        await DownloadAndUnpackSteamcmd(steamcmdFolder);
        _logger.LogInformation("Steamcmd downloaded and unpacked");

        if (CheckIfSteamcmdIsInstalled(steamcmdFolder, steamcmdShFileName, steamcmdPythonScriptName) == false)
        {
            throw new Exception("Failed to install steamcmd");
        }

        await SetLinuxPermissionRecursive(steamcmdFolder, "770");
    }

    private static async Task SetLinuxPermissionRecursive(string directoryPath, string umask)
    {
        var result = await Cli.Wrap($"chmod").WithArguments(new[] { "-R", umask, directoryPath })
            .ExecuteBufferedAsync();
        if (result.ExitCode != 0)
        {
            throw new Exception($"Error while trying to set file permissions. Output: {result.StandardError}");
        }
    }

    #endregion

    private void OnUpdateOrInstallOutputLog(object? _, UpdateOrInstallOutputEventArg arg)
    {
        try
        {
            using (_logger.BeginScope(new Dictionary<string, object> { ["UpdateOrInstallId"] = arg.UpdateOrInstallId }))
            {
                _logger.LogInformation("UpdateOrInstall: {Output}", arg.Message);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Exception OnUpdateOrInstallOutputLog");
        }
    }

    private async void OnUpdateOrInstallOutputToDatabase(object? _, UpdateOrInstallOutputEventArg arg)
    {
        try
        {
            var currentUpdateOrInstallStart = UpdateOrInstallStart;
            if (currentUpdateOrInstallStart is null ||
                currentUpdateOrInstallStart.Id.Equals(arg.UpdateOrInstallId) == false)
            {
                _logger.LogError(
                    "Error while trying to add update or install output to database. The ID of the current update or install dose not match the id of the output. Id of output: {OutputId} | Output: {Output}",
                    arg.UpdateOrInstallId, arg.Message);
                return;
            }

            using var scope = _services.CreateScope();
            var unitOfWork = scope.GetUnitOfWork();
            await unitOfWork.UpdateOrInstallRepo.AddLog(currentUpdateOrInstallStart.Id, arg.Message);
            await unitOfWork.Save();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Exception OnUpdateOrInstallOutputToDatabase");
        }
    }
}