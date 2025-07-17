using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 充值记录管理Repository接口
    /// 提供充值记录的状态跟踪和统计功能
    /// </summary>
    public interface IRechargeRecordsRepository : IRepository<RechargeRecord>
    {
        // 用户充值记录查询
        Task<(IEnumerable<RechargeRecord> Records, int TotalCount)> GetByUserIdAsync(
            int userId, int pageIndex = 0, int pageSize = 10);
        Task<IEnumerable<RechargeRecord>> GetPendingRechargesAsync(int userId);
        Task<decimal> GetTotalRechargeAmountByUserAsync(int userId);

        // 状态管理
        Task<IEnumerable<RechargeRecord>> GetRecordsByStatusAsync(string status);
        Task<bool> UpdateRechargeStatusAsync(int rechargeId, string status, DateTime? completeTime = null);
        Task<IEnumerable<RechargeRecord>> GetExpiredRechargesAsync(TimeSpan expiration);

        // 充值统计
        Task<decimal> GetTotalRechargeAmountAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<int> GetRechargeCountAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, int>> GetRechargeStatusStatisticsAsync();
        Task<IEnumerable<dynamic>> GetDailyRechargeStatisticsAsync(DateTime startDate, DateTime endDate);

        // 风险监控
        Task<IEnumerable<RechargeRecord>> GetLargeAmountRechargesAsync(decimal minAmount);
        Task<IEnumerable<RechargeRecord>> GetFrequentRechargesAsync(int userId, TimeSpan timeSpan, int minCount);
        Task<bool> HasRecentFailedRechargesAsync(int userId, TimeSpan timeSpan, int maxFailures);
    }
} 