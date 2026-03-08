using ServerEye.Core.Enums;

namespace ServerEye.UnitTests.Services;

using ServerEye.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

public class JwtServiceTests
{
    private readonly Mock<ILogger<JwtService>> loggerMock;
    private readonly JwtSettings jwtSettings;
    private readonly JwtService sut;

    public JwtServiceTests()
    {
        this.loggerMock = new Mock<ILogger<JwtService>>();
        var configurationMock1 = new Mock<IConfiguration>();
        
        this.jwtSettings = new JwtSettings
        {
            SecretKey = "TestSecretKey123456789012345678901234567890",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpiration = TimeSpan.FromMinutes(60),
            RefreshTokenExpiration = TimeSpan.FromDays(7)
        };

        using var keyPair = System.Security.Cryptography.RSA.Create(2048);
        this.jwtSettings.PrivateKeyBase64 = Convert.ToBase64String(keyPair.ExportPkcs8PrivateKey());
        this.jwtSettings.PublicKeyBase64 = Convert.ToBase64String(keyPair.ExportSubjectPublicKeyInfo());

        this.sut = new JwtService(this.jwtSettings, configurationMock1.Object);
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidToken()
    {
        var userId = Guid.NewGuid();
        const string email = "test@example.com";
        const string userName = "tester";

        var user = new ServerEye.Core.Entities.User
        {
            Id = userId,
            Email = email,
            UserName = userName,
            Role = ServerEye.Core.Enums.UserRole.User
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
        const string email = "test@example.com";
        const string userName = "tester";
        const UserRole role = ServerEye.Core.Enums.UserRole.User;

        var user = new ServerEye.Core.Entities.User
        {
            Id = userId,
            Email = email,
            UserName = userName,
            Role = role
        };
        var token = this.sut.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Name && c.Value == userName);
        jwtToken.Claims.Should().Contain(c => c.Type == "role" && string.Equals(c.Value, role.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GenerateToken_ShouldSetCorrectIssuerAndAudience()
    {
        var user = new ServerEye.Core.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "tester",
            Role = ServerEye.Core.Enums.UserRole.User,
            IsEmailVerified = false
        };
        var token = this.sut.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be(this.jwtSettings.Issuer);
        jwtToken.Audiences.Should().Contain(this.jwtSettings.Audience);
    }

    [Fact]
    public void GenerateToken_ShouldSetCorrectExpiration()
    {
        var user = new ServerEye.Core.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "tester",
            Role = ServerEye.Core.Enums.UserRole.User,
            IsEmailVerified = false
        };
        var beforeGeneration = DateTime.UtcNow;
        
        var token = this.sut.GenerateAccessToken(user);
        
        var afterGeneration = DateTime.UtcNow;
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedExpiration = beforeGeneration.Add(this.jwtSettings.AccessTokenExpiration);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyString()
    {
        var user = new ServerEye.Core.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "tester",
            Role = ServerEye.Core.Enums.UserRole.User
        };
        var refreshToken = this.sut.GenerateRefreshToken(user);

        refreshToken.Should().NotBeNullOrEmpty();
        refreshToken.Length.Should().BeGreaterThan(20);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueTokens()
    {
        var user = new ServerEye.Core.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "tester",
            Role = ServerEye.Core.Enums.UserRole.User
        };
        var token1 = this.sut.GenerateRefreshToken(user);
        var token2 = this.sut.GenerateRefreshToken(user);

        token1.Should().NotBe(token2);
    }

    [Theory]
    [InlineData(ServerEye.Core.Enums.UserRole.User)]
    [InlineData(ServerEye.Core.Enums.UserRole.Admin)]
    [InlineData(ServerEye.Core.Enums.UserRole.Support)]
    public void GenerateToken_ShouldHandleDifferentRoles(ServerEye.Core.Enums.UserRole role)
    {
        var user = new ServerEye.Core.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "tester",
            Role = role,
            IsEmailVerified = false
        };
        var token = this.sut.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "role" && string.Equals(c.Value, role.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GenerateToken_WithEmailVerified_ShouldSetCorrectClaim()
    {
        var user = new ServerEye.Core.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "tester",
            Role = ServerEye.Core.Enums.UserRole.User,
            IsEmailVerified = true
        };
        var token = this.sut.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "email_verified" && c.Value == "TRUE");
    }

    [Fact]
    public void GenerateToken_WithEmailNotVerified_ShouldSetCorrectClaim()
    {
        var user = new ServerEye.Core.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "tester",
            Role = ServerEye.Core.Enums.UserRole.User,
            IsEmailVerified = false
        };
        var token = this.sut.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "email_verified" && c.Value == "FALSE");
    }
}
