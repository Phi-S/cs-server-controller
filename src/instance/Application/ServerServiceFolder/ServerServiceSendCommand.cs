using System.Diagnostics;
using System.Text;
using Domain;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Application.ServerServiceFolder;

public partial class ServerService
{
    private const string StartPrefix = "#####_START_";
    private const string EndPrefix = "#####_END_";

    private const int GetResultTimeoutMs = 10_000;

    private readonly SemaphoreSlim _executeCommandLock = new(1);
    private volatile bool _currentlyExecutingCommand;

    private ErrorOr<Success> IsServerReadyToExecuteCommand()
    {
        if (_statusService.ServerStarted == false)
        {
            return Errors.Fail("Server is not started");
        }

        if (_currentlyExecutingCommand)
        {
            return Errors.ServerIsBusy(Errors.ServerBusyTypes.ExecutingCommand);
        }

        if (_statusService.ServerUpdatingOrInstalling)
        {
            return Errors.ServerIsBusy(Errors.ServerBusyTypes.UpdatingOrInstalling);
        }

        if (_statusService.ServerStopping)
        {
            return Errors.ServerIsBusy(Errors.ServerBusyTypes.Stopping);
        }

        return Result.Success;
    }

    public async Task<ErrorOr<string>> ExecuteCommand(string command)
    {
        var commandId = Guid.NewGuid();
        var resultReceived = false;
        var captureOutput = false;
        var result = new StringBuilder();

        void CaptureCommandResult(object? _, ServerOutputEventArg output)
        {
            if (output.Output.Equals($"{StartPrefix}{commandId}"))
            {
                captureOutput = true;
                return;
            }

            if (output.Output.Equals($"{EndPrefix}{commandId}"))
            {
                captureOutput = false;
                resultReceived = true;
                return;
            }

            if (captureOutput)
            {
                result.AppendLine(output.Output);
            }
        }

        try
        {
            await _executeCommandLock.WaitAsync();

            var isServerReadyToExecuteCommand = IsServerReadyToExecuteCommand();
            if (isServerReadyToExecuteCommand.IsError)
            {
                _logger.LogError("Failed to execute command {Command}. {Error}", command,
                    isServerReadyToExecuteCommand.FirstError);
                _executeCommandLock.Release();
                return Errors.Fail(
                    $"Failed to execute command {command}. {isServerReadyToExecuteCommand.FirstError.Description}");
            }

            _executeCommandLock.Release();

            _currentlyExecutingCommand = true;
            _logger.LogInformation("Executing command [{CommandId}] | {Command}", commandId, command);

            var finalCommand =
                $"echo {StartPrefix}{commandId}{Environment.NewLine}" +
                $"{command}{Environment.NewLine}" +
                $"echo {EndPrefix}{commandId}";

            ServerOutputEvent += CaptureCommandResult;
            var writeLine = WriteLine(finalCommand);
            if (writeLine.IsError)
            {
                _logger.LogError("Failed to execute command {Command}. {Error}", command,
                    writeLine.FirstError.Description);
                return Errors.Fail(
                    $"Failed to execute command {command}. Failed to write line {writeLine.FirstError.Description}");
            }

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds <= GetResultTimeoutMs)
            {
                await Task.Delay(10);
                if (resultReceived)
                {
                    break;
                }
            }

            if (resultReceived)
            {
                return result.ToString();
            }

            _logger.LogError("Failed to execute command {Command}. Failed to receive result in time", command);
            return Errors.Fail($"Failed to execute command {command}. Failed to receive result in time");
        }
        finally
        {
            ServerOutputEvent -= CaptureCommandResult;
            _currentlyExecutingCommand = false;
        }
    }
}