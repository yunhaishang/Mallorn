using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Implementations;
using CampusTrade.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Repositories
{
    /// <summary>
    /// Repository集成测试
    /// 测试UnitOfWork和所有Repository的协同工作
    /// </summary>
    public class RepositoryIntegrationTests : IDisposable
    {
        private readonly CampusTradeDbContext _context;
        private readonly UnitOfWork _unitOfWork;

        public RepositoryIntegrationTests()
        {
            _context = TestDbContextFactory.CreateInMemoryDbContext();
            _unitOfWork = new UnitOfWork(_context);
        }

        public void Dispose()
        {
            _unitOfWork.Dispose();
            _context.Dispose();
        }

        [Fact]
        public async Task UnitOfWork_AllRepositories_AreInitializedCorrectly()
        {
            // Act & Assert - 验证所有Repository都能正确初始化
            Assert.NotNull(_unitOfWork.Users);
            Assert.NotNull(_unitOfWork.Products);
            Assert.NotNull(_unitOfWork.Orders);
            Assert.NotNull(_unitOfWork.Categories);
            Assert.NotNull(_unitOfWork.VirtualAccounts);
            Assert.NotNull(_unitOfWork.RechargeRecords);
            Assert.NotNull(_unitOfWork.Reviews);
            Assert.NotNull(_unitOfWork.Negotiations);
            Assert.NotNull(_unitOfWork.ExchangeRequests);
            Assert.NotNull(_unitOfWork.RefreshTokens);
            Assert.NotNull(_unitOfWork.CreditHistory);
            Assert.NotNull(_unitOfWork.Notifications);
            Assert.NotNull(_unitOfWork.Reports);
            Assert.NotNull(_unitOfWork.Admins);
        }

        [Fact]
        public async Task UnitOfWork_TransactionManagement_WorksCorrectly()
        {
            // Arrange
            await SeedBasicDataAsync();

            // Act
            await _unitOfWork.BeginTransactionAsync();
            
            // 在事务中进行多个操作
            var category = new Category { Name = "测试分类" };
            await _unitOfWork.Categories.AddAsync(category);
            
            var product = new Product 
            { 
                Title = "测试商品", 
                Description = "测试描述", 
                BasePrice = 100.0m,
                UserId = 1,
                CategoryId = 1
            };
            await _unitOfWork.Products.AddAsync(product);
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            // Assert
            var savedCategory = await _unitOfWork.Categories.GetCategoryByNameAsync("测试分类");
            var savedProduct = (await _unitOfWork.Products.GetByTitleAsync("测试商品")).Products.FirstOrDefault();
            
            Assert.NotNull(savedCategory);
            Assert.NotNull(savedProduct);
        }

        [Fact]
        public async Task UnitOfWork_CrossRepositoryOperations_WorkCorrectly()
        {
            // Arrange
            await SeedBasicDataAsync();

            // Act - 跨Repository操作：创建用户、虚拟账户、充值记录
            var user = new User 
            { 
                Username = "testuser", 
                Email = "test@test.com", 
                PasswordHash = "hashedpassword",
                StudentId = "TEST001"
            };
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // 创建虚拟账户
            var virtualAccount = new VirtualAccount 
            { 
                UserId = user.UserId, 
                Balance = 100.0m,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.VirtualAccounts.AddAsync(virtualAccount);

            // 创建充值记录
            var rechargeRecord = new RechargeRecord 
            { 
                UserId = user.UserId, 
                Amount = 100.0m, 
                Status = "成功",
                CreateTime = DateTime.UtcNow
            };
            await _unitOfWork.RechargeRecords.AddAsync(rechargeRecord);

            await _unitOfWork.SaveChangesAsync();

            // Assert - 验证跨Repository数据一致性
            var savedUser = await _unitOfWork.Users.GetByEmailAsync("test@test.com");
            var savedAccount = await _unitOfWork.VirtualAccounts.GetByUserIdAsync(user.UserId);
            var (rechargeRecords, count) = await _unitOfWork.RechargeRecords.GetByUserIdAsync(user.UserId);

            Assert.NotNull(savedUser);
            Assert.NotNull(savedAccount);
            Assert.Equal(100.0m, savedAccount.Balance);
            Assert.True(count > 0);
        }

        [Fact]
        public async Task UnitOfWork_ComplexBusinessScenario_WorksCorrectly()
        {
            // Arrange - 复杂业务场景：用户发布商品、其他用户议价、交易完成、评价
            await SeedBasicDataAsync();

            // Act
            // 1. 创建卖家和买家
            var seller = new User { Username = "seller", Email = "seller@test.com", PasswordHash = "hash", StudentId = "SELL001" };
            var buyer = new User { Username = "buyer", Email = "buyer@test.com", PasswordHash = "hash", StudentId = "BUY001" };
            
            await _unitOfWork.Users.AddAsync(seller);
            await _unitOfWork.Users.AddAsync(buyer);
            await _unitOfWork.SaveChangesAsync();

            // 2. 创建商品
            var product = new Product 
            { 
                Title = "测试商品", 
                Description = "测试描述", 
                BasePrice = 100.0m,
                UserId = seller.UserId,
                CategoryId = 1,
                Status = "在售"
            };
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            // 3. 创建抽象订单
            var abstractOrder = new AbstractOrder 
            { 
                OrderType = "normal"
            };
            await _unitOfWork.AbstractOrders.AddAsync(abstractOrder);
            await _unitOfWork.SaveChangesAsync();

            // 4. 创建订单
            var order = new Order 
            { 
                OrderId = abstractOrder.AbstractOrderId, // 使用抽象订单的ID
                BuyerId = buyer.UserId, 
                SellerId = seller.UserId, 
                ProductId = product.ProductId,
                Status = "已完成",
                TotalAmount = 90.0m,
                CreateTime = DateTime.UtcNow
            };
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // 5. 创建议价记录
            var negotiation = new Negotiation 
            { 
                OrderId = order.OrderId, 
                ProposedPrice = 90.0m, 
                Status = "接受",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Negotiations.AddAsync(negotiation);

            // 6. 创建评价
            var review = new Review 
            { 
                OrderId = abstractOrder.AbstractOrderId, // 评价关联到抽象订单
                Rating = 4.5m, 
                Content = "很好的商品",
                CreateTime = DateTime.UtcNow
            };
            await _unitOfWork.Reviews.AddAsync(review);

            // 7. 更新信用记录
            var creditHistory = new CreditHistory 
            { 
                UserId = seller.UserId, 
                ChangeType = "交易完成", 
                NewScore = 85.0m,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.CreditHistory.AddAsync(creditHistory);

            await _unitOfWork.SaveChangesAsync();

            // Assert - 验证整个业务流程
            var savedOrder = await _unitOfWork.Orders.GetOrderWithDetailsAsync(order.OrderId);
            var negotiations = await _unitOfWork.Negotiations.GetByOrderIdAsync(order.OrderId);
            var orderReview = await _unitOfWork.Reviews.GetByOrderIdAsync(order.OrderId);
            var sellerCreditHistory = await _unitOfWork.CreditHistory.GetByUserIdAsync(seller.UserId);

            Assert.NotNull(savedOrder);
            Assert.Equal("已完成", savedOrder.Status);
            Assert.NotEmpty(negotiations);
            Assert.NotNull(orderReview);
            Assert.Equal(4.5m, orderReview.Rating);
            Assert.NotEmpty(sellerCreditHistory);
        }

        [Fact]
        public async Task UnitOfWork_RepositorySpecificMethods_WorkCorrectly()
        {
            // Arrange
            await SeedBasicDataAsync();

            // Act & Assert - 测试各Repository的特定方法
            
            // 1. 测试分类Repository的层级功能
            var rootCategories = await _unitOfWork.Categories.GetRootCategoriesAsync();
            Assert.NotEmpty(rootCategories);

            // 2. 测试虚拟账户Repository的余额操作
            var account = new VirtualAccount { UserId = 1, Balance = 100.0m, CreatedAt = DateTime.UtcNow };
            await _unitOfWork.VirtualAccounts.AddAsync(account);
            await _unitOfWork.SaveChangesAsync();

            var debitResult = await _unitOfWork.VirtualAccounts.DebitAsync(1, 50.0m, "测试扣款");
            Assert.True(debitResult);

            var balance = await _unitOfWork.VirtualAccounts.GetBalanceAsync(1);
            Assert.Equal(50.0m, balance);

            // 3. 测试刷新令牌Repository的令牌管理
            var token = new RefreshToken 
            { 
                Token = "test_token", 
                UserId = 1, 
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IsRevoked = 0,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.RefreshTokens.AddAsync(token);
            await _unitOfWork.SaveChangesAsync();

            var isValid = await _unitOfWork.RefreshTokens.IsTokenValidAsync("test_token");
            Assert.True(isValid);

            var revokeResult = await _unitOfWork.RefreshTokens.RevokeTokenAsync("test_token");
            Assert.True(revokeResult);

            var isValidAfterRevoke = await _unitOfWork.RefreshTokens.IsTokenValidAsync("test_token");
            Assert.False(isValidAfterRevoke);
        }

        private async Task SeedBasicDataAsync()
        {
            // 创建基础分类数据
            var category = new Category { CategoryId = 1, Name = "测试分类", ParentId = null };
            _context.Categories.Add(category);

            // 创建基础学生数据
            var student = new Student { StudentId = "TEST001", Name = "测试学生", Department = "测试学院" };
            _context.Students.Add(student);

            await _context.SaveChangesAsync();
        }
    }
} 