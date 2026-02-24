namespace ServerEye.Core.Configuration;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public bool UseAwsSes { get; set; } = true;
    public string AwsRegion { get; set; } = "eu-north-1";
    public string AwsAccessKey { get; set; } = string.Empty;
    public string AwsSecretKey { get; set; } = string.Empty;
}
