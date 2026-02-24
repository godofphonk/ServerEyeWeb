namespace ServerEye.Core.DTOs.Ticket;

using ServerEye.Core.Enums;

public class TicketListItemDto
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public string PriorityDisplay { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int MessagesCount { get; set; }
}
