#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.UnitTests.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;
using ServerEye.Infrastructure;
using ServerEye.Infrastructure.Repositories;
using Xunit;

public class RefreshTokenRepositoryTests : IDisposable
{
    private readonly ServerEyeDbContext context;
    private readonly RefreshTokenRepository repository;

    public RefreshTokenRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ServerEyeDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        this.context = new ServerEyeDbContext(options);
        this.repository = new RefreshTokenRepository(this.context);
    }

    public void Dispose()
    {
        this.context.Database.EnsureDeleted();
        this.context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AddAsync_ShouldAddRefreshToken()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await this.repository.AddAsync(token);

        // Assert
        var savedToken = await this.context.RefreshTokens.FindAsync(token.Id);
        savedToken.Should().NotBeNull();
        savedToken!.Token.Should().Be("test-token");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingToken_ShouldReturnToken()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        this.context.RefreshTokens.Add(token);
        await this.context.SaveChangesAsync();

        // Act
        var result = await this.repository.GetByIdAsync(token.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(token.Id);
        result.Token.Should().Be("test-token");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentToken_ShouldReturnNull()
    {
        // Act
        var result = await this.repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTokenAsync_WithValidToken_ShouldReturnToken()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "valid-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        this.context.RefreshTokens.Add(token);
        await this.context.SaveChangesAsync();

        // Act
        var result = await this.repository.GetByTokenAsync("valid-token");

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be("valid-token");
    }

    [Fact]
    public async Task GetByTokenAsync_WithRevokedToken_ShouldReturnNull()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "revoked-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true,
            CreatedAt = DateTime.UtcNow
        };
        this.context.RefreshTokens.Add(token);
        await this.context.SaveChangesAsync();

        // Act
        var result = await this.repository.GetByTokenAsync("revoked-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTokenAsync_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "expired-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        this.context.RefreshTokens.Add(token);
        await this.context.SaveChangesAsync();

        // Act
        var result = await this.repository.GetByTokenAsync("expired-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTokenAsync_WithEmptyToken_ShouldReturnNull()
    {
        // Act
        var result = await this.repository.GetByTokenAsync(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTokenAsync_WithNullToken_ShouldReturnNull()
    {
        // Act
        var result = await this.repository.GetByTokenAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_WithValidUserId_ShouldReturnLatestToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = "old-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = "new-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        this.context.RefreshTokens.AddRange(oldToken, newToken);
        await this.context.SaveChangesAsync();

        // Act
        var result = await this.repository.GetByUserIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be("new-token");
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNoValidTokens_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await this.repository.GetByUserIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateToken()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "original-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        this.context.RefreshTokens.Add(token);
        await this.context.SaveChangesAsync();

        // Act
        token.IsRevoked = true;
        await this.repository.UpdateAsync(token);

        // Assert
        var updatedToken = await this.context.RefreshTokens.FindAsync(token.Id);
        updatedToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveToken()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "delete-me",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        this.context.RefreshTokens.Add(token);
        await this.context.SaveChangesAsync();

        // Act
        await this.repository.DeleteAsync(token.Id);

        // Assert
        var deletedToken = await this.context.RefreshTokens.FindAsync(token.Id);
        deletedToken.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentToken_ShouldNotThrow()
    {
        // Act & Assert
        await this.repository.Invoking(r => r.DeleteAsync(Guid.NewGuid()))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldRevokeAllUserTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokens = new List<RefreshToken>
        {
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = "token1",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            },
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = "token2",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            }
        };
        this.context.RefreshTokens.AddRange(tokens);
        await this.context.SaveChangesAsync();

        // Act
        await this.repository.RevokeAllUserTokensAsync(userId);

        // Assert
        var revokedTokens = await this.context.RefreshTokens
            .Where(t => t.UserId == userId)
            .ToListAsync();
        revokedTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
    }

    [Fact]
    public async Task RevokeTokenAsync_ShouldRevokeSpecificToken()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "revoke-me",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        this.context.RefreshTokens.Add(token);
        await this.context.SaveChangesAsync();

        // Act
        await this.repository.RevokeTokenAsync(token.Id);

        // Assert
        var revokedToken = await this.context.RefreshTokens.FindAsync(token.Id);
        revokedToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTokens()
    {
        // Arrange
        var tokens = new List<RefreshToken>
        {
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = "token1",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            },
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = "token2",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            }
        };
        this.context.RefreshTokens.AddRange(tokens);
        await this.context.SaveChangesAsync();

        // Act
        var result = await this.repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }
}
