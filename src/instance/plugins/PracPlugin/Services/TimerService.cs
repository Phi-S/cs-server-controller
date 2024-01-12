using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;


namespace PracPlugin.Services;

public class TimerService : IDisposable
{
    public readonly List<Timer> Timers = new();
    
    public Timer AddTimer(float interval, Action callback, TimerFlags? flags = null)
    {
        var timer = new Timer(interval, callback, flags ?? 0);
        Timers.Add(timer);
        return timer;
    }

    public void Dispose()
    {
        foreach (var timer in Timers)
        {
            timer.Kill();
        }
    }
}