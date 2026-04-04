namespace ServerEye.API.Controllers;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.DTOs.Ticket;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Services;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService ticketService;
    private readonly ILogger<TicketsController> logger;

    public TicketsController(ITicketService ticketService, ILogger<TicketsController> logger)
    {
        this.ticketService = ticketService;
        this.logger = logger;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<TicketResponseDto>> CreateTicket([FromBody] CreateTicketDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Name) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Subject) ||
                string.IsNullOrWhiteSpace(dto.Message))
            {
                return this.BadRequest(new { message = "All fields are required" });
            }

            // Get UserId from JWT token
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                dto.UserId = userId;
            }

            var ticket = await this.ticketService.CreateTicketAsync(dto);
            this.logger.LogInformation("Ticket created: {TicketNumber}", ticket.TicketNumber);

            return this.CreatedAtAction(nameof(this.GetTicketByNumber), new { ticketNumber = ticket.TicketNumber }, ticket);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error creating ticket");
            return this.StatusCode(500, new { message = "Failed to create ticket" });
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<TicketListItemDto>>> GetAllTickets([FromQuery][Range(1, int.MaxValue)] int page = 1, [FromQuery][Range(1, 100)] int pageSize = 50)
    {
        try
        {
            var tickets = await this.ticketService.GetAllTicketsAsync(page, pageSize);
            var totalCount = await this.ticketService.GetTotalCountAsync();

            return this.Ok(new
            {
                tickets,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting tickets");
            return this.StatusCode(500, new { message = "Failed to retrieve tickets" });
        }
    }

    [HttpGet("status/{status}")]
    [Authorize]
    public async Task<ActionResult<List<TicketListItemDto>>> GetTicketsByStatus(TicketStatus status, [FromQuery][Range(1, int.MaxValue)] int page = 1, [FromQuery][Range(1, 100)] int pageSize = 50)
    {
        try
        {
            var tickets = await this.ticketService.GetTicketsByStatusAsync(status, page, pageSize);
            return this.Ok(tickets);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting tickets by status {Status}", status);
            return this.StatusCode(500, new { message = "Failed to retrieve tickets" });
        }
    }

    [HttpGet("number/{ticketNumber}")]
    public async Task<ActionResult<TicketResponseDto>> GetTicketByNumber(string ticketNumber)
    {
        try
        {
            var ticket = await this.ticketService.GetTicketByNumberAsync(ticketNumber);
            if (ticket == null)
            {
                return this.NotFound(new { message = "Ticket not found" });
            }

            return this.Ok(ticket);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting ticket {TicketNumber}", ticketNumber);
            return this.StatusCode(500, new { message = "Failed to retrieve ticket" });
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<TicketResponseDto>> GetTicketById(Guid id)
    {
        try
        {
            var ticket = await this.ticketService.GetTicketByIdAsync(id);
            if (ticket == null)
            {
                return this.NotFound(new { message = "Ticket not found" });
            }

            return this.Ok(ticket);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting ticket {TicketId}", id);
            return this.StatusCode(500, new { message = "Failed to retrieve ticket" });
        }
    }

    [HttpGet("email/{email}")]
    public async Task<ActionResult<List<TicketResponseDto>>> GetTicketsByEmail(string email)
    {
        try
        {
            var tickets = await this.ticketService.GetTicketsByEmailAsync(email);
            return this.Ok(tickets);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting tickets for email {Email}", email?.Contains('@', StringComparison.Ordinal) == true ? $"{email[..Math.Min(email.IndexOf('@', StringComparison.Ordinal), 5)]}***" : "***");
            return this.StatusCode(500, new { message = "Failed to retrieve tickets" });
        }
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<List<TicketListItemDto>>> GetTicketsByUserId(Guid userId, [FromQuery][Range(1, int.MaxValue)] int page = 1, [FromQuery][Range(1, 100)] int pageSize = 50)
    {
        try
        {
            var tickets = await this.ticketService.GetTicketsByUserIdAsync(userId, page, pageSize);
            return this.Ok(tickets);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting tickets for user {UserId}", userId);
            return this.StatusCode(500, new { message = "Failed to retrieve tickets" });
        }
    }

    [HttpPut("{id:guid}/status")]
    [Authorize]
    public async Task<ActionResult<TicketResponseDto>> UpdateTicketStatus(Guid id, [FromBody] UpdateTicketStatusDto dto)
    {
        try
        {
            var ticket = await this.ticketService.UpdateTicketStatusAsync(id, dto.Status);
            this.logger.LogInformation("Ticket {TicketId} status updated to {Status}", id, dto.Status);

            return this.Ok(ticket);
        }
        catch (InvalidOperationException ex)
        {
            return this.NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error updating ticket status {TicketId}", id);
            return this.StatusCode(500, new { message = "Failed to update ticket status" });
        }
    }

    [HttpPost("{id:guid}/messages")]
    [Authorize]
    public async Task<ActionResult<TicketMessageDto>> AddMessage(Guid id, [FromBody] AddTicketMessageDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                return this.BadRequest(new { message = "Message is required" });
            }

            var message = await this.ticketService.AddMessageAsync(id, dto);
            this.logger.LogInformation("Message added to ticket {TicketId}", id);

            return this.CreatedAtAction(nameof(this.GetTicketById), new { id }, message);
        }
        catch (InvalidOperationException ex)
        {
            return this.NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error adding message to ticket {TicketId}", id);
            return this.StatusCode(500, new { message = "Failed to add message" });
        }
    }

    [HttpGet("stats")]
    [Authorize]
    public async Task<ActionResult> GetTicketStats()
    {
        try
        {
            var totalCount = await this.ticketService.GetTotalCountAsync();
            var statusCounts = await this.ticketService.GetStatusCountsAsync();

            return this.Ok(new
            {
                totalCount,
                statusCounts
            });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting ticket stats");
            return this.StatusCode(500, new { message = "Failed to retrieve stats" });
        }
    }

    [HttpDelete("{ticketId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteTicket(Guid ticketId)
    {
        try
        {
            // Проверка роли пользователя - только админы могут удалять тикеты
            var userRoleClaim = this.User.FindFirst("role")?.Value ?? this.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userRoleClaim) || userRoleClaim != "ADMIN")
            {
                this.logger.LogWarning("User attempted to delete ticket {TicketId} without admin rights. Role: {Role}", ticketId, userRoleClaim);
                return this.StatusCode(403, new { message = "Only administrators can delete tickets" });
            }

            await this.ticketService.DeleteTicketAsync(ticketId);
            this.logger.LogInformation("Ticket {TicketId} deleted successfully", ticketId);

            return this.Ok(new { message = "Ticket deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            this.logger.LogWarning("Ticket not found for deletion: {TicketId}", ticketId);
            return this.NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error deleting ticket {TicketId}", ticketId);
            return this.StatusCode(500, new { message = "Error deleting ticket", error = ex.Message });
        }
    }
}
