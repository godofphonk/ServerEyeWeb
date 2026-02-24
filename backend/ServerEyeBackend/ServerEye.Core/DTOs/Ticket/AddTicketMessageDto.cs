namespace ServerEye.Core.DTOs.Ticket;

public class AddTicketMessageDto
{
    public string Message { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public bool IsStaffReply { get; set; }
}
