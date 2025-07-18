using CampusTrade.PersonalTests.Fixtures;
using CampusTrade.PersonalTests.Helpers;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using CampusTrade.API.Hubs;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CampusTrade.PersonalTests.Tests.Integration;

/// <summary>
/// 通知发送服务的集成测试
/// </summary>
public class NotificationSenderIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationSenderIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public void NotificationSenderService_ShouldBeRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var senderService = scope.ServiceProvider.GetService<NotifiSenderService>();

        // Assert
        senderService.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessNotificationQueue_ShouldHandleEmptyQueue()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var senderService = scope.ServiceProvider.GetRequiredService<NotifiSenderService>();

        // Act
        var (total, success, failed) = await senderService.ProcessNotificationQueueAsync(10);

        // Assert
        total.Should().Be(0);
        success.Should().Be(0);
        failed.Should().Be(0);
    }

    [Fact]
    public async Task GetQueueStats_ShouldReturnCorrectStatistics()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();
        var senderService = scope.ServiceProvider.GetRequiredService<NotifiSenderService>();

        // 创建测试数据
        var template = CreateTestTemplate();
        var users = new[]
        {
            CreateTestUser("user1", "user1@test.com"),
            CreateTestUser("user2", "user2@test.com")
        };

        context.NotificationTemplates.Add(template);
        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        // 创建不同状态的通知
        var notifications = new[]
        {
            CreateTestNotification(template.TemplateId, users[0].UserId, Notification.SendStatuses.Pending),
            CreateTestNotification(template.TemplateId, users[1].UserId, Notification.SendStatuses.Success),
            CreateTestNotification(template.TemplateId, users[0].UserId, Notification.SendStatuses.Failed)
        };

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();

        // Act
        var (pending, success, failed, total) = await senderService.GetQueueStatsAsync();

        // Assert
        pending.Should().Be(1);
        success.Should().Be(1);
        failed.Should().Be(1);
        total.Should().Be(3);
    }

    [Fact]
    public async Task SendNotification_WithValidNotification_ShouldUpdateStatus()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();
        var senderService = scope.ServiceProvider.GetRequiredService<NotifiSenderService>();

        // 创建测试数据
        var template = CreateTestTemplate("欢迎 {userName}！");
        var user = CreateTestUser("testuser", "test@example.com");

        context.NotificationTemplates.Add(template);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var notification = Notification.CreateSystemNotification(
            template.TemplateId,
            user.UserId,
            new Dictionary<string, object> { ["userName"] = "testuser" }
        );

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Act
        var (success, errorMessage) = await senderService.SendNotificationAsync(notification.NotificationId);

        // Assert
        success.Should().BeTrue();
        errorMessage.Should().Be("发送成功");

        // 验证数据库状态已更新
        var updatedNotification = await context.Notifications
            .FirstAsync(n => n.NotificationId == notification.NotificationId);
        
        updatedNotification.SendStatus.Should().Be(Notification.SendStatuses.Success);
        updatedNotification.SentAt.Should().NotBeNull();
        updatedNotification.SentAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task SendNotification_WithNonExistentNotification_ShouldReturnError()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var senderService = scope.ServiceProvider.GetRequiredService<NotifiSenderService>();

        // Act
        var (success, errorMessage) = await senderService.SendNotificationAsync(99999);

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().Be("通知不存在");
    }

    [Fact]
    public async Task SendNotification_WithAlreadySentNotification_ShouldReturnSuccess()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();
        var senderService = scope.ServiceProvider.GetRequiredService<NotifiSenderService>();

        // 创建已发送的通知
        var template = CreateTestTemplate();
        var user = CreateTestUser("testuser", "test@example.com");

        context.NotificationTemplates.Add(template);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var notification = CreateTestNotification(
            template.TemplateId,
            user.UserId,
            Notification.SendStatuses.Success
        );
        notification.SentAt = DateTime.Now.AddMinutes(-5);

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Act
        var (success, errorMessage) = await senderService.SendNotificationAsync(notification.NotificationId);

        // Assert
        success.Should().BeTrue();
        errorMessage.Should().Be("通知已发送");
    }

    [Fact]
    public async Task RetryFailedNotifications_ShouldOnlyRetryEligibleNotifications()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();
        var senderService = scope.ServiceProvider.GetRequiredService<NotifiSenderService>();

        var template = CreateTestTemplate();
        var user = CreateTestUser("testuser", "test@example.com");

        context.NotificationTemplates.Add(template);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // 创建不同重试状态的失败通知
        var recentFailedNotification = CreateTestNotification(
            template.TemplateId,
            user.UserId,
            Notification.SendStatuses.Failed
        );
        recentFailedNotification.LastAttemptTime = DateTime.Now.AddMinutes(-1); // 刚刚失败，不应重试
        recentFailedNotification.RetryCount = 1;

        var eligibleFailedNotification = CreateTestNotification(
            template.TemplateId,
            user.UserId,
            Notification.SendStatuses.Failed
        );
        eligibleFailedNotification.LastAttemptTime = DateTime.Now.AddMinutes(-30); // 30分钟前失败，应该重试
        eligibleFailedNotification.RetryCount = 1;

        var maxRetriedNotification = CreateTestNotification(
            template.TemplateId,
            user.UserId,
            Notification.SendStatuses.Failed
        );
        maxRetriedNotification.RetryCount = Notification.MaxRetryCount; // 已达最大重试次数

        context.Notifications.AddRange(
            recentFailedNotification,
            eligibleFailedNotification,
            maxRetriedNotification
        );
        await context.SaveChangesAsync();

        // Act
        var (totalRetry, successRetry, failedRetry) = await senderService.RetryFailedNotificationsAsync(10);

        // Assert
        totalRetry.Should().Be(1); // 只有一个符合重试条件
        successRetry.Should().Be(1); // 应该成功发送
        failedRetry.Should().Be(0);
    }

    [Fact]
    public void NotificationHub_ShouldBeRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var hubContext = scope.ServiceProvider.GetService<IHubContext<NotificationHub>>();

        // Assert
        hubContext.Should().NotBeNull();
    }

    /// <summary>
    /// 创建测试用的通知模板
    /// </summary>
    private NotificationTemplate CreateTestTemplate(string content = "测试通知：{message}")
    {
        return new NotificationTemplate
        {
            TemplateName = "集成测试模板",
            TemplateType = NotificationTemplate.TemplateTypes.SystemNotification,
            TemplateContent = content,
            Description = "集成测试用模板",
            Priority = NotificationTemplate.PriorityLevels.Normal,
            IsActive = 1,
            CreatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 创建测试用户
    /// </summary>
    private User CreateTestUser(string username, string email)
    {
        return new User
        {
            Email = email,
            Username = username,
            StudentId = $"STU{DateTime.Now.Ticks % 100000:D5}",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = 1,
            CreditScore = 60.0m
        };
    }

    /// <summary>
    /// 创建测试通知
    /// </summary>
    private Notification CreateTestNotification(int templateId, int recipientId, string status)
    {
        return new Notification
        {
            TemplateId = templateId,
            RecipientId = recipientId,
            SendStatus = status,
            RetryCount = 0,
            CreatedAt = DateTime.Now,
            LastAttemptTime = DateTime.Now,
            TemplateParams = """{"message":"测试消息"}"""
        };
    }
}
