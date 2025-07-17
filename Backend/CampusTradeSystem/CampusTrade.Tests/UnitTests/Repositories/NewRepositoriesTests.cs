using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Implementations;
using CampusTrade.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Repositories
{
    /// <summary>
    /// 新实现的Repository测试集合
    /// </summary>
    public class NewRepositoriesTests : IDisposable
    {
        private readonly CampusTradeDbContext _context;

        public NewRepositoriesTests()
        {
            _context = TestDbContextFactory.CreateInMemoryDbContext();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region CategoriesRepository Tests

        [Fact]
        public async Task CategoriesRepository_GetRootCategories_ReturnsOnlyRootCategories()
        {
            // Arrange
            var repository = new CategoriesRepository(_context);
            await SeedCategoriesAsync();

            // Act
            var result = await repository.GetRootCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.All(result, c => Assert.Null(c.ParentId));
        }

        [Fact]
        public async Task CategoriesRepository_GetCategoryPath_ReturnsCorrectPath()
        {
            // Arrange
            var repository = new CategoriesRepository(_context);
            await SeedCategoriesAsync();

            // Act
            var result = await repository.GetCategoryPathAsync(4); // 计算机类

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count()); // 教材 -> 计算机类
        }

        [Fact]
        public async Task CategoriesRepository_GetCategoryFullName_ReturnsCorrectFullName()
        {
            // Arrange
            var repository = new CategoriesRepository(_context);
            await SeedCategoriesAsync();

            // Act
            var result = await repository.GetCategoryFullNameAsync(4);

            // Assert
            Assert.Contains("教材", result);
            Assert.Contains("计算机类", result);
            Assert.Contains(" > ", result);
        }

        #endregion

        #region VirtualAccountsRepository Tests

        [Fact]
        public async Task VirtualAccountsRepository_GetByUserId_ReturnsCorrectAccount()
        {
            // Arrange
            var repository = new VirtualAccountsRepository(_context);
            await SeedVirtualAccountsAsync();

            // Act
            var result = await repository.GetByUserIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
        }

        [Fact]
        public async Task VirtualAccountsRepository_DebitAsync_WithSufficientBalance_ReturnsTrue()
        {
            // Arrange
            var repository = new VirtualAccountsRepository(_context);
            await SeedVirtualAccountsAsync();

            // Act
            var result = await repository.DebitAsync(1, 50.0m, "测试扣款");

            // Assert
            Assert.True(result);

            // 验证余额变化
            var account = await repository.GetByUserIdAsync(1);
            Assert.Equal(50.0m, account.Balance); // 原来100，扣除50
        }

        [Fact]
        public async Task VirtualAccountsRepository_CreditAsync_IncreasesBalance()
        {
            // Arrange
            var repository = new VirtualAccountsRepository(_context);
            await SeedVirtualAccountsAsync();

            // Act
            var result = await repository.CreditAsync(1, 50.0m, "测试充值");

            // Assert
            Assert.True(result);

            // 验证余额变化
            var account = await repository.GetByUserIdAsync(1);
            Assert.Equal(150.0m, account.Balance); // 原来100，增加50
        }

        [Fact]
        public async Task VirtualAccountsRepository_TransferAsync_TransfersCorrectly()
        {
            // Arrange
            var repository = new VirtualAccountsRepository(_context);
            await SeedVirtualAccountsAsync();

            // Act
            var result = await repository.TransferAsync(1, 2, 30.0m, "测试转账");

            // Assert
            Assert.True(result);

            // 验证转账结果
            var fromAccount = await repository.GetByUserIdAsync(1);
            var toAccount = await repository.GetByUserIdAsync(2);
            Assert.Equal(70.0m, fromAccount.Balance); // 100 - 30
            Assert.Equal(80.0m, toAccount.Balance);   // 50 + 30
        }



        #endregion

        #region RechargeRecordsRepository Tests

        [Fact]
        public async Task RechargeRecordsRepository_GetByUserId_ReturnsUserRecords()
        {
            // Arrange
            var repository = new RechargeRecordsRepository(_context);
            await SeedRechargeRecordsAsync();

            // Act
            var (records, totalCount) = await repository.GetByUserIdAsync(1);

            // Assert
            Assert.NotNull(records);
            Assert.True(totalCount > 0);
            Assert.All(records, r => Assert.Equal(1, r.UserId));
        }

        [Fact]
        public async Task RechargeRecordsRepository_UpdateRechargeStatus_UpdatesSuccessfully()
        {
            // Arrange
            var repository = new RechargeRecordsRepository(_context);
            await SeedRechargeRecordsAsync();

            // Act
            var result = await repository.UpdateRechargeStatusAsync(1, "成功");

            // Assert
            Assert.True(result);

            // 验证状态更新
            var record = await repository.GetByPrimaryKeyAsync(1);
            Assert.Equal("成功", record.Status);
            Assert.NotNull(record.CompleteTime);
        }

        [Fact]
        public async Task RechargeRecordsRepository_GetTotalRechargeAmount_ReturnsCorrectAmount()
        {
            // Arrange
            var repository = new RechargeRecordsRepository(_context);
            await SeedRechargeRecordsAsync();

            // Act
            var result = await repository.GetTotalRechargeAmountAsync();

            // Assert
            Assert.True(result > 0);
        }

        #endregion

        #region ReviewsRepository Tests

        [Fact]
        public async Task ReviewsRepository_GetByOrderId_ReturnsCorrectReview()
        {
            // Arrange
            var repository = new ReviewsRepository(_context);
            await SeedReviewsAsync();

            // Act
            var result = await repository.GetByOrderIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.OrderId);
        }

        [Fact]
        public async Task ReviewsRepository_GetAverageRatingByUser_ReturnsCorrectAverage()
        {
            // Arrange
            var repository = new ReviewsRepository(_context);
            await SeedReviewsAsync();

            // Act
            var result = await repository.GetAverageRatingByUserAsync(2); // 卖家ID

            // Assert
            Assert.True(result > 0);
            Assert.True(result <= 5);
        }

        [Fact]
        public async Task ReviewsRepository_AddSellerReply_AddsReplySuccessfully()
        {
            // Arrange
            var repository = new ReviewsRepository(_context);
            await SeedReviewsAsync();

            // Act
            var result = await repository.AddSellerReplyAsync(1, "感谢您的评价！");

            // Assert
            Assert.True(result);

            // 验证回复添加
            var review = await repository.GetByPrimaryKeyAsync(1);
            Assert.Equal("感谢您的评价！", review.SellerReply);
        }

        #endregion

        #region NegotiationsRepository Tests

        [Fact]
        public async Task NegotiationsRepository_GetByOrderId_ReturnsOrderNegotiations()
        {
            // Arrange
            var repository = new NegotiationsRepository(_context);
            await SeedNegotiationsAsync();

            // Act
            var result = await repository.GetByOrderIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.All(result, n => Assert.Equal(1, n.OrderId));
        }

        [Fact]
        public async Task NegotiationsRepository_AcceptNegotiation_UpdatesStatus()
        {
            // Arrange
            var repository = new NegotiationsRepository(_context);
            await SeedNegotiationsAsync();

            // Act
            var result = await repository.AcceptNegotiationAsync(1);

            // Assert
            Assert.True(result);

            // 验证状态更新
            var negotiation = await repository.GetByPrimaryKeyAsync(1);
            Assert.Equal("接受", negotiation.Status);
        }

        [Fact]
        public async Task NegotiationsRepository_HasActiveNegotiation_ReturnsCorrectStatus()
        {
            // Arrange
            var repository = new NegotiationsRepository(_context);
            await SeedNegotiationsAsync();

            // Act
            var result = await repository.HasActiveNegotiationAsync(1);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region ExchangeRequestsRepository Tests

        [Fact]
        public async Task ExchangeRequestsRepository_GetByOfferProductId_ReturnsCorrectRequests()
        {
            // Arrange
            var repository = new ExchangeRequestsRepository(_context);
            await SeedExchangeRequestsAsync();

            // Act
            var result = await repository.GetByOfferProductIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.All(result, e => Assert.Equal(1, e.OfferProductId));
        }

        [Fact]
        public async Task ExchangeRequestsRepository_AcceptExchange_UpdatesStatus()
        {
            // Arrange
            var repository = new ExchangeRequestsRepository(_context);
            await SeedExchangeRequestsAsync();

            // Act
            var result = await repository.AcceptExchangeAsync(1);

            // Assert
            Assert.True(result);

            // 验证状态更新
            var exchange = await repository.GetByPrimaryKeyAsync(1);
            Assert.Equal("接受", exchange.Status);
        }

        [Fact]
        public async Task ExchangeRequestsRepository_FindMatchingExchanges_ReturnsMatches()
        {
            // Arrange
            var repository = new ExchangeRequestsRepository(_context);
            await SeedExchangeRequestsAsync();

            // Act
            var result = await repository.FindMatchingExchangesAsync(1);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region RefreshTokenRepository Tests

        [Fact]
        public async Task RefreshTokenRepository_GetByToken_ReturnsCorrectToken()
        {
            // Arrange
            var repository = new RefreshTokenRepository(_context);
            await SeedRefreshTokensAsync();

            // Act
            var result = await repository.GetByTokenAsync("test_token_1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test_token_1", result.Token);
        }

        [Fact]
        public async Task RefreshTokenRepository_RevokeToken_RevokesSuccessfully()
        {
            // Arrange
            var repository = new RefreshTokenRepository(_context);
            await SeedRefreshTokensAsync();

            // Act
            var result = await repository.RevokeTokenAsync("test_token_1", "测试撤销");

            // Assert
            Assert.True(result);

            // 验证撤销状态
            var token = await repository.GetByTokenAsync("test_token_1");
            Assert.Equal(1, token.IsRevoked);
            Assert.Equal("测试撤销", token.RevokeReason);
        }

        [Fact]
        public async Task RefreshTokenRepository_IsTokenValid_ReturnsCorrectStatus()
        {
            // Arrange
            var repository = new RefreshTokenRepository(_context);
            await SeedRefreshTokensAsync();

            // Act
            var result = await repository.IsTokenValidAsync("test_token_1");

            // Assert
            Assert.True(result);
        }

        #endregion

        #region CreditHistoryRepository Tests

        [Fact]
        public async Task CreditHistoryRepository_GetByUserId_ReturnsUserHistory()
        {
            // Arrange
            var repository = new CreditHistoryRepository(_context);
            await SeedCreditHistoryAsync();

            // Act
            var result = await repository.GetByUserIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.All(result, c => Assert.Equal(1, c.UserId));
        }

        [Fact]
        public async Task CreditHistoryRepository_GetByChangeType_ReturnsCorrectRecords()
        {
            // Arrange
            var repository = new CreditHistoryRepository(_context);
            await SeedCreditHistoryAsync();

            // Act
            var result = await repository.GetByChangeTypeAsync("交易完成");

            // Assert
            Assert.NotNull(result);
            Assert.All(result, c => Assert.Equal("交易完成", c.ChangeType));
        }

        [Fact]
        public async Task CreditHistoryRepository_GetTotalCreditChange_ReturnsCorrectTotal()
        {
            // Arrange
            var repository = new CreditHistoryRepository(_context);
            await SeedCreditHistoryAsync();

            // Act
            var result = await repository.GetTotalCreditChangeAsync(1);

            // Assert
            Assert.True(result > 0);
        }

        #endregion

        #region 数据种子方法

        private async Task SeedCategoriesAsync()
        {
            var categories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "教材", ParentId = null },
                new Category { CategoryId = 2, Name = "数码", ParentId = null },
                new Category { CategoryId = 3, Name = "日用", ParentId = null },
                new Category { CategoryId = 4, Name = "计算机类", ParentId = 1 },
                new Category { CategoryId = 5, Name = "数学类", ParentId = 1 }
            };

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();
        }

        private async Task SeedVirtualAccountsAsync()
        {
            var accounts = new List<VirtualAccount>
            {
                new VirtualAccount { AccountId = 1, UserId = 1, Balance = 100.0m, CreatedAt = DateTime.UtcNow },
                new VirtualAccount { AccountId = 2, UserId = 2, Balance = 50.0m, CreatedAt = DateTime.UtcNow }
            };

            _context.VirtualAccounts.AddRange(accounts);
            await _context.SaveChangesAsync();
        }

        private async Task SeedRechargeRecordsAsync()
        {
            var records = new List<RechargeRecord>
            {
                new RechargeRecord { RechargeId = 1, UserId = 1, Amount = 100.0m, Status = "处理中", CreateTime = DateTime.UtcNow },
                new RechargeRecord { RechargeId = 2, UserId = 1, Amount = 50.0m, Status = "成功", CreateTime = DateTime.UtcNow.AddDays(-1) }
            };

            _context.RechargeRecords.AddRange(records);
            await _context.SaveChangesAsync();
        }

        private async Task SeedReviewsAsync()
        {
            // 先创建必要的相关数据
            var abstractOrder = new AbstractOrder { AbstractOrderId = 1, OrderType = "normal" };
            var order = new Order { OrderId = 1, BuyerId = 1, SellerId = 2, ProductId = 1, Status = "已完成", CreateTime = DateTime.UtcNow };
            var review = new Review { ReviewId = 1, OrderId = 1, Rating = 4.5m, Content = "很好的商品", CreateTime = DateTime.UtcNow };

            _context.AbstractOrders.Add(abstractOrder);
            _context.Orders.Add(order);
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
        }

        private async Task SeedNegotiationsAsync()
        {
            var negotiations = new List<Negotiation>
            {
                new Negotiation { NegotiationId = 1, OrderId = 1, ProposedPrice = 80.0m, Status = "等待回应", CreatedAt = DateTime.UtcNow },
                new Negotiation { NegotiationId = 2, OrderId = 1, ProposedPrice = 85.0m, Status = "反报价", CreatedAt = DateTime.UtcNow.AddHours(-1) }
            };

            _context.Negotiations.AddRange(negotiations);
            await _context.SaveChangesAsync();
        }

        private async Task SeedExchangeRequestsAsync()
        {
            var exchanges = new List<ExchangeRequest>
            {
                new ExchangeRequest { ExchangeId = 1, OfferProductId = 1, RequestProductId = 2, Status = "等待回应", CreatedAt = DateTime.UtcNow },
                new ExchangeRequest { ExchangeId = 2, OfferProductId = 2, RequestProductId = 1, Status = "等待回应", CreatedAt = DateTime.UtcNow.AddHours(-1) }
            };

            _context.ExchangeRequests.AddRange(exchanges);
            await _context.SaveChangesAsync();
        }

        private async Task SeedRefreshTokensAsync()
        {
            var tokens = new List<RefreshToken>
            {
                new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = "test_token_1",
                    UserId = 1,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    IsRevoked = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = "test_token_2",
                    UserId = 2,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    IsRevoked = 0,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.RefreshTokens.AddRange(tokens);
            await _context.SaveChangesAsync();
        }

        private async Task SeedCreditHistoryAsync()
        {
            var histories = new List<CreditHistory>
            {
                new CreditHistory { LogId = 1, UserId = 1, ChangeType = "交易完成", NewScore = 85.0m, CreatedAt = DateTime.UtcNow },
                new CreditHistory { LogId = 2, UserId = 1, ChangeType = "好评奖励", NewScore = 87.0m, CreatedAt = DateTime.UtcNow.AddDays(-1) }
            };

            _context.CreditHistories.AddRange(histories);
            await _context.SaveChangesAsync();
        }

        #endregion
    }
}