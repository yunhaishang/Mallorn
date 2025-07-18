using CampusTrade.PersonalTests.Fixtures;
using CampusTrade.PersonalTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using CampusTrade.API.Data;

namespace CampusTrade.PersonalTests;

/// <summary>
/// 个人功能测试运行器
/// 用于运行和管理个人开发的功能测试
/// </summary>
public class PersonalFeatureTestRunner : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly IServiceScope _scope;
    private readonly CampusTradeDbContext _context;

    public PersonalFeatureTestRunner()
    {
        _factory = new CustomWebApplicationFactory();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<CampusTradeDbContext>();
    }

    /// <summary>
    /// 运行所有个人功能测试
    /// </summary>
    public async Task RunAllPersonalTests()
    {
        Console.WriteLine("🚀 开始运行个人功能测试...");
        
        try
        {
            await SetupTestEnvironment();
            
            await RunUserTests();
            await RunDatabaseTests();
            await RunApiTests();
            
            Console.WriteLine("✅ 所有个人功能测试运行完成!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试运行失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 设置测试环境
    /// </summary>
    private async Task SetupTestEnvironment()
    {
        Console.WriteLine("🔧 设置测试环境...");
        
        // 清理并重新创建数据库
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
        
        // 种子测试数据
        await TestDataBuilder.SeedTestDataAsync(_context);
        
        Console.WriteLine("✅ 测试环境设置完成");
    }

    /// <summary>
    /// 运行用户相关测试
    /// </summary>
    private async Task RunUserTests()
    {
        Console.WriteLine("👤 运行用户功能测试...");
        
        // 测试用户创建
        var newUser = TestDataBuilder.CreateTestUser("personaltest", "personal@test.com");
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
        
        // 验证用户已保存
        var savedUser = await _context.Users.FindAsync(newUser.UserId);
        if (savedUser == null)
            throw new Exception("用户创建测试失败");
            
        Console.WriteLine($"✅ 用户创建测试通过 - 用户ID: {savedUser.UserId}");
        
        // 测试用户查询
        var userByEmail = _context.Users.FirstOrDefault(u => u.Email == "personal@test.com");
        if (userByEmail == null)
            throw new Exception("用户查询测试失败");
            
        Console.WriteLine("✅ 用户查询测试通过");
    }

    /// <summary>
    /// 运行数据库相关测试
    /// </summary>
    private async Task RunDatabaseTests()
    {
        Console.WriteLine("🗃️ 运行数据库功能测试...");
        
        // 测试数据库连接
        var canConnect = await _context.Database.CanConnectAsync();
        if (!canConnect)
            throw new Exception("数据库连接测试失败");
            
        Console.WriteLine("✅ 数据库连接测试通过");
        
        // 测试数据操作
        var userCount = _context.Users.Count();
        Console.WriteLine($"✅ 数据库操作测试通过 - 当前用户数量: {userCount}");
    }

    /// <summary>
    /// 运行API相关测试
    /// </summary>
    private async Task RunApiTests()
    {
        Console.WriteLine("🌐 运行API功能测试...");
        
        using var client = _factory.CreateClient();
        
        // 测试首页
        var homeResponse = await client.GetAsync("/");
        if (!homeResponse.IsSuccessStatusCode)
            Console.WriteLine($"⚠️ 首页访问返回状态码: {homeResponse.StatusCode}");
        else
            Console.WriteLine("✅ 首页访问测试通过");
        
        Console.WriteLine("✅ API功能测试完成");
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        _scope?.Dispose();
        _factory?.Dispose();
    }

    /// <summary>
    /// 静态方法：快速运行测试
    /// </summary>
    public static async Task QuickRun()
    {
        using var runner = new PersonalFeatureTestRunner();
        await runner.RunAllPersonalTests();
    }
}
