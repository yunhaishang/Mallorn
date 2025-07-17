using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 评价管理Repository接口
    /// 提供订单评价、统计分析等功能
    /// </summary>
    public interface IReviewsRepository : IRepository<Review>
    {
        // 评价查询
        Task<Review?> GetByOrderIdAsync(int orderId);
        Task<IEnumerable<Review>> GetByUserIdAsync(int userId);
        Task<(IEnumerable<Review> Reviews, int TotalCount)> GetPagedReviewsAsync(
            int pageIndex, int pageSize, int? userId = null, int? productId = null);

        // 评价统计
        Task<decimal> GetAverageRatingByUserAsync(int userId);
        Task<decimal> GetAverageRatingByProductAsync(int productId);
        Task<Dictionary<int, int>> GetRatingDistributionByUserAsync(int userId);
        Task<int> GetReviewCountByUserAsync(int userId);

        // 商品评价相关
        Task<IEnumerable<Review>> GetReviewsByProductIdAsync(int productId);
        Task<decimal> GetProductAverageRatingAsync(int productId);
        Task<Dictionary<string, decimal>> GetProductDetailedRatingsAsync(int productId);

        // 匿名评价管理
        Task<IEnumerable<Review>> GetAnonymousReviewsAsync();
        Task<int> GetAnonymousReviewCountAsync();

        // 评价回复管理
        Task<IEnumerable<Review>> GetReviewsWithRepliesAsync();
        Task<IEnumerable<Review>> GetReviewsWithoutRepliesAsync(int sellerId);
        Task<bool> AddSellerReplyAsync(int reviewId, string reply);

        // 评价质量分析
        Task<IEnumerable<Review>> GetHighRatingReviewsAsync(decimal minRating = 4.0m);
        Task<IEnumerable<Review>> GetLowRatingReviewsAsync(decimal maxRating = 2.0m);
        Task<IEnumerable<Review>> GetRecentReviewsAsync(int days = 7);
    }
} 