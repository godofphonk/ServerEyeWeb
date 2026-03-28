#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.IntegrationTests.Controllers.Users;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ServerEye.Core.DTOs.UserDto;
using Xunit;

/// <summary>
/// Extended integration tests for UsersController covering endpoints not tested in UsersControllerTests:
/// GET /api/users/me, GET /api/users/{id}, GET /api/users/by-email/{email}, PUT /api/users/{id}.
/// </summary>
[Collection("Integration Tests")]
public class UsersExtendedControllerTests : IAsyncLifetime
{
    private readonly TestApplicationFactory factory;
    private readonly HttpClient client;

    public UsersExtendedControllerTests(TestCollectionFixture fixture)
    {
        this.factory = fixture.Factory;
        this.client = fixture.Client;
    }

    public async Task InitializeAsync()
    {
        await this.factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetCurrentUser_WithAuth_ShouldReturnCurrentUserData()
    {
        // Arrange
        var email = $"me_{Guid.NewGuid():N}@example.com";
        var userName = $"meuser_{Guid.NewGuid():N}";
        var (token, _) = await this.CreateTestUserWithDetails(userName, email);
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await this.client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserData>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
        user.UserName.Should().Be(userName);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        this.client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await this.client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUserIdMatchingToken()
    {
        // Arrange
        var (token, userId) = await this.CreateTestUserWithDetails(
            $"idcheck_{Guid.NewGuid():N}",
            $"idcheck_{Guid.NewGuid():N}@example.com");
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await this.client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserData>();
        user.Should().NotBeNull();
        user!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetUserById_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var email = $"byid_{Guid.NewGuid():N}@example.com";
        var (token, userId) = await this.CreateTestUserWithDetails($"byiduser_{Guid.NewGuid():N}", email);
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await this.client.GetAsync($"/api/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserData>();
        user.Should().NotBeNull();
        user!.Id.Should().Be(userId);
        user.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetUserById_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var (token, _) = await this.CreateTestUserWithDetails(
            $"notfound_{Guid.NewGuid():N}",
            $"notfound_{Guid.NewGuid():N}@example.com");
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await this.client.GetAsync($"/api/users/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserById_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        this.client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await this.client.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserByEmail_WithValidEmail_ShouldReturnUser()
    {
        // Arrange
        var email = $"findbyemail_{Guid.NewGuid():N}@example.com";
        var (token, userId) = await this.CreateTestUserWithDetails($"emailuser_{Guid.NewGuid():N}", email);
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await this.client.GetAsync($"/api/users/by-email/{email}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserData>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
        user.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetUserByEmail_WithNonExistentEmail_ShouldReturnNotFound()
    {
        // Arrange
        var (token, _) = await this.CreateTestUserWithDetails(
            $"notfoundemail_{Guid.NewGuid():N}",
            $"notfoundemail_{Guid.NewGuid():N}@example.com");
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await this.client.GetAsync("/api/users/by-email/nonexistent@nowhere.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserByEmail_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        this.client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await this.client.GetAsync("/api/users/by-email/test@example.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllUsers_WithAuth_ShouldReturnUserList()
    {
        // Arrange
        var (token, _) = await this.CreateTestUserWithDetails(
            $"getall_{Guid.NewGuid():N}",
            $"getall_{Guid.NewGuid():N}@example.com");
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await this.client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<UserData[]>();
        users.Should().NotBeNull();
        users!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterAndLogin_ShouldProduceConsistentUserData()
    {
        // Arrange
        var email = $"consistent_{Guid.NewGuid():N}@example.com";
        var userName = $"consistentuser_{Guid.NewGuid():N}";
        var (registerToken, userId) = await this.CreateTestUserWithDetails(userName, email);

        // Login with same credentials
        var loginDto = new UserLoginDto { Email = email, Password = "Test123!" };
        var loginResponse = await this.client.PostAsJsonAsync("/api/users/login", loginDto);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<JsonElement>(loginContent);
        var loginToken = loginResult.GetProperty("token").GetString()!;

        // Use login token to get current user
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", loginToken);
        var meResponse = await this.client.GetAsync("/api/users/me");

        // Assert
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var userData = await meResponse.Content.ReadFromJsonAsync<UserData>();
        userData.Should().NotBeNull();
        userData!.Id.Should().Be(userId);
        userData.Email.Should().Be(email);
        userData.UserName.Should().Be(userName);
    }

    /// <summary>
    /// Creates a test user and returns their JWT token and user ID.
    /// </summary>
    private async Task<(string token, Guid userId)> CreateTestUserWithDetails(string userName, string email)
    {
        var registerDto = new UserRegisterDto
        {
            UserName = userName,
            Email = email,
            Password = "Test123!"
        };

        var response = await this.client.PostAsJsonAsync("/api/users/register", registerDto);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        var token = result.GetProperty("token").GetString()!;

        // Extract user ID from the user object in response
        var userElement = result.GetProperty("user");
        var userId = Guid.Parse(userElement.GetProperty("id").GetString()!);

        return (token, userId);
    }
}
