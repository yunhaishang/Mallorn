using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CampusTrade.API.Data;

namespace CampusTrade.PersonalTests.Fixtures;

/// <summary>
/// 自定义 Web 应用程序工厂，用于集成测试
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 移除原有的数据库上下文配置
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CampusTradeDbContext>));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 添加内存数据库用于测试
            services.AddDbContext<CampusTradeDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });

            // 构建服务提供者并初始化数据库
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var context = scopedServices.GetRequiredService<CampusTradeDbContext>();
            
            // 确保数据库被创建
            context.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }
}

/// <summary>
/// 数据库测试基类
/// </summary>
public class DatabaseTestBase : IDisposable
{
    protected readonly CampusTradeDbContext _context;
    protected readonly DbContextOptions<CampusTradeDbContext> _options;

    public DatabaseTestBase()
    {
        _options = new DbContextOptionsBuilder<CampusTradeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CampusTradeDbContext(_options);
        _context.Database.EnsureCreated();
    }

    protected CampusTradeDbContext CreateNewContext()
    {
        return new CampusTradeDbContext(_options);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
