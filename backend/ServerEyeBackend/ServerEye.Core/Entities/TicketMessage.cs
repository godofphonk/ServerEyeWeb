namespace ServerEye.Core.Entities;

public class TicketMessage
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public bool IsStaffReply { get; set; }
    public DateTime CreatedAt { get; set; }
    public Ticket Ticket { get; set; } = null!;
}
