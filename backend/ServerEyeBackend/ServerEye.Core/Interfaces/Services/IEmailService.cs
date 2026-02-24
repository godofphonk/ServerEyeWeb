namespace ServerEye.Core.Interfaces.Services;

public interface IEmailService
{
    public Task SendTicketCreatedEmailAsync(string ticketNumber, string customerName, string customerEmail, string subject, string message);
    public Task SendTicketUpdatedEmailAsync(string ticketNumber, string customerEmail, string statusUpdate);
    public Task SendTicketMessageEmailAsync(string ticketNumber, string customerEmail, string messageContent, bool isStaffReply);
}
