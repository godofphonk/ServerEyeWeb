namespace ServerEye.Core.Interfaces.Services;

public interface IEmailService
{
    public Task SendTicketCreatedEmailAsync(string ticketNumber, string customerName, string customerEmail, string subject, string message);
    public Task SendTicketUpdatedEmailAsync(string ticketNumber, string customerEmail, string statusUpdate);
    public Task SendTicketMessageEmailAsync(string ticketNumber, string customerEmail, string messageContent, bool isStaffReply);
    public Task SendRegistrationEmailAsync(string userName, string userEmail);
    public Task SendEmailVerificationCodeAsync(string userName, string userEmail, string code);
    public Task SendPasswordResetEmailAsync(string userName, string userEmail, string resetToken);
    public Task SendPasswordChangedNotificationAsync(string userName, string userEmail);
    public Task SendEmailChangeConfirmationAsync(string userName, string newEmail, string code);
    public Task SendEmailChangedNotificationAsync(string userName, string oldEmail, string newEmail);
}
