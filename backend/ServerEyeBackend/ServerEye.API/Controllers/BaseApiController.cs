namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

/// <summary>
/// Base controller with common functionality for all API controllers.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Gets the current user ID from JWT claims.
    /// </summary>
    /// <returns>User ID.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user ID is invalid or missing.</exception>
    protected Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user identifier");
        }

        return userId;
    }

    /// <summary>
    /// Gets the current user email from JWT claims.
    /// </summary>
    /// <returns>User email.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user email is missing.</exception>
    protected string GetUserEmail()
    {
        var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(emailClaim))
        {
            throw new UnauthorizedAccessException("User email not found");
        }

        return emailClaim;
    }

    /// <summary>
    /// Gets the current user name from JWT claims.
    /// </summary>
    /// <returns>User name.</returns>
    protected string GetUserName()
    {
        var nameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
        return nameClaim ?? string.Empty;
    }

    /// <summary>
    /// Validates user ID and throws if invalid.
    /// </summary>
    /// <param name="userId">User ID to validate.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when user ID doesn't match current user.</exception>
    protected void ValidateUserId(Guid userId)
    {
        var currentUserId = GetUserId();
        if (currentUserId != userId)
        {
            throw new UnauthorizedAccessException("Access denied: User ID mismatch");
        }
    }

    /// <summary>
    /// Returns a successful result with data.
    /// </summary>
    /// <typeparam name="T">Data type.</typeparam>
    /// <param name="data">Data to return.</param>
    /// <returns>Success result.</returns>
    protected ActionResult<T> Success<T>(T data)
    {
        return Ok(data);
    }

    /// <summary>
    /// Returns a success result with message.
    /// </summary>
    /// <param name="message">Success message.</param>
    /// <returns>Success result.</returns>
    protected ActionResult Success(string message)
    {
        return Ok(new { message });
    }

    /// <summary>
    /// Returns a bad request result with validation message.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <returns>Bad request result.</returns>
    protected ActionResult BadRequest(string message)
    {
        return BadRequest(new { error = message, message });
    }

    /// <summary>
    /// Returns a not found result.
    /// </summary>
    /// <param name="resourceName">Name of the resource that was not found.</param>
    /// <returns>Not found result.</returns>
    protected ActionResult NotFound(string resourceName)
    {
        return NotFound(new { error = "Not Found", message = $"{resourceName} not found" });
    }

    /// <summary>
    /// Returns an unauthorized result.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <returns>Unauthorized result.</returns>
    protected ActionResult Unauthorized(string message)
    {
        return Unauthorized(new { error = "Unauthorized", message });
    }

    /// <summary>
    /// Executes an operation with unified error handling.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="operation">Operation to execute.</param>
    /// <returns>Result of the operation.</returns>
    protected async Task<ActionResult<T>> ExecuteWithErrorHandling<T>(Func<Task<T>> operation)
    {
        try
        {
            var result = await operation();
            return Success(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception)
        {
            // Let the global exception handler deal with unexpected exceptions
            throw;
        }
    }

    /// <summary>
    /// Executes an operation with unified error handling (no return value).
    /// </summary>
    /// <param name="operation">Operation to execute.</param>
    /// <returns>Result of the operation.</returns>
    protected async Task<ActionResult> ExecuteWithErrorHandling(Func<Task> operation)
    {
        try
        {
            await operation();
            return Success("Operation completed successfully");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception)
        {
            // Let the global exception handler deal with unexpected exceptions
            throw;
        }
    }
}
