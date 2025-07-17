using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// Reports实体的Repository实现类
    /// 继承基础Repository，提供Reports特有的查询和操作方法
    /// </summary>
    public class ReportsRepository : Repository<Reports>, IReportsRepository
    {
        public ReportsRepository(CampusTradeDbContext context) : base(context)
        {
        }

        #region 举报查询相关

        public async Task<IEnumerable<Reports>> GetByReporterIdAsync(int reporterId)
        {
            return await _dbSet
                .Where(r => r.ReporterId == reporterId)
                .Include(r => r.AbstractOrder)
                .Include(r => r.Reporter)
                .Include(r => r.Evidences)
                .OrderByDescending(r => r.CreateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reports>> GetByOrderIdAsync(int orderId)
        {
            return await _dbSet
                .Where(r => r.OrderId == orderId)
                .Include(r => r.Reporter)
                .Include(r => r.Evidences)
                .OrderByDescending(r => r.CreateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reports>> GetByStatusAsync(string status)
        {
            return await _dbSet
                .Where(r => r.Status == status)
                .Include(r => r.AbstractOrder)
                .Include(r => r.Reporter)
                .Include(r => r.Evidences)
                .OrderByDescending(r => r.CreateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reports>> GetPendingReportsAsync()
        {
            return await _dbSet
                .Where(r => r.Status == "待处理")
                .Include(r => r.AbstractOrder)
                .Include(r => r.Reporter)
                .Include(r => r.Evidences)
                .OrderByDescending(r => r.Priority)
                .ThenBy(r => r.CreateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reports>> GetOverdueReportsAsync()
        {
            var overdueTime = DateTime.Now.AddHours(-24);
            return await _dbSet
                .Where(r => r.Status == "待处理" && r.CreateTime < overdueTime)
                .Include(r => r.AbstractOrder)
                .Include(r => r.Reporter)
                .Include(r => r.Evidences)
                .OrderBy(r => r.CreateTime)
                .ToListAsync();
        }

        #endregion

        #region 分页查询

        public async Task<(IEnumerable<Reports> Reports, int TotalCount)> GetPagedReportsAsync(
            int pageIndex,
            int pageSize,
            string? status = null,
            string? type = null,
            int? priority = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _dbSet.AsQueryable();

            // 应用过滤条件
            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            if (!string.IsNullOrEmpty(type))
                query = query.Where(r => r.Type == type);

            if (priority.HasValue)
                query = query.Where(r => r.Priority == priority.Value);

            if (startDate.HasValue)
                query = query.Where(r => r.CreateTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreateTime <= endDate.Value);

            var totalCount = await query.CountAsync();
            var reports = await query
                .Include(r => r.AbstractOrder)
                .Include(r => r.Reporter)
                .Include(r => r.Evidences)
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.CreateTime)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (reports, totalCount);
        }

        #endregion

        #region 举报处理

        public async Task<bool> UpdateReportStatusAsync(int reportId, string newStatus)
        {
            var report = await GetByPrimaryKeyAsync(reportId);
            if (report == null || !report.IsValidStatus())
                return false;

            switch (newStatus)
            {
                case "处理中":
                    report.StartProcessing();
                    break;
                case "已处理":
                    report.CompleteProcessing();
                    break;
                case "已关闭":
                    report.CloseReport();
                    break;
                default:
                    return false;
            }

            Update(report);
            return true;
        }

        public async Task<bool> AssignPriorityAsync(int reportId, int priority)
        {
            if (priority < 1 || priority > 10)
                return false;

            var report = await GetByPrimaryKeyAsync(reportId);
            if (report == null)
                return false;

            report.SetPriority(priority);
            Update(report);
            return true;
        }

        #endregion

        #region 举报证据相关

        public async Task<IEnumerable<ReportEvidence>> GetReportEvidencesAsync(int reportId)
        {
            return await _context.Set<ReportEvidence>()
                .Where(re => re.ReportId == reportId)
                .OrderBy(re => re.UploadedAt)
                .ToListAsync();
        }

        public async Task AddReportEvidenceAsync(int reportId, string fileType, string fileUrl)
        {
            var evidence = new ReportEvidence
            {
                ReportId = reportId,
                FileType = fileType,
                FileUrl = fileUrl,
                UploadedAt = DateTime.Now
            };

            await _context.Set<ReportEvidence>().AddAsync(evidence);
        }

        #endregion

        #region 统计相关

        public async Task<Dictionary<string, int>> GetReportStatisticsAsync()
        {
            var stats = new Dictionary<string, int>();

            // 按状态统计
            var statusStats = await _dbSet
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            foreach (var stat in statusStats)
                stats[stat.Key] = stat.Value;

            // 按类型统计
            var typeStats = await _dbSet
                .GroupBy(r => r.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);

            foreach (var stat in typeStats)
                stats[$"类型_{stat.Key}"] = stat.Value;

            // 按优先级统计
            var highPriorityCount = await _dbSet
                .CountAsync(r => r.Priority >= 7);
            stats["高优先级"] = highPriorityCount;

            // 超时举报统计
            var overdueCount = await _dbSet
                .CountAsync(r => r.Status == "待处理" && r.CreateTime < DateTime.Now.AddHours(-24));
            stats["超时未处理"] = overdueCount;

            return stats;
        }

        public async Task<IEnumerable<Reports>> GetHighPriorityReportsAsync()
        {
            return await _dbSet
                .Where(r => r.Priority >= 7 && r.Status != "已处理" && r.Status != "已关闭")
                .Include(r => r.AbstractOrder)
                .Include(r => r.Reporter)
                .Include(r => r.Evidences)
                .OrderByDescending(r => r.Priority)
                .ThenBy(r => r.CreateTime)
                .ToListAsync();
        }

        public async Task<int> GetReportCountByTypeAsync(string type)
        {
            return await _dbSet.CountAsync(r => r.Type == type);
        }

        #endregion

        #region 扩展举报方法

        /// <summary>
        /// 获取举报详情（包含完整信息）
        /// </summary>
        public async Task<Reports?> GetReportWithDetailsAsync(int reportId)
        {
            return await _dbSet
                .Include(r => r.AbstractOrder)
                .Include(r => r.Reporter)
                .Include(r => r.Evidences)
                .FirstOrDefaultAsync(r => r.ReportId == reportId);
        }

        /// <summary>
        /// 批量处理举报
        /// </summary>
        public async Task<int> BulkUpdateReportStatusAsync(List<int> reportIds, string newStatus)
        {
            var reports = await _dbSet
                .Where(r => reportIds.Contains(r.ReportId))
                .ToListAsync();

            int updatedCount = 0;
            foreach (var report in reports)
            {
                try
                {
                    switch (newStatus)
                    {
                        case "处理中":
                            if (report.IsPending())
                            {
                                report.StartProcessing();
                                updatedCount++;
                            }
                            break;
                        case "已处理":
                            if (report.IsInProgress())
                            {
                                report.CompleteProcessing();
                                updatedCount++;
                            }
                            break;
                        case "已关闭":
                            if (!report.IsProcessed())
                            {
                                report.CloseReport();
                                updatedCount++;
                            }
                            break;
                    }
                }
                catch
                {
                    // 忽略单个举报的处理错误，继续处理其他举报
                }
            }

            if (updatedCount > 0)
            {
                UpdateRange(reports.Take(updatedCount));
            }

            return updatedCount;
        }

        /// <summary>
        /// 获取用户被举报统计
        /// </summary>
        public async Task<Dictionary<int, int>> GetUserReportStatisticsAsync()
        {
            // 这里需要通过订单关联到被举报的用户
            // 由于举报是针对订单的，需要根据业务逻辑确定被举报的是买家还是卖家
            // 这里简化为通过订单ID统计
            return await _dbSet
                .Include(r => r.AbstractOrder)
                .GroupBy(r => r.OrderId)
                .Select(g => new { OrderId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OrderId, x => x.Count);
        }

        #endregion
    }
}