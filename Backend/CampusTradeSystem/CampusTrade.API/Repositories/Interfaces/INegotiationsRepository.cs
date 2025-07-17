using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 议价管理Repository接口
    /// 提供价格协商、状态跟踪等功能
    /// </summary>
    public interface INegotiationsRepository : IRepository<Negotiation>
    {
        // 议价查询
        Task<IEnumerable<Negotiation>> GetByOrderIdAsync(int orderId);
        Task<Negotiation?> GetLatestNegotiationAsync(int orderId);
        Task<IEnumerable<Negotiation>> GetPendingNegotiationsAsync(int userId);

        // 状态管理
        Task<bool> UpdateNegotiationStatusAsync(int negotiationId, string status);
        Task<IEnumerable<Negotiation>> GetNegotiationsByStatusAsync(string status);
        Task<bool> AcceptNegotiationAsync(int negotiationId);
        Task<bool> RejectNegotiationAsync(int negotiationId);

        // 议价历史
        Task<IEnumerable<Negotiation>> GetNegotiationHistoryAsync(int orderId);
        Task<int> GetNegotiationCountByOrderAsync(int orderId);
        Task<bool> HasActiveNegotiationAsync(int orderId);

        // 统计分析
        Task<decimal> GetAverageNegotiationRateAsync();
        Task<int> GetSuccessfulNegotiationCountAsync();
        Task<IEnumerable<dynamic>> GetNegotiationStatisticsAsync();
        Task<IEnumerable<Negotiation>> GetRecentNegotiationsAsync(int days = 7);
    }
} 