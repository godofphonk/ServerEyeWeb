#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.IntegrationTests.Controllers.Ticket;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ServerEye.Core.DTOs.Ticket;
using ServerEye.Core.Enums;
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
    public async Task GetAllTickets_WithAuth_ShouldReturnPaginatedTickets()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Act
        var response = await this.client.GetAsync("/api/tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("tickets");
        content.Should().Contain("pagination");
    }

    [Fact]
    public async Task GetAllTickets_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await this.client.GetAsync("/api/tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTicket_WithValidData_ShouldCreateTicket()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        var createDto = new CreateTicketDto
        {
            Name = "Test User",
            Email = $"ticket_{Guid.NewGuid():N}@example.com",
            Subject = "Integration Test Ticket",
            Message = "This is a test ticket created during integration testing"
        };

        // Act
        var response = await this.client.PostAsJsonAsync("/api/tickets", createDto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TicketResponseDto>();
        result.Should().NotBeNull();
        result!.Subject.Should().Be(createDto.Subject);
        result.Message.Should().Be(createDto.Message);
        result.Name.Should().Be(createDto.Name);
        result.Email.Should().Be(createDto.Email);
        result.Status.Should().Be(TicketStatus.New);
    }

    [Fact]
    public async Task CreateTicket_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var createDto = new CreateTicketDto
        {
            Name = "Test User",
            Email = "noauth@example.com",
            Subject = "Unauthorized Ticket",
            Message = "This ticket should not be created"
        };

        // Act
        var response = await this.client.PostAsJsonAsync("/api/tickets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTicket_WithMissingFields_ShouldReturnBadRequest()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        var invalidDto = new CreateTicketDto
        {
            Name = "",
            Email = "",
            Subject = "",
            Message = ""
        };

        // Act
        var response = await this.client.PostAsJsonAsync("/api/tickets", invalidDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTicketById_WithValidTicket_ShouldReturnTicket()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        var createDto = new CreateTicketDto
        {
            Name = "Test User",
            Email = $"getbyid_{Guid.NewGuid():N}@example.com",
            Subject = "Ticket to Retrieve",
            Message = "This ticket will be retrieved by ID"
        };
        var createResponse = await this.client.PostAsJsonAsync("/api/tickets", createDto);
        var createdTicket = await createResponse.Content.ReadFromJsonAsync<TicketResponseDto>();

        // Act
        var response = await this.client.GetAsync($"/api/tickets/{createdTicket!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponseDto>();
        ticket.Should().NotBeNull();
        ticket!.Id.Should().Be(createdTicket.Id);
        ticket.Subject.Should().Be(createDto.Subject);
    }

    [Fact]
    public async Task GetTicketById_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Act
        var response = await this.client.GetAsync($"/api/tickets/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTicketByNumber_ShouldReturnTicket()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        var createDto = new CreateTicketDto
        {
            Name = "Test User",
            Email = $"bynumber_{Guid.NewGuid():N}@example.com",
            Subject = "Ticket by Number",
            Message = "This ticket will be retrieved by ticket number"
        };
        var createResponse = await this.client.PostAsJsonAsync("/api/tickets", createDto);
        var createdTicket = await createResponse.Content.ReadFromJsonAsync<TicketResponseDto>();

        // Act
        var response = await this.client.GetAsync($"/api/tickets/number/{createdTicket!.TicketNumber}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponseDto>();
        ticket.Should().NotBeNull();
        ticket!.TicketNumber.Should().Be(createdTicket.TicketNumber);
    }

    [Fact]
    public async Task UpdateTicketStatus_WithValidStatus_ShouldUpdateTicket()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        var createDto = new CreateTicketDto
        {
            Name = "Test User",
            Email = $"status_{Guid.NewGuid():N}@example.com",
            Subject = "Ticket to Update Status",
            Message = "This ticket's status will be updated"
        };
        var createResponse = await this.client.PostAsJsonAsync("/api/tickets", createDto);
        var createdTicket = await createResponse.Content.ReadFromJsonAsync<TicketResponseDto>();

        var updateDto = new UpdateTicketStatusDto
        {
            Status = TicketStatus.InProgress
        };

        // Act
        var response = await this.client.PutAsJsonAsync($"/api/tickets/{createdTicket!.Id}/status", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTicket = await response.Content.ReadFromJsonAsync<TicketResponseDto>();
        updatedTicket.Should().NotBeNull();
        updatedTicket!.Status.Should().Be(TicketStatus.InProgress);
    }

    [Fact]
    public async Task UpdateTicketStatus_WithInvalidTicketId_ShouldReturnNotFound()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        var updateDto = new UpdateTicketStatusDto
        {
            Status = TicketStatus.Resolved
        };

        // Act
        var response = await this.client.PutAsJsonAsync($"/api/tickets/{Guid.NewGuid()}/status", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddMessage_WithValidData_ShouldCreateMessage()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        var createDto = new CreateTicketDto
        {
            Name = "Test User",
            Email = $"msg_{Guid.NewGuid():N}@example.com",
            Subject = "Ticket for Message",
            Message = "This ticket will receive a reply message"
        };
        var createResponse = await this.client.PostAsJsonAsync("/api/tickets", createDto);
        var createdTicket = await createResponse.Content.ReadFromJsonAsync<TicketResponseDto>();

        var messageDto = new AddTicketMessageDto
        {
            Message = "This is a reply to the ticket",
            SenderName = "Support Staff",
            SenderEmail = "support@example.com",
            IsStaffReply = true
        };

        // Act
        var response = await this.client.PostAsJsonAsync($"/api/tickets/{createdTicket!.Id}/messages", messageDto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
        var message = await response.Content.ReadFromJsonAsync<TicketMessageDto>();
        message.Should().NotBeNull();
        message!.Message.Should().Be(messageDto.Message);
        message.SenderName.Should().Be(messageDto.SenderName);
        message.IsStaffReply.Should().BeTrue();
    }

    [Fact]
    public async Task AddMessage_WithEmptyMessage_ShouldReturnBadRequest()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        var createDto = new CreateTicketDto
        {
            Name = "Test User",
            Email = $"emptymsg_{Guid.NewGuid():N}@example.com",
            Subject = "Ticket for Empty Message",
            Message = "This ticket will test empty message validation"
        };
        var createResponse = await this.client.PostAsJsonAsync("/api/tickets", createDto);
        var createdTicket = await createResponse.Content.ReadFromJsonAsync<TicketResponseDto>();

        var emptyMessageDto = new AddTicketMessageDto
        {
            Message = "",
            SenderName = "Support Staff",
            SenderEmail = "support@example.com"
        };

        // Act
        var response = await this.client.PostAsJsonAsync($"/api/tickets/{createdTicket!.Id}/messages", emptyMessageDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTicketStats_WithAuth_ShouldReturnStats()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Act
        var response = await this.client.GetAsync("/api/tickets/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("totalCount");
    }

    [Fact]
    public async Task GetTicketsByStatus_WithValidStatus_ShouldReturnTickets()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Create a ticket first
        var createDto = new CreateTicketDto
        {
            Name = "Test User",
            Email = $"statusfilter_{Guid.NewGuid():N}@example.com",
            Subject = "Ticket for Status Filter",
            Message = "This ticket will be filtered by status"
        };
        await this.client.PostAsJsonAsync("/api/tickets", createDto);

        // Act
        var response = await this.client.GetAsync("/api/tickets/status/New");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tickets = await response.Content.ReadFromJsonAsync<TicketListItemDto[]>();
        tickets.Should().NotBeNull();
        tickets!.Should().AllSatisfy(t => t.Status.Should().Be(TicketStatus.New));
    }

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
