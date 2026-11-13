using System.Collections.Concurrent;

namespace Qwek.Interfaces.RealTime;

public sealed class HubRateLimiter
{
    private readonly ConcurrentDictionary<string, Window> _windows = new();

    public bool TryAcquire(
        string userId, string action, int limit, TimeSpan window)
    {
        long now = Environment.TickCount64;
        string key = $"{userId}:{action}";
        Window entry = _windows.GetOrAdd(key, _ => new Window(now));

        lock (entry)
        {
            if (now - entry.Start >= (long)window.TotalMilliseconds)
            {
                entry.Start = now;
                entry.Count = 0;
            }

            if (entry.Count >= limit)
            {
                return false;
            }

            entry.Count++;
            return true;
        }
    }

    private sealed class Window(long start)
    {
        public long Start { get; set; } = start;
        public int Count { get; set; }
    }
}
