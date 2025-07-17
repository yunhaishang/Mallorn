using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// 充值记录管理Repository实现
    /// 提供充值记录的状态跟踪和统计功能
    /// </summary>
    public class RechargeRecordsRepository : Repository<RechargeRecord>, IRechargeRecordsRepository
    {
        public RechargeRecordsRepository(CampusTradeDbContext context) : base(context)
        {
        }

        // 用户充值记录查询
        public async Task<(IEnumerable<RechargeRecord> Records, int TotalCount)> GetByUserIdAsync(
            int userId, int pageIndex = 0, int pageSize = 10)
        {
            var query = _context.RechargeRecords
                .Include(r => r.User)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreateTime);

            var totalCount = await query.CountAsync();
            var records = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (records, totalCount);
        }

        public async Task<IEnumerable<RechargeRecord>> GetPendingRechargesAsync(int userId)
        {
            return await _context.RechargeRecords
                .Include(r => r.User)
                .Where(r => r.UserId == userId && r.Status == "处理中")
                .OrderBy(r => r.CreateTime)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalRechargeAmountByUserAsync(int userId)
        {
            return await _context.RechargeRecords
                .Where(r => r.UserId == userId && r.Status == "成功")
                .SumAsync(r => r.Amount);
        }

        // 状态管理
        public async Task<IEnumerable<RechargeRecord>> GetRecordsByStatusAsync(string status)
        {
            return await _context.RechargeRecords
                .Include(r => r.User)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.CreateTime)
                .ToListAsync();
        }

        public async Task<bool> UpdateRechargeStatusAsync(int rechargeId, string status, DateTime? completeTime = null)
        {
            try
            {
                var record = await GetByPrimaryKeyAsync(rechargeId);
                if (record == null) return false;

                record.Status = status;
                if (completeTime.HasValue)
                {
                    record.CompleteTime = completeTime.Value;
                }
                else if (status == "成功" || status == "失败")
                {
                    record.CompleteTime = DateTime.UtcNow;
                }

                Update(record);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<RechargeRecord>> GetExpiredRechargesAsync(TimeSpan expiration)
        {
            var cutoffTime = DateTime.UtcNow - expiration;
            return await _context.RechargeRecords
                .Where(r => r.Status == "处理中" && r.CreateTime < cutoffTime)
                .OrderBy(r => r.CreateTime)
                .ToListAsync();
        }

        // 充值统计
        public async Task<decimal> GetTotalRechargeAmountAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.RechargeRecords.Where(r => r.Status == "成功");

            if (startDate.HasValue)
                query = query.Where(r => r.CreateTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreateTime <= endDate.Value);

            return await query.SumAsync(r => r.Amount);
        }

        public async Task<int> GetRechargeCountAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.RechargeRecords.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(r => r.CreateTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreateTime <= endDate.Value);

            return await query.CountAsync();
        }

        public async Task<Dictionary<string, int>> GetRechargeStatusStatisticsAsync()
        {
            return await _context.RechargeRecords
                .GroupBy(r => r.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<IEnumerable<dynamic>> GetDailyRechargeStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.RechargeRecords
                .Where(r => r.CreateTime >= startDate && r.CreateTime <= endDate)
                .GroupBy(r => new { Date = r.CreateTime.Date, r.Status })
                .Select(g => new
                {
                    Date = g.Key.Date,
                    Status = g.Key.Status,
                    Count = g.Count(),
                    Amount = g.Sum(r => r.Amount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync<dynamic>();
        }

        // 风险监控
        public async Task<IEnumerable<RechargeRecord>> GetLargeAmountRechargesAsync(decimal minAmount)
        {
            return await _context.RechargeRecords
                .Include(r => r.User)
                .Where(r => r.Amount >= minAmount)
                .OrderByDescending(r => r.Amount)
                .ToListAsync();
        }

        public async Task<IEnumerable<RechargeRecord>> GetFrequentRechargesAsync(int userId, TimeSpan timeSpan, int minCount)
        {
            var cutoffTime = DateTime.UtcNow - timeSpan;
            var recharges = await _context.RechargeRecords
                .Where(r => r.UserId == userId && r.CreateTime >= cutoffTime)
                .OrderByDescending(r => r.CreateTime)
                .ToListAsync();

            return recharges.Count >= minCount ? recharges : new List<RechargeRecord>();
        }

        public async Task<bool> HasRecentFailedRechargesAsync(int userId, TimeSpan timeSpan, int maxFailures)
        {
            var cutoffTime = DateTime.UtcNow - timeSpan;
            var failedCount = await _context.RechargeRecords
                .CountAsync(r => r.UserId == userId &&
                           r.Status == "失败" &&
                           r.CreateTime >= cutoffTime);

            return failedCount >= maxFailures;
        }
    }
}