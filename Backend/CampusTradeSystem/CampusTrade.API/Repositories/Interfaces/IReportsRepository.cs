using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// Reports实体的Repository接口
    /// 继承基础IRepository，提供Reports特有的查询和操作方法
    /// </summary>
    public interface IReportsRepository : IRepository<Reports>
    {
        // 举报查询相关
        Task<IEnumerable<Reports>> GetByReporterIdAsync(int reporterId);
        Task<IEnumerable<Reports>> GetByOrderIdAsync(int orderId);
        Task<IEnumerable<Reports>> GetByStatusAsync(string status);
        Task<IEnumerable<Reports>> GetPendingReportsAsync();
        Task<IEnumerable<Reports>> GetOverdueReportsAsync();

        // 分页查询
        Task<(IEnumerable<Reports> Reports, int TotalCount)> GetPagedReportsAsync(
            int pageIndex,
            int pageSize,
            string? status = null,
            string? type = null,
            int? priority = null,
            DateTime? startDate = null,
            DateTime? endDate = null);

        // 举报处理
        Task<bool> UpdateReportStatusAsync(int reportId, string newStatus);
        Task<bool> AssignPriorityAsync(int reportId, int priority);

        // 举报证据相关
        Task<IEnumerable<ReportEvidence>> GetReportEvidencesAsync(int reportId);
        Task AddReportEvidenceAsync(int reportId, string fileType, string fileUrl);

        // 统计相关
        Task<Dictionary<string, int>> GetReportStatisticsAsync();
        Task<IEnumerable<Reports>> GetHighPriorityReportsAsync();
        Task<int> GetReportCountByTypeAsync(string type);
    }
} 