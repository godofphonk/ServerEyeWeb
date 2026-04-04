namespace ServerEye.Core.Services.OAuth.Providers;

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Core.Services.OAuth;

public sealed class GoogleOAuthProvider(
    OAuthSettings oauthSettings,
    ILogger<GoogleOAuthProvider> logger)
    : BaseOAuthProvider(oauthSettings, logger), IOAuthProvider
{
    public override OAuthProvider ProviderType => OAuthProvider.Google;

    public override bool IsEnabled()
    {
        return Settings.Google.Enabled;
    }

    public override async Task<OAuthChallengeResponseDto> CreateChallengeAsync(
        string state,
        string codeChallenge,
        Uri? returnUrl)
    {
        using var activity = OAuthActivitySource.StartCreateChallengeActivity("Google", null, returnUrl);
        var startTime = DateTime.UtcNow;

        Logger.LogInformation(
            "CreateGoogleChallengeAsync called - State: {State}, CodeChallenge: {CodeChallenge}, ReturnUrl: {ReturnUrl}",
            state,
            codeChallenge[..Math.Min(codeChallenge.Length, 20)] + "...",
            returnUrl?.ToString() ?? "null");

        try
        {
            var settings = Settings.Google;
            var scopes = "openid email profile";
            var redirectUri = Uri.EscapeDataString(settings.RedirectUri.ToString());

            var url = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                      $"client_id={settings.ClientId}&" +
                      $"redirect_uri={redirectUri}&" +
                      $"response_type=code&" +
                      $"scope={scopes}&" +
                      $"state={state}&" +
                      $"code_challenge={codeChallenge}&" +
                      $"code_challenge_method=S256&" +
                      $"access_type=offline&" + // Request refresh token
                      $"prompt=consent"; // Force consent dialog to get refresh token

            if (returnUrl != null)
            {
                url += $"&return_url={Uri.EscapeDataString(returnUrl.ToString())}";
            }

            Logger.LogInformation("Created Google OAuth challenge URL: {Url}", url[..Math.Min(url.Length, 100)] + "...");

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            activity?.SetTag(OAuthActivitySource.StateAttribute, state);
            activity?.SetTag(OAuthActivitySource.CodeVerifierAttribute, codeChallenge[..Math.Min(codeChallenge.Length, 10)] + "...");
            activity?.SetSuccess();

            return new OAuthChallengeResponseDto
            {
                ChallengeUrl = new Uri(url),
                State = state,
                CodeVerifier = codeChallenge, // Store for verification
                Action = null
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating Google OAuth challenge");
            activity?.SetError(ex.GetType().Name, ex.Message, ex);
            throw;
        }
    }

    public override async Task<OAuthUserInfoDto> GetUserInfoAsync(string accessToken, string? idToken, CancellationToken cancellationToken)
    {
        using var activity = OAuthActivitySource.StartGetUserInfoActivity("Google", accessToken);
        var startTime = DateTime.UtcNow;

        Logger.LogInformation(
            "GetGoogleUserInfoAsync called - AccessToken: {AccessToken}, IdToken: {IdToken}",
            accessToken[..Math.Min(accessToken.Length, 20)] + "...",
            string.IsNullOrEmpty(idToken) ? "NULL" : idToken[..Math.Min(idToken.Length, 20)] + "...");

        try
        {
            OAuthUserInfoDto userInfo;

            // If we have an ID token, decode it first
            if (!string.IsNullOrEmpty(idToken))
            {
                Logger.LogInformation("Decoding user info from ID token");

                // This is a simplified implementation - in production, you should validate the token signature
                var parts = idToken.Split('.');
                if (parts.Length > 1)
                {
                    var payload = DecodeBase64Url(parts[1]);
                    var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(payload!) ?? throw new InvalidOperationException("Failed to parse ID token payload");

                    userInfo = new OAuthUserInfoDto
                    {
                        Id = tokenData.GetValueOrDefault("sub")?.ToString() ?? string.Empty,
                        Email = tokenData.GetValueOrDefault("email")?.ToString() ?? string.Empty,
                        Name = tokenData.GetValueOrDefault("name")?.ToString() ?? string.Empty,
                        Username = tokenData.GetValueOrDefault("given_name")?.ToString() ?? string.Empty,
                        AvatarUrl = !string.IsNullOrEmpty(tokenData.GetValueOrDefault("picture")?.ToString()) ? new Uri(tokenData.GetValueOrDefault("picture")?.ToString()!) : null,
                        EmailVerified = bool.TryParse(tokenData.GetValueOrDefault("email_verified")?.ToString(), out var verified) && verified,
                        RawData = tokenData
                    };

                    activity?.SetTag(OAuthActivitySource.ExternalIdAttribute, userInfo.Id);
                    activity?.SetTag(OAuthActivitySource.EmailAttribute, userInfo.Email);
                    activity?.SetSuccess();

                    return userInfo;
                }
            }

            // Fallback to API call
            Logger.LogInformation("Fetching user info from Google API");

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(new Uri($"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}"), cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, JsonOptions)
                   ?? throw new InvalidOperationException("Failed to get user info from Google");

            userInfo = new OAuthUserInfoDto
            {
                Id = userData.GetValueOrDefault("id")?.ToString() ?? string.Empty,
                Email = userData.GetValueOrDefault("email")?.ToString() ?? string.Empty,
                Name = userData.GetValueOrDefault("name")?.ToString() ?? string.Empty,
                Username = userData.GetValueOrDefault("given_name")?.ToString() ?? string.Empty,
                AvatarUrl = !string.IsNullOrEmpty(userData.GetValueOrDefault("picture")?.ToString()) ? new Uri(userData.GetValueOrDefault("picture")?.ToString()!) : null,
                EmailVerified = bool.TryParse(userData.GetValueOrDefault("verified_email")?.ToString(), out var emailVerified) && emailVerified,
                RawData = userData
            };

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            activity?.SetTag(OAuthActivitySource.ExternalIdAttribute, userInfo.Id);
            activity?.SetTag(OAuthActivitySource.EmailAttribute, userInfo.Email);
            activity?.SetSuccess();

            return userInfo;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting Google user info");
            activity?.SetError(ex.GetType().Name, ex.Message, ex);
            throw;
        }
    }

    public override async Task<TokenResponseDto> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken)
    {
        using var activity = OAuthActivitySource.StartExchangeCodeActivity("Google", code[..Math.Min(code.Length, 10)] + "...");
        var startTime = DateTime.UtcNow;

        using var httpClient = new HttpClient();
        var settings = Settings.Google;
        var tokenEndpoint = "https://oauth2.googleapis.com/token";

        Logger.LogInformation(
            "Exchanging Google code for token - ClientId: {ClientId}, RedirectUri: {RedirectUri}, CodeVerifier: {CodeVerifier}",
            settings.ClientId,
            settings.RedirectUri.ToString(),
            codeVerifier.Length > 20 ? $"{codeVerifier[..20]}..." : codeVerifier);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = settings.ClientId,
                ["client_secret"] = settings.ClientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = settings.RedirectUri.ToString(),
                ["code_verifier"] = codeVerifier // Add PKCE code verifier
            };

            Logger.LogInformation(
                "Google token request parameters: {Parameters}",
                string.Join(
                    ", ",
                    parameters.Select(p => $"{p.Key}={p.Value[..Math.Min(p.Value.Length, 20)]}...")));

            using var content = new FormUrlEncodedContent(parameters);
            var response = await httpClient.PostAsync(new Uri(tokenEndpoint), content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            Logger.LogInformation(
                "Google token response - Status: {Status}, Content: {Content}",
                (int)response.StatusCode,
                responseContent);

            response.EnsureSuccessStatusCode();

            // Manual JSON parsing to extract id_token since JsonPropertyName doesn't work
            Logger.LogInformation("Raw JSON response: {Json}", responseContent);

            // Parse JSON manually to get all fields
            using var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            var tokenResponse = new TokenResponseDto
            {
                AccessToken = root.GetProperty("access_token").GetString() ?? string.Empty,
                RefreshToken = root.GetProperty("refresh_token").GetString() ?? string.Empty,
                IdToken = root.GetProperty("id_token").GetString() ?? string.Empty,
                TokenType = root.GetProperty("token_type").GetString() ?? string.Empty,
                ExpiresIn = root.GetProperty("expires_in").GetInt32(),
                Scope = root.GetProperty("scope").GetString() ?? string.Empty
            };

            Logger.LogInformation(
                "TokenResponse fields - AccessToken: {AccessToken}, RefreshToken: {RefreshToken}, IdToken: {IdToken}, TokenType: {TokenType}, ExpiresIn: {ExpiresIn}, Scope: {Scope}",
                tokenResponse.AccessToken[..Math.Min(tokenResponse.AccessToken.Length, 20)] + "...",
                string.IsNullOrEmpty(tokenResponse.RefreshToken) ? "NULL" : tokenResponse.RefreshToken[..Math.Min(tokenResponse.RefreshToken.Length, 20)] + "...",
                string.IsNullOrEmpty(tokenResponse.IdToken) ? "NULL" : tokenResponse.IdToken[..Math.Min(tokenResponse.IdToken.Length, 20)] + "...",
                tokenResponse.TokenType,
                tokenResponse.ExpiresIn,
                tokenResponse.Scope);

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            activity?.SetSuccess();

            return tokenResponse;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exchanging Google code for token");
            activity?.SetError(ex.GetType().Name, ex.Message, ex);
            throw;
        }
    }
}
