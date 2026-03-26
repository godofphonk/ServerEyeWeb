#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.IntegrationTests.Controllers.Ticket;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ServerEye.Core.DTOs.Ticket;
using ServerEye.Core.Interfaces.Repository;
using Xunit;

[Collection("Integration Tests")]
public class TicketsControllerTests : IAsyncLifetime
{
    private readonly TestApplicationFactory factory;
    private readonly HttpClient client;

    public TicketsControllerTests(TestCollectionFixture fixture)
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
    public async Task GetTickets_WithAuth_ShouldReturnUserTickets()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Act
        var response = await this.client.GetAsync("/api/tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tickets = await response.Content.ReadFromJsonAsync<TicketDto[]>();
        tickets.Should().NotBeNull();
        tickets!.Should().BeEmpty(); // New user should have no tickets
    }

    [Fact]
    public async Task CreateTicket_WithValidData_ShouldCreateTicket()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        var createDto = new CreateTicketDto
        {
            Title = "Test Ticket",
            Description = "This is a test ticket for integration testing",
            Priority = TicketPriority.Medium,
            Category = "General"
        };

        // Act
        var response = await this.client.PostAsJsonAsync("/api/tickets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TicketDto>();
        result.Should().NotBeNull();
        result!.Title.Should().Be(createDto.Title);
        result.Description.Should().Be(createDto.Description);
        result.Priority.Should().Be(createDto.Priority);
        result.Status.Should().Be(TicketStatus.Open);
    }

    [Fact]
    public async Task GetTicketById_WithValidTicket_ShouldReturnTicket()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Create a ticket first
        var createDto = new CreateTicketDto
        {
            Title = "Ticket to Get",
            Description = "This ticket will be retrieved",
            Priority = TicketPriority.High,
            Category = "Technical"
        };
        var createResponse = await this.client.PostAsJsonAsync("/api/tickets", createDto);
        var createdTicket = await createResponse.Content.ReadFromJsonAsync<TicketDto>();

        // Act
        var response = await this.client.GetAsync($"/api/tickets/{createdTicket!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ticket = await response.Content.ReadFromJsonAsync<TicketDto>();
        ticket.Should().NotBeNull();
        ticket!.Id.Should().Be(createdTicket.Id);
        ticket.Title.Should().Be(createDto.Title);
    }

    [Fact]
    public async Task ResolveTicket_WithValidTicket_ShouldResolveTicket()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Create a ticket first
        var createDto = new CreateTicketDto
        {
            Title = "Ticket to Resolve",
            Description = "This ticket will be resolved",
            Priority = TicketPriority.Medium
        };
        var createResponse = await this.client.PostAsJsonAsync("/api/tickets", createDto);
        var createdTicket = await createResponse.Content.ReadFromJsonAsync<TicketDto>();

        var resolveDto = new ResolveTicketDto
        {
            Resolution = "Issue has been resolved successfully"
        };

        // Act
        var response = await this.client.PostAsJsonAsync($"/api/tickets/{createdTicket!.Id}/resolve", resolveDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resolvedTicket = await response.Content.ReadFromJsonAsync<TicketDto>();
        resolvedTicket.Should().NotBeNull();
        resolvedTicket!.Status.Should().Be(TicketStatus.Resolved);
        resolvedTicket.Resolution.Should().Be(resolveDto.Resolution);
        resolvedTicket.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AddTicketComment_WithValidData_ShouldCreateComment()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Create a ticket first
        var createDto = new CreateTicketDto
        {
            Title = "Ticket for Comment",
            Description = "This ticket will receive a comment",
            Priority = TicketPriority.Medium
        };
        var createResponse = await this.client.PostAsJsonAsync("/api/tickets", createDto);
        var createdTicket = await createResponse.Content.ReadFromJsonAsync<TicketDto>();

        var commentDto = new CreateTicketCommentDto
        {
            Content = "This is a test comment"
        };

        // Act
        var response = await this.client.PostAsJsonAsync($"/api/tickets/{createdTicket!.Id}/comments", commentDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var comment = await response.Content.ReadFromJsonAsync<TicketCommentDto>();
        comment.Should().NotBeNull();
        comment!.Content.Should().Be(commentDto.Content);
        comment.TicketId.Should().Be(createdTicket.Id);
    }

    [Fact]
    public async Task GetTickets_ByPriority_ShouldFilterCorrectly()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Create tickets with different priorities
        var highPriorityDto = new CreateTicketDto
        {
            Title = "High Priority Ticket",
            Description = "This is high priority",
            Priority = TicketPriority.High
        };
        await this.client.PostAsJsonAsync("/api/tickets", highPriorityDto);

        var lowPriorityDto = new CreateTicketDto
        {
            Title = "Low Priority Ticket", 
            Description = "This is low priority",
            Priority = TicketPriority.Low
        };
        await this.client.PostAsJsonAsync("/api/tickets", lowPriorityDto);

        // Act
        var response = await this.client.GetAsync("/api/tickets?priority=High");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tickets = await response.Content.ReadFromJsonAsync<TicketDto[]>();
        tickets.Should().NotBeNull();
        tickets!.Should().HaveCount(1);
        tickets[0].Priority.Should().Be(TicketPriority.High);
    }

    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private async Task<string> CreateTestUser(string prefix = "ticket")
    {
        var registerDto = new ServerEye.Core.DTOs.UserDto.UserRegisterDto
        {
            UserName = $"{prefix}test_{Guid.NewGuid():N}",
            Email = $"{prefix}_{Guid.NewGuid():N}@example.com",
            Password = "Test123!"
        };

        var response = await this.client.PostAsJsonAsync("/api/users/register", registerDto);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        return result.GetProperty("token").GetString()!;
    }
}
