using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;


namespace PracPlugin.Services;

public class TimerService : IDisposable
{
    private readonly List<Timer> _timers = new();
    
    public Timer AddTimer(float interval, Action callback, TimerFlags? flags = null)
    {
        var timer = new Timer(interval, callback, flags ?? 0);
        _timers.Add(timer);
        return timer;
    }

    public void Dispose()
    {
        foreach (var timer in _timers)
        {
            timer.Kill();
        }
    }
}