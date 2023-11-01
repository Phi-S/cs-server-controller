﻿using System.Diagnostics;
using System.Text;
using ExceptionsLib;
using Microsoft.Extensions.Logging;
using ResultLib;

namespace ServerServiceLib;

public partial class ServerService
{
    private const string START_PREFIX = "#####_START_";
    private const string END_PREFIX = "#####_END_";

    private const int GET_RESULT_TIMEOUT_MS = 10_000;

    private readonly SemaphoreSlim _executeCommandLock = new(1);
    private volatile bool _currentlyExecutingCommand;

    public async Task<Result<string>> ExecuteCommand(string command)
    {
        var commandId = Guid.NewGuid();
        var resultReceived = false;
        var captureOutput = false;
        var result = new StringBuilder();

        void CaptureCommandResult(object? _, ServerOutputEventArg output)
        {
            if (output.Output.Equals($"{START_PREFIX}{commandId}"))
            {
                captureOutput = true;
                return;
            }

            if (output.Output.Equals($"{END_PREFIX}{commandId}"))
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
            if (_currentlyExecutingCommand)
            {
                var e = new ServerIsBusyException(ServerBusyAction.EXECUTING_COMMAND);
                logger.LogError(e, "Failed to execute command {Command}", command);
                _executeCommandLock.Release();
                return e;
            }

            if (statusService.ServerStarted == false)
            {
                logger.LogError("Failed to execute command {Command}. Server is not started", command);
                _executeCommandLock.Release();
                return new ServerNotStartedException($"Failed to execute command {command}. Server is not started.");
            }

            if (statusService.ServerUpdatingOrInstalling)
            {
                var e = new ServerIsBusyException(ServerBusyAction.UPDATING_OR_INSTALLING);
                logger.LogError(e, "Failed to execute command {Command}", command);
                _executeCommandLock.Release();
                return e;
            }

            if (statusService.ServerStopping)
            {
                var e = new ServerIsBusyException(ServerBusyAction.STOPPING);
                logger.LogError(e, "Failed to execute command {Command}", command);
                _executeCommandLock.Release();
                return e;
            }

            _executeCommandLock.Release();

            _currentlyExecutingCommand = true;
            logger.LogInformation("Executing command [{CommandId}] | {Command}", commandId, command);

            var finalCommand =
                $"echo {START_PREFIX}{commandId}{Environment.NewLine}" +
                $"{command}{Environment.NewLine}" +
                $"echo {END_PREFIX}{commandId}";

            ServerOutputEvent += CaptureCommandResult;
            var writeLine = WriteLine(finalCommand);
            if (writeLine.IsFailed)
            {
                logger.LogError(writeLine.Exception, "Failed to execute command {Command}", command);
                return Result<string>.Fail(
                    $"Failed to execute command {command}. Failed to write line {writeLine.Exception}");
            }

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds <= GET_RESULT_TIMEOUT_MS)
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

            logger.LogError("Failed to execute command {Command}. Failed to receive result in time", command);
            return Result<string>.Fail($"Failed to execute command {command}. Failed to receive result in time");
        }
        finally
        {
            ServerOutputEvent -= CaptureCommandResult;
            _currentlyExecutingCommand = false;
        }
    }
}