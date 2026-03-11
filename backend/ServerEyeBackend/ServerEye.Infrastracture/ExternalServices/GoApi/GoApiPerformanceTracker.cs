namespace ServerEye.Infrastracture.ExternalServices.GoApi;

using System.Diagnostics;

/// <summary>
/// Performance tracking for Go API operations.
/// </summary>
public class GoApiPerformanceTracker : IDisposable
{
    private readonly Stopwatch stopwatch;
    private readonly string operation;
    private readonly string url;

    public GoApiPerformanceTracker(string operation, string url)
    {
        this.operation = operation;
        this.url = url;
        this.stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Gets elapsed milliseconds.
    /// </summary>
    public long ElapsedMilliseconds => stopwatch.ElapsedMilliseconds;

    /// <summary>
    /// Stops the stopwatch and returns elapsed time.
    /// </summary>
    public long Stop()
    {
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// Creates performance tracker for operation.
    /// </summary>
    public static GoApiPerformanceTracker Start(string operation, string url)
    {
        return new GoApiPerformanceTracker(operation, url);
    }

    /// <summary>
    /// Calculates parse time from total and request time.
    /// </summary>
    public static long CalculateParseTime(long totalTimeMs, long requestTimeMs)
    {
        return totalTimeMs - requestTimeMs;
    }

    public void Dispose()
    {
        stopwatch.Stop();
    }
}
