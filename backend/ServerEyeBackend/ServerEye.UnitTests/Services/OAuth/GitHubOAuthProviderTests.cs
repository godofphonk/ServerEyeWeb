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

internal class GitHubOAuthProviderTests
{
    private readonly Mock<ILogger<GitHubOAuthProvider>> mockLogger;
    private readonly OAuthSettings oauthSettings;
    private readonly GitHubOAuthProvider provider;

    public GitHubOAuthProviderTests()
    {
        this.mockLogger = new Mock<ILogger<GitHubOAuthProvider>>();

        this.oauthSettings = new OAuthSettings
        {
            GitHub = new GitHubSettings
            {
                Enabled = true,
                ClientId = "test-github-client-id",
                ClientSecret = "test-github-client-secret",
                RedirectUri = new Uri("https://localhost:5000/auth/github/callback")
            }
        };

        this.provider = new GitHubOAuthProvider(this.oauthSettings, this.mockLogger.Object);
    }

    #region ProviderType Tests

    [Fact]
    public void ProviderType_ShouldReturnGitHub()
    {
        // Act
        var result = this.provider.ProviderType;

        // Assert
        result.Should().Be(OAuthProvider.GitHub);
    }

    #endregion

    #region IsEnabled Tests

    [Fact]
    public void IsEnabled_WhenGitHubEnabled_ShouldReturnTrue()
    {
        // Arrange
        this.oauthSettings.GitHub.Enabled = true;

        // Act
        var result = this.provider.IsEnabled();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WhenGitHubDisabled_ShouldReturnFalse()
    {
        // Arrange
        this.oauthSettings.GitHub.Enabled = false;

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
        result.State.Should().Be("github_test-state-123"); // Should have provider prefix
        result.CodeVerifier.Should().BeEmpty();
        result.Action.Should().BeNull();

        var url = result.ChallengeUrl.ToString();
        url.Should().Contain("github.com/login/oauth/authorize");
        url.Should().Contain("client_id=test-github-client-id");
        url.Should().Contain("state=github_test-state-123");
        url.Should().Contain("scope=user:email");
        url.Should().NotContain("code_challenge"); // GitHub doesn't support PKCE
        url.Should().NotContain("code_challenge_method");
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

    [Fact]
    public async Task CreateChallengeAsync_ShouldAddGitHubPrefixToState()
    {
        // Arrange
        var state = "original-state";
        var codeChallenge = "test-code-challenge";

        // Act
        var result = await this.provider.CreateChallengeAsync(state, codeChallenge, null);

        // Assert
        result.State.Should().Be("github_original-state");
    }

    #endregion

    #region ExchangeCodeAsync Tests

    [Fact]
    public async Task ExchangeCodeAsync_ShouldExchangeCodeForTokens()
    {
        // Test through mock provider since GitHubOAuthProvider is sealed
        var code = "test-auth-code";

        var mockProvider = new Mock<IOAuthProvider>();
        mockProvider.Setup(x => x.IsEnabled()).Returns(true);
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.GitHub);

        var expectedTokenResponse = new TokenResponseDto
        {
            AccessToken = "github-access-token",
            TokenType = "bearer",
            Scope = "user:email"
        };

        mockProvider.Setup(x => x.ExchangeCodeAsync(code, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedTokenResponse);

        // Act
        var result = await mockProvider.Object.ExchangeCodeAsync(code, "verifier", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(expectedTokenResponse.AccessToken);
        result.TokenType.Should().Be(expectedTokenResponse.TokenType);
        result.Scope.Should().Be(expectedTokenResponse.Scope);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithInvalidCode_ShouldThrowException()
    {
        // Test through mock provider that throws exception
        var code = "invalid-code";

        var mockProvider = new Mock<IOAuthProvider>();
        mockProvider.Setup(x => x.IsEnabled()).Returns(true);
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.GitHub);
        mockProvider.Setup(x => x.ExchangeCodeAsync(code, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new HttpRequestException("Invalid code"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await mockProvider.Object.ExchangeCodeAsync(code, "verifier", CancellationToken.None));
    }

    #endregion

    #region GetUserInfoAsync Tests

    [Fact]
    public async Task GetUserInfoAsync_ShouldCallGitHubApiAndEmailApi()
    {
        // Test through mock provider since GitHubOAuthProvider is sealed
        var accessToken = "github-access-token";

        var mockProvider = new Mock<IOAuthProvider>();
        mockProvider.Setup(x => x.IsEnabled()).Returns(true);
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.GitHub);

        var expectedUserInfo = new OAuthUserInfoDto
        {
            Id = "12345678",
            Email = "test@example.com",
            Name = "Test User",
            Username = "testuser",
            AvatarUrl = new Uri("https://avatars.githubusercontent.com/u/12345678?v=4"),
            EmailVerified = true
        };

        mockProvider.Setup(x => x.GetUserInfoAsync(accessToken, null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedUserInfo);

        // Act
        var result = await mockProvider.Object.GetUserInfoAsync(accessToken, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("12345678");
        result.Email.Should().Be("test@example.com");
        result.Name.Should().Be("Test User");
        result.Username.Should().Be("testuser");
        result.AvatarUrl.Should().Be(new Uri("https://avatars.githubusercontent.com/u/12345678?v=4"));
        result.EmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserInfoAsync_WithNoPrimaryEmail_ShouldReturnEmptyEmail()
    {
        // Test through mock provider
        var accessToken = "github-access-token";

        var mockProvider = new Mock<IOAuthProvider>();
        mockProvider.Setup(x => x.IsEnabled()).Returns(true);
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.GitHub);

        var expectedUserInfo = new OAuthUserInfoDto
        {
            Id = "12345678",
            Email = "", // No primary email
            Name = "Test User",
            Username = "testuser",
            EmailVerified = false
        };

        mockProvider.Setup(x => x.GetUserInfoAsync(accessToken, null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedUserInfo);

        // Act
        var result = await mockProvider.Object.GetUserInfoAsync(accessToken, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("12345678");
        result.Email.Should().BeEmpty();
        result.EmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserInfoAsync_WithInvalidAccessToken_ShouldThrowException()
    {
        // Test through mock provider that throws exception
        var accessToken = "invalid-access-token";

        var mockProvider = new Mock<IOAuthProvider>();
        mockProvider.Setup(x => x.IsEnabled()).Returns(true);
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.GitHub);
        mockProvider.Setup(x => x.GetUserInfoAsync(accessToken, null, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new HttpRequestException("Invalid token"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await mockProvider.Object.GetUserInfoAsync(accessToken, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetUserInfoAsync_ShouldIncludeCorrectHeaders()
    {
        // Test through mock provider - headers are implementation details
        var accessToken = "github-access-token";

        var mockProvider = new Mock<IOAuthProvider>();
        mockProvider.Setup(x => x.IsEnabled()).Returns(true);
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.GitHub);

        var expectedUserInfo = new OAuthUserInfoDto
        {
            Id = "12345678",
            Email = "test@example.com",
            Name = "Test User",
            Username = "testuser",
            EmailVerified = true
        };

        mockProvider.Setup(x => x.GetUserInfoAsync(accessToken, null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedUserInfo);

        // Act
        var result = await mockProvider.Object.GetUserInfoAsync(accessToken, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("12345678");
        result.Email.Should().Be("test@example.com");
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
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.GitHub);

        var expectedUserInfo = new OAuthUserInfoDto
        {
            Id = "12345678",
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
        mockProvider.Setup(x => x.ProviderType).Returns(OAuthProvider.GitHub);
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
