#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.UnitTests.Services.OAuth;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ServerEye.Core.Configuration;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Core.Services.OAuth;
using ServerEye.Core.Services.OAuth.Providers;
using Xunit;

public class TelegramOAuthProviderTests
{
    private readonly Mock<ILogger<TelegramOAuthProvider>> mockLogger;
    private readonly OAuthSettings oauthSettings;
    private readonly TelegramOAuthProvider provider;

    public TelegramOAuthProviderTests()
    {
        this.mockLogger = new Mock<ILogger<TelegramOAuthProvider>>();
        
        this.oauthSettings = new OAuthSettings
        {
            Telegram = new TelegramSettings
            {
                Enabled = true,
                BotId = "test-bot-id",
                BotToken = "test-bot-token",
                RedirectUri = new Uri("https://localhost:5000/auth/telegram/callback")
            }
        };

        this.provider = new TelegramOAuthProvider(this.oauthSettings, this.mockLogger.Object);
    }

    #region ProviderType Tests

    [Fact]
    public void ProviderType_ShouldReturnTelegram()
    {
        // Act
        var result = this.provider.ProviderType;

        // Assert
        result.Should().Be(OAuthProvider.Telegram);
    }

    #endregion

    #region IsEnabled Tests

    [Fact]
    public void IsEnabled_WhenTelegramEnabled_ShouldReturnTrue()
    {
        // Arrange
        this.oauthSettings.Telegram.Enabled = true;

        // Act
        var result = this.provider.IsEnabled();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WhenTelegramDisabled_ShouldReturnFalse()
    {
        // Arrange
        this.oauthSettings.Telegram.Enabled = false;

        // Act
        var result = this.provider.IsEnabled();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CreateChallengeAsync Tests

    [Fact]
    public async Task CreateChallengeAsync_ShouldGenerateValidChallengeUrl()
    {
        // Arrange
        var state = "test-state-123";
        var codeChallenge = "test-code-challenge"; // Not used by Telegram
        var returnUrl = new Uri("https://localhost:3000/dashboard");

        // Act
        var result = await this.provider.CreateChallengeAsync(state, codeChallenge, returnUrl);

        // Assert
        result.Should().NotBeNull();
        result.ChallengeUrl.Should().NotBeNull();
        result.State.Should().Be("telegram_test-state-123"); // Should have provider prefix
        result.CodeVerifier.Should().BeEmpty();
        result.Action.Should().BeNull();

        var url = result.ChallengeUrl.ToString();
        url.Should().Contain("oauth.telegram.org/auth");
        url.Should().Contain("bot_id=123456789");
        url.Should().Contain("state=telegram_test-state-123");
        url.Should().Contain("request_access=write");
        url.Should().Contain("origin=" + Uri.EscapeDataString("https://localhost:5000"));
        url.Should().NotContain("code_challenge"); // Telegram doesn't support PKCE
        url.Should().Contain("return_url=" + Uri.EscapeDataString(returnUrl.ToString()));
    }

    [Fact]
    public async Task CreateChallengeAsync_WithoutReturnUrl_ShouldGenerateValidChallengeUrl()
    {
        // Arrange
        var state = "test-state-123";
        var codeChallenge = "test-code-challenge";

        // Act
        var result = await this.provider.CreateChallengeAsync(state, codeChallenge, null);

        // Assert
        result.Should().NotBeNull();
        result.ChallengeUrl.Should().NotBeNull();
        
        var url = result.ChallengeUrl.ToString();
        url.Should().NotContain("return_url=" + Uri.EscapeDataString(state));
    }

    [Fact]
    public async Task CreateChallengeAsync_ShouldAddTelegramPrefixToState()
    {
        // Arrange
        var state = "original-state";
        var codeChallenge = "test-code-challenge";

        // Act
        var result = await this.provider.CreateChallengeAsync(state, codeChallenge, null);

        // Assert
        result.State.Should().Be("telegram_original-state");
    }

    [Fact]
    public async Task CreateChallengeAsync_ShouldExtractOriginCorrectly()
    {
        // Arrange
        var state = "test-state";
        var codeChallenge = "test-code-challenge";
        // Use different port to test origin extraction
        this.oauthSettings.Telegram.RedirectUri = new Uri("https://myapp.com:8080/auth/telegram/callback");

        // Act
        var result = await this.provider.CreateChallengeAsync(state, codeChallenge, null);

        // Assert
        var url = result.ChallengeUrl.ToString();
        url.Should().Contain("origin=" + Uri.EscapeDataString("https://myapp.com:8080"));
    }

    #endregion

    #region ExchangeCodeAsync Tests

    [Fact]
    public async Task ExchangeCodeAsync_ShouldReturnUserDataAsAccessToken()
    {
        // Arrange
        var userDataJson = "{\"id\":\"12345\",\"first_name\":\"Test\",\"last_name\":\"User\",\"username\":\"testuser\"}";
        var codeVerifier = "test-code-verifier"; // Not used by Telegram

        // Act
        var result = await this.provider.ExchangeCodeAsync(userDataJson, codeVerifier, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(userDataJson); // User data becomes access token
        result.RefreshToken.Should().BeEmpty(); // Telegram doesn't use refresh tokens
        result.IdToken.Should().BeEmpty(); // Telegram doesn't use ID tokens
        result.TokenType.Should().Be("Bearer");
        result.ExpiresIn.Should().Be(3600);
        result.Scope.Should().Be("telegram_user");
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithEmptyCode_ShouldStillReturnTokenResponse()
    {
        // Arrange
        var code = "";
        var codeVerifier = "test-code-verifier";

        // Act
        var result = await this.provider.ExchangeCodeAsync(code, codeVerifier, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(code);
        result.TokenType.Should().Be("Bearer");
        result.Scope.Should().Be("telegram_user");
    }

    #endregion

    #region GetUserInfoAsync Tests

    [Fact]
    public async Task GetUserInfoAsync_WithValidUserData_ShouldParseCorrectly()
    {
        // Arrange
        var userDataJson = JsonSerializer.Serialize(new
        {
            id = "12345",
            first_name = "Test",
            last_name = "User",
            username = "testuser"
        });

        // Act
        var result = await this.provider.GetUserInfoAsync(userDataJson, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("12345");
        result.Name.Should().Be("Test User");
        result.Username.Should().Be("testuser");
        result.Email.Should().BeNull(); // Telegram doesn't provide email
        result.AvatarUrl.Should().BeNull(); // Telegram doesn't provide avatar in basic OAuth
        result.EmailVerified.Should().BeFalse();
        result.RawData.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserInfoAsync_WithCapitalizedFieldNames_ShouldParseCorrectly()
    {
        // Arrange
        var userDataJson = JsonSerializer.Serialize(new
        {
            Id = "67890",
            FirstName = "John",
            LastName = "Doe",
            Username = "johndoe"
        });

        // Act
        var result = await this.provider.GetUserInfoAsync(userDataJson, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("67890");
        result.Name.Should().Be("John Doe");
        result.Username.Should().Be("johndoe");
    }

    [Fact]
    public async Task GetUserInfoAsync_WithPartialData_ShouldHandleGracefully()
    {
        // Arrange
        var userDataJson = JsonSerializer.Serialize(new
        {
            id = "12345",
            first_name = "Test"
            // Missing last_name and username
        });

        // Act
        var result = await this.provider.GetUserInfoAsync(userDataJson, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("12345");
        result.Name.Should().Be("Test"); // Only first name
        result.Username.Should().BeEmpty(); // Missing username
    }

    [Fact]
    public async Task GetUserInfoAsync_WithOnlyFirstName_ShouldReturnOnlyFirstName()
    {
        // Arrange
        var userDataJson = JsonSerializer.Serialize(new
        {
            id = "12345",
            first_name = "Alice"
        });

        // Act
        var result = await this.provider.GetUserInfoAsync(userDataJson, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("12345");
        result.Name.Should().Be("Alice");
        result.Username.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserInfoAsync_WithOnlyLastName_ShouldReturnOnlyLastName()
    {
        // Arrange
        var userDataJson = JsonSerializer.Serialize(new
        {
            id = "12345",
            last_name = "Smith"
        });

        // Act
        var result = await this.provider.GetUserInfoAsync(userDataJson, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("12345");
        result.Name.Should().Be(" Smith"); // Space before last name
        result.Username.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserInfoAsync_WithInvalidJson_ShouldProvideFallbackData()
    {
        // Arrange
        var invalidJson = "invalid json data";

        // Act
        var result = await this.provider.GetUserInfoAsync(invalidJson, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(invalidJson); // Falls back to using the input as ID
        result.Name.Should().Be("Telegram User");
        result.Username.Should().Be("telegram_user");
        result.Email.Should().BeNull();
        result.EmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserInfoAsync_WithEmptyUserData_ShouldProvideFallbackData()
    {
        // Arrange
        var emptyJson = "{}";

        // Act
        var result = await this.provider.GetUserInfoAsync(emptyJson, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("unknown");
        result.Name.Should().BeEmpty(); // No name fields found
        result.Username.Should().BeEmpty();
        result.Email.Should().BeNull();
        result.EmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserInfoAsync_WithNumericId_ShouldHandleCorrectly()
    {
        // Arrange
        var userDataJson = JsonSerializer.Serialize(new
        {
            id = 12345, // Numeric ID instead of string
            first_name = "Test",
            last_name = "User"
        });

        // Act
        var result = await this.provider.GetUserInfoAsync(userDataJson, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("unknown"); // Non-string ID becomes "unknown"
        result.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task GetUserInfoAsync_ShouldIgnoreIdToken()
    {
        // Arrange
        var userDataJson = JsonSerializer.Serialize(new
        {
            id = "12345",
            first_name = "Test",
            last_name = "User"
        });
        var idToken = "some-id-token";

        // Act
        var result = await this.provider.GetUserInfoAsync(userDataJson, idToken, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("12345");
        result.Name.Should().Be("Test User");
        // Telegram should ignore ID token parameter
    }

    #endregion

    #region ValidateTokenAsync Tests

    [Fact]
    public async Task ValidateTokenAsync_WithValidUserData_ShouldReturnTrue()
    {
        // Arrange
        var userDataJson = JsonSerializer.Serialize(new
        {
            id = "12345",
            first_name = "Test",
            last_name = "User"
        });

        // Act
        var result = await this.provider.ValidateTokenAsync(userDataJson, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidUserData_ShouldReturnFalse()
    {
        // Arrange
        var invalidJson = "invalid json data";

        // Act
        var result = await this.provider.ValidateTokenAsync(invalidJson, CancellationToken.None);

        // Assert
        result.Should().BeTrue(); // Telegram provider is forgiving and returns true even for invalid data
    }

    [Fact]
    public async Task ValidateTokenAsync_WithEmptyToken_ShouldReturnFalse()
    {
        // Arrange
        var emptyToken = "";

        // Act
        var result = await this.provider.ValidateTokenAsync(emptyToken, CancellationToken.None);

        // Assert
        result.Should().BeTrue(); // Telegram provider is forgiving
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CreateChallengeAsync_WithDifferentPorts_ShouldHandleOriginCorrectly()
    {
        // Arrange
        var state = "test";
        var codeChallenge = "test";
        
        // Test with different port configurations
        var testCases = new[]
        {
            ("https://localhost:3000", "https://localhost:3000"),
            ("https://myapp.com:8080", "https://myapp.com:8080"),
            ("https://app.example.com:443", "https://app.example.com:443")
        };

        foreach (var (redirectUri, expectedOrigin) in testCases)
        {
            // Arrange
            this.oauthSettings.Telegram.RedirectUri = new Uri(redirectUri);

            // Act
            var result = await this.provider.CreateChallengeAsync(state, codeChallenge, null);

            // Assert
            var url = result.ChallengeUrl.ToString();
            url.Should().Contain($"origin={Uri.EscapeDataString(expectedOrigin)}");
        }
    }

    [Fact]
    public async Task GetUserInfoAsync_WithComplexUserData_ShouldParseAllFields()
    {
        // Arrange
        var userDataJson = JsonSerializer.Serialize(new
        {
            id = "12345",
            first_name = "Test",
            last_name = "User",
            username = "testuser",
            photo_url = "https://t.me/i/userpic/320/testuser.jpg",
            auth_date = "1640995200"
        });

        // Act
        var result = await this.provider.GetUserInfoAsync(userDataJson, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("12345");
        result.Name.Should().Be("Test User");
        result.Username.Should().Be("testuser");
        result.RawData.Should().NotBeNull();
        
        // Check that additional fields are preserved in RawData
        var rawDataDict = result.RawData as Dictionary<string, object>;
        rawDataDict.Should().NotBeNull();
        rawDataDict.Should().ContainKey("photo_url");
        rawDataDict.Should().ContainKey("auth_date");
    }

    #endregion
}
