namespace ServerEye.Infrastracture.ExternalServices.GoApi;

using ServerEye.Core.Exceptions;

/// <summary>
/// Error handling and exception mapping for Go API operations.
/// </summary>
public class GoApiErrorHandler
{
    /// <summary>
    /// Handles HTTP response and maps to appropriate exception if needed.
    /// </summary>
    public void HandleResponse(System.Net.HttpStatusCode statusCode, string content)
    {
        if (statusCode == System.Net.HttpStatusCode.NotFound)
        {
            return; // Not found is not an error for some operations
        }

        if (statusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            throw new GoApiException(
                "Go API service is unavailable",
                GoApiErrorType.ServiceUnavailable,
                new System.Net.Http.HttpRequestException($"Status: {statusCode}"));
        }

        // For other error status codes, log but don't throw (let caller decide)
    }

    /// <summary>
    /// Maps exceptions to GoApiException.
    /// </summary>
    public GoApiException MapException(Exception exception)
    {
        return exception switch
        {
            TaskCanceledException ex => new GoApiException(
                "Go API request timed out",
                GoApiErrorType.Timeout,
                ex),
            
            System.Net.Http.HttpRequestException ex when ex.InnerException is System.Net.Sockets.SocketException => new GoApiException(
                "Go API service is unavailable",
                GoApiErrorType.ServiceUnavailable,
                ex),
            
            GoApiException => (GoApiException)exception,
            
            _ => new GoApiException(
                $"Unexpected error in {operation}",
                GoApiErrorType.Unknown,
                exception)
        };
    }

    /// <summary>
    /// Checks if error is retryable.
    /// </summary>
    public bool IsRetryableError(Exception exception)
    {
        return exception switch
        {
            GoApiException goEx => goEx.ErrorType == GoApiErrorType.Timeout ||
                                 goEx.ErrorType == GoApiErrorType.ServiceUnavailable,
            
            TaskCanceledException => true,
            
            System.Net.Http.HttpRequestException => true,
            
            _ => false
        };
    }

    /// <summary>
    /// Creates validation exception for server key.
    /// </summary>
    public GoApiException CreateServerKeyValidationException(System.Net.HttpStatusCode statusCode)
    {
        if (statusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new GoApiException(
                "Server key not found",
                GoApiErrorType.ServerNotFound);
        }

        return new GoApiException(
            $"Go API returned {statusCode}",
            GoApiErrorType.InvalidResponse,
            new System.Net.Http.HttpRequestException($"Status: {statusCode}"));
    }
}
