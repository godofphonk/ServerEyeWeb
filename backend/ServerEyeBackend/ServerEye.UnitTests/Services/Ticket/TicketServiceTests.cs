#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.UnitTests.Services.Ticket;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ServerEye.Core.DTOs.Ticket;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using Xunit;
using TicketServiceImpl = ServerEye.Core.Services.TicketService;

public class TicketServiceTests
{
    private readonly Mock<ITicketRepository> mockTicketRepository;
    private readonly Mock<ITicketMessageRepository> mockMessageRepository;
    private readonly Mock<IEmailService> mockEmailService;
    private readonly Mock<INotificationService> mockNotificationService;
    private readonly Mock<INotificationRepository> mockNotificationRepository;
    private readonly Mock<ILogger<TicketServiceImpl>> mockLogger;
    private readonly TicketServiceImpl ticketService;

    public TicketServiceTests()
    {
        this.mockTicketRepository = new Mock<ITicketRepository>();
        this.mockMessageRepository = new Mock<ITicketMessageRepository>();
        this.mockEmailService = new Mock<IEmailService>();
        this.mockNotificationService = new Mock<INotificationService>();
        this.mockNotificationRepository = new Mock<INotificationRepository>();
        this.mockLogger = new Mock<ILogger<TicketServiceImpl>>();

        this.ticketService = new TicketServiceImpl(
            this.mockTicketRepository.Object,
            this.mockMessageRepository.Object,
            this.mockEmailService.Object,
            this.mockNotificationService.Object,
            this.mockNotificationRepository.Object,
            this.mockLogger.Object);
    }

    #region CreateTicketAsync Tests

    [Fact]
    public async Task CreateTicketAsync_WithValidData_ShouldCreateTicket()
    {
        // Arrange
        var dto = new CreateTicketDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            UserId = Guid.NewGuid(),
            Subject = "Test Issue",
            Message = "This is a test message"
        };

        this.mockTicketRepository
            .Setup(x => x.AddAsync(It.IsAny<Core.Entities.Ticket>()))
            .ReturnsAsync((Core.Entities.Ticket t) => t);

        this.mockEmailService
            .Setup(x => x.SendTicketCreatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        this.mockNotificationService
            .Setup(x => x.NotifyAdminsAboutNewTicketAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.ticketService.CreateTicketAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Subject.Should().Be(dto.Subject);
        result.Email.Should().Be(dto.Email);
        result.Status.Should().Be(TicketStatus.New);
        result.Priority.Should().Be(TicketPriority.Medium);
        result.TicketNumber.Should().NotBeNullOrEmpty();

        this.mockTicketRepository.Verify(x => x.AddAsync(It.IsAny<Core.Entities.Ticket>()), Times.Once);
        this.mockEmailService.Verify(x => x.SendTicketCreatedEmailAsync(It.IsAny<string>(), dto.Name, dto.Email, dto.Subject, dto.Message), Times.Once);
        this.mockNotificationService.Verify(x => x.NotifyAdminsAboutNewTicketAsync(It.IsAny<Guid>(), It.IsAny<string>(), dto.Subject), Times.Once);
    }

    [Fact]
    public async Task CreateTicketAsync_WhenEmailFails_ShouldStillCreateTicket()
    {
        // Arrange
        var dto = new CreateTicketDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            Subject = "Test Issue",
            Message = "Test message"
        };

        this.mockTicketRepository
            .Setup(x => x.AddAsync(It.IsAny<Core.Entities.Ticket>()))
            .ReturnsAsync((Core.Entities.Ticket t) => t);

        this.mockEmailService
            .Setup(x => x.SendTicketCreatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email service unavailable"));

        this.mockNotificationService
            .Setup(x => x.NotifyAdminsAboutNewTicketAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.ticketService.CreateTicketAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Subject.Should().Be(dto.Subject);
        this.mockTicketRepository.Verify(x => x.AddAsync(It.IsAny<Core.Entities.Ticket>()), Times.Once);
    }

    #endregion

    #region GetTicketByIdAsync Tests

    [Fact]
    public async Task GetTicketByIdAsync_WithExistingId_ShouldReturnTicket()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticket = new Core.Entities.Ticket
        {
            Id = ticketId,
            TicketNumber = "TKT-001",
            Subject = "Test",
            Email = "test@example.com",
            Name = "Test User",
            Message = "Test message",
            Status = TicketStatus.New,
            Priority = TicketPriority.Medium,
            CreatedAt = DateTime.UtcNow
        };

        this.mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId))
            .ReturnsAsync(ticket);

        // Act
        var result = await this.ticketService.GetTicketByIdAsync(ticketId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(ticketId);
        result.TicketNumber.Should().Be("TKT-001");
    }

