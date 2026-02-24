namespace ServerEye.Core.Services;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.Ticket;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public sealed class TicketService : ITicketService
{
    private readonly ITicketRepository ticketRepository;
    private readonly ITicketMessageRepository messageRepository;
    private readonly IEmailService emailService;
    private readonly INotificationService notificationService;
    private readonly ILogger<TicketService> logger;

    public TicketService(
        ITicketRepository ticketRepository,
        ITicketMessageRepository messageRepository,
        IEmailService emailService,
        INotificationService notificationService,
        ILogger<TicketService> logger)
    {
        this.ticketRepository = ticketRepository;
        this.messageRepository = messageRepository;
        this.emailService = emailService;
        this.notificationService = notificationService;
        this.logger = logger;
    }

    public async Task<TicketResponseDto> CreateTicketAsync(CreateTicketDto dto)
    {
        var ticketNumber = GenerateTicketNumber();

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TicketNumber = ticketNumber,
            Name = dto.Name,
            Email = dto.Email,
            UserId = dto.UserId,
            Subject = dto.Subject,
            Message = dto.Message,
            Status = TicketStatus.New,
            Priority = TicketPriority.Medium,
            CreatedAt = DateTime.UtcNow
        };

        await this.ticketRepository.AddAsync(ticket);

        this.logger.LogInformation("Ticket created: {TicketNumber} for {Email}", ticketNumber, dto.Email);

        try
        {
            await this.emailService.SendTicketCreatedEmailAsync(
                ticketNumber,
                dto.Name,
                dto.Email,
                dto.Subject,
                dto.Message);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to send ticket creation email for ticket {TicketNumber}", ticketNumber);
        }

        try
        {
            await this.notificationService.NotifyAdminsAboutNewTicketAsync(ticket.Id, ticketNumber, dto.Subject);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to create notifications for ticket {TicketNumber}", ticketNumber);
        }

        return this.MapToResponseDto(ticket);
    }

    public async Task<TicketResponseDto?> GetTicketByIdAsync(Guid id)
    {
        var ticket = await this.ticketRepository.GetByIdAsync(id);
        return ticket == null ? null : this.MapToResponseDto(ticket);
    }

    public async Task<TicketResponseDto?> GetTicketByNumberAsync(string ticketNumber)
    {
        var ticket = await this.ticketRepository.GetByTicketNumberAsync(ticketNumber);
        return ticket == null ? null : this.MapToResponseDto(ticket);
    }

    public async Task<List<TicketListItemDto>> GetAllTicketsAsync(int page = 1, int pageSize = 50)
    {
        var tickets = await this.ticketRepository.GetAllAsync(page, pageSize);
        return tickets.Select(this.MapToListItemDto).ToList();
    }

    public async Task<List<TicketListItemDto>> GetTicketsByStatusAsync(TicketStatus status, int page = 1, int pageSize = 50)
    {
        var tickets = await this.ticketRepository.GetByStatusAsync(status, page, pageSize);
        return tickets.Select(this.MapToListItemDto).ToList();
    }

    public async Task<List<TicketResponseDto>> GetTicketsByEmailAsync(string email)
    {
        var tickets = await this.ticketRepository.GetByEmailAsync(email);
        return tickets.Select(this.MapToResponseDto).ToList();
    }

    public async Task<List<TicketListItemDto>> GetTicketsByUserIdAsync(Guid userId, int page = 1, int pageSize = 50)
    {
        var tickets = await this.ticketRepository.GetByUserIdAsync(userId, page, pageSize);
        return tickets.Select(this.MapToListItemDto).ToList();
    }

    public async Task<TicketResponseDto> UpdateTicketStatusAsync(Guid id, TicketStatus status)
    {
        var ticket = await this.ticketRepository.GetByIdAsync(id) ?? throw new InvalidOperationException("Ticket not found");

        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (status == TicketStatus.Resolved)
        {
            ticket.ResolvedAt = DateTime.UtcNow;
        }
        else if (status == TicketStatus.Closed)
        {
            ticket.ClosedAt = DateTime.UtcNow;
        }

        await this.ticketRepository.UpdateAsync(ticket);

        this.logger.LogInformation("Ticket {TicketNumber} status updated to {Status}", ticket.TicketNumber, status);

        try
        {
            await this.emailService.SendTicketUpdatedEmailAsync(
                ticket.TicketNumber,
                ticket.Email,
                GetStatusDisplay(status));
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to send status update email for ticket {TicketNumber}", ticket.TicketNumber);
        }

        if (ticket.UserId.HasValue)
        {
            try
            {
                await this.notificationService.NotifyUserAboutStatusChangeAsync(
                    ticket.UserId.Value,
                    ticket.Id,
                    ticket.TicketNumber,
                    GetStatusDisplay(status));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to create status change notification for ticket {TicketNumber}", ticket.TicketNumber);
            }
        }

        return this.MapToResponseDto(ticket);
    }

    public async Task<TicketMessageDto> AddMessageAsync(Guid ticketId, AddTicketMessageDto dto)
    {
        var ticket = await this.ticketRepository.GetByIdAsync(ticketId) ?? throw new InvalidOperationException("Ticket not found");

        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Message = dto.Message,
            SenderName = dto.SenderName,
            SenderEmail = dto.SenderEmail,
            IsStaffReply = dto.IsStaffReply,
            CreatedAt = DateTime.UtcNow
        };

        await this.messageRepository.AddAsync(message);

        ticket.UpdatedAt = DateTime.UtcNow;
        if (ticket.Status == TicketStatus.New)
        {
            ticket.Status = TicketStatus.Open;
        }

        await this.ticketRepository.UpdateAsync(ticket);

        this.logger.LogInformation("Message added to ticket {TicketNumber}", ticket.TicketNumber);

        try
        {
            await this.emailService.SendTicketMessageEmailAsync(
                ticket.TicketNumber,
                ticket.Email,
                dto.Message,
                dto.IsStaffReply);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to send message notification email for ticket {TicketNumber}", ticket.TicketNumber);
        }

        if (ticket.UserId.HasValue && dto.IsStaffReply)
        {
            try
            {
                await this.notificationService.NotifyUserAboutNewMessageAsync(
                    ticket.UserId.Value,
                    ticket.Id,
                    ticket.TicketNumber,
                    dto.SenderName);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to create message notification for ticket {TicketNumber}", ticket.TicketNumber);
            }
        }

        return new TicketMessageDto
        {
            Id = message.Id,
            Message = message.Message,
            SenderName = message.SenderName,
            SenderEmail = message.SenderEmail,
            IsStaffReply = message.IsStaffReply,
            CreatedAt = message.CreatedAt
        };
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await this.ticketRepository.GetTotalCountAsync();
    }

    public async Task<Dictionary<TicketStatus, int>> GetStatusCountsAsync()
    {
        var counts = new Dictionary<TicketStatus, int>();
        foreach (TicketStatus status in Enum.GetValues<TicketStatus>())
        {
            counts[status] = await this.ticketRepository.GetCountByStatusAsync(status);
        }

        return counts;
    }

    private static string GenerateTicketNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var random = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1000, 10000);
        return $"TKT-{timestamp}-{random}";
    }

    private static string GetStatusDisplay(TicketStatus status)
    {
        return status switch
        {
            TicketStatus.New => "New",
            TicketStatus.Open => "Open",
            TicketStatus.InProgress => "In Progress",
            TicketStatus.Resolved => "Resolved",
            TicketStatus.Closed => "Closed",
            TicketStatus.Reopened => "Reopened",
            _ => "Unknown"
        };
    }

    private static string GetPriorityDisplay(TicketPriority priority)
    {
        return priority switch
        {
            TicketPriority.Low => "Low",
            TicketPriority.Medium => "Medium",
            TicketPriority.High => "High",
            TicketPriority.Critical => "Critical",
            _ => "Unknown"
        };
    }

    private TicketResponseDto MapToResponseDto(Ticket ticket)
    {
        return new TicketResponseDto
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Name = ticket.Name,
            Email = ticket.Email,
            UserId = ticket.UserId,
            Subject = ticket.Subject,
            Message = ticket.Message,
            Status = ticket.Status,
            StatusDisplay = GetStatusDisplay(ticket.Status),
            Priority = ticket.Priority,
            PriorityDisplay = GetPriorityDisplay(ticket.Priority),
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            AssignedToUserName = ticket.AssignedToUserName,
            Messages = ticket.Messages.Select(m => new TicketMessageDto
            {
                Id = m.Id,
                Message = m.Message,
                SenderName = m.SenderName,
                SenderEmail = m.SenderEmail,
                IsStaffReply = m.IsStaffReply,
                CreatedAt = m.CreatedAt
            }).ToList()
        };
    }

    private TicketListItemDto MapToListItemDto(Ticket ticket)
    {
        return new TicketListItemDto
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Name = ticket.Name,
            Email = ticket.Email,
            UserId = ticket.UserId,
            Subject = ticket.Subject,
            Status = ticket.Status,
            StatusDisplay = GetStatusDisplay(ticket.Status),
            Priority = ticket.Priority,
            PriorityDisplay = GetPriorityDisplay(ticket.Priority),
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            MessagesCount = ticket.Messages.Count
        };
    }
}
