using System.Diagnostics;
using System.Text;
using ExceptionsLib;
using Microsoft.Extensions.Logging;

namespace ServerServiceLib;

public partial class ServerService
{
    private const string START_PREFIX = "#####_START_";
    private const string END_PREFIX = "#####_END_";

    private const int GET_RESULT_TIMEOUT_MS = 10000;

    private readonly SemaphoreSlim _executeCommandLock = new(1);
    private volatile bool _currentlyExecutingCommand = false;

    public async Task<string?> ExecuteCommand(string command, bool withResult = false)
    {
        var commandId = Guid.NewGuid();
        var resultReceived = false;
        var captureOutput = false;
        var result = new StringBuilder();

        void CaptureCommandResult(object? _, string s)
        {
            s = s.Trim();

            if (s.Equals($"{START_PREFIX}{commandId}"))
            {
                captureOutput = true;
                return;
            }

            if (s.Equals($"{END_PREFIX}{commandId}"))
            {
                captureOutput = false;
                resultReceived = true;
                return;
            }

            if (captureOutput)
            {
                result.AppendLine(s);
            }
        }

        try
        {
            await _executeCommandLock.WaitAsync();
            if (_currentlyExecutingCommand)
            {
                throw new ServerIsBusyException(ServerBusyAction.EXECUTING_COMMAND);
            }

            if (statusService.ServerStarted == false)
            {
                throw new ServerNotStartedException();
            }

            if (statusService.ServerUpdatingOrInstalling)
            {
                throw new ServerIsBusyException(ServerBusyAction.UPDATING_OR_INSTALLING);
            }

            if (statusService.ServerStopping)
            {
                throw new ServerIsBusyException(ServerBusyAction.STOPPING);
            }

            logger.LogInformation("Executing command [{CommandId}] | {Command}", commandId, command);

            command =
                $"echo {START_PREFIX}{commandId}{Environment.NewLine}" +
                $"{command}{Environment.NewLine}" +
                $"echo {END_PREFIX}{commandId}";

            // If no result is required, just fire and forget
            if (withResult == false)
            {
                WriteLine(command);
                return null;
            }

            ServerOutputEvent += CaptureCommandResult;
            WriteLine(command);
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds <= GET_RESULT_TIMEOUT_MS)
            {
                await Task.Delay(10);
                if (resultReceived)
                {
                    break;
                }
            }

            if (resultReceived == false)
            {
                throw new ServerCommandExecutionFailedException("Failed to receive result in time");
            }

            return result.ToString();
        }
        finally
        {
            ServerOutputEvent -= CaptureCommandResult;
            _executeCommandLock.Release();
        }
    }
}