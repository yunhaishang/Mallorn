using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// Order实体的Repository接口
    /// 继承基础IRepository，提供Order特有的查询和操作方法
    /// </summary>
    public interface IOrderRepository : IRepository<Order>
    {
        // 订单查询相关
        Task<IEnumerable<Order>> GetByBuyerIdAsync(int buyerId);
        Task<IEnumerable<Order>> GetBySellerIdAsync(int sellerId);
        Task<IEnumerable<Order>> GetByProductIdAsync(int productId);

        // 订单数据统计相关
        Task<int> GetTotalOrdersNumberAsync();

        // 扩展订单查询方法
        Task<(IEnumerable<Order> Orders, int TotalCount)> GetPagedOrdersAsync(
            int pageIndex,
            int pageSize,
            string? status = null,
            int? buyerId = null,
            int? sellerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null);

        Task<Order?> GetOrderWithDetailsAsync(int orderId);
        Task<IEnumerable<Order>> GetExpiredOrdersAsync();
        Task<Dictionary<string, int>> GetOrderStatisticsByUserAsync(int userId);
        Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus);
        Task<decimal> GetTotalOrderAmountAsync(string? status = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<(int ProductId, string ProductTitle, int OrderCount)>> GetPopularProductsAsync(int count);
    }
}