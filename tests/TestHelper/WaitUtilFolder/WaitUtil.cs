using Domain;
using ErrorOr;

namespace TestHelper.WaitUtilFolder;

using System.Diagnostics;

public static class WaitUtil
{
    public static async Task<ErrorOr<Success>> WaitUntil(
        TimeSpan timeout,
        Func<bool> breakAction) =>
        await WaitUntil(timeout, breakAction, null);

    public static async Task<ErrorOr<Success>> WaitUntil(
        Func<bool> breakAction,
        Action<string>? logAction)
    {
        return await WaitUntil(TimeSpan.FromSeconds(1), breakAction, logAction);
    }

    public static async Task<ErrorOr<Success>> WaitUntil(
        TimeSpan timeout,
        Func<bool> breakAction,
        Action<string>? logAction)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            logAction?.Invoke("waiting");
            await Task.Delay(10);
            var breakActionRes = breakAction.Invoke();
            if (breakActionRes == false)
            {
                continue;
            }

            logAction?.Invoke("break");
            return Result.Success;
        }

        logAction?.Invoke("failed");
        return Errors.Fail($"Timout after {timeout.ToString()}");
    }
}