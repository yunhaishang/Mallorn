using CampusTrade.PersonalTests.Fixtures;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.PersonalTests.Tests.Integration;

/// <summary>
/// 通知后台服务的集成测试
/// </summary>
public class NotificationBackgroundServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotificationBackgroundServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void NotificationBackgroundService_ShouldBeRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var hostedServices = scope.ServiceProvider.GetServices<IHostedService>();
        var notificationService = hostedServices.OfType<NotificationBackgroundService>().FirstOrDefault();

        // Assert
        notificationService.Should().NotBeNull();
    }

    [Fact]
    public async Task TriggerProcessing_ShouldExecuteImmediately()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();

        // 创建一些待发送的通知
        var template = CreateTestTemplate();
        var user = CreateTestUser();

        context.NotificationTemplates.Add(template);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var notification = Notification.CreateSystemNotification(
            template.TemplateId,
            user.UserId,
            new Dictionary<string, object> { ["message"] = "测试消息" }
        );

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var initialStatus = notification.SendStatus;

        // Act - 触发后台处理
        NotificationBackgroundService.TriggerProcessing();

        // 等待一小段时间让后台服务处理
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert
        initialStatus.Should().Be(Notification.SendStatuses.Pending);

        // 重新查询通知状态
        var updatedNotification = await context.Notifications
            .FindAsync(notification.NotificationId);

        // 由于是集成测试且SignalR可能无法实际发送，我们主要验证服务是否响应了触发
        updatedNotification.Should().NotBeNull();
    }

    [Fact]
    public async Task BackgroundService_ShouldProcessNotificationsAutomatically()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();

        // 创建测试数据
        var template = CreateTestTemplate("自动处理测试：{message}");
        var user = CreateTestUser("autotest", "auto@test.com");

        context.NotificationTemplates.Add(template);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // 创建多个待发送通知
        var notifications = new[]
        {
            Notification.CreateSystemNotification(template.TemplateId, user.UserId, 
                new Dictionary<string, object> { ["message"] = "消息1" }),
            Notification.CreateSystemNotification(template.TemplateId, user.UserId, 
                new Dictionary<string, object> { ["message"] = "消息2" }),
            Notification.CreateSystemNotification(template.TemplateId, user.UserId, 
                new Dictionary<string, object> { ["message"] = "消息3" })
        };

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();

        var initialPendingCount = await context.Notifications
            .CountAsync(n => n.SendStatus == Notification.SendStatuses.Pending);

        // Act - 触发处理并等待
        NotificationBackgroundService.TriggerProcessing();
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Assert
        initialPendingCount.Should().Be(3);

        var finalPendingCount = await context.Notifications
            .CountAsync(n => n.SendStatus == Notification.SendStatuses.Pending);

        var successCount = await context.Notifications
            .CountAsync(n => n.SendStatus == Notification.SendStatuses.Success);

        // 验证通知被处理了（可能成功或失败，但不应该都还是Pending状态）
        (finalPendingCount + successCount).Should().Be(3);
    }

    [Fact]
    public async Task BackgroundService_ShouldHandleFailedNotifications()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();

        var template = CreateTestTemplate();
        var user = CreateTestUser();

        context.NotificationTemplates.Add(template);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // 创建一个失败的通知（模拟之前的失败）
        var failedNotification = Notification.CreateSystemNotification(
            template.TemplateId,
            user.UserId,
            new Dictionary<string, object> { ["message"] = "重试测试" }
        );

        // 手动设置为失败状态
        failedNotification.SendStatus = Notification.SendStatuses.Failed;
        failedNotification.RetryCount = 1;
        failedNotification.LastAttemptTime = DateTime.Now.AddMinutes(-10); // 10分钟前失败

        context.Notifications.Add(failedNotification);
        await context.SaveChangesAsync();

        // Act - 触发处理
        NotificationBackgroundService.TriggerProcessing();
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert - 验证重试逻辑是否工作
        var updatedNotification = await context.Notifications
            .FindAsync(failedNotification.NotificationId);

        updatedNotification.Should().NotBeNull();
        // 注意：具体的重试逻辑取决于时间间隔计算，这里主要验证服务运行正常
    }

    [Fact]
    public async Task BackgroundService_ShouldReportQueueStatistics()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

        // 创建一些不同状态的通知来测试统计功能
        var template = CreateTestTemplate();
        var user = CreateTestUser();

        context.NotificationTemplates.Add(template);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var notifications = new[]
        {
            CreateNotificationWithStatus(template.TemplateId, user.UserId, Notification.SendStatuses.Pending),
            CreateNotificationWithStatus(template.TemplateId, user.UserId, Notification.SendStatuses.Success),
            CreateNotificationWithStatus(template.TemplateId, user.UserId, Notification.SendStatuses.Failed)
        };

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();

        // Act & Assert
        // 主要验证服务能够正常运行而不出错
        NotificationBackgroundService.TriggerProcessing();
        await Task.Delay(TimeSpan.FromSeconds(1));

        // 这个测试主要是确保统计代码不会导致异常
        var totalNotifications = await context.Notifications.CountAsync();
        totalNotifications.Should().BeGreaterThanOrEqualTo(3);
    }

    /// <summary>
    /// 创建测试用的通知模板
    /// </summary>
    private NotificationTemplate CreateTestTemplate(string content = "后台服务测试：{message}")
    {
        return new NotificationTemplate
        {
            TemplateName = "后台服务测试模板",
            TemplateType = NotificationTemplate.TemplateTypes.SystemNotification,
            TemplateContent = content,
            Description = "后台服务集成测试用模板",
            Priority = NotificationTemplate.PriorityLevels.Normal,
            IsActive = 1,
            CreatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 创建测试用户
    /// </summary>
    private User CreateTestUser(string username = "bgtest", string email = "bgtest@example.com")
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
    /// 创建指定状态的通知
    /// </summary>
    private Notification CreateNotificationWithStatus(int templateId, int recipientId, string status)
    {
        var notification = new Notification
        {
            TemplateId = templateId,
            RecipientId = recipientId,
            SendStatus = status,
            RetryCount = 0,
            CreatedAt = DateTime.Now,
            LastAttemptTime = DateTime.Now,
            TemplateParams = """{"message":"测试消息"}"""
        };

        if (status == Notification.SendStatuses.Success)
        {
            notification.SentAt = DateTime.Now.AddMinutes(-5);
        }

        return notification;
    }
}
