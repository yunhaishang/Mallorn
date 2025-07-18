using Oracle.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CampusTrade.API.Data;
using Microsoft.Extensions.Configuration;

namespace CampusTrade.PersonalTests.Tests.Integration;

/// <summary>
/// 数据库连接和操作的集成测试
/// </summary>
public class DatabaseIntegrationTests
{
    private readonly IConfiguration _configuration;

    public DatabaseIntegrationTests()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Testing.json", optional: true)
            .AddEnvironmentVariables();
        
        _configuration = builder.Build();
    }

    [Fact]
    public void CanCreateInMemoryDatabase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CampusTradeDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

        // Act & Assert
        using var context = new CampusTradeDbContext(options);
        context.Database.EnsureCreated().Should().BeTrue();
    }

    [Fact]
    public async Task CanPerformBasicDatabaseOperations()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CampusTradeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new CampusTradeDbContext(options);
        context.Database.EnsureCreated();

        // Act
        var user = new CampusTrade.API.Models.Entities.User
        {
            Email = "test@example.com",
            Username = "testuser",
            StudentId = "STU12345",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = 1 // 1表示激活
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Assert
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be("testuser");
    }

    /// <summary>
    /// 测试Oracle数据库连接（仅在有真实数据库连接字符串时运行）
    /// </summary>
    [Fact(Skip = "需要真实的Oracle数据库连接")]
    public async Task CanConnectToOracleDatabase()
    {
        // Arrange
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // 跳过测试如果没有连接字符串
            return;
        }

        var options = new DbContextOptionsBuilder<CampusTradeDbContext>()
            .UseOracle(connectionString)
            .Options;

        // Act & Assert
        using var context = new CampusTradeDbContext(options);
        var canConnect = await context.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }
}
