namespace ServerEye.Core.Services.OAuth.Factory;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Core.Services.OAuth.Providers;

public interface IOAuthProviderFactory
{
    public IOAuthProvider GetProvider(OAuthProvider provider);
    public bool IsProviderEnabled(OAuthProvider provider);
    public IEnumerable<OAuthProvider> GetEnabledProviders();
}

public sealed class OAuthProviderFactory(
    GoogleOAuthProvider google,
    GitHubOAuthProvider github,
    TelegramOAuthProvider telegram,
    ILogger<OAuthProviderFactory> logger) : IOAuthProviderFactory
{
    private readonly Dictionary<OAuthProvider, IOAuthProvider> _providers = new()
    {
        [OAuthProvider.Google] = google,
        [OAuthProvider.GitHub] = github,
        [OAuthProvider.Telegram] = telegram
    };

    public IOAuthProvider GetProvider(OAuthProvider provider)
    {
        logger.LogDebug("Getting OAuth provider: {Provider}", provider);

        if (_providers.TryGetValue(provider, out var providerInstance))
        {
            logger.LogDebug("OAuth provider {Provider} found and enabled: {Enabled}", provider, providerInstance.IsEnabled());
            return providerInstance;
        }

        logger.LogError("OAuth provider {Provider} not supported", provider);
        throw new NotSupportedException($"Provider {provider} not supported");
    }

    public bool IsProviderEnabled(OAuthProvider provider)
    {
        if (_providers.TryGetValue(provider, out var providerInstance))
        {
            var enabled = providerInstance.IsEnabled();
            logger.LogDebug("OAuth provider {Provider} enabled status: {Enabled}", provider, enabled);
            return enabled;
        }

        logger.LogDebug("OAuth provider {Provider} not found, returning false", provider);
        return false;
    }

    public IEnumerable<OAuthProvider> GetEnabledProviders()
    {
        var enabledProviders = _providers
            .Where(kvp => kvp.Value.IsEnabled())
            .Select(kvp => kvp.Key)
            .ToList();

        logger.LogDebug("Enabled OAuth providers: {Providers}", string.Join(", ", enabledProviders));
        return enabledProviders;
    }
}
