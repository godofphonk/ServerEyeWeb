namespace ServerEye.Core.DTOs.Ticket;

public class TicketMessageDto
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public bool IsStaffReply { get; set; }
    public DateTime CreatedAt { get; set; }
}
