using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.PersonalTests.Helpers;

/// <summary>
/// 测试数据构建器
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// 创建测试用户
    /// </summary>
    public static User CreateTestUser(string username = "testuser", string email = "test@example.com")
    {
        return new User
        {
            Email = email,
            Username = username,
            StudentId = $"STU{DateTime.Now.Ticks % 100000:D5}", // 生成唯一的学生ID
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = 1, // 1表示激活
            CreditScore = 60.0m,
            // 根据您的User实体添加其他必要属性
        };
    }

    /// <summary>
    /// 种子测试数据到数据库
    /// </summary>
    public static async Task SeedTestDataAsync(CampusTradeDbContext context)
    {
        // 清理现有数据
        context.RemoveRange(context.Users);
        await context.SaveChangesAsync();

        // 添加测试用户
        var testUsers = new[]
        {
            CreateTestUser("user1", "user1@test.com"),
            CreateTestUser("user2", "user2@test.com"),
            CreateTestUser("admin", "admin@test.com")
        };

        context.Users.AddRange(testUsers);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// 创建JWT令牌用于测试
    /// </summary>
    public static string CreateTestJwtToken(string userId = "test-user-id", string role = "User")
    {
        // 这里应该使用与您的API相同的JWT配置
        // 根据您的JwtOptions和token生成逻辑进行调整
        return "test-jwt-token";
    }
}
