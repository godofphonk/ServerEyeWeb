namespace ServerEye.Core.Configuration;

public class OAuthSettings
{
    public GoogleSettings Google { get; init; } = new();
    public GitHubSettings GitHub { get; init; } = new();
    public TelegramSettings Telegram { get; init; } = new();
}

public class GoogleSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public Uri RedirectUri { get; set; } = new Uri("https://127.0.0.1");
    public bool Enabled { get; set; }
}

public class GitHubSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public Uri RedirectUri { get; set; } = new Uri("https://127.0.0.1");
    public bool Enabled { get; set; }
}

public class TelegramSettings
{
    public string BotToken { get; set; } = string.Empty;
    public Uri RedirectUri { get; set; } = new Uri("https://127.0.0.1");
    public bool Enabled { get; set; }
}
