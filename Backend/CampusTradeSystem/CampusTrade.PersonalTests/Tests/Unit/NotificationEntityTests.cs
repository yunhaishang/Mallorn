using CampusTrade.PersonalTests.Fixtures;
using CampusTrade.PersonalTests.Helpers;
using CampusTrade.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CampusTrade.PersonalTests.Tests.Unit;

/// <summary>
/// 通知实体业务逻辑的单元测试
/// </summary>
public class NotificationEntityTests : DatabaseTestBase
{
    [Fact]
    public async Task CreateNotification_ShouldSetCorrectDefaults()
    {
        // Arrange
        var template = CreateTestNotificationTemplate();
        var user = TestDataBuilder.CreateTestUser("testuser", "test@example.com");
        
        _context.NotificationTemplates.Add(template);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var parameters = new Dictionary<string, object>
        {
            ["userName"] = user.Username ?? "Unknown",
            ["message"] = "测试消息"
        };

        // Act
        var notification = Notification.CreateSystemNotification(
            template.TemplateId, 
            user.UserId, 
            parameters
        );

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Assert
        notification.Should().NotBeNull();
        notification.SendStatus.Should().Be(Notification.SendStatuses.Pending);
        notification.RetryCount.Should().Be(0);
        notification.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(10));
        notification.TemplateId.Should().Be(template.TemplateId);
        notification.RecipientId.Should().Be(user.UserId);
        notification.OrderId.Should().BeNull(); // 系统通知不关联订单
    }

    [Fact]
    public void SetTemplateParameters_ShouldSerializeToJson()
    {
        // Arrange
        var notification = new Notification();
        var parameters = new Dictionary<string, object>
        {
            ["productName"] = "测试商品",
            ["price"] = 99.99,
            ["isAvailable"] = true
        };

        // Act
        notification.SetTemplateParameters(parameters);

        // Assert
        notification.TemplateParams.Should().NotBeNullOrEmpty();
        
        var deserializedParams = notification.GetTemplateParameters();
        deserializedParams.Should().NotBeNull();
        deserializedParams!["productName"].ToString().Should().Be("测试商品");
    }

    [Fact]
    public void NotificationStatus_BusinessLogicShouldWork()
    {
        // Arrange
        var notification = new Notification();

        // Act & Assert - 初始状态
        notification.IsPending().Should().BeTrue();
        notification.IsSuccessful().Should().BeFalse();
        notification.IsFailed().Should().BeFalse();
        notification.CanRetry().Should().BeTrue();

        // 标记为失败
        notification.MarkAsFailed();
        notification.IsFailed().Should().BeTrue();
        notification.RetryCount.Should().Be(1);

        // 标记为成功
        notification.MarkAsSuccessful();
        notification.IsSuccessful().Should().BeTrue();
        notification.SentAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void NotificationRetry_ShouldRespectMaxRetryLimit()
    {
        // Arrange
        var notification = new Notification();

        // Act - 达到最大重试次数
        for (int i = 0; i < Notification.MaxRetryCount; i++)
        {
            notification.MarkAsFailed();
        }

        // Assert
        notification.HasReachedMaxRetries().Should().BeTrue();
        notification.CanRetry().Should().BeFalse();
        notification.RetryCount.Should().Be(Notification.MaxRetryCount);
    }

    [Fact]
    public void NotificationRetry_ShouldCalculateNextRetryTime()
    {
        // Arrange
        var notification = new Notification
        {
            LastAttemptTime = DateTime.Now.AddMinutes(-10),
            RetryCount = 2
        };

        // Act
        var nextRetryTime = notification.GetNextRetryTime();
        var shouldRetryNow = notification.ShouldRetryNow();

        // Assert
        nextRetryTime.Should().BeAfter(notification.LastAttemptTime);
        // 指数退避：5 * 2^2 = 20分钟后重试
        var expectedTime = notification.LastAttemptTime.AddMinutes(20);
        nextRetryTime.Should().BeCloseTo(expectedTime, TimeSpan.FromMinutes(1));
        
        // 如果距离上次尝试已经超过20分钟，应该可以重试
        shouldRetryNow.Should().BeFalse(); // 因为只过了10分钟
    }

    [Fact]
    public async Task CreateBatchNotifications_ShouldCreateMultipleNotifications()
    {
        // Arrange
        var template = CreateTestNotificationTemplate();
        var users = new[]
        {
            TestDataBuilder.CreateTestUser("user1", "user1@test.com"),
            TestDataBuilder.CreateTestUser("user2", "user2@test.com"),
            TestDataBuilder.CreateTestUser("user3", "user3@test.com")
        };

        _context.NotificationTemplates.Add(template);
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        var userIds = users.Select(u => u.UserId).ToList();
        var parameters = new Dictionary<string, object> { ["message"] = "批量通知测试" };

        // Act
        var notifications = Notification.CreateBatchNotifications(
            template.TemplateId,
            userIds,
            null,
            parameters
        );

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        // Assert
        notifications.Should().HaveCount(3);
        notifications.All(n => n.TemplateId == template.TemplateId).Should().BeTrue();
        notifications.All(n => n.SendStatus == Notification.SendStatuses.Pending).Should().BeTrue();
        
        var savedNotifications = await _context.Notifications.ToListAsync();
        savedNotifications.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetRenderedContent_ShouldRenderTemplateWithParameters()
    {
        // Arrange
        var template = CreateTestNotificationTemplate("您好 {userName}，您的订单 {orderId} 已创建成功！");
        var user = TestDataBuilder.CreateTestUser("张三", "zhangsan@test.com");
        
        _context.NotificationTemplates.Add(template);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var notification = Notification.CreateSystemNotification(
            template.TemplateId,
            user.UserId,
            new Dictionary<string, object>
            {
                ["userName"] = "张三",
                ["orderId"] = "ORD123456"
            }
        );

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // 重新加载以包含导航属性
        var savedNotification = await _context.Notifications
            .Include(n => n.Template)
            .FirstAsync(n => n.NotificationId == notification.NotificationId);

        // Act
        var renderedContent = savedNotification.GetRenderedContent();

        // Assert
        renderedContent.Should().Be("您好 张三，您的订单 ORD123456 已创建成功！");
    }

    [Fact]
    public void ValidateSendStatus_ShouldWorkCorrectly()
    {
        // Arrange & Act & Assert
        Notification.IsValidSendStatus(Notification.SendStatuses.Pending).Should().BeTrue();
        Notification.IsValidSendStatus(Notification.SendStatuses.Success).Should().BeTrue();
        Notification.IsValidSendStatus(Notification.SendStatuses.Failed).Should().BeTrue();
        Notification.IsValidSendStatus("无效状态").Should().BeFalse();

        var availableStatuses = Notification.GetAvailableSendStatuses();
        availableStatuses.Should().Contain(Notification.SendStatuses.Pending);
        availableStatuses.Should().Contain(Notification.SendStatuses.Success);
        availableStatuses.Should().Contain(Notification.SendStatuses.Failed);
        availableStatuses.Should().HaveCount(3);
    }

    /// <summary>
    /// 创建测试用的通知模板
    /// </summary>
    private NotificationTemplate CreateTestNotificationTemplate(string content = "测试通知：{message}")
    {
        return new NotificationTemplate
        {
            TemplateName = "测试模板",
            TemplateType = NotificationTemplate.TemplateTypes.SystemNotification,
            TemplateContent = content,
            Description = "单元测试用模板",
            Priority = NotificationTemplate.PriorityLevels.Normal,
            IsActive = 1,
            CreatedAt = DateTime.Now
        };
    }
}
