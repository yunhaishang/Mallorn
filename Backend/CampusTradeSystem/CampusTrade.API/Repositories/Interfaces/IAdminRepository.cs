using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// Admin实体的Repository接口
    /// 继承基础IRepository，提供Admin特有的查询和操作方法
    /// </summary>
    public interface IAdminRepository : IRepository<Admin>
    {
        // 管理员查询相关
        Task<Admin?> GetByUserIdAsync(int userId);
        Task<IEnumerable<Admin>> GetByRoleAsync(string role);
        Task<IEnumerable<Admin>> GetCategoryAdminsAsync();
        Task<Admin?> GetCategoryAdminByCategoryIdAsync(int categoryId);

        // 权限相关
        Task<bool> IsUserAdminAsync(int userId);
        Task<bool> HasPermissionForCategoryAsync(int userId, int categoryId);
        Task<bool> CanHandleReportsAsync(int userId);

        // 审计日志相关
        Task<IEnumerable<AuditLog>> GetAuditLogsByAdminAsync(int adminId, DateTime? startDate = null, DateTime? endDate = null);
        Task CreateAuditLogAsync(int adminId, string actionType, int? targetId = null, string? detail = null);

        // 统计相关
        Task<Dictionary<string, int>> GetAdminStatisticsAsync();
        Task<IEnumerable<Admin>> GetActiveAdminsAsync();
    }
} 