namespace ServerEye.API.Middleware;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => this.logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        this.logger.LogError(
            exception,
            "Exception occurred: {Message}",
            exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = GetStatusCode(exception),
            Title = GetTitle(exception),
            Detail = GetDetail(exception),
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static int GetStatusCode(Exception exception) =>
        exception switch
        {
            ArgumentNullException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

    private static string GetTitle(Exception exception) =>
        exception switch
        {
            ArgumentNullException => "Bad Request",
            ArgumentException => "Bad Request",
            KeyNotFoundException => "Not Found",
            UnauthorizedAccessException => "Unauthorized",
            InvalidOperationException => "Bad Request",
            _ => "Internal Server Error"
        };

    private static string GetDetail(Exception exception) =>
        exception switch
        {
            ArgumentNullException argNull => $"Required parameter is missing: {argNull.ParamName}",
            ArgumentException arg => arg.Message,
            KeyNotFoundException => "The requested resource was not found",
            UnauthorizedAccessException => "You are not authorized to access this resource",
            InvalidOperationException => exception.Message,
            _ => "An unexpected error occurred. Please try again later."
        };
}
