namespace ServerEye.Infrastracture.ExternalServices.GoApi;

using Microsoft.Extensions.Logging;

/// <summary>
/// Centralized logging for Go API operations.
/// </summary>
public class GoApiLogger(ILogger<GoApiLogger> logger)
{
    private readonly ILogger<GoApiLogger> logger = logger;

    /// <summary>
    /// Logs HTTP request initiation.
    /// </summary>
    public void LogRequest(string operation, string url)
    {
        logger.LogInformation("[GoApi] {Operation} - Requesting: {Url}", operation, url);
    }

    /// <summary>
    /// Logs successful HTTP response.
    /// </summary>
    public void LogResponse(string operation, string url, long elapsedMs, object? data = null)
    {
        if (data != null)
        {
            logger.LogInformation(
                "[GoApi] {Operation} - Success in {ElapsedMs}ms: {Url} - Data: {@Data}",
                operation, elapsedMs, url, data);
        }
        else
        {
            logger.LogInformation(
                "[GoApi] {Operation} - Success in {ElapsedMs}ms: {Url}",
                operation, elapsedMs, url);
        }
    }

    /// <summary>
    /// Logs HTTP error response.
    /// </summary>
    public void LogError(string operation, string url, long elapsedMs, int statusCode, string content)
    {
        logger.LogError(
            "[GoApi] {Operation} - Error in {ElapsedMs}ms: {Url} - Status: {StatusCode} - Content: {Content}",
            operation,
            elapsedMs,
            url,
            statusCode,
            content);
    }

    /// <summary>
    /// Logs exception during operation.
    /// </summary>
    public void LogException(string operation, string url, long elapsedMs, Exception exception)
    {
        logger.LogError(
            exception,
            "[GoApi] {Operation} - Exception in {ElapsedMs}ms: {Url} - {ExceptionType}: {Message}",
            operation, elapsedMs, url, exception.GetType().Name, exception.Message);
    }

    /// <summary>
    /// Logs performance metrics.
    /// </summary>
    public void LogPerformance(string operation, int dataPoints, long totalTimeMs, long requestTimeMs)
    {
        logger.LogInformation(
            "[GoApi][PERF] {Operation} - Retrieved {Points} points in {TotalMs}ms (request: {RequestMs}ms, parse: {ParseMs}ms)",
            operation,
            dataPoints,
            totalTimeMs,
            requestTimeMs,
            totalTimeMs - requestTimeMs);
    }

    /// <summary>
    /// Logs empty data response.
    /// </summary>
    public void LogEmptyData(string operation, string identifier, long elapsedMs)
    {
        logger.LogWarning(
            "[GoApi] {Operation} - Empty data after {ElapsedMs}ms for {Identifier}",
            operation, elapsedMs, identifier);
    }

    /// <summary>
    /// Logs debug information.
    /// </summary>
    public void LogDebug(string operation, string message, object? context = null)
    {
        if (context != null)
        {
            logger.LogInformation(
                "[GoApi][DEBUG] {Operation} - {Message} - Context: {@Context}",
                operation,
                message,
                context);
        }
        else
        {
            logger.LogInformation("[GoApi][DEBUG] {Operation} - {Message}", operation, message);
        }
    }
}
