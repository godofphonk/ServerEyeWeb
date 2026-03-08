namespace ServerEye.Core.Configuration;

using System;

public class EmailSettings
{
    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; }
    public string SmtpUsername { get; init; } = string.Empty;
    public string SmtpPassword { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
    public string SupportEmail { get; init; } = string.Empty;
    public bool EnableSsl { get; init; } = true;
    public bool UseAwsSes { get; init; } = true;
    public string AwsRegion { get; init; } = "eu-north-1";
    public string AwsAccessKey { get; init; } = string.Empty;
    public string AwsSecretKey { get; init; } = string.Empty;
    public Uri FrontendUrl { get; init; } = new Uri("http://localhost:3000");
}