    [Fact]
    public async Task GetTicketByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        this.mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId))
            .ReturnsAsync((Core.Entities.Ticket?)null);

        // Act
        var result = await this.ticketService.GetTicketByIdAsync(ticketId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetTicketByNumberAsync Tests

    [Fact]
    public async Task GetTicketByNumberAsync_WithExistingNumber_ShouldReturnTicket()
    {
        // Arrange
        const string ticketNumber = "TKT-001";
        var ticket = new Core.Entities.Ticket
        {
            Id = Guid.NewGuid(),
            TicketNumber = ticketNumber,
            Subject = "Test",
            Email = "test@example.com",
            Name = "Test User",
            Message = "Test message",
            Status = TicketStatus.New,
            Priority = TicketPriority.Medium,
            CreatedAt = DateTime.UtcNow
        };

        this.mockTicketRepository
            .Setup(x => x.GetByTicketNumberAsync(ticketNumber))
            .ReturnsAsync(ticket);

        // Act
        var result = await this.ticketService.GetTicketByNumberAsync(ticketNumber);

        // Assert
        result.Should().NotBeNull();
        result!.TicketNumber.Should().Be(ticketNumber);
    }

    #endregion

    #region GetAllTicketsAsync Tests

    [Fact]
    public async Task GetAllTicketsAsync_ShouldReturnTickets()
    {
        // Arrange
        var tickets = new List<Core.Entities.Ticket>
        {
            new Core.Entities.Ticket { Id = Guid.NewGuid(), TicketNumber = "TKT-001", Subject = "Issue 1", Email = "user1@test.com", Name = "User 1", Message = "Msg 1", Status = TicketStatus.New, Priority = TicketPriority.High, CreatedAt = DateTime.UtcNow },
            new Core.Entities.Ticket { Id = Guid.NewGuid(), TicketNumber = "TKT-002", Subject = "Issue 2", Email = "user2@test.com", Name = "User 2", Message = "Msg 2", Status = TicketStatus.Open, Priority = TicketPriority.Medium, CreatedAt = DateTime.UtcNow }
        };

        this.mockTicketRepository
            .Setup(x => x.GetAllAsync(1, 50))
            .ReturnsAsync(tickets);

        // Act
        var result = await this.ticketService.GetAllTicketsAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].TicketNumber.Should().Be("TKT-001");
        result[1].TicketNumber.Should().Be("TKT-002");
    }

    #endregion

    #region GetTicketsByStatusAsync Tests

    [Fact]
    public async Task GetTicketsByStatusAsync_ShouldReturnFilteredTickets()
    {
        // Arrange
        var tickets = new List<Core.Entities.Ticket>
        {
            new Core.Entities.Ticket { Id = Guid.NewGuid(), TicketNumber = "TKT-001", Subject = "Issue 1", Email = "user1@test.com", Name = "User 1", Message = "Msg 1", Status = TicketStatus.Open, Priority = TicketPriority.High, CreatedAt = DateTime.UtcNow }
        };

        this.mockTicketRepository
            .Setup(x => x.GetByStatusAsync(TicketStatus.Open, 1, 50))
            .ReturnsAsync(tickets);

        // Act
        var result = await this.ticketService.GetTicketsByStatusAsync(TicketStatus.Open);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(TicketStatus.Open);
    }

    #endregion

    #region UpdateTicketStatusAsync Tests

    [Fact]
    public async Task UpdateTicketStatusAsync_WithValidId_ShouldUpdateStatus()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticket = new Core.Entities.Ticket
        {
            Id = ticketId,
            TicketNumber = "TKT-001",
            Subject = "Test",
            Email = "test@example.com",
            Name = "Test User",
            Message = "Test message",
            Status = TicketStatus.New,
            Priority = TicketPriority.Medium,
            CreatedAt = DateTime.UtcNow
        };

        this.mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId))
            .ReturnsAsync(ticket);

        this.mockTicketRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Core.Entities.Ticket>()))
            .Returns(Task.CompletedTask);

        this.mockEmailService
            .Setup(x => x.SendTicketUpdatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.ticketService.UpdateTicketStatusAsync(ticketId, TicketStatus.Open);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(TicketStatus.Open);
        this.mockTicketRepository.Verify(x => x.UpdateAsync(It.Is<Core.Entities.Ticket>(t => t.Status == TicketStatus.Open)), Times.Once);
    }

    [Fact]
    public async Task UpdateTicketStatusAsync_ToResolved_ShouldSetResolvedAt()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticket = new Core.Entities.Ticket
        {
            Id = ticketId,
            TicketNumber = "TKT-001",
            Subject = "Test",
            Email = "test@example.com",
            Name = "Test User",
            Message = "Test message",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Medium,
            CreatedAt = DateTime.UtcNow
        };

        this.mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId))
            .ReturnsAsync(ticket);

        this.mockTicketRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Core.Entities.Ticket>()))
            .Returns(Task.CompletedTask);

        this.mockEmailService
            .Setup(x => x.SendTicketUpdatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.ticketService.UpdateTicketStatusAsync(ticketId, TicketStatus.Resolved);

        // Assert
        result.Status.Should().Be(TicketStatus.Resolved);
        this.mockTicketRepository.Verify(x => x.UpdateAsync(It.Is<Core.Entities.Ticket>(t => t.ResolvedAt != null)), Times.Once);
    }

    [Fact]
    public async Task UpdateTicketStatusAsync_WithNonExistingId_ShouldThrowException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        this.mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId))
            .ReturnsAsync((Core.Entities.Ticket?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.ticketService.UpdateTicketStatusAsync(ticketId, TicketStatus.Open));
    }

    #endregion

    #region AddMessageAsync Tests

    [Fact]
    public async Task AddMessageAsync_WithValidData_ShouldAddMessage()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticket = new Core.Entities.Ticket
        {
            Id = ticketId,
            TicketNumber = "TKT-001",
            Subject = "Test",
            Email = "test@example.com",
            Name = "Test User",
            Message = "Test message",
            Status = TicketStatus.New,
            Priority = TicketPriority.Medium,
            CreatedAt = DateTime.UtcNow
        };

        var messageDto = new AddTicketMessageDto
        {
            Message = "Reply message",
            SenderName = "Support Agent",
            SenderEmail = "support@example.com",
            IsStaffReply = true
        };

        this.mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId))
            .ReturnsAsync(ticket);

        this.mockMessageRepository
            .Setup(x => x.AddAsync(It.IsAny<TicketMessage>()))
            .ReturnsAsync((TicketMessage m) => m);

        this.mockTicketRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Core.Entities.Ticket>()))
            .Returns(Task.CompletedTask);

        this.mockEmailService
            .Setup(x => x.SendTicketMessageEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.ticketService.AddMessageAsync(ticketId, messageDto);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be(messageDto.Message);
        result.SenderName.Should().Be(messageDto.SenderName);
        result.IsStaffReply.Should().BeTrue();

        this.mockMessageRepository.Verify(x => x.AddAsync(It.IsAny<TicketMessage>()), Times.Once);
        this.mockTicketRepository.Verify(x => x.UpdateAsync(It.Is<Core.Entities.Ticket>(t => t.Status == TicketStatus.Open)), Times.Once);
    }

    [Fact]
    public async Task AddMessageAsync_WithNonExistingTicket_ShouldThrowException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var messageDto = new AddTicketMessageDto
        {
            Message = "Test",
            SenderName = "User",
            SenderEmail = "user@test.com",
            IsStaffReply = false
        };

        this.mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId))
            .ReturnsAsync((Core.Entities.Ticket?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.ticketService.AddMessageAsync(ticketId, messageDto));
    }

    #endregion

    #region GetTotalCountAsync Tests

    [Fact]
    public async Task GetTotalCountAsync_ShouldReturnCount()
    {
        // Arrange
        const int expectedCount = 42;

        this.mockTicketRepository
            .Setup(x => x.GetTotalCountAsync())
            .ReturnsAsync(expectedCount);

        // Act
        var result = await this.ticketService.GetTotalCountAsync();

        // Assert
        result.Should().Be(expectedCount);
    }

    #endregion

    #region GetStatusCountsAsync Tests

    [Fact]
    public async Task GetStatusCountsAsync_ShouldReturnCountsForAllStatuses()
    {
        // Arrange
        this.mockTicketRepository
            .Setup(x => x.GetCountByStatusAsync(It.IsAny<TicketStatus>()))
            .ReturnsAsync((TicketStatus status) => status == TicketStatus.New ? 5 : 3);

        // Act
        var result = await this.ticketService.GetStatusCountsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey(TicketStatus.New);
        result.Should().ContainKey(TicketStatus.Open);
        result.Should().ContainKey(TicketStatus.Resolved);
        result.Should().ContainKey(TicketStatus.Closed);
        result[TicketStatus.New].Should().Be(5);
    }

    #endregion

    #region DeleteTicketAsync Tests

    [Fact]
    public async Task DeleteTicketAsync_WithExistingTicket_ShouldDeleteTicketAndRelatedData()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticket = new Core.Entities.Ticket
        {
            Id = ticketId,
            TicketNumber = "TKT-001",
            Subject = "Test",
            Email = "test@example.com",
            Name = "Test User",
            Message = "Test message",
            Status = TicketStatus.Closed,
            Priority = TicketPriority.Low,
            CreatedAt = DateTime.UtcNow
        };

        this.mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId))
            .ReturnsAsync(ticket);

        this.mockMessageRepository
            .Setup(x => x.DeleteByTicketIdAsync(ticketId))
            .Returns(Task.CompletedTask);

        this.mockNotificationRepository
            .Setup(x => x.DeleteByTicketIdAsync(ticketId))
            .Returns(Task.CompletedTask);

        this.mockTicketRepository
            .Setup(x => x.DeleteAsync(ticketId))
            .Returns(Task.CompletedTask);

        // Act
        await this.ticketService.DeleteTicketAsync(ticketId);

        // Assert
        this.mockMessageRepository.Verify(x => x.DeleteByTicketIdAsync(ticketId), Times.Once);
        this.mockNotificationRepository.Verify(x => x.DeleteByTicketIdAsync(ticketId), Times.Once);
        this.mockTicketRepository.Verify(x => x.DeleteAsync(ticketId), Times.Once);
    }

    [Fact]
    public async Task DeleteTicketAsync_WithNonExistingTicket_ShouldThrowException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        this.mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId))
            .ReturnsAsync((Core.Entities.Ticket?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.ticketService.DeleteTicketAsync(ticketId));
    }

    #endregion
}
