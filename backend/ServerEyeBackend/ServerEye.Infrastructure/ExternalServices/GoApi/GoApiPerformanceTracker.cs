namespace ServerEye.Infrastructure.ExternalServices.GoApi;

using System.Diagnostics;

/// <summary>
/// Performance tracking for Go API operations.
/// </summary>
public class GoApiPerformanceTracker : IDisposable
{
    private readonly Stopwatch stopwatch;

    public GoApiPerformanceTracker() => stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// Gets elapsed milliseconds.
    /// </summary>
    public long ElapsedMilliseconds => stopwatch.ElapsedMilliseconds;

    /// <summary>
    /// Creates performance tracker for operation.
    /// </summary>
    public static GoApiPerformanceTracker Start()
    {
        return new GoApiPerformanceTracker();
    }

    /// <summary>
    /// Calculates parse time from total and request time.
    /// </summary>
    public static long CalculateParseTime(long totalTimeMs, long requestTimeMs)
    {
        return totalTimeMs - requestTimeMs;
    }

    /// <summary>
    /// Stops the stopwatch and returns elapsed time.
    /// </summary>
    public long Stop()
    {
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            stopwatch.Stop();
        }
    }
}
