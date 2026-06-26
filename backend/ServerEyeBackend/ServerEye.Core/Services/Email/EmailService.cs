namespace ServerEye.Core.Services;

using System.Net;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.Helpers;
using ServerEye.Core.Interfaces.Services;

public sealed class EmailService : IEmailService, IDisposable
{
    private readonly EmailSettings settings;
    private readonly ILogger<EmailService> logger;
    private readonly IEmailTemplateService templateService;
    private readonly AmazonSimpleEmailServiceClient? sesClient;
    private readonly HttpClient httpClient;

    public EmailService(EmailSettings settings, ILogger<EmailService> logger, IEmailTemplateService templateService)
    {
        this.settings = settings;
        this.logger = logger;
        this.templateService = templateService;
        this.httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

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
        var parameters = new Dictionary<string, string>
        {
            { "UserName", userName },
            { "FrontendUrl", this.settings.FrontendUrl.AbsoluteUri.TrimEnd('/') },
            { "SupportEmail", this.settings.SupportEmail }
        };

        var emailBody = await this.templateService.RenderTemplateAsync("WelcomeEmail", parameters);
        await this.SendEmailAsync(userEmail, emailSubject, emailBody);
    }

    public async Task SendEmailVerificationCodeAsync(string userName, string userEmail, string code)
    {
        this.logger.LogInformation("SendEmailVerificationCodeAsync called for {Email}", LogSanitizer.MaskEmail(userEmail));
        
        var emailSubject = "Verify your email - ServerEye";
        var parameters = new Dictionary<string, string>
        {
            { "UserName", userName },
            { "VerificationCode", code },
            { "SupportEmail", this.settings.SupportEmail }
        };

        this.logger.LogInformation("Rendering email template...");
        var emailBody = await this.templateService.RenderTemplateAsync("EmailVerification", parameters);
        this.logger.LogInformation("Template rendered, sending email...");
        await this.SendEmailAsync(userEmail, emailSubject, emailBody);
        this.logger.LogInformation("SendEmailAsync completed");
    }

    public async Task SendPasswordResetEmailAsync(string userName, string userEmail, string resetToken)
    {
        var emailSubject = "Reset your password - ServerEye";
        var resetLink = $"{this.settings.FrontendUrl.AbsoluteUri.TrimEnd('/')}/reset-password?token={resetToken}";
        var parameters = new Dictionary<string, string>
        {
            { "UserName", userName },
            { "ResetLink", resetLink },
            { "SupportEmail", this.settings.SupportEmail }
        };

        var emailBody = await this.templateService.RenderTemplateAsync("PasswordReset", parameters);
        await this.SendEmailAsync(userEmail, emailSubject, emailBody);
    }

    public async Task SendPasswordChangedNotificationAsync(string userName, string userEmail)
    {
        var emailSubject = "Your password has been changed - ServerEye";
        var parameters = new Dictionary<string, string>
        {
            { "UserName", userName },
            { "ChangeDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") },
            { "SupportEmail", this.settings.SupportEmail }
        };

        var emailBody = await this.templateService.RenderTemplateAsync("PasswordChanged", parameters);
        await this.SendEmailAsync(userEmail, emailSubject, emailBody);
    }

    public async Task SendEmailChangeConfirmationAsync(string userName, string newEmail, string code)
    {
        var emailSubject = "Confirm your new email - ServerEye";
        var parameters = new Dictionary<string, string>
        {
            { "UserName", userName },
            { "VerificationCode", code },
            { "SupportEmail", this.settings.SupportEmail }
        };

        var emailBody = await this.templateService.RenderTemplateAsync("EmailChangeConfirmation", parameters);
        await this.SendEmailAsync(newEmail, emailSubject, emailBody);
    }

    public async Task SendEmailChangedNotificationAsync(string userName, string oldEmail, string newEmail)
    {
        var emailSubject = "Your email has been changed - ServerEye";
        var parameters = new Dictionary<string, string>
        {
            { "UserName", userName },
            { "OldEmail", oldEmail },
            { "NewEmail", newEmail },
            { "SupportEmail", this.settings.SupportEmail }
        };

        var emailBody = await this.templateService.RenderTemplateAsync("EmailChanged", parameters);
        await this.SendEmailAsync(oldEmail, emailSubject, emailBody);
    }

    public async Task SendAccountDeletionConfirmationAsync(string userName, string userEmail, string code)
    {
        var emailSubject = "Account Deletion Confirmation - ServerEye";
        var parameters = new Dictionary<string, string>
        {
            { "UserName", userName },
            { "ConfirmationCode", code },
            { "SupportEmail", this.settings.SupportEmail }
        };

        var emailBody = await this.templateService.RenderTemplateAsync("AccountDeletionConfirmation", parameters);
        await this.SendEmailAsync(userEmail, emailSubject, emailBody);
    }

    public void Dispose()
    {
        this.sesClient?.Dispose();
        this.httpClient?.Dispose();
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            this.logger.LogInformation(
                "Starting email send to {Email} with subject: {Subject}",
                LogSanitizer.MaskEmail(toEmail),
                LogSanitizer.Sanitize(subject));

            this.logger.LogInformation(
                "Email settings - UseAwsSes: {UseAwsSes}, SmtpHost: {SmtpHost}, SmtpPort: {SmtpPort}, FromEmail: {FromEmail}",
                this.settings.UseAwsSes,
                this.settings.SmtpHost,
                this.settings.SmtpPort,
                this.settings.FromEmail);

            if (this.settings.UseAwsSes && this.sesClient != null)
            {
                this.logger.LogInformation("Using AWS SES for email sending");

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
                        Body = new Amazon.SimpleEmail.Model.Body
                        {
                            Html = new Content(body)
                        }
                    }
                };

                this.logger.LogInformation("Sending SES request...");
                var response = await this.sesClient.SendEmailAsync(sendRequest);
                this.logger.LogInformation(
                    "Email sent successfully via AWS SES to {Email} with subject: {Subject}, MessageId: {MessageId}",
                    LogSanitizer.MaskEmail(toEmail),
                    LogSanitizer.Sanitize(subject),
                    response.MessageId);
            }
            else
            {
                this.logger.LogInformation("Using Brevo API for email sending");

                this.httpClient.DefaultRequestHeaders.Clear();
                this.httpClient.DefaultRequestHeaders.Add("api-key", this.settings.SmtpPassword);
                this.httpClient.DefaultRequestHeaders.Add("accept", "application/json");

                var payload = new
                {
                    sender = new { name = this.settings.FromName, email = this.settings.FromEmail },
                    to = new[] { new { email = toEmail } },
                    subject,
                    htmlContent = body
                };

                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                this.logger.LogInformation(
                    "Brevo configuration - FromEmail: {FromEmail}, ToEmail: {ToEmail}",
                    this.settings.FromEmail,
                    LogSanitizer.MaskEmail(toEmail));

                this.logger.LogInformation("Sending email via Brevo API...");
                var response = await this.httpClient.PostAsync(new Uri("https://api.brevo.com/v3/smtp/email"), content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                this.logger.LogInformation(
                    "Email sent successfully via Brevo to {Email} with subject: {Subject}, Response: {Response}",
                    LogSanitizer.MaskEmail(toEmail),
                    LogSanitizer.Sanitize(subject),
                    responseBody);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to send email to {Email} with subject: {Subject}", LogSanitizer.MaskEmail(toEmail), LogSanitizer.Sanitize(subject));
            throw;
        }
    }
}
