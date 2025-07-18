using CampusTrade.PersonalTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace CampusTrade.PersonalTests;

/// <summary>
/// 专门用于通知功能测试的运行器
/// </summary>
public class NotificationTestRunner : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotificationTestRunner(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void RunAllNotificationTests()
    {
        // 此测试作为通知功能测试的入口点
        // 验证所有通知相关的组件都已正确配置

        // 1. 验证依赖注入配置
        using var scope = _factory.Services.CreateScope();
        
        // 检查数据库上下文
        var dbContext = scope.ServiceProvider.GetService<CampusTrade.API.Data.CampusTradeDbContext>();
        dbContext.Should().NotBeNull("数据库上下文应该已注册");

        // 检查通知发送服务
        var notifiSender = scope.ServiceProvider.GetService<CampusTrade.API.Services.Auth.NotifiSenderService>();
        notifiSender.Should().NotBeNull("通知发送服务应该已注册");

        // 检查后台服务
        var hostedServices = scope.ServiceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
        var notificationBackgroundService = hostedServices
            .OfType<CampusTrade.API.Services.Background.NotificationBackgroundService>()
            .FirstOrDefault();
        notificationBackgroundService.Should().NotBeNull("通知后台服务应该已注册");

        // 检查SignalR Hub
        var hubContext = scope.ServiceProvider.GetService<Microsoft.AspNetCore.SignalR.IHubContext<CampusTrade.API.Hubs.NotificationHub>>();
        hubContext.Should().NotBeNull("NotificationHub应该已注册");

        Console.WriteLine("✅ 所有通知系统组件都已正确注册和配置");
        Console.WriteLine("✅ 通知功能测试环境准备完成");
    }
}
