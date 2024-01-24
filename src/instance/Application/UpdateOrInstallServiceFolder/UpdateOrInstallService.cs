using System.Text;
using Application.EventServiceFolder;
using Application.StatusServiceFolder;
using Application.SystemLogFolder;
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
    private readonly SystemLogService _systemLogService;
    private readonly IServiceProvider _services;

    public UpdateOrInstallService(ILogger<UpdateOrInstallService> logger,
        IOptions<AppOptions> options,
        StatusService statusService,
        EventService eventService,
        HttpClient httpClient,
        SystemLogService systemLogService,
        IServiceProvider services
    )
    {
        _logger = logger;
        _options = options;
        _statusService = statusService;
        _eventService = eventService;
        _httpClient = httpClient;
        _systemLogService = systemLogService;
        _services = services;
    }

    #region Properties

    public event EventHandler<UpdateOrInstallOutputEventArg>? UpdateOrInstallOutput;
    private volatile CancellationTokenSource _cancellationTokenSource = new();
    private readonly SemaphoreSlim _updateOrInstallLock = new(1);
    private readonly object _idLock = new();
    private UpdateOrInstallStartDbModel? _updateOrInstallStart;

    private UpdateOrInstallStartDbModel? UpdateOrInstallStart
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
            _logger.LogInformation("Starting server update or install");
            _systemLogService.LogHeader();
            _systemLogService.Log("Starting server update");
            var isServerReadyToUpdateOrInstall = IsServerReadyToUpdateOrInstall();
            if (isServerReadyToUpdateOrInstall.IsError)
            {
                _logger.LogError("Failed to start server update or install. {Error}",
                    isServerReadyToUpdateOrInstall.ErrorMessage());
                _systemLogService.Log(
                    $"Failed to start server update. Server is not ready to update.");
                return Errors.Fail(
                    $"Failed to start server update or install. {isServerReadyToUpdateOrInstall.ErrorMessage()}");
            }

            using var scope = _services.CreateScope();
            var unitOfWork = scope.GetUnitOfWork();
            var updateOrInstallStart = await unitOfWork.UpdateOrInstallRepo.AddStart(DateTime.UtcNow);
            UpdateOrInstallStart = updateOrInstallStart;

            _ = UpdateOrInstallServerTask(
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

    private ErrorOr<Success> IsServerReadyToUpdateOrInstall()
    {
        if (_statusService.ServerUpdatingOrInstalling)
        {
            return Errors.Fail("Another update or install process is still running");
        }

        if (_statusService.ServerStarting)
        {
            return InstanceErrors.ServerIsBusy(InstanceErrors.ServerBusyTypes.Starting);
        }

        if (_statusService.ServerStopping)
        {
            return InstanceErrors.ServerIsBusy(InstanceErrors.ServerBusyTypes.Stopping);
        }

        if (_statusService.ServerStarted)
        {
            return InstanceErrors.ServerIsBusy(InstanceErrors.ServerBusyTypes.Started);
        }

        if (_statusService.ServerPluginsUpdatingOrInstalling)
        {
            return InstanceErrors.ServerIsBusy(InstanceErrors.ServerBusyTypes.PluginsUpdatingOrInstalling);
        }

        return Result.Success;
    }

    private async Task UpdateOrInstallServerTask(
        Guid id,
        AppOptions options,
        Func<Task>? afterUpdateOrInstallSuccessfulAction)
    {
        try
        {
            _eventService.OnUpdateOrInstallStarted(id);
            _cancellationTokenSource = new CancellationTokenSource();
            _logger.LogInformation("UpdateOrInstallId: {UpdateOrInstallId}", id);
            _systemLogService.Log($"Update id: {id}");

            #region install steamcmd

            const string steamcmdShName = "steamcmd.sh";
            const string pythonScriptName = "steamcmd.py";

            _logger.LogInformation("Installing steamcmd");
            _systemLogService.Log("Installing steamcmd");
            if (CheckIfSteamcmdIsInstalled(options.STEAMCMD_FOLDER) == false)
            {
                var pythonScriptSrcPath =
                    Path.Combine(options.EXECUTING_FOLDER, "UpdateOrInstallServiceFolder", pythonScriptName);
                var installSteamcmd = await InstallSteamcmd(options.STEAMCMD_FOLDER, pythonScriptSrcPath);
                if (installSteamcmd.IsError)
                {
                    _logger.LogInformation("Failed to install steamcmd. {Error}", installSteamcmd.ErrorMessage());
                    _systemLogService.Log($"Failed to install steamcmd. {installSteamcmd.ErrorMessage()}");
                    _eventService.OnUpdateOrInstallFailed(id);
                    return;
                }
                else
                {
                    _logger.LogInformation("steamcmd installed successfully");
                    _systemLogService.Log("steamcmd installed successfully");
                }
            }
            else
            {
                _logger.LogInformation("steamcmd already installed");
                _systemLogService.Log("steamcmd already installed");
            }

            #endregion

            #region executing update or install process

            _logger.LogInformation("Starting the update or install process");
            _systemLogService.Log("Starting update process");
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
                    executeUpdateOrInstallProcess.ErrorMessage());
                _systemLogService.Log(
                    $"Failed to start update process. {executeUpdateOrInstallProcess.ErrorMessage()}");
                _eventService.OnUpdateOrInstallFailed(id);
                return;
            }

            #endregion

            _systemLogService.Log("Server updated finished");
            _logger.LogInformation("Done updating or installing server");
            _eventService.OnUpdateOrInstallDone(id);

            if (afterUpdateOrInstallSuccessfulAction != null)
            {
                _logger.LogInformation("Invoking after update or install action");
                _systemLogService.Log("After update action started");
                await afterUpdateOrInstallSuccessfulAction.Invoke();
                _logger.LogInformation("After update or install action finished");
                _systemLogService.Log("After update action finished");
            }
        }
        catch (Exception e)
        {
            if (e is OperationCanceledException)
            {
                _logger.LogInformation("Server update or install cancelled");
                _systemLogService.Log("Server update cancelled");
                _eventService.OnUpdateOrInstallCancelled(id);
            }
            else
            {
                _logger.LogError(e, "Failed to update or install server");
                _systemLogService.Log("Failed to update server for unknown reasons");
                _eventService.OnUpdateOrInstallFailed(id);
            }
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
            _systemLogService.Log("Failed to cancel server update. Update Ids do not match");
            return Errors.Fail(
                $"Failed to cancel update or install. Ids dont match. IdToCancel: {id} CurrentId: {UpdateOrInstallStart?.Id}");
        }

        _cancellationTokenSource.Cancel();
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

    private bool CheckIfSteamcmdIsInstalled(string steamCmdFolder)
    {
        var steamcmdShPath = Path.Combine(steamCmdFolder, "steamcmd.sh");
        var steamcmdPythonScriptPath = Path.Combine(steamCmdFolder, "steamcmd.py");

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
    private async Task<ErrorOr<Success>> InstallSteamcmd(
        string steamcmdFolder,
        string pythonScriptSrcPath)
    {
        var pythonScriptSrc = new FileInfo(pythonScriptSrcPath);
        if (pythonScriptSrc.Exists == false)
        {
            return Errors.Fail("Python script source file not found");
        }

        if (Directory.Exists(steamcmdFolder))
        {
            Directory.Delete(steamcmdFolder, true);
        }

        Directory.CreateDirectory(steamcmdFolder);
        await DownloadAndUnpackSteamcmd(steamcmdFolder);

        var pythonScriptDestPath = Path.Combine(steamcmdFolder, pythonScriptSrc.Name);
        File.Delete(pythonScriptDestPath);
        pythonScriptSrc.CopyTo(pythonScriptDestPath);

        if (CheckIfSteamcmdIsInstalled(steamcmdFolder) == false)
        {
            return Errors.Fail("steamcmd not installed after download");
        }

        var setPermissions = await SetLinuxPermissionRecursive(steamcmdFolder, "770");
        if (setPermissions.IsError)
        {
            return setPermissions.FirstError;
        }

        return Result.Success;
    }

    private static async Task<ErrorOr<string>> SetLinuxPermissionRecursive(string directoryPath, string umask)
    {
        var result = await Cli.Wrap("chmod")
            .WithArguments(new[] { "-R", umask, directoryPath })
            .ExecuteBufferedAsync();
        if (result.ExitCode != 0)
        {
            return Errors.Fail($"Error while trying to set file permissions. Output: {result.StandardError}");
        }

        if (string.IsNullOrWhiteSpace(result.StandardError))
        {
            return result.StandardOutput;
        }

        return Errors.Fail($"Failed to set permissions. {result.StandardError}");
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