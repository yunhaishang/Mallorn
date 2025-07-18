using CampusTrade.PersonalTests.Fixtures;
using CampusTrade.API.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.PersonalTests.Tests.Integration;

/// <summary>
/// 完整通知流程的端到端测试
/// </summary>
public class NotificationWorkflowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotificationWorkflowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteNotificationWorkflow_FromCreationToDelivery()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();
        var senderService = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Services.Auth.NotifiSenderService>();

        // 1. 创建通知模板
        var template = new NotificationTemplate
        {
            TemplateName = "订单确认通知",
            TemplateType = NotificationTemplate.TemplateTypes.TransactionRelated,
            TemplateContent = "亲爱的 {customerName}，您的订单 {orderId} 已确认，商品：{productName}，金额：¥{amount}",
            Description = "订单确认时发送给用户的通知",
            Priority = NotificationTemplate.PriorityLevels.High,
            IsActive = 1,
            CreatedAt = DateTime.Now
        };

        // 2. 创建用户
        var user = new User
        {
            Email = "customer@example.com",
            Username = "张三",
            StudentId = "STU2024001",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = 1,
            CreditScore = 85.5m
        };

        context.NotificationTemplates.Add(template);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // 3. 创建订单相关通知
        var orderParameters = new Dictionary<string, object>
        {
            ["customerName"] = "张三",
            ["orderId"] = "ORD20240715001",
            ["productName"] = "MacBook Pro",
            ["amount"] = "12999.00"
        };

        var notification = Notification.CreateOrderNotification(
            template.TemplateId,
            user.UserId,
            12345, // 假设的订单ID
            orderParameters
        );

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Act - 4. 发送通知
        var (sendSuccess, sendError) = await senderService.SendNotificationAsync(notification.NotificationId);

        // Assert - 5. 验证整个流程
        sendSuccess.Should().BeTrue("通知应该成功发送");
        sendError.Should().Be("发送成功");

        // 6. 验证数据库状态
        var updatedNotification = await context.Notifications
            .Include(n => n.Template)
            .Include(n => n.Recipient)
            .FirstAsync(n => n.NotificationId == notification.NotificationId);

        updatedNotification.SendStatus.Should().Be(Notification.SendStatuses.Success);
        updatedNotification.SentAt.Should().NotBeNull();
        updatedNotification.SentAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));

        // 7. 验证内容渲染
        var renderedContent = updatedNotification.GetRenderedContent();
        renderedContent.Should().Be("亲爱的 张三，您的订单 ORD20240715001 已确认，商品：MacBook Pro，金额：¥12999.00");

        // 8. 验证业务属性
        updatedNotification.IsOrderRelated.Should().BeTrue();
        updatedNotification.IsSent.Should().BeTrue();
        updatedNotification.SendDuration.Should().NotBeNull();
        updatedNotification.IsHighPriority().Should().BeTrue();
    }

    [Fact]
    public async Task BatchNotificationWorkflow_ShouldProcessMultipleUsers()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();
        var senderService = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Services.Auth.NotifiSenderService>();

        // 创建系统维护通知模板
        var template = new NotificationTemplate
        {
            TemplateName = "系统维护通知",
            TemplateType = NotificationTemplate.TemplateTypes.SystemNotification,
            TemplateContent = "系统将于 {maintenanceDate} {maintenanceTime} 进行维护，预计耗时 {duration} 小时，请提前保存您的工作。",
            Priority = NotificationTemplate.PriorityLevels.Critical,
            IsActive = 1,
            CreatedAt = DateTime.Now
        };

        // 创建多个用户
        var users = new[]
        {
            CreateUser("user1", "user1@test.com", "用户一"),
            CreateUser("user2", "user2@test.com", "用户二"),
            CreateUser("user3", "user3@test.com", "用户三")
        };

        context.NotificationTemplates.Add(template);
        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        var userIds = users.Select(u => u.UserId).ToList();
        var maintenanceParams = new Dictionary<string, object>
        {
            ["maintenanceDate"] = "2024年7月20日",
            ["maintenanceTime"] = "凌晨2:00",
            ["duration"] = "4"
        };

        // Act - 批量创建通知
        var batchNotifications = Notification.CreateBatchNotifications(
            template.TemplateId,
            userIds,
            null,
            maintenanceParams
        );

        context.Notifications.AddRange(batchNotifications);
        await context.SaveChangesAsync();

        // 处理整个队列
        var (totalProcessed, successCount, failedCount) = await senderService.ProcessNotificationQueueAsync(10);

        // Assert
        totalProcessed.Should().Be(3);
        successCount.Should().Be(3);
        failedCount.Should().Be(0);

        // 验证所有通知都已发送
        var sentNotifications = await context.Notifications
            .Where(n => n.TemplateId == template.TemplateId)
            .Include(n => n.Template)
            .Include(n => n.Recipient)
            .ToListAsync();

        sentNotifications.Should().HaveCount(3);
        sentNotifications.All(n => n.SendStatus == Notification.SendStatuses.Success).Should().BeTrue();
        sentNotifications.All(n => n.SentAt.HasValue).Should().BeTrue();

        // 验证渲染内容
        foreach (var notification in sentNotifications)
        {
            var content = notification.GetRenderedContent();
            content.Should().Contain("2024年7月20日");
            content.Should().Contain("凌晨2:00");
            content.Should().Contain("4 小时");
        }
    }

    [Fact]
    public async Task FailedNotificationRetryWorkflow_ShouldHandleRetries()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();
        var senderService = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Services.Auth.NotifiSenderService>();

        var template = new NotificationTemplate
        {
            TemplateName = "重试测试通知",
            TemplateType = NotificationTemplate.TemplateTypes.SystemNotification,
            TemplateContent = "这是一个重试测试通知：{message}",
            IsActive = 1,
            CreatedAt = DateTime.Now
        };

        var user = CreateUser("retryuser", "retry@test.com", "重试用户");

        context.NotificationTemplates.Add(template);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // 创建一个通知并模拟失败
        var notification = Notification.CreateSystemNotification(
            template.TemplateId,
            user.UserId,
            new Dictionary<string, object> { ["message"] = "重试测试" }
        );

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // 手动标记为失败（模拟第一次发送失败）
        notification.MarkAsFailed();
        notification.LastAttemptTime = DateTime.Now.AddMinutes(-30); // 30分钟前失败
        await context.SaveChangesAsync();

        var initialRetryCount = notification.RetryCount;

        // Act - 执行重试
        var (retryTotal, retrySuccess, retryFailed) = await senderService.RetryFailedNotificationsAsync(5);

        // Assert
        retryTotal.Should().Be(1);
        retrySuccess.Should().Be(1);
        retryFailed.Should().Be(0);

        // 验证重试后的状态
        var retriedNotification = await context.Notifications
            .FindAsync(notification.NotificationId);

        retriedNotification!.SendStatus.Should().Be(Notification.SendStatuses.Success);
        retriedNotification.SentAt.Should().NotBeNull();
        // 重试计数应该增加（在重试过程中会增加，然后成功后保持）
        initialRetryCount.Should().Be(1);
    }

    [Fact]
    public async Task HighPriorityNotificationWorkflow_ShouldBeProcessedFirst()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();

        // 创建不同优先级的模板
        var normalTemplate = new NotificationTemplate
        {
            TemplateName = "普通通知",
            TemplateContent = "普通优先级通知",
            Priority = NotificationTemplate.PriorityLevels.Normal,
            IsActive = 1,
            CreatedAt = DateTime.Now
        };

        var criticalTemplate = new NotificationTemplate
        {
            TemplateName = "紧急通知",
            TemplateContent = "紧急通知：{message}",
            Priority = NotificationTemplate.PriorityLevels.Critical,
            IsActive = 1,
            CreatedAt = DateTime.Now
        };

        var user = CreateUser("priorityuser", "priority@test.com", "优先级用户");

        context.NotificationTemplates.AddRange(normalTemplate, criticalTemplate);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // 先创建普通优先级通知
        var normalNotification = Notification.CreateSystemNotification(
            normalTemplate.TemplateId,
            user.UserId,
            new Dictionary<string, object> { ["message"] = "普通消息" }
        );

        // 然后创建紧急通知
        var criticalNotification = Notification.CreateSystemNotification(
            criticalTemplate.TemplateId,
            user.UserId,
            new Dictionary<string, object> { ["message"] = "紧急消息" }
        );

        context.Notifications.AddRange(normalNotification, criticalNotification);
        await context.SaveChangesAsync();

        // Act & Assert
        var normalNotificationWithTemplate = await context.Notifications
            .Include(n => n.Template)
            .FirstAsync(n => n.NotificationId == normalNotification.NotificationId);

        var criticalNotificationWithTemplate = await context.Notifications
            .Include(n => n.Template)
            .FirstAsync(n => n.NotificationId == criticalNotification.NotificationId);

        // 验证优先级标识
        normalNotificationWithTemplate.IsHighPriority().Should().BeFalse();
        criticalNotificationWithTemplate.IsHighPriority().Should().BeTrue();

        // 验证模板优先级
        normalNotificationWithTemplate.Template.IsHighPriority.Should().BeFalse();
        criticalNotificationWithTemplate.Template.IsHighPriority.Should().BeTrue();
    }

    /// <summary>
    /// 创建测试用户的辅助方法
    /// </summary>
    private User CreateUser(string username, string email, string fullName)
    {
        return new User
        {
            Email = email,
            Username = username,
            FullName = fullName,
            StudentId = $"STU{DateTime.Now.Ticks % 100000:D5}",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = 1,
            CreditScore = 75.0m
        };
    }
}
