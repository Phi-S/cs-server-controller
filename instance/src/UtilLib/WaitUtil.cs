using System.Diagnostics;
using ResultLib;

namespace UtilLib;

public static class WaitUtil
{
    public static async Task<Result> WaitUntil(TimeSpan timeout, Func<bool> breakAction) =>
        await WaitUntil(timeout, breakAction, null);

    public static async Task<Result> WaitUntil(TimeSpan timeout, Func<bool> breakAction, Action<string>? logAction)
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

            logAction?.Invoke("ok");
            return Result.Ok();
        }

        logAction?.Invoke("failed");
        return Result.Fail(new TimeoutException($"Timout after {timeout.ToString()}"));
    }
}