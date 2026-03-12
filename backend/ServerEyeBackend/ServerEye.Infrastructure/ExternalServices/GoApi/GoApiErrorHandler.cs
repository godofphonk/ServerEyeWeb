namespace ServerEye.Infrastructure.ExternalServices.GoApi;

using ServerEye.Core.Exceptions;

/// <summary>
/// Error handling and exception mapping for Go API operations.
/// </summary>
public static class GoApiErrorHandler
{
    /// <summary>
    /// Handles HTTP response and maps to appropriate exception if needed.
    /// </summary>
    public static void HandleResponse(System.Net.HttpStatusCode statusCode)
    {
        if (statusCode == System.Net.HttpStatusCode.NotFound)
        {
            return; // Not found is not an error for some operations
        }

        if (statusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            throw new GoApiException(
                "Go API service is unavailable",
                GoApiErrorType.ServiceUnavailable);
        }

        // For other error status codes, log but don't throw (let caller decide)
    }

    /// <summary>
    /// Maps exceptions to GoApiException.
    /// </summary>
    public static GoApiException MapException(Exception exception)
    {
        return exception switch
        {
            TaskCanceledException => new GoApiException(
                "Go API request timed out",
                GoApiErrorType.Timeout),
            
            System.Net.Http.HttpRequestException ex when ex.InnerException is System.Net.Sockets.SocketException => new GoApiException(
                "Go API service is unavailable",
                GoApiErrorType.ServiceUnavailable),
            
            GoApiException => (GoApiException)exception,
            
            _ => new GoApiException(
                "Unexpected error",
                GoApiErrorType.Unknown)
        };
    }

    /// <summary>
    /// Checks if error is retryable.
    /// </summary>
    public static bool IsRetryableError(Exception exception)
    {
        return exception switch
        {
            TaskCanceledException => true,
            System.Net.Http.HttpRequestException httpRequestException when httpRequestException.InnerException is System.Net.Sockets.SocketException => true,
            GoApiException => true,
            _ => false
        };
    }

    /// <summary>
    /// Creates validation exception for server key.
    /// </summary>
    public static GoApiException CreateServerKeyValidationException(System.Net.HttpStatusCode statusCode)
    {
        if (statusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new GoApiException(
                "Server key not found",
                GoApiErrorType.ServerNotFound);
        }

        return new GoApiException(
            $"Go API returned {statusCode}",
            GoApiErrorType.InvalidResponse);
    }
}
