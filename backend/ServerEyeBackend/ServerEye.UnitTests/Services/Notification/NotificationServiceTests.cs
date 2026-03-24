#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.UnitTests.Services.Notification;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ServerEye.Core.DTOs.Notification;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using Xunit;
using NotificationServiceImpl = ServerEye.Core.Services.NotificationService;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> mockNotificationRepository;
    private readonly Mock<IUserRepository> mockUserRepository;
    private readonly NotificationServiceImpl notificationService;

    public NotificationServiceTests()
    {
        this.mockNotificationRepository = new Mock<INotificationRepository>();
        this.mockUserRepository = new Mock<IUserRepository>();
        
        this.notificationService = new NotificationServiceImpl(
            this.mockNotificationRepository.Object,
            this.mockUserRepository.Object,
            Mock.Of<ILogger<NotificationServiceImpl>>());
    }

    #region GetUserNotificationsAsync Tests

    [Fact]
    public async Task GetUserNotificationsAsync_WithValidUserId_ShouldReturnNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var notifications = new List<Core.Entities.Notification>
        {
            new Core.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.TicketCreated,
                Title = "New Ticket",
                Message = "A new ticket has been created",
                TicketId = ticketId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            },
            new Core.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.NewMessage,
                Title = "New Message",
                Message = "You have a new message",
                TicketId = ticketId,
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        this.mockNotificationRepository
            .Setup(x => x.GetByUserIdAsync(userId, 1, 50))
            .ReturnsAsync(notifications);

        // Act
        var result = await this.notificationService.GetUserNotificationsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Type.Should().Be(NotificationType.TicketCreated);
        result[0].IsRead.Should().BeFalse();
        result[1].Type.Should().Be(NotificationType.NewMessage);
        result[1].IsRead.Should().BeTrue();

        this.mockNotificationRepository.Verify(x => x.GetByUserIdAsync(userId, 1, 50), Times.Once);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_WithCustomPagination_ShouldUseProvidedValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const int page = 2;
        const int pageSize = 25;
        var notifications = new List<Core.Entities.Notification>();

        this.mockNotificationRepository
            .Setup(x => x.GetByUserIdAsync(userId, page, pageSize))
            .ReturnsAsync(notifications);

        // Act
        await this.notificationService.GetUserNotificationsAsync(userId, page, pageSize);

        // Assert
        this.mockNotificationRepository.Verify(x => x.GetByUserIdAsync(userId, page, pageSize), Times.Once);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_WithNoNotifications_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifications = new List<Core.Entities.Notification>();

        this.mockNotificationRepository
            .Setup(x => x.GetByUserIdAsync(userId, 1, 50))
            .ReturnsAsync(notifications);

        // Act
        var result = await this.notificationService.GetUserNotificationsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetUnreadCountAsync Tests

    [Fact]
    public async Task GetUnreadCountAsync_WithValidUserId_ShouldReturnCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const int expectedCount = 5;

        this.mockNotificationRepository
            .Setup(x => x.GetUnreadCountAsync(userId))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await this.notificationService.GetUnreadCountAsync(userId);

        // Assert
        result.Should().Be(expectedCount);
        this.mockNotificationRepository.Verify(x => x.GetUnreadCountAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUnreadCountAsync_WithNoUnreadNotifications_ShouldReturnZero()
    {
        // Arrange
        var userId = Guid.NewGuid();

        this.mockNotificationRepository
            .Setup(x => x.GetUnreadCountAsync(userId))
            .ReturnsAsync(0);

        // Act
        var result = await this.notificationService.GetUnreadCountAsync(userId);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region MarkAsReadAsync Tests

    [Fact]
    public async Task MarkAsReadAsync_WithValidNotificationId_ShouldCallRepository()
    {
        // Arrange
        var notificationId = Guid.NewGuid();

        this.mockNotificationRepository
            .Setup(x => x.MarkAsReadAsync(notificationId))
            .Returns(Task.CompletedTask);

        // Act
        await this.notificationService.MarkAsReadAsync(notificationId);

        // Assert
        this.mockNotificationRepository.Verify(x => x.MarkAsReadAsync(notificationId), Times.Once);
    }

    #endregion

    #region MarkAllAsReadAsync Tests

    [Fact]
    public async Task MarkAllAsReadAsync_WithValidUserId_ShouldCallRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();

        this.mockNotificationRepository
            .Setup(x => x.MarkAllAsReadAsync(userId))
            .Returns(Task.CompletedTask);

        // Act
        await this.notificationService.MarkAllAsReadAsync(userId);

        // Assert
        this.mockNotificationRepository.Verify(x => x.MarkAllAsReadAsync(userId), Times.Once);
    }

    #endregion

    #region CreateNotificationAsync Tests

    [Fact]
    public async Task CreateNotificationAsync_WithValidData_ShouldCreateNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        const NotificationType type = NotificationType.TicketCreated;
        const string title = "Test Notification";
        const string message = "Test Message";

        this.mockNotificationRepository
            .Setup(x => x.AddAsync(It.IsAny<Core.Entities.Notification>()))
            .ReturnsAsync((Core.Entities.Notification n) => n);

        // Act
        await this.notificationService.CreateNotificationAsync(userId, type, title, message, ticketId);

        // Assert
        this.mockNotificationRepository.Verify(
            x => x.AddAsync(It.Is<Core.Entities.Notification>(n =>
                n.UserId == userId &&
                n.Type == type &&
                n.Title == title &&
                n.Message == message &&
                n.TicketId == ticketId &&
                n.IsRead == false &&
                n.Id != Guid.Empty)),
            Times.Once);
    }

    [Fact]
    public async Task CreateNotificationAsync_WithoutTicketId_ShouldCreateNotificationWithNullTicketId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const NotificationType type = NotificationType.TicketAssigned;
        const string title = "Ticket Assigned";
        const string message = "A ticket has been assigned to you";

        this.mockNotificationRepository
            .Setup(x => x.AddAsync(It.IsAny<Core.Entities.Notification>()))
            .ReturnsAsync((Core.Entities.Notification n) => n);

        // Act
        await this.notificationService.CreateNotificationAsync(userId, type, title, message);

        // Assert
        this.mockNotificationRepository.Verify(
            x => x.AddAsync(It.Is<Core.Entities.Notification>(n =>
                n.TicketId == null)),
            Times.Once);
    }

    #endregion

    #region NotifyAdminsAboutNewTicketAsync Tests

    [Fact]
    public async Task NotifyAdminsAboutNewTicketAsync_WithAdminsAndSupport_ShouldNotifyAll()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        const string ticketNumber = "TKT-001";
        const string subject = "Test Ticket";

        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Role = UserRole.Admin, UserName = "admin1", Email = "admin1@test.com", Password = "hash", IsEmailVerified = true, HasPassword = true, CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Role = UserRole.Support, UserName = "support1", Email = "support1@test.com", Password = "hash", IsEmailVerified = true, HasPassword = true, CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Role = UserRole.User, UserName = "user1", Email = "user1@test.com", Password = "hash", IsEmailVerified = true, HasPassword = true, CreatedAt = DateTime.UtcNow }
        };

        this.mockUserRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        this.mockNotificationRepository
            .Setup(x => x.AddAsync(It.IsAny<Core.Entities.Notification>()))
            .ReturnsAsync((Core.Entities.Notification n) => n);

        // Act
        await this.notificationService.NotifyAdminsAboutNewTicketAsync(ticketId, ticketNumber, subject);

        // Assert
        this.mockNotificationRepository.Verify(
            x => x.AddAsync(It.Is<Core.Entities.Notification>(n =>
                n.Type == NotificationType.TicketCreated &&
                n.TicketId == ticketId)),
            Times.Exactly(2)); // Only admin and support, not regular user
    }

    [Fact]
    public async Task NotifyAdminsAboutNewTicketAsync_WithNoAdmins_ShouldNotCreateNotifications()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        const string ticketNumber = "TKT-002";
        const string subject = "Test Ticket";

        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Role = UserRole.User, UserName = "user1", Email = "user1@test.com", Password = "hash", IsEmailVerified = true, HasPassword = true, CreatedAt = DateTime.UtcNow }
        };

        this.mockUserRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        await this.notificationService.NotifyAdminsAboutNewTicketAsync(ticketId, ticketNumber, subject);

        // Assert
        this.mockNotificationRepository.Verify(
            x => x.AddAsync(It.IsAny<Core.Entities.Notification>()),
            Times.Never);
    }

    #endregion

    #region NotifyUserAboutNewMessageAsync Tests

    [Fact]
    public async Task NotifyUserAboutNewMessageAsync_WithValidData_ShouldCreateNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        const string ticketNumber = "TKT-003";
        const string senderName = "John Doe";

        this.mockNotificationRepository
            .Setup(x => x.AddAsync(It.IsAny<Core.Entities.Notification>()))
            .ReturnsAsync((Core.Entities.Notification n) => n);

        // Act
        await this.notificationService.NotifyUserAboutNewMessageAsync(userId, ticketId, ticketNumber, senderName);

        // Assert
        this.mockNotificationRepository.Verify(
            x => x.AddAsync(It.Is<Core.Entities.Notification>(n =>
                n.UserId == userId &&
                n.Type == NotificationType.NewMessage &&
                n.TicketId == ticketId &&
                n.Title.Contains(ticketNumber) &&
                n.Message.Contains(senderName))),
            Times.Once);
    }

    #endregion

    #region NotifyUserAboutStatusChangeAsync Tests

    [Fact]
    public async Task NotifyUserAboutStatusChangeAsync_WithValidData_ShouldCreateNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        const string ticketNumber = "TKT-004";
        const string newStatus = "Resolved";

        this.mockNotificationRepository
            .Setup(x => x.AddAsync(It.IsAny<Core.Entities.Notification>()))
            .ReturnsAsync((Core.Entities.Notification n) => n);

        // Act
        await this.notificationService.NotifyUserAboutStatusChangeAsync(userId, ticketId, ticketNumber, newStatus);

        // Assert
        this.mockNotificationRepository.Verify(
            x => x.AddAsync(It.Is<Core.Entities.Notification>(n =>
                n.UserId == userId &&
                n.Type == NotificationType.TicketStatusChanged &&
                n.TicketId == ticketId &&
                n.Title.Contains(ticketNumber) &&
                n.Message.Contains(newStatus))),
            Times.Once);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task GetUserNotificationsAsync_WithMixedNotificationTypes_ShouldReturnAllTypes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifications = new List<Core.Entities.Notification>
        {
            new Core.Entities.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.TicketCreated, Title = "T1", Message = "M1", IsRead = false, CreatedAt = DateTime.UtcNow },
            new Core.Entities.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.NewMessage, Title = "T2", Message = "M2", IsRead = false, CreatedAt = DateTime.UtcNow },
            new Core.Entities.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.TicketStatusChanged, Title = "T3", Message = "M3", IsRead = true, CreatedAt = DateTime.UtcNow },
            new Core.Entities.Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.TicketAssigned, Title = "T4", Message = "M4", IsRead = false, CreatedAt = DateTime.UtcNow }
        };

        this.mockNotificationRepository
            .Setup(x => x.GetByUserIdAsync(userId, 1, 50))
            .ReturnsAsync(notifications);

        // Act
        var result = await this.notificationService.GetUserNotificationsAsync(userId);

        // Assert
        result.Should().HaveCount(4);
        result.Should().Contain(n => n.Type == NotificationType.TicketCreated);
        result.Should().Contain(n => n.Type == NotificationType.NewMessage);
        result.Should().Contain(n => n.Type == NotificationType.TicketStatusChanged);
        result.Should().Contain(n => n.Type == NotificationType.TicketAssigned);
    }

    [Fact]
    public async Task NotifyAdminsAboutNewTicketAsync_WithMultipleAdmins_ShouldCreateSeparateNotifications()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        const string ticketNumber = "TKT-005";
        const string subject = "Important Ticket";

        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Role = UserRole.Admin, UserName = "admin1", Email = "admin1@test.com", Password = "hash", IsEmailVerified = true, HasPassword = true, CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Role = UserRole.Admin, UserName = "admin2", Email = "admin2@test.com", Password = "hash", IsEmailVerified = true, HasPassword = true, CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Role = UserRole.Support, UserName = "support1", Email = "support1@test.com", Password = "hash", IsEmailVerified = true, HasPassword = true, CreatedAt = DateTime.UtcNow }
        };

        this.mockUserRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        this.mockNotificationRepository
            .Setup(x => x.AddAsync(It.IsAny<Core.Entities.Notification>()))
            .ReturnsAsync((Core.Entities.Notification n) => n);

        // Act
        await this.notificationService.NotifyAdminsAboutNewTicketAsync(ticketId, ticketNumber, subject);

        // Assert
        this.mockNotificationRepository.Verify(
            x => x.AddAsync(It.IsAny<Core.Entities.Notification>()),
            Times.Exactly(3)); // 2 admins + 1 support
    }

    #endregion
}
