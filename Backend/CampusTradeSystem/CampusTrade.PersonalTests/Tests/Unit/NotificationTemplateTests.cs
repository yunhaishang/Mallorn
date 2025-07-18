using CampusTrade.PersonalTests.Fixtures;
using CampusTrade.PersonalTests.Helpers;
using CampusTrade.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.PersonalTests.Tests.Unit;

/// <summary>
/// 通知模板实体业务逻辑的单元测试
/// </summary>
public class NotificationTemplateTests : DatabaseTestBase
{
    [Fact]
    public void CreateSystemTemplate_ShouldSetCorrectProperties()
    {
        // Arrange & Act
        var template = NotificationTemplate.CreateSystemTemplate(
            "系统维护通知",
            "系统将于 {maintenanceTime} 进行维护，预计耗时 {duration} 小时。",
            "系统维护相关通知",
            NotificationTemplate.PriorityLevels.High
        );

        // Assert
        template.Should().NotBeNull();
        template.TemplateName.Should().Be("系统维护通知");
        template.TemplateType.Should().Be(NotificationTemplate.TemplateTypes.SystemNotification);
        template.Priority.Should().Be(NotificationTemplate.PriorityLevels.High);
        template.CreatedBy.Should().BeNull(); // 系统模板无创建者
        template.IsSystemTemplate.Should().BeTrue();
        template.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SetActive_ShouldUpdateStatusAndTime()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            IsActive = 1,
            UpdatedAt = DateTime.Now.AddDays(-1)
        };

        // Act
        template.SetActive(false);

