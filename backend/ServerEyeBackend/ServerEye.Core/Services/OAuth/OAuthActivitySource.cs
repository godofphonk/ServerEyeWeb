namespace ServerEye.Core.Services.OAuth;

using System.Diagnostics;

/// <summary>
/// OAuth activity source for distributed tracing.
/// </summary>
public static class OAuthActivitySource
{
    // OAuth operation names
    public const string CreateChallengeOperation = "oauth.create_challenge";
    public const string ExchangeCodeOperation = "oauth.exchange_code";
    public const string GetUserInfoOperation = "oauth.get_user_info";
    public const string ValidateTokenOperation = "oauth.validate_token";
    public const string ProcessCallbackOperation = "oauth.process_callback";
    public const string LinkExternalLoginOperation = "oauth.link_external_login";

    // OAuth attribute keys
    public const string ProviderAttribute = "oauth.provider";
    public const string ActionAttribute = "oauth.action";
    public const string StateAttribute = "oauth.state";
    public const string CodeVerifierAttribute = "oauth.code_verifier";
    public const string ReturnUrlAttribute = "oauth.return_url";
    public const string ErrorCodeAttribute = "oauth.error_code";
    public const string ErrorTypeAttribute = "oauth.error_type";
    public const string ErrorMessageAttribute = "oauth.error_message";
    public const string UserIdAttribute = "oauth.user_id";
    public const string EmailAttribute = "oauth.email";
    public const string ExternalIdAttribute = "oauth.external_id";
    public const string LinkingActionAttribute = "oauth.linking_action";
    public const string IpAddressAttribute = "oauth.ip_address";
    public const string UserAgentAttribute = "oauth.user_agent";

    public static readonly ActivitySource Instance = new("ServerEye.OAuth");

    /// <summary>
    /// Create an activity for OAuth challenge creation.
    /// </summary>
    public static Activity? StartCreateChallengeActivity(string provider, string? action = null, Uri? returnUrl = null)
    {
        var activity = Instance.StartActivity(CreateChallengeOperation);
        if (activity != null)
        {
            activity.SetTag(ProviderAttribute, provider);
            
            if (!string.IsNullOrEmpty(action))
            {
                activity.SetTag(ActionAttribute, action);
            }
            
            if (returnUrl != null)
            {
                activity.SetTag(ReturnUrlAttribute, returnUrl.ToString());
            }
        }
        
        return activity;
    }

    /// <summary>
    /// Create an activity for OAuth code exchange.
    /// </summary>
    public static Activity? StartExchangeCodeActivity(string provider, string state)
    {
        var activity = Instance.StartActivity(ExchangeCodeOperation);
        if (activity != null)
        {
            activity.SetTag(ProviderAttribute, provider);
            activity.SetTag(StateAttribute, state);
        }
        
        return activity;
    }

    /// <summary>
    /// Create an activity for OAuth user info retrieval.
    /// </summary>
    public static Activity? StartGetUserInfoActivity(string provider, string accessToken)
    {
        var activity = Instance.StartActivity(GetUserInfoOperation);
        if (activity != null)
        {
            activity.SetTag(ProviderAttribute, provider);
            
            // Don't log full access token for security
            activity.SetTag("oauth.access_token_length", accessToken?.Length ?? 0);
        }
        
        return activity;
    }

    /// <summary>
    /// Create an activity for OAuth token validation.
    /// </summary>
    public static Activity? StartValidateTokenActivity(string provider, string accessToken)
    {
        var activity = Instance.StartActivity(ValidateTokenOperation);
        if (activity != null)
        {
            activity.SetTag(ProviderAttribute, provider);
            
            // Don't log full access token for security
            activity.SetTag("oauth.access_token_length", accessToken?.Length ?? 0);
        }
        
        return activity;
    }

    /// <summary>
    /// Create an activity for OAuth callback processing.
    /// </summary>
    public static Activity? StartProcessCallbackActivity(string provider, string? ipAddress = null, string? userAgent = null)
    {
        var activity = Instance.StartActivity(ProcessCallbackOperation);
        if (activity != null)
        {
            activity.SetTag(ProviderAttribute, provider);
            
            if (!string.IsNullOrEmpty(ipAddress))
            {
                activity.SetTag(IpAddressAttribute, ipAddress);
            }
            
            if (!string.IsNullOrEmpty(userAgent))
            {
                activity.SetTag(UserAgentAttribute, userAgent);
            }
        }
        
        return activity;
    }

    /// <summary>
    /// Create an activity for OAuth external login linking.
    /// </summary>
    public static Activity? StartLinkExternalLoginActivity(string provider, Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        var activity = Instance.StartActivity(LinkExternalLoginOperation);
        if (activity != null)
        {
            activity.SetTag(ProviderAttribute, provider);
            activity.SetTag(UserIdAttribute, userId.ToString());
            
            if (!string.IsNullOrEmpty(ipAddress))
            {
                activity.SetTag(IpAddressAttribute, ipAddress);
            }
            
            if (!string.IsNullOrEmpty(userAgent))
            {
                activity.SetTag(UserAgentAttribute, userAgent);
            }
        }
        
        return activity;
    }

    /// <summary>
    /// Set success status on activity with optional user info.
    /// </summary>
    public static void SetSuccess(this Activity activity, string? userId = null, string? email = null, string? externalId = null)
    {
        activity.SetStatus(ActivityStatusCode.Ok);
        
        if (!string.IsNullOrEmpty(userId))
        {
            activity.SetTag(UserIdAttribute, userId);
        }
        
        if (!string.IsNullOrEmpty(email))
        {
            activity.SetTag(EmailAttribute, email);
        }
        
        if (!string.IsNullOrEmpty(externalId))
        {
            activity.SetTag(ExternalIdAttribute, externalId);
        }
    }

    /// <summary>
    /// Set error status on activity.
    /// </summary>
    public static void SetError(this Activity activity, string errorType, string? errorMessage = null, Exception? exception = null)
    {
        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
        activity.SetTag(ErrorTypeAttribute, errorType);
        
        if (!string.IsNullOrEmpty(errorMessage))
        {
            activity.SetTag(ErrorMessageAttribute, errorMessage);
        }
        
        if (exception != null)
        {
            activity.SetTag("exception.type", exception.GetType().Name);
            activity.SetTag("exception.message", exception.Message);
            
            // Add exception as event instead of RecordException
            var exceptionTags = new ActivityTagsCollection
            {
                { "exception.type", exception.GetType().Name },
                { "exception.message", exception.Message },
                { "exception.stacktrace", exception.StackTrace ?? string.Empty }
            };
            activity.AddEvent(new ActivityEvent("exception", tags: exceptionTags));
        }
    }

    /// <summary>
    /// Add linking context to activity.
    /// </summary>
    public static void SetLinkingContext(this Activity activity, string linkingAction, string? linkingProvider = null, string? linkingUserId = null)
    {
        activity.SetTag(LinkingActionAttribute, linkingAction);
        
        if (!string.IsNullOrEmpty(linkingProvider))
        {
            activity.SetTag("oauth.linking_provider", linkingProvider);
        }
        
        if (!string.IsNullOrEmpty(linkingUserId))
        {
            activity.SetTag("oauth.linking_user_id", linkingUserId);
        }
    }
}
