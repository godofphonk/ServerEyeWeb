namespace ServerEye.Core.Exceptions;

public enum GoApiErrorType
{
    Unknown = 0,
    ServiceUnavailable = 1,
    Timeout = 2,
    InvalidResponse = 3,
    ServerNotFound = 4,
    Unauthorized = 5,
    NetworkError = 6
}

public class GoApiException : Exception
{
    public GoApiException()
        : this("Go API error occurred")
    {
    }

    public GoApiException(string message)
        : this(message, GoApiErrorType.Unknown)
    {
    }

    public GoApiException(string message, Exception innerException)
        : this(message, GoApiErrorType.Unknown, null, innerException)
    {
    }

    public GoApiException(
        string message, 
        GoApiErrorType errorType = GoApiErrorType.Unknown,
        string? userMessage = null,
        Exception? innerException = null) 
        : base(message, innerException)
    {
        ErrorType = errorType;
        UserMessage = userMessage ?? GetDefaultUserMessage(errorType);
        SupportContact = "support@servereye.dev";
    }

    public GoApiErrorType ErrorType { get; }
    
    public string? UserMessage { get; }
    
    public string? SupportContact { get; }

    private static string GetDefaultUserMessage(GoApiErrorType errorType)
    {
        return errorType switch
        {
            GoApiErrorType.ServiceUnavailable => "Monitoring service is temporarily unavailable. We're working on it. Please try again in a few minutes.",
            GoApiErrorType.Timeout => "Request to monitoring service timed out. Please try again.",
            GoApiErrorType.InvalidResponse => "Received invalid response from monitoring service. Please contact support if this persists.",
            GoApiErrorType.ServerNotFound => "Server not found in monitoring system.",
            GoApiErrorType.Unauthorized => "Unable to access server data. Please check your permissions.",
            GoApiErrorType.NetworkError => "Network error occurred while communicating with monitoring service.",
            GoApiErrorType.Unknown => "An error occurred while communicating with monitoring service. Please try again later.",
            _ => "An error occurred while communicating with monitoring service. Please try again later."
        };
    }
}
