using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 换物请求管理Repository接口
    /// 提供物品交换、匹配等功能
    /// </summary>
    public interface IExchangeRequestsRepository : IRepository<ExchangeRequest>
    {
        // 交换请求查询
        Task<IEnumerable<ExchangeRequest>> GetByOfferProductIdAsync(int productId);
        Task<IEnumerable<ExchangeRequest>> GetByRequestProductIdAsync(int productId);
        Task<IEnumerable<ExchangeRequest>> GetByUserIdAsync(int userId);
        Task<(IEnumerable<ExchangeRequest> Requests, int TotalCount)> GetPagedRequestsAsync(
            int pageIndex, int pageSize, string? status = null);

        // 状态管理
        Task<bool> UpdateExchangeStatusAsync(int exchangeId, string status);
        Task<IEnumerable<ExchangeRequest>> GetPendingExchangesAsync();
        Task<bool> AcceptExchangeAsync(int exchangeId);
        Task<bool> RejectExchangeAsync(int exchangeId);

        // 匹配功能
        Task<IEnumerable<ExchangeRequest>> FindMatchingExchangesAsync(int productId);
        Task<IEnumerable<ExchangeRequest>> GetMutualExchangeOpportunitiesAsync();
        Task<bool> HasPendingExchangeAsync(int productId);

        // 统计分析
        Task<int> GetSuccessfulExchangeCountAsync();
        Task<Dictionary<string, int>> GetExchangeStatusStatisticsAsync();
        Task<IEnumerable<ExchangeRequest>> GetRecentExchangesAsync(int days = 7);
        Task<IEnumerable<dynamic>> GetPopularExchangeCategoriesAsync();
    }
} 