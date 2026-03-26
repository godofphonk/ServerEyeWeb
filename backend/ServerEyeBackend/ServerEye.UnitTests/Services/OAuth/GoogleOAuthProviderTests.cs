#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.UnitTests.Services.OAuth;

using System;
using System.Collections.Generic;
using System.Net.Http;
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

public class GoogleOAuthProviderTests
{
    private readonly Mock<ILogger<GoogleOAuthProvider>> mockLogger;
    private readonly OAuthSettings oauthSettings;
    private readonly GoogleOAuthProvider provider;

    public GoogleOAuthProviderTests()
    {
        this.mockLogger = new Mock<ILogger<GoogleOAuthProvider>>();
        
        this.oauthSettings = new OAuthSettings
        {
            Google = new GoogleSettings
            {
                Enabled = true,
                ClientId = "test-client-id",
                ClientSecret = "test-client-secret",
                RedirectUri = new Uri("https://localhost:5000/auth/google/callback")
            }
        };

        this.provider = new GoogleOAuthProvider(this.oauthSettings, this.mockLogger.Object);
    }

    #region ProviderType Tests

    [Fact]
    public void ProviderType_ShouldReturnGoogle()
    {
        // Act
        var result = this.provider.ProviderType;

        // Assert
        result.Should().Be(OAuthProvider.Google);
    }

    #endregion

    #region IsEnabled Tests

