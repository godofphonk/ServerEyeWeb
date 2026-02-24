namespace ServerEye.Core.Services;

using System.Net;
using System.Net.Mail;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.Interfaces.Services;

public sealed class EmailService : IEmailService, IDisposable
{
    private readonly EmailSettings settings;
    private readonly ILogger<EmailService> logger;
    private readonly AmazonSimpleEmailServiceClient? sesClient;

    public EmailService(EmailSettings settings, ILogger<EmailService> logger)
    {
        this.settings = settings;
        this.logger = logger;

        if (this.settings.UseAwsSes)
        {
            var config = new AmazonSimpleEmailServiceConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(this.settings.AwsRegion)
            };

            this.sesClient = new AmazonSimpleEmailServiceClient(
                this.settings.AwsAccessKey,
                this.settings.AwsSecretKey,
                config);
        }
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

    public async Task SendRegistrationEmailAsync(string userName, string userEmail)
    {
        var emailSubject = "Welcome to ServerEye!";
        var emailBody = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Welcome to ServerEye, {userName}!</h2>
    <p>Your account has been successfully created.</p>
    <p>You can now log in and start monitoring your servers.</p>
    <hr>
    <p style='color: #666; font-size: 12px;'>Best regards,<br>ServerEye Team</p>
</body>
</html>";

        await this.SendEmailAsync(userEmail, emailSubject, emailBody);
    }

    public void Dispose()
    {
        this.sesClient?.Dispose();
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            if (this.settings.UseAwsSes && this.sesClient != null)
            {
                var sendRequest = new SendEmailRequest
                {
                    Source = this.settings.FromEmail,
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { toEmail }
                    },
                    Message = new Message
                    {
                        Subject = new Content(subject),
                        Body = new Body
                        {
                            Html = new Content(body)
                        }
                    }
                };

                var response = await this.sesClient.SendEmailAsync(sendRequest);
                this.logger.LogInformation(
                    "Email sent successfully via AWS SES to {Email} with subject: {Subject}, MessageId: {MessageId}",
                    toEmail,
                    subject,
                    response.MessageId);
            }
            else
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

                this.logger.LogInformation("Email sent successfully via SMTP to {Email} with subject: {Subject}", toEmail, subject);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to send email to {Email} with subject: {Subject}", toEmail, subject);
            throw;
        }
    }
}
