namespace ServerEye.UnitTests.Services;

using ServerEye.Core.Services;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

public class JwtServiceTests
{
    private readonly Mock<ILogger<JwtService>> loggerMock;
    private readonly JwtSettings jwtSettings;
    private readonly JwtService sut;

    public JwtServiceTests()
    {
        this.loggerMock = new Mock<ILogger<JwtService>>();
        
        this.jwtSettings = new JwtSettings
        {
            SecretKey = "TestSecretKey123456789012345678901234567890",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpiration = TimeSpan.FromMinutes(60),
            RefreshTokenExpiration = TimeSpan.FromDays(7)
        };

        var keyPair = System.Security.Cryptography.RSA.Create(2048);
        this.jwtSettings.PrivateKeyBase64 = Convert.ToBase64String(keyPair.ExportRSAPrivateKey());
        this.jwtSettings.PublicKeyBase64 = Convert.ToBase64String(keyPair.ExportRSAPublicKey());

        this.sut = new JwtService(this.jwtSettings);
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidToken()
    {
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var userName = "testuser";
        var role = "USER";

        var user = new ServerEye.Core.Entities.User
        {
            Id = userId,
            Email = email,
            UserName = userName,
            Role = Enum.Parse<ServerEye.Core.Enums.UserRole>(role)
        };
        var token = this.sut.GenerateAccessToken(user);

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_ShouldContainCorrectClaims()
    {
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var userName = "testuser";
        var role = "USER";

        var user = new ServerEye.Core.Entities.User
        {
            Id = userId,
            Email = email,
            UserName = userName,
            Role = Enum.Parse<ServerEye.Core.Enums.UserRole>(role)
        };
        var token = this.sut.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Name && c.Value == userName);
        jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == role);
    }

    [Fact]
    public void GenerateToken_ShouldSetCorrectIssuerAndAudience()
    {
        var userId = Guid.NewGuid();
        var token = this.sut.GenerateToken(userId, "test@example.com", "testuser", "USER", false);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be(this.jwtSettings.Issuer);
        jwtToken.Audiences.Should().Contain(this.jwtSettings.Audience);
    }

    [Fact]
    public void GenerateToken_ShouldSetCorrectExpiration()
    {
        var userId = Guid.NewGuid();
        var beforeGeneration = DateTime.UtcNow;
        
        var token = this.sut.GenerateToken(userId, "test@example.com", "testuser", "USER", false);
        
        var afterGeneration = DateTime.UtcNow;
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedExpiration = beforeGeneration.AddMinutes(this.jwtSettings.ExpirationMinutes);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyString()
    {
        var refreshToken = this.sut.GenerateRefreshToken();

        refreshToken.Should().NotBeNullOrEmpty();
        refreshToken.Length.Should().BeGreaterThan(20);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueTokens()
    {
        var token1 = this.sut.GenerateRefreshToken();
        var token2 = this.sut.GenerateRefreshToken();

        token1.Should().NotBe(token2);
    }

    [Theory]
    [InlineData("USER")]
    [InlineData("ADMIN")]
    [InlineData("MODERATOR")]
    public void GenerateToken_ShouldHandleDifferentRoles(string role)
    {
        var userId = Guid.NewGuid();
        var token = this.sut.GenerateToken(userId, "test@example.com", "testuser", role, false);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == role);
    }

    [Fact]
    public void GenerateToken_WithEmailVerified_ShouldSetCorrectClaim()
    {
        var userId = Guid.NewGuid();
        var token = this.sut.GenerateToken(userId, "test@example.com", "testuser", "USER", true);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "email_verified" && c.Value == "TRUE");
    }

    [Fact]
    public void GenerateToken_WithEmailNotVerified_ShouldSetCorrectClaim()
    {
        var userId = Guid.NewGuid();
        var token = this.sut.GenerateToken(userId, "test@example.com", "testuser", "USER", false);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "email_verified" && c.Value == "FALSE");
    }
}