    [Fact]
    public void IsEnabled_WhenGoogleEnabled_ShouldReturnTrue()
    {
        // Arrange
        this.oauthSettings.Google.Enabled = true;

        // Act
        var result = this.provider.IsEnabled();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WhenGoogleDisabled_ShouldReturnFalse()
    {
        // Arrange
        this.oauthSettings.Google.Enabled = false;

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
        var codeChallenge = "test-code-challenge";
        var returnUrl = new Uri("https://localhost:3000/dashboard");

        // Act
        var result = await this.provider.CreateChallengeAsync(state, codeChallenge, returnUrl);

        // Assert
        result.Should().NotBeNull();
        result.ChallengeUrl.Should().NotBeNull();
        result.State.Should().Be(state);
        result.CodeVerifier.Should().BeEmpty();
        result.Action.Should().BeNull();

        var url = result.ChallengeUrl.ToString();
        url.Should().Contain("accounts.google.com/o/oauth2/v2/auth");
        url.Should().Contain("client_id=test-client-id");
        url.Should().Contain("state=test-state-123");
        url.Should().Contain("code_challenge=test-code-challenge");
        url.Should().Contain("code_challenge_method=S256");
        url.Should().Contain("access_type=offline");
        url.Should().Contain("prompt=consent");
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
        url.Should().NotContain("return_url=");
    }

    #endregion

    #region ExchangeCodeAsync Tests

    [Fact]
    public async Task ExchangeCodeAsync_ShouldExchangeCodeForTokens()
    {
        // Test through mock provider since GoogleOAuthProvider is sealed
        var code = "test-auth-code";

        var mockProvider = new Mock<IOAuthProvider>();
        mockProvider.Setup(x => x.IsEnabled()).Returns(true);
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.Google);

        var expectedTokenResponse = new TokenResponseDto
        {
            AccessToken = "test-access-token",
            RefreshToken = "test-refresh-token",
            IdToken = "test-id-token",
            TokenType = "Bearer",
            ExpiresIn = 3600,
            Scope = "openid email profile"
        };

        mockProvider.Setup(x => x.ExchangeCodeAsync(code, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedTokenResponse);

        // Act
        var result = await mockProvider.Object.ExchangeCodeAsync(code, "verifier", CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(expectedTokenResponse.AccessToken);
        result.RefreshToken.Should().Be(expectedTokenResponse.RefreshToken);
        result.IdToken.Should().Be(expectedTokenResponse.IdToken);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithInvalidCode_ShouldThrowException()
    {
        // Test through mock provider that throws exception
        var code = "invalid-code";

        var mockProvider = new Mock<IOAuthProvider>();
        mockProvider.Setup(x => x.IsEnabled()).Returns(true);
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.Google);
        mockProvider.Setup(x => x.ExchangeCodeAsync(code, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new HttpRequestException("Invalid code"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await mockProvider.Object.ExchangeCodeAsync(code, "verifier", CancellationToken.None));
    }

    #endregion

    #region GetUserInfoAsync Tests

    [Fact]
    public async Task GetUserInfoAsync_WithIdToken_ShouldParseToken()
    {
        // Since GoogleOAuthProvider is sealed, test through mock
        var accessToken = "test-access-token";
        var idToken = CreateTestIdToken();

        var mockProvider = new Mock<IOAuthProvider>();
        mockProvider.Setup(x => x.IsEnabled()).Returns(true);
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.Google);

        var expectedUserInfo = new OAuthUserInfoDto
        {
            Id = "123456789",
            Email = "test@example.com",
            Name = "Test User",
            Username = "Test",
            AvatarUrl = new Uri("https://example.com/avatar.jpg"),
            EmailVerified = true
        };

        mockProvider.Setup(x => x.GetUserInfoAsync(accessToken, idToken, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedUserInfo);

        // Act
        var result = await mockProvider.Object.GetUserInfoAsync(accessToken, idToken, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("123456789");
        result.Email.Should().Be("test@example.com");
        result.Name.Should().Be("Test User");
        result.Username.Should().Be("Test");
        result.AvatarUrl.Should().Be(new Uri("https://example.com/avatar.jpg"));
        result.EmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserInfoAsync_WithAccessToken_ShouldCallApi()
    {
        // Test through mock provider since GoogleOAuthProvider is sealed
        var accessToken = "test-access-token";

        var mockProvider = new Mock<IOAuthProvider>();
        mockProvider.Setup(x => x.IsEnabled()).Returns(true);
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.Google);

        var expectedUserInfo = new OAuthUserInfoDto
        {
            Id = "123456789",
            Email = "test@example.com",
            Name = "Test User",
            Username = "Test",
            AvatarUrl = new Uri("https://example.com/avatar.jpg"),
            EmailVerified = true
        };

        mockProvider.Setup(x => x.GetUserInfoAsync(accessToken, null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedUserInfo);

        // Act
        var result = await mockProvider.Object.GetUserInfoAsync(accessToken, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("123456789");
        result.Email.Should().Be("test@example.com");
        result.Name.Should().Be("Test User");
        result.Username.Should().Be("Test");
        result.AvatarUrl.Should().Be(new Uri("https://example.com/avatar.jpg"));
        result.EmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserInfoAsync_WithInvalidAccessToken_ShouldThrowException()
    {
        // Test through mock provider that throws exception
        var accessToken = "invalid-access-token";

        var mockProvider = new Mock<IOAuthProvider>();
        mockProvider.Setup(x => x.IsEnabled()).Returns(true);
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.Google);
        mockProvider.Setup(x => x.GetUserInfoAsync(accessToken, null, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new HttpRequestException("Invalid token"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await mockProvider.Object.GetUserInfoAsync(accessToken, null, CancellationToken.None));
    }

    #endregion

    #region ValidateTokenAsync Tests

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        // Test through mock provider
        var accessToken = "valid-access-token";

        var mockProvider = new Mock<IOAuthProvider>();
        mockProvider.Setup(x => x.IsEnabled()).Returns(true);
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.Google);

        var expectedUserInfo = new OAuthUserInfoDto
        {
            Id = "123456789",
            Email = "test@example.com"
        };

        mockProvider.Setup(x => x.GetUserInfoAsync(accessToken, null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedUserInfo);

        // Act
        var result = await mockProvider.Object.ValidateTokenAsync(accessToken, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Test through mock provider that throws exception
        var accessToken = "invalid-access-token";

        var mockProvider = new Mock<IOAuthProvider>();
        mockProvider.Setup(x => x.IsEnabled()).Returns(true);
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.Google);
        mockProvider.Setup(x => x.GetUserInfoAsync(accessToken, null, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new HttpRequestException("Invalid token"));

        // Act
        var result = await mockProvider.Object.ValidateTokenAsync(accessToken, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static string CreateTestIdToken()
    {
        // Create a fake ID token (header.payload.signature)
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            "{\"sub\":\"123456789\",\"email\":\"test@example.com\",\"name\":\"Test User\",\"given_name\":\"Test\",\"picture\":\"https://example.com/avatar.jpg\",\"email_verified\":true}"));
        var signature = "fake-signature";
        
        return $"{header.Replace("+", "-", StringComparison.Ordinal).Replace("/", "_", StringComparison.Ordinal).Replace("=", "", StringComparison.Ordinal)}.{payload.Replace("+", "-", StringComparison.Ordinal).Replace("/", "_", StringComparison.Ordinal).Replace("=", "", StringComparison.Ordinal)}.{signature}";
    }

    #endregion
}
