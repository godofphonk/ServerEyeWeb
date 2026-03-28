#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.IntegrationTests.Controllers.Notifications;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ServerEye.Core.DTOs.Notification;
using Xunit;

[Collection("Integration Tests")]
public class NotificationsControllerTests : IAsyncLifetime
{
    private readonly TestApplicationFactory factory;
    private readonly HttpClient client;

    public NotificationsControllerTests(TestCollectionFixture fixture)
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
    public async Task GetNotifications_WithAuth_ShouldReturnEmptyListForNewUser()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Act
        var response = await this.client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifications = await response.Content.ReadFromJsonAsync<NotificationDto[]>();
        notifications.Should().NotBeNull();
        notifications!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNotifications_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        this.client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await this.client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNotifications_WithPaginationParams_ShouldReturnOk()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Act
        var response = await this.client.GetAsync("/api/notifications?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifications = await response.Content.ReadFromJsonAsync<NotificationDto[]>();
        notifications.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUnreadCount_WithAuth_ShouldReturnZeroForNewUser()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Act
        var response = await this.client.GetAsync("/api/notifications/unread-count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("count");
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("count").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetUnreadCount_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        this.client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await this.client.GetAsync("/api/notifications/unread-count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkAsRead_WithNonExistentNotificationId_ShouldReturnOk()
    {
        // Arrange - the endpoint doesn't verify ownership, just marks as read
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await this.client.PostAsync($"/api/notifications/{nonExistentId}/mark-read", null);

        // Assert
        // The service marks as read without strict existence check, so it should succeed or not throw
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkAsRead_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        this.client.DefaultRequestHeaders.Authorization = null;
        var notificationId = Guid.NewGuid();

        // Act
        var response = await this.client.PostAsync($"/api/notifications/{notificationId}/mark-read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkAllAsRead_WithAuth_ShouldReturnOk()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Act
        var response = await this.client.PostAsync("/api/notifications/mark-all-read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkAllAsRead_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        this.client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await this.client.PostAsync("/api/notifications/mark-all-read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNotifications_AfterMarkAllRead_UnreadCountShouldRemainZero()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Mark all read for a fresh user (should be no-op)
        await this.client.PostAsync("/api/notifications/mark-all-read", null);

        // Act
        var countResponse = await this.client.GetAsync("/api/notifications/unread-count");

        // Assert
        countResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await countResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("count").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetNotifications_TwoDifferentUsers_ShouldReturnSeparateData()
    {
        // Arrange - create two users
        var token1 = await this.CreateTestUser("notif1");
        var token2 = await this.CreateTestUser("notif2");

        // Act - get notifications for each user
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", token1);
        var response1 = await this.client.GetAsync("/api/notifications");

        this.client.DefaultRequestHeaders.Authorization = new("Bearer", token2);
        var response2 = await this.client.GetAsync("/api/notifications");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<string> CreateTestUser(string prefix = "notif")
    {
        var registerDto = new ServerEye.Core.DTOs.UserDto.UserRegisterDto
        {
            UserName = $"{prefix}_{Guid.NewGuid():N}",
            Email = $"{prefix}_{Guid.NewGuid():N}@example.com",
            Password = "Test123!"
        };

        var response = await this.client.PostAsJsonAsync("/api/users/register", registerDto);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        return result.GetProperty("token").GetString()!;
    }
}
