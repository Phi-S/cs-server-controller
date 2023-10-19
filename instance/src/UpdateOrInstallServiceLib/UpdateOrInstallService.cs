using System.Text;
using AppOptionsLib;
using CliWrap;
using CliWrap.Buffered;
using DatabaseLib.Models;
using DatabaseLib.Repos;
using ExceptionsLib;
using EventsServiceLib;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResultLib;
using StatusServiceLib;

namespace UpdateOrInstallServiceLib;

public class UpdateOrInstallService(
    ILogger<UpdateOrInstallService> logger,
    IOptions<AppOptions> options,
    StatusService statusService,
    EventService eventService,
    HttpClient httpClient,
    UpdateOrInstallRepo updateOrInstallRepo)
{
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

    public async Task<Result<Guid>> StartUpdateOrInstall(Func<Task>? afterUpdateOrInstallSuccessfulAction = null)
    {
        try
        {
            await _updateOrInstallLock.WaitAsync();
            if (statusService.ServerUpdatingOrInstalling)
            {
                logger.LogWarning(
                    "Failed to start updating or installing. Another update or install process is still running");
                return Result<Guid>.Fail(
                    "Failed to start updating or installing. Another update or install process is still running");
            }

            UpdateOrInstallStart updateOrInstallStart = await updateOrInstallRepo.AddStart(DateTime.UtcNow);


            UpdateOrInstallStart = updateOrInstallStart;
            _ = UpdateOrInstallServer(
                updateOrInstallStart.Id,
                options.Value.STEAMCMD_FOLDER,
                options.Value.STEAMCMD_SH_PATH,
                options.Value.STEAMCMD_PYTHON_SCRIPT_PATH,
                options.Value.SERVER_FOLDER,
                options.Value.STEAM_USERNAME,
                options.Value.STEAM_PASSWORD,
                afterUpdateOrInstallSuccessfulAction);

            return Result<Guid>.Ok(updateOrInstallStart.Id);
        }
        finally
        {
            _updateOrInstallLock.Release();
        }
    }

    private async Task UpdateOrInstallServer(
        Guid id,
        string steamcmdFolder,
        string steamcmdShFileLocation,
        string pythonScriptLocation,
        string serverFolder,
        string steamUsername,
        string steamPassword,
        Func<Task>? afterUpdateOrInstallSuccessfulAction)
    {
        try
        {
            eventService.OnUpdateOrInstallStarted(id);
            _cancellationTokenSource = new CancellationTokenSource();
            logger.LogInformation("Starting to update or install server");
            if (statusService.ServerStarting)
            {
                throw new ServerIsBusyException(ServerBusyAction.STARTING);
            }

            if (statusService.ServerStopping)
            {
                throw new ServerIsBusyException(ServerBusyAction.STOPPING);
            }

            if (statusService.ServerStarted)
            {
                throw new ServerIsBusyException(ServerBusyAction.STARTED);
            }

            logger.LogInformation("Installing steamcmd");
            if (CheckIfSteamcmdIsInstalled(
                    steamcmdFolder,
                    steamcmdShFileLocation,
                    pythonScriptLocation) == false)
            {
                await InstallSteamcmd(steamcmdFolder, steamcmdShFileLocation, pythonScriptLocation);
                logger.LogInformation("steamcmd installed successfully");
            }
            else
            {
                logger.LogInformation("steamcmd already installed");
            }

            UpdateOrInstallOutput += OnUpdateOrInstallOutputLog;
            UpdateOrInstallOutput += OnUpdateOrInstallOutputToDatabase;

            logger.LogInformation("Starting the update or install process");
            await ExecuteUpdateOrInstallProcess(
                id,
                steamcmdShFileLocation,
                pythonScriptLocation,
                serverFolder,
                steamUsername,
                steamPassword);

            logger.LogInformation("Done updating or installing csgo server");

            if (afterUpdateOrInstallSuccessfulAction != null)
            {
                await afterUpdateOrInstallSuccessfulAction.Invoke();
            }

            eventService.OnUpdateOrInstallDone(id);
            UpdateOrInstallStart = null;
            UpdateOrInstallOutput -= OnUpdateOrInstallOutputLog;
            UpdateOrInstallOutput -= OnUpdateOrInstallOutputToDatabase;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update or install csgo server");
            eventService.OnUpdateOrInstallFailed(id);
            UpdateOrInstallOutput -= OnUpdateOrInstallOutputLog;
            UpdateOrInstallOutput -= OnUpdateOrInstallOutputToDatabase;
        }
    }

    public Result CancelUpdate(Guid id)
    {
        if (id.Equals(UpdateOrInstallStart?.Id) == false)
        {
            logger.LogWarning(
                "Failed to cancel update or install. Ids dont match. IdToCancel: {CancelId} CurrentId: {CurrentId}",
                id, UpdateOrInstallStart?.Id);
            return Result.Fail(
                $"Failed to cancel update or install. Ids dont match. IdToCancel: {id} CurrentId: {UpdateOrInstallStart?.Id}");
        }

        _cancellationTokenSource.Cancel();
        eventService.OnUpdateOrInstallCancelled(id);
        return Result.Ok();
    }

    private async Task ExecuteUpdateOrInstallProcess(
        Guid updateOrInstallId,
        string steamcmdShFilePath,
        string steamcmdPythonScriptPath,
        string serverFolder,
        string steamUsername,
        string steamPassword)
    {
        var command =
            $"{steamcmdPythonScriptPath} " +
            $"\"" +
            $"{steamcmdShFilePath} " +
            $"+force_install_dir {serverFolder} " +
            $"+login {steamUsername} {steamPassword} " +
            $"+app_update 730 " +
            $"validate " +
            $"+quit" +
            $"\"";

        logger.LogDebug("Steamcmd command: {Command}", command);
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
                UpdateOrInstallOutput?.Invoke(this, new UpdateOrInstallOutputEventArg(updateOrInstallId, output));
            }))
            .ExecuteAsync(_cancellationTokenSource.Token);

        if (cli.ExitCode is not 0)
        {
            throw new UpdateOrInstallFailedException($"Update or install failed with error code {cli.ExitCode}");
        }

        if (successMessageReceived == false)
        {
            throw new UpdateOrInstallFailedException(
                $"Failed to receive success message from update or install process");
        }
    }

    #region streamcmd

    private bool CheckIfSteamcmdIsInstalled(string steamCmdFolder, string steamcmdShFileLocation,
        string pythonUpdateScriptDestinationPath)
    {
        return Directory.Exists(steamCmdFolder)
               && File.Exists(steamcmdShFileLocation)
               && File.Exists(pythonUpdateScriptDestinationPath);
    }

    private async Task DownloadAndUnpackSteamcmd(string steamCmdFolder)
    {
        var fileOutputPath = Path.Combine(steamCmdFolder, "steamcmd_linux.tar.gz");
        const string downloadUrl = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz";

        await using (var downloadStream = await httpClient.GetStreamAsync(downloadUrl))
        await using (var fw = File.Create(fileOutputPath))
        {
            await downloadStream.CopyToAsync(fw);
        }

        await using (var fs = File.OpenRead(fileOutputPath))
        await using (var gzipFs = new GZipInputStream(fs))
        using (var tarArchive = TarArchive.CreateInputTarArchive(gzipFs, Encoding.Default))
        {
            tarArchive.ExtractContents(steamCmdFolder);
        }

        File.Delete(fileOutputPath);
    }

    /// <summary>
    /// Will install or reinstall steamcmd. If steamcmd already exists, it will be deleted and reinstalled.
    /// </summary>
    /// <exception cref="Exception"></exception>
    private async Task InstallSteamcmd(string steamCmdFolder, string steamcmdShFileLocation,
        string pythonScriptLocation)
    {
        if (Directory.Exists(steamCmdFolder))
            Directory.Delete(steamCmdFolder, true);

        Directory.CreateDirectory(steamCmdFolder);

        await DownloadAndUnpackSteamcmd(steamCmdFolder);
        CreatePythonUpdateScriptFile(pythonScriptLocation);

        if (CheckIfSteamcmdIsInstalled(steamCmdFolder, steamcmdShFileLocation, pythonScriptLocation) == false)
        {
            throw new Exception("Failed to install steamcmd.");
        }

        await SetLinuxPermissionRecursive(steamCmdFolder, "770");
    }

    private async Task SetLinuxPermissionRecursive(string directoryPath, string umask)
    {
        var result = await Cli.Wrap($"chmod").WithArguments(new[] {"-R", umask, directoryPath}).ExecuteBufferedAsync();
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
            logger.LogInformation("UpdateOrInstall: Id: {Id} | Output: {Output}",
                arg.UpdateOrInstallId, arg.Message);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Exception");
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
                logger.LogError(
                    "Error while trying to add update or install output to database. The ID of the current update or install dose not match the id of the output. Id of output: {OutputId} | Output: {Output}",
                    arg.UpdateOrInstallId, arg.Message);
                return;
            }

            await updateOrInstallRepo.AddLog(currentUpdateOrInstallStart.Id, arg.Message);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Exception");
        }
    }

    /// <summary>
    /// The Python update script is needed because the steamcmd output is not recorded properly if steamcmd is started with c#.
    /// Some buffer issue?! https://github.com/ValveSoftware/Source-1-Games/issues/1684
    /// </summary>
    private void CreatePythonUpdateScriptFile(string pythonScriptLocation)
    {
        var pythonScriptSrcFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrWhiteSpace(pythonScriptSrcFolder))
        {
            throw new NullReferenceException(nameof(pythonScriptSrcFolder));
        }

        var pythonScriptSrc = Path.Combine(pythonScriptSrcFolder, "steamcmd.py");
        if (File.Exists(pythonScriptSrc) == false)
        {
            throw new Exception(
                $"Python script at \"{pythonScriptSrc}\" doesn't exists.");
        }

        File.Copy(pythonScriptSrc, pythonScriptLocation, true);
    }
}