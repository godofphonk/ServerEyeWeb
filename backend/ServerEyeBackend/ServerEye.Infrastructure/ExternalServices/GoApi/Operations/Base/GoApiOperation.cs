namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

using ServerEye.Infrastructure.ExternalServices.GoApi;

/// <summary>
/// Base class for all Go API operations using Template Method Pattern.
/// Eliminates code duplication and provides consistent error handling, logging, and performance tracking.
/// </summary>
/// <typeparam name="T">Return type of the operation.</typeparam>
public abstract class GoApiOperation<T>
{
    protected GoApiOperation(GoApiHttpHandler httpHandler, GoApiLogger logger)
    {
        HttpHandler = httpHandler ?? throw new ArgumentNullException(nameof(httpHandler));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected GoApiHttpHandler HttpHandler { get; }
    protected GoApiLogger Logger { get; }

    /// <summary>
    /// Template method that defines the algorithm for executing Go API operations.
    /// This method eliminates code duplication across all operations.
    /// </summary>
    public async Task<T?> ExecuteAsync()
    {
        using var perfTracker = GoApiPerformanceTracker.Start();
        var url = BuildUrl();

        try
        {
            Logger.LogRequest(GetOperationName(), url);

            var response = await ExecuteRequestAsync(url);
            var requestTime = perfTracker.ElapsedMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorAsync(response, perfTracker, requestTime);
            }

            var content = await GoApiHttpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return default;
            }

            var result = ProcessResponse(content);

            perfTracker.Stop();
            var totalTime = perfTracker.ElapsedMilliseconds;

            LogSuccess(url, result, totalTime, requestTime);

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            Logger.LogException(GetOperationName(), url, perfTracker.ElapsedMilliseconds, ex);
            throw GoApiErrorHandler.MapException(ex);
        }
    }

    /// <summary>
    /// Builds the URL for the specific operation.
    /// </summary>
    protected abstract Uri BuildUrl();

    /// <summary>
    /// Executes the HTTP request for the operation.
    /// </summary>
    protected abstract Task<HttpResponseMessage> ExecuteRequestAsync(Uri url);

    /// <summary>
    /// Processes the successful response content.
    /// </summary>
    protected abstract T? ProcessResponse(string content);

    /// <summary>
    /// Gets the operation name for logging purposes.
    /// </summary>
    protected abstract string GetOperationName();

    /// <summary>
    /// Handles error responses. Can be overridden for custom error handling.
    /// </summary>
    protected virtual async Task<T?> HandleErrorAsync(HttpResponseMessage response, GoApiPerformanceTracker perfTracker, long requestTime)
    {
        var errorContent = await GoApiHttpHandler.GetErrorContentAsync(response);
        Logger.LogError(GetOperationName(), BuildUrl(), requestTime, (int)response.StatusCode, errorContent);

        // For 404 errors, return default value (null) instead of throwing
        if (GoApiHttpHandler.IsNotFound(response))
        {
            return default;
        }

        // For other errors, throw appropriate exception
        throw GoApiErrorHandler.MapException(new InvalidOperationException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"));
    }

    /// <summary>
    /// Logs successful operation completion. Can be overridden for custom logging.
    /// </summary>
    protected virtual void LogSuccess(Uri url, T? result, long totalTime, long requestTime)
    {
        var logData = CreateLogData(result);
        Logger.LogResponse(GetOperationName(), url, totalTime, logData);

        // Log performance metrics if applicable
        if (ShouldLogPerformance(result))
        {
            Logger.LogPerformance(GetOperationName(), GetPerformanceMetric(result), totalTime, requestTime);
        }
    }

    /// <summary>
    /// Creates log data for the response. Override in derived classes for specific logging.
    /// </summary>
    protected virtual object CreateLogData(T? result)
    {
        return new { };
    }

    /// <summary>
    /// Determines if performance metrics should be logged.
    /// </summary>
    protected virtual bool ShouldLogPerformance(T? result)
    {
        return false;
    }

    /// <summary>
    /// Gets the performance metric value for logging.
    /// </summary>
    protected virtual int GetPerformanceMetric(T? result)
    {
        return 0;
    }
}
