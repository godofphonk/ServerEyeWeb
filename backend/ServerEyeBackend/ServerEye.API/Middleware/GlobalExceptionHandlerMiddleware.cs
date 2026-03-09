namespace ServerEye.API.Middleware;

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs;
using ServerEye.Core.Exceptions;

public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (GoApiException ex)
        {
            logger.LogWarning(ex, "Go API error occurred: {ErrorType}", ex.ErrorType);
            await HandleGoApiExceptionAsync(context, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt");
            await HandleUnauthorizedExceptionAsync(context, ex);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation");
            await HandleInvalidOperationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred");
            await HandleUnknownExceptionAsync(context);
        }
    }

    private static async Task HandleGoApiExceptionAsync(HttpContext context, GoApiException exception)
    {
        var statusCode = exception.ErrorType switch
        {
            GoApiErrorType.ServiceUnavailable => HttpStatusCode.ServiceUnavailable,
            GoApiErrorType.Timeout => HttpStatusCode.GatewayTimeout,
            GoApiErrorType.ServerNotFound => HttpStatusCode.NotFound,
            GoApiErrorType.Unauthorized => HttpStatusCode.Forbidden,
            GoApiErrorType.InvalidResponse => HttpStatusCode.BadGateway,
            GoApiErrorType.NetworkError => HttpStatusCode.ServiceUnavailable,
            GoApiErrorType.Unknown => HttpStatusCode.BadGateway,
            _ => HttpStatusCode.BadGateway
        };

        var errorResponse = new ErrorResponseDto
        {
            Error = "MonitoringServiceError",
            Message = exception.Message,
            UserMessage = exception.UserMessage,
            ErrorCode = $"GO_API_{exception.ErrorType.ToString().ToUpperInvariant()}",
            SupportContact = exception.SupportContact,
            Details = new Dictionary<string, object>
            {
                ["error_type"] = exception.ErrorType.ToString(),
                ["is_temporary"] = exception.ErrorType is GoApiErrorType.ServiceUnavailable or GoApiErrorType.Timeout or GoApiErrorType.NetworkError
            }
        };

        await WriteJsonResponseAsync(context, statusCode, errorResponse);
    }

    private static async Task HandleUnauthorizedExceptionAsync(HttpContext context, UnauthorizedAccessException exception)
    {
        var errorResponse = new ErrorResponseDto
        {
            Error = "Unauthorized",
            Message = exception.Message,
            UserMessage = "You don't have permission to access this resource.",
            ErrorCode = "UNAUTHORIZED"
        };

        await WriteJsonResponseAsync(context, HttpStatusCode.Forbidden, errorResponse);
    }

    private static async Task HandleInvalidOperationExceptionAsync(HttpContext context, InvalidOperationException exception)
    {
        var errorResponse = new ErrorResponseDto
        {
            Error = "InvalidOperation",
            Message = exception.Message,
            UserMessage = exception.Message,
            ErrorCode = "INVALID_OPERATION"
        };

        await WriteJsonResponseAsync(context, HttpStatusCode.BadRequest, errorResponse);
    }

    private static async Task HandleUnknownExceptionAsync(HttpContext context)
    {
        var errorResponse = new ErrorResponseDto
        {
            Error = "InternalServerError",
            Message = "An unexpected error occurred",
            UserMessage = "Something went wrong on our end. Our team has been notified. Please try again later.",
            ErrorCode = "INTERNAL_ERROR",
            SupportContact = "support@servereye.dev"
        };

        await WriteJsonResponseAsync(context, HttpStatusCode.InternalServerError, errorResponse);
    }

    private static async Task WriteJsonResponseAsync(HttpContext context, HttpStatusCode statusCode, ErrorResponseDto errorResponse)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(errorResponse, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}
