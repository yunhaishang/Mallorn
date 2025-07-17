using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 信用记录管理Repository接口
    /// 提供信用变更跟踪等功能
    /// </summary>
    public interface ICreditHistoryRepository : IRepository<CreditHistory>
    {
        // 用户信用记录查询
        Task<IEnumerable<CreditHistory>> GetByUserIdAsync(int userId);
        Task<(IEnumerable<CreditHistory> Records, int TotalCount)> GetPagedByUserIdAsync(
            int userId, int pageIndex = 0, int pageSize = 10);

        // 信用变更类型查询
        Task<IEnumerable<CreditHistory>> GetByChangeTypeAsync(string changeType);
        Task<IEnumerable<CreditHistory>> GetRecentChangesAsync(int days = 30);

        // 统计分析
        Task<decimal> GetTotalCreditChangeAsync(int userId, string? changeType = null);
        Task<Dictionary<string, int>> GetChangeTypeStatisticsAsync();
        Task<IEnumerable<dynamic>> GetCreditTrendsAsync(int userId, int days = 30);
    }
} 