        // Assert
        template.IsActive.Should().Be(0);
        template.IsActiveTemplate.Should().BeFalse();
        template.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateContent_ShouldUpdateContentAndTime()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            TemplateContent = "原始内容",
            UpdatedAt = DateTime.Now.AddDays(-1)
        };

        // Act
        template.UpdateContent("更新后的内容 {param1}");

        // Assert
        template.TemplateContent.Should().Be("更新后的内容 {param1}");
        template.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateContent_WithEmptyContent_ShouldThrowException()
    {
        // Arrange
        var template = new NotificationTemplate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => template.UpdateContent(""));
        Assert.Throws<ArgumentException>(() => template.UpdateContent(null!));
        Assert.Throws<ArgumentException>(() => template.UpdateContent("   "));
    }

    [Fact]
    public void RenderContent_ShouldReplaceParameters()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            TemplateContent = "欢迎 {userName}！您的信用分数是 {creditScore}，当前时间：{currentTime}"
        };

        var parameters = new Dictionary<string, object>
        {
            ["userName"] = "张三",
            ["creditScore"] = 85.5,
            ["currentTime"] = "2024-01-15 10:30:00"
        };

        // Act
        var renderedContent = template.RenderContent(parameters);

        // Assert
        renderedContent.Should().Be("欢迎 张三！您的信用分数是 85.5，当前时间：2024-01-15 10:30:00");
    }

    [Fact]
    public void RenderContent_WithMissingParameters_ShouldKeepPlaceholders()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            TemplateContent = "您好 {userName}，您有 {messageCount} 条新消息，{missingParam} 应该保持原样。"
        };

        var parameters = new Dictionary<string, object>
        {
            ["userName"] = "李四",
            ["messageCount"] = 3
        };

        // Act
        var renderedContent = template.RenderContent(parameters);

        // Assert
        renderedContent.Should().Be("您好 李四，您有 3 条新消息，{missingParam} 应该保持原样。");
    }

    [Fact]
    public void RenderContentFromJson_ShouldDeserializeAndRender()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            TemplateContent = "商品 {productName} 的价格是 ¥{price}，是否有库存：{inStock}"
        };

        var jsonParams = """{"productName":"iPhone 15","price":7999,"inStock":true}""";

        // Act
        var renderedContent = template.RenderContentFromJson(jsonParams);

        // Assert
        renderedContent.Should().Be("商品 iPhone 15 的价格是 ¥7999，是否有库存：True");
    }

    [Fact]
    public void RenderContentFromJson_WithInvalidJson_ShouldReturnOriginalContent()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            TemplateContent = "测试内容 {param1}"
        };

        // Act
        var renderedContent = template.RenderContentFromJson("invalid json");

        // Assert
        renderedContent.Should().Be("测试内容 {param1}");
    }

    [Fact]
    public void GetParameterPlaceholders_ShouldExtractAllParameters()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            TemplateContent = "您好 {userName}，订单 {orderId} 状态已更新为 {status}。请在 {deadline} 前完成 {action}。"
        };

        // Act
        var placeholders = template.GetParameterPlaceholders();

        // Assert
        placeholders.Should().HaveCount(5);
        placeholders.Should().Contain("userName");
        placeholders.Should().Contain("orderId");
        placeholders.Should().Contain("status");
        placeholders.Should().Contain("deadline");
        placeholders.Should().Contain("action");
    }

    [Fact]
    public void GetParameterPlaceholders_WithDuplicates_ShouldReturnUnique()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            TemplateContent = "用户 {userName} 你好，{userName} 的账户余额不足。请 {userName} 及时充值。"
        };

        // Act
        var placeholders = template.GetParameterPlaceholders();

        // Assert
        placeholders.Should().HaveCount(1);
        placeholders.Should().Contain("userName");
    }

    [Fact]
    public void GetPriorityDisplayName_ShouldReturnCorrectNames()
    {
        // Arrange & Act & Assert
        var template = new NotificationTemplate();

        template.Priority = NotificationTemplate.PriorityLevels.Critical;
        template.GetPriorityDisplayName().Should().Be("紧急");

        template.Priority = NotificationTemplate.PriorityLevels.High;
        template.GetPriorityDisplayName().Should().Be("高");

        template.Priority = NotificationTemplate.PriorityLevels.Medium;
        template.GetPriorityDisplayName().Should().Be("中");

        template.Priority = NotificationTemplate.PriorityLevels.Normal;
        template.GetPriorityDisplayName().Should().Be("普通");

        template.Priority = NotificationTemplate.PriorityLevels.Low;
        template.GetPriorityDisplayName().Should().Be("低");

        template.Priority = 999; // 未知优先级
        template.GetPriorityDisplayName().Should().Be("未知");
    }

    [Fact]
    public void GetTemplateTypeDisplayName_ShouldReturnCorrectNames()
    {
        // Arrange
        var template = new NotificationTemplate();

        // Act & Assert
        template.TemplateType = NotificationTemplate.TemplateTypes.ProductRelated;
        template.GetTemplateTypeDisplayName().Should().Be("商品相关");

        template.TemplateType = NotificationTemplate.TemplateTypes.TransactionRelated;
        template.GetTemplateTypeDisplayName().Should().Be("交易相关");

        template.TemplateType = NotificationTemplate.TemplateTypes.ReviewRelated;
        template.GetTemplateTypeDisplayName().Should().Be("评价相关");

        template.TemplateType = NotificationTemplate.TemplateTypes.SystemNotification;
        template.GetTemplateTypeDisplayName().Should().Be("系统通知");

        template.TemplateType = "未知类型";
        template.GetTemplateTypeDisplayName().Should().Be("其他");
    }

    [Fact]
    public void IsValidTemplate_ShouldValidateCorrectly()
    {
        // Arrange
        var validTemplate = new NotificationTemplate
        {
            TemplateName = "有效模板",
            TemplateContent = "这是有效的模板内容 {param1}",
            TemplateType = NotificationTemplate.TemplateTypes.SystemNotification
        };

        var invalidTemplate1 = new NotificationTemplate
        {
            TemplateName = "", // 空名称
            TemplateContent = "内容",
            TemplateType = NotificationTemplate.TemplateTypes.SystemNotification
        };

        var invalidTemplate2 = new NotificationTemplate
        {
            TemplateName = "名称",
            TemplateContent = "", // 空内容
            TemplateType = NotificationTemplate.TemplateTypes.SystemNotification
        };

        // Act & Assert
        validTemplate.IsValidTemplate().Should().BeTrue();
        invalidTemplate1.IsValidTemplate().Should().BeFalse();
        invalidTemplate2.IsValidTemplate().Should().BeFalse();
    }

    [Fact]
    public void IsHighPriority_ShouldIdentifyHighPriorityTemplates()
    {
        // Arrange
        var template = new NotificationTemplate();

        // Act & Assert
        template.Priority = NotificationTemplate.PriorityLevels.Critical;
        template.IsHighPriority.Should().BeTrue();

        template.Priority = NotificationTemplate.PriorityLevels.High;
        template.IsHighPriority.Should().BeTrue();

        template.Priority = NotificationTemplate.PriorityLevels.Medium;
        template.IsHighPriority.Should().BeFalse();

        template.Priority = NotificationTemplate.PriorityLevels.Normal;
        template.IsHighPriority.Should().BeFalse();

        template.Priority = NotificationTemplate.PriorityLevels.Low;
        template.IsHighPriority.Should().BeFalse();
    }

    [Fact]
    public async Task TemplateUsageCount_ShouldReflectNotificationCount()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            TemplateName = "测试模板",
            TemplateType = NotificationTemplate.TemplateTypes.SystemNotification,
            TemplateContent = "测试内容",
            IsActive = 1,
            CreatedAt = DateTime.Now
        };

        var user1 = TestDataBuilder.CreateTestUser("user1", "user1@test.com");
        var user2 = TestDataBuilder.CreateTestUser("user2", "user2@test.com");

        _context.NotificationTemplates.Add(template);
        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        // 创建使用该模板的通知
        var notifications = new[]
        {
            Notification.CreateSystemNotification(template.TemplateId, user1.UserId),
            Notification.CreateSystemNotification(template.TemplateId, user2.UserId)
        };

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        // Act
        var templateWithNotifications = await _context.NotificationTemplates
            .Include(t => t.Notifications)
            .FirstAsync(t => t.TemplateId == template.TemplateId);

        // Assert
        templateWithNotifications.Notifications.Should().HaveCount(2);
    }
}
