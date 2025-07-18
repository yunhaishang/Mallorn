using CampusTrade.PersonalTests.Fixtures;
using CampusTrade.PersonalTests.Helpers;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.PersonalTests.Tests.Unit;

/// <summary>
/// 用户相关功能的单元测试示例
/// </summary>
public class UserServiceTests : DatabaseTestBase
{
    [Fact]
    public async Task CreateUser_ShouldAddUserToDatabase()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser("newuser", "newuser@test.com");

        // Act
        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FindAsync(testUser.UserId);
        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be("newuser");
        savedUser.Email.Should().Be("newuser@test.com");
    }

    [Fact]
    public async Task GetUserByEmail_ShouldReturnCorrectUser()
    {
        // Arrange
        await TestDataBuilder.SeedTestDataAsync(_context);
        var expectedEmail = "user1@test.com";

        // Act
        var user = _context.Users.FirstOrDefault(u => u.Email == expectedEmail);

        // Assert
        user.Should().NotBeNull();
        user!.Email.Should().Be(expectedEmail);
    }

    [Fact]
    public void ValidateUserPassword_ShouldReturnTrueForCorrectPassword()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        // Act
        var isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);

        // Assert
        isValid.Should().BeTrue();
    }
}
