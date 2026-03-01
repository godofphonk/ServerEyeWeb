namespace ServerEye.Core.Configuration;

public class OAuthSettings
{
    public GoogleSettings Google { get; set; } = new();
    public GitHubSettings GitHub { get; set; } = new();
    public TelegramSettings Telegram { get; set; } = new();
    public MicrosoftSettings Microsoft { get; set; } = new();
}

public class GoogleSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public Uri RedirectUri { get; set; } = new Uri("https://localhost");
    public bool Enabled { get; set; }
}

public class GitHubSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public Uri RedirectUri { get; set; } = new Uri("https://localhost");
    public bool Enabled { get; set; }
}

public class TelegramSettings
{
    public string BotToken { get; set; } = string.Empty;
    public Uri RedirectUri { get; set; } = new Uri("https://localhost");
    public bool Enabled { get; set; }
}

public class MicrosoftSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public Uri RedirectUri { get; set; } = new Uri("https://localhost");
    public bool Enabled { get; set; }
}
