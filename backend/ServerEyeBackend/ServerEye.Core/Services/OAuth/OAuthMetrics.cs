namespace ServerEye.Core.Services.OAuth;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

/// <summary>
/// OAuth telemetry metrics collection.
/// </summary>
public sealed class OAuthMetrics : IDisposable
{
    private readonly ILogger<OAuthMetrics> logger;
    private readonly Meter meter;

    // Counters
    private readonly Counter<long> oauthChallengesCreated;
    private readonly Counter<long> oauthTokenExchanges;
    private readonly Counter<long> oauthUserInfoRequests;
    private readonly Counter<long> oauthErrors;
    private readonly Counter<long> oauthLinkingAttempts;

    // Histograms
    private readonly Histogram<double> challengeCreationDuration;
    private readonly Histogram<double> tokenExchangeDuration;
    private readonly Histogram<double> userInfoRetrievalDuration;

    public OAuthMetrics(IMeterFactory meterFactory, ILogger<OAuthMetrics> logger)
    {
        this.logger = logger;
        this.meter = meterFactory.Create("ServerEye.OAuth");

        // Initialize counters
        this.oauthChallengesCreated = this.meter.CreateCounter<long>(
            "oauth_challenges_created_total",
            "Total number of OAuth challenges created");

        this.oauthTokenExchanges = this.meter.CreateCounter<long>(
            "oauth_token_exchanges_total",
            "Total number of OAuth token exchanges");

        this.oauthUserInfoRequests = this.meter.CreateCounter<long>(
            "oauth_user_info_requests_total",
            "Total number of OAuth user info requests");

        this.oauthErrors = this.meter.CreateCounter<long>(
            "oauth_errors_total",
            "Total number of OAuth errors");

        this.oauthLinkingAttempts = this.meter.CreateCounter<long>(
            "oauth_linking_attempts_total",
            "Total number of OAuth account linking attempts");

        // Initialize histograms
        this.challengeCreationDuration = this.meter.CreateHistogram<double>(
            "oauth_challenge_creation_duration_seconds",
            "Duration of OAuth challenge creation in seconds");

        this.tokenExchangeDuration = this.meter.CreateHistogram<double>(
            "oauth_token_exchange_duration_seconds",
            "Duration of OAuth token exchange in seconds");

        this.userInfoRetrievalDuration = this.meter.CreateHistogram<double>(
            "oauth_user_info_retrieval_duration_seconds",
            "Duration of OAuth user info retrieval in seconds");

        this.logger.LogInformation("OAuth metrics initialized successfully");
    }

    /// <summary>
    /// Record OAuth challenge creation.
    /// </summary>
    public void RecordChallengeCreated(string provider, string? action = null)
    {
        var tags = new TagList
        {
            { "provider", provider },
            { "action", action ?? "login" }
        };

        this.oauthChallengesCreated.Add(1, tags);
        this.logger.LogDebug("OAuth challenge created metric recorded - Provider: {Provider}, Action: {Action}", (provider ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", (action ?? "login").Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
    }

    /// <summary>
    /// Record OAuth challenge creation duration.
    /// </summary>
    public void RecordChallengeCreationDuration(string provider, double duration, string? action = null)
    {
        var tags = new TagList
        {
            { "provider", provider },
            { "action", action ?? "login" }
        };

        this.challengeCreationDuration.Record(duration, tags);
        this.logger.LogDebug("OAuth challenge creation duration recorded - Provider: {Provider}, Duration: {Duration}s", provider, duration);
    }

    /// <summary>
    /// Record OAuth token exchange.
    /// </summary>
    public void RecordTokenExchange(string provider, bool success, string? errorType = null)
    {
        var tags = new TagList
        {
            { "provider", provider },
            { "status", success ? "success" : "error" }
        };

        if (!success && !string.IsNullOrEmpty(errorType))
        {
            tags.Add("error_type", errorType);
        }

        this.oauthTokenExchanges.Add(1, tags);
        this.logger.LogDebug(
            "OAuth token exchange metric recorded - Provider: {Provider}, Success: {Success}, ErrorType: {ErrorType}",
            (provider ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
            success,
            (errorType ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
    }

    /// <summary>
    /// Record OAuth token exchange duration.
    /// </summary>
    public void RecordTokenExchangeDuration(string provider, double duration, bool success)
    {
        var tags = new TagList
        {
            { "provider", provider },
            { "status", success ? "success" : "error" }
        };

        this.tokenExchangeDuration.Record(duration, tags);
        this.logger.LogDebug(
            "OAuth token exchange duration recorded - Provider: {Provider}, Duration: {Duration}s, Success: {Success}",
            (provider ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
            duration,
            success);
    }

    /// <summary>
    /// Record OAuth user info request.
    /// </summary>
    public void RecordUserInfoRequest(string provider, bool success, string? errorType = null)
    {
        var tags = new TagList
        {
            { "provider", provider },
            { "status", success ? "success" : "error" }
        };

        if (!success && !string.IsNullOrEmpty(errorType))
        {
            tags.Add("error_type", errorType);
        }

        this.oauthUserInfoRequests.Add(1, tags);
        this.logger.LogDebug(
            "OAuth user info request metric recorded - Provider: {Provider}, Success: {Success}, ErrorType: {ErrorType}",
            provider,
            success,
            errorType);
    }

    /// <summary>
    /// Record OAuth user info retrieval duration.
    /// </summary>
    public void RecordUserInfoRetrievalDuration(string provider, double duration, bool success)
    {
        var tags = new TagList
        {
            { "provider", provider },
            { "status", success ? "success" : "error" }
        };

        this.userInfoRetrievalDuration.Record(duration, tags);
        this.logger.LogDebug(
            "OAuth user info retrieval duration recorded - Provider: {Provider}, Duration: {Duration}s, Success: {Success}",
            provider,
            duration,
            success);
    }

    /// <summary>
    /// Record OAuth error.
    /// </summary>
    public void RecordError(string provider, string operation, string errorType, string? errorMessage = null)
    {
        var tags = new TagList
        {
            { "provider", provider },
            { "operation", operation },
            { "error_type", errorType }
        };

        this.oauthErrors.Add(1, tags);
        this.logger.LogWarning(
            "OAuth error recorded - Provider: {Provider}, Operation: {Operation}, ErrorType: {ErrorType}, Message: {Message}",
            provider,
            operation,
            errorType,
            errorMessage);
    }

    /// <summary>
    /// Record OAuth account linking attempt.
    /// </summary>
    public void RecordLinkingAttempt(string provider, bool success, string? errorType = null)
    {
        var tags = new TagList
        {
            { "provider", provider },
            { "status", success ? "success" : "error" }
        };

        if (!success && !string.IsNullOrEmpty(errorType))
        {
            tags.Add("error_type", errorType);
        }

        this.oauthLinkingAttempts.Add(1, tags);
        this.logger.LogDebug(
            "OAuth linking attempt recorded - Provider: {Provider}, Success: {Success}, ErrorType: {ErrorType}",
            provider,
            success,
            errorType);
    }

    public void Dispose()
    {
        this.meter?.Dispose();
        this.logger.LogInformation("OAuth metrics disposed");
    }
}
