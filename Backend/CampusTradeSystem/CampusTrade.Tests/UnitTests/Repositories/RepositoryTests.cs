using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Implementations;
using CampusTrade.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Repositories
{
    [Trait("TestCategory", "Repository")]
    public class RepositoryTests : IDisposable
    {
        private readonly CampusTradeDbContext _context;
        private readonly Repository<User> _userRepository;

        public RepositoryTests()
        {
            // 为每个测试实例创建独立的内存数据库，不自动播种数据
            var databaseName = $"TestDb_{Guid.NewGuid()}";
            _context = TestDbContextFactory.CreateInMemoryDbContext(databaseName, seedData: false);
            _userRepository = new Repository<User>(_context);

            // 初始化测试数据
            SeedTestDataAsync().Wait();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task SeedTestDataAsync()
        {
            // 确保数据库是空的
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();

            var users = new List<User>
            {
                new User
                {
                    UserId = 1,
                    StudentId = "2023001",
                    Email = "test1@example.com",
                    Username = "testuser1",
                    PasswordHash = "hashedpassword1",
                    FullName = "测试用户1",
                    CreditScore = 85.5m,
                    IsActive = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
                new User
                {
                    UserId = 2,
                    StudentId = "2023002",
                    Email = "test2@example.com",
                    Username = "testuser2",
                    PasswordHash = "hashedpassword2",
                    FullName = "测试用户2",
                    CreditScore = 70.0m,
                    IsActive = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
                new User
                {
                    UserId = 3,
                    StudentId = "2023003",
                    Email = "test3@example.com",
                    Username = "testuser3",
                    PasswordHash = "hashedpassword3",
                    FullName = "测试用户3",
                    CreditScore = 60.0m,
                    IsActive = 0, // 非活跃用户
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();
        }

        #region 基础查询测试

        [Fact]
        public async Task GetByPrimaryKeyAsync_ExistingUser_ReturnsUser()
        {
            // Act
            var result = await _userRepository.GetByPrimaryKeyAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
            Assert.Equal("test1@example.com", result.Email);
        }

        [Fact]
        public async Task GetByPrimaryKeyAsync_NonExistingUser_ReturnsNull()
        {
            // Act
            var result = await _userRepository.GetByPrimaryKeyAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            // Act
            var result = await _userRepository.GetAllAsync();

            // Assert
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task FindAsync_ActiveUsersOnly_ReturnsFilteredUsers()
        {
            // Act
            var result = await _userRepository.FindAsync(u => u.IsActive == 1);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.True(result.All(u => u.IsActive == 1));
        }

        [Fact]
        public async Task FirstOrDefaultAsync_ExistingEmail_ReturnsUser()
        {
            // Act
            var result = await _userRepository.FirstOrDefaultAsync(u => u.Email == "test1@example.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test1@example.com", result.Email);
        }

        [Fact]
        public async Task FirstOrDefaultAsync_NonExistingEmail_ReturnsNull()
        {
            // Act
            var result = await _userRepository.FirstOrDefaultAsync(u => u.Email == "nonexistent@example.com");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AnyAsync_ExistingCondition_ReturnsTrue()
        {
            // Act
            var result = await _userRepository.AnyAsync(u => u.CreditScore > 80);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task AnyAsync_NonExistingCondition_ReturnsFalse()
        {
            // Act
            var result = await _userRepository.AnyAsync(u => u.CreditScore > 100);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CountAsync_WithPredicate_ReturnsCorrectCount()
        {
            // Act
            var result = await _userRepository.CountAsync(u => u.IsActive == 1);

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task CountAsync_WithoutPredicate_ReturnsTotal()
        {
            // Act
            var result = await _userRepository.CountAsync();

            // Assert
            Assert.Equal(3, result);
        }

        #endregion

        #region 分页查询测试

        [Fact]
        public async Task GetPagedAsync_FirstPage_ReturnsCorrectPage()
        {
            // Act
            var result = await _userRepository.GetPagedAsync(
                pageNumber: 1,
                pageSize: 2,
                filter: u => u.IsActive == 1,
                orderBy: query => query.OrderBy(u => u.UserId)
            );

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(1, result.Items.First().UserId);
        }

        [Fact]
        public async Task GetPagedAsync_SecondPage_ReturnsCorrectPage()
        {
            // Act
            var result = await _userRepository.GetPagedAsync(
                pageNumber: 2,
                pageSize: 1,
                filter: u => u.IsActive == 1,
                orderBy: query => query.OrderBy(u => u.UserId)
            );

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.Items.Count());
            Assert.Equal(2, result.Items.First().UserId);
        }

        #endregion

        #region 创建和更新测试

        [Fact]
        public async Task AddAsync_NewUser_AddsSuccessfully()
        {
            // Arrange
            var newUser = new User
            {
                StudentId = "2023004",
                Email = "test4@example.com",
                Username = "testuser4",
                PasswordHash = "hashedpassword4",
                FullName = "测试用户4",
                CreditScore = 60.0m,
                IsActive = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            // Act
            var result = await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, await _userRepository.CountAsync());
        }

        [Fact]
        public async Task AddRangeAsync_MultipleUsers_AddsSuccessfully()
        {
            // Arrange
            var newUsers = new List<User>
            {
                new User
                {
                    StudentId = "2023005",
                    Email = "test5@example.com",
                    Username = "testuser5",
                    PasswordHash = "hashedpassword5",
                    FullName = "测试用户5",
                    CreditScore = 60.0m,
                    IsActive = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
                new User
                {
                    StudentId = "2023006",
                    Email = "test6@example.com",
                    Username = "testuser6",
                    PasswordHash = "hashedpassword6",
                    FullName = "测试用户6",
                    CreditScore = 60.0m,
                    IsActive = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                }
            };

            // Act
            await _userRepository.AddRangeAsync(newUsers);
            await _userRepository.SaveChangesAsync();

            // Assert
            Assert.Equal(5, await _userRepository.CountAsync());
        }

        [Fact]
        public async Task Update_ExistingUser_UpdatesSuccessfully()
        {
            // Arrange
            var user = await _userRepository.GetByPrimaryKeyAsync(1);
            Assert.NotNull(user);

            user.FullName = "更新后的用户名";
            user.UpdatedAt = DateTime.UtcNow;

            // Act
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            // Assert
            var updatedUser = await _userRepository.GetByPrimaryKeyAsync(1);
            Assert.Equal("更新后的用户名", updatedUser.FullName);
        }

        #endregion

        #region 删除测试

        [Fact]
        public async Task Delete_ExistingUser_DeletesSuccessfully()
        {
            // Arrange
            var user = await _userRepository.GetByPrimaryKeyAsync(1);
            Assert.NotNull(user);

            // Act
            _userRepository.Delete(user);
            await _userRepository.SaveChangesAsync();

            // Assert
            Assert.Equal(2, await _userRepository.CountAsync());
            Assert.Null(await _userRepository.GetByPrimaryKeyAsync(1));
        }

        [Fact]
        public async Task DeleteByPrimaryKeyAsync_ExistingUser_DeletesSuccessfully()
        {
            // Act
            await _userRepository.DeleteByPrimaryKeyAsync(1);
            await _userRepository.SaveChangesAsync();

            // Assert
            Assert.Equal(2, await _userRepository.CountAsync());
            Assert.Null(await _userRepository.GetByPrimaryKeyAsync(1));
        }

        [Fact]
        public async Task DeleteRange_MultipleUsers_DeletesSuccessfully()
        {
            // Arrange
            var users = await _userRepository.FindAsync(u => u.IsActive == 1);

            // Act
            _userRepository.DeleteRange(users);
            await _userRepository.SaveChangesAsync();

            // Assert
            Assert.Equal(1, await _userRepository.CountAsync());
            Assert.Equal(0, await _userRepository.CountAsync(u => u.IsActive == 1));
        }

        #endregion

        #region 高级查询测试

        [Fact]
        public async Task GetWithIncludeAsync_WithFilter_ReturnsFilteredResults()
        {
            // Act
            var result = await _userRepository.GetWithIncludeAsync(
                filter: u => u.IsActive == 1,
                orderBy: query => query.OrderByDescending(u => u.CreditScore)
            );

            // Assert
            Assert.Equal(2, result.Count());
            Assert.True(result.First().CreditScore >= result.Last().CreditScore);
        }

        #endregion

        #region 批量操作测试

        [Fact]
        public async Task BulkDeleteAsync_WithPredicate_DeletesMatchingRecords()
        {
            // Act
            var deletedCount = await _userRepository.BulkDeleteAsync(u => u.IsActive == 0);
            await _userRepository.SaveChangesAsync();

            // Assert
            Assert.Equal(1, deletedCount);
            Assert.Equal(2, await _userRepository.CountAsync());
        }

        #endregion
    }
}