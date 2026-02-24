namespace ServerEye.Core.Services;

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.Interfaces.Services;

public sealed class EmailService : IEmailService
{
    private readonly EmailSettings settings;
    private readonly ILogger<EmailService> logger;

    public EmailService(EmailSettings settings, ILogger<EmailService> logger)
    {
        this.settings = settings;
        this.logger = logger;
    }

    public async Task SendTicketCreatedEmailAsync(string ticketNumber, string customerName, string customerEmail, string subject, string message)
    {
        var emailSubject = $"[Ticket #{ticketNumber}] New Support Ticket Created";
        var emailBody = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>New Support Ticket Created</h2>
    <p><strong>Ticket Number:</strong> {ticketNumber}</p>
    <p><strong>Customer Name:</strong> {customerName}</p>
    <p><strong>Customer Email:</strong> {customerEmail}</p>
    <p><strong>Subject:</strong> {subject}</p>
    <hr>
    <h3>Message:</h3>
    <p>{message.Replace("\n", "<br>", StringComparison.Ordinal)}</p>
    <hr>
    <p style='color: #666; font-size: 12px;'>This is an automated message from ServerEye Support System.</p>
</body>
</html>";

        await this.SendEmailAsync(this.settings.SupportEmail, emailSubject, emailBody);

        var customerEmailSubject = $"[Ticket #{ticketNumber}] Your Support Request Has Been Received";
        var customerEmailBody = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Thank you for contacting ServerEye Support</h2>
    <p>Dear {customerName},</p>
    <p>We have received your support request and assigned it ticket number <strong>#{ticketNumber}</strong>.</p>
    <p><strong>Subject:</strong> {subject}</p>
    <p>Our support team will review your request and respond as soon as possible.</p>
    <p>You can reference this ticket number in any future correspondence regarding this issue.</p>
    <hr>
    <p style='color: #666; font-size: 12px;'>Best regards,<br>ServerEye Support Team</p>
</body>
</html>";

        await this.SendEmailAsync(customerEmail, customerEmailSubject, customerEmailBody);
    }

    public async Task SendTicketUpdatedEmailAsync(string ticketNumber, string customerEmail, string statusUpdate)
    {
        var emailSubject = $"[Ticket #{ticketNumber}] Status Update";
        var emailBody = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Ticket Status Update</h2>
    <p>Your support ticket <strong>#{ticketNumber}</strong> has been updated.</p>
    <p><strong>New Status:</strong> {statusUpdate}</p>
    <p>If you have any questions, please reply to this email or reference your ticket number.</p>
    <hr>
    <p style='color: #666; font-size: 12px;'>Best regards,<br>ServerEye Support Team</p>
</body>
</html>";

        await this.SendEmailAsync(customerEmail, emailSubject, emailBody);
    }

    public async Task SendTicketMessageEmailAsync(string ticketNumber, string customerEmail, string messageContent, bool isStaffReply)
    {
        var emailSubject = $"[Ticket #{ticketNumber}] New {(isStaffReply ? "Reply" : "Message")}";
        var emailBody = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>New {(isStaffReply ? "Reply from Support Team" : "Message Added")}</h2>
    <p>A new message has been added to ticket <strong>#{ticketNumber}</strong>.</p>
    <hr>
    <p>{messageContent.Replace("\n", "<br>", StringComparison.Ordinal)}</p>
    <hr>
    <p style='color: #666; font-size: 12px;'>Best regards,<br>ServerEye Support Team</p>
</body>
</html>";

        await this.SendEmailAsync(customerEmail, emailSubject, emailBody);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            using var smtpClient = new SmtpClient(this.settings.SmtpHost, this.settings.SmtpPort)
            {
                EnableSsl = this.settings.EnableSsl,
                Credentials = new NetworkCredential(this.settings.SmtpUsername, this.settings.SmtpPassword)
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(this.settings.FromEmail, this.settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);

            this.logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to send email to {Email} with subject: {Subject}", toEmail, subject);
            throw;
        }
    }
}
