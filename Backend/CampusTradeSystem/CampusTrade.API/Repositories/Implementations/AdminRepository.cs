using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// Admin实体的Repository实现类
    /// 继承基础Repository，提供Admin特有的查询和操作方法
    /// </summary>
    public class AdminRepository : Repository<Admin>, IAdminRepository
    {
        public AdminRepository(CampusTradeDbContext context) : base(context)
        {
        }

        #region 管理员查询相关

        public async Task<Admin?> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(a => a.User)
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.UserId == userId);
        }

        public async Task<IEnumerable<Admin>> GetByRoleAsync(string role)
        {
            return await _dbSet
                .Where(a => a.Role == role)
                .Include(a => a.User)
                .Include(a => a.Category)
                .ToListAsync();
        }

        public async Task<IEnumerable<Admin>> GetCategoryAdminsAsync()
        {
            return await _dbSet
                .Where(a => a.Role == Admin.Roles.CategoryAdmin)
                .Include(a => a.User)
                .Include(a => a.Category)
                .ToListAsync();
        }

        public async Task<Admin?> GetCategoryAdminByCategoryIdAsync(int categoryId)
        {
            return await _dbSet
                .Include(a => a.User)
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.AssignedCategory == categoryId
                                         && a.Role == Admin.Roles.CategoryAdmin);
        }

        #endregion

        #region 权限相关

        public async Task<bool> IsUserAdminAsync(int userId)
        {
            return await _dbSet.AnyAsync(a => a.UserId == userId);
        }

        public async Task<bool> HasPermissionForCategoryAsync(int userId, int categoryId)
        {
            return await _dbSet.AnyAsync(a => a.UserId == userId
                && (a.Role == Admin.Roles.Super ||
                    (a.Role == Admin.Roles.CategoryAdmin && a.AssignedCategory == categoryId)));
        }

        public async Task<bool> CanHandleReportsAsync(int userId)
        {
            return await _dbSet.AnyAsync(a => a.UserId == userId
                && (a.Role == Admin.Roles.Super || a.Role == Admin.Roles.ReportAdmin));
        }

        #endregion

        #region 审计日志相关

        public async Task<IEnumerable<AuditLog>> GetAuditLogsByAdminAsync(int adminId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Set<AuditLog>()
                .Where(al => al.AdminId == adminId)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(al => al.LogTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(al => al.LogTime <= endDate.Value);

            return await query
                .Include(al => al.Admin)
                    .ThenInclude(a => a.User)
                .OrderByDescending(al => al.LogTime)
                .ToListAsync();
        }

        public async Task CreateAuditLogAsync(int adminId, string actionType, int? targetId = null, string? detail = null)
        {
            var auditLog = AuditLog.CreateLog(adminId, actionType, targetId, detail);
            await _context.Set<AuditLog>().AddAsync(auditLog);
        }

        #endregion

        #region 统计相关

        public async Task<Dictionary<string, int>> GetAdminStatisticsAsync()
        {
            var stats = new Dictionary<string, int>();

            // 按角色统计
            var roleStats = await _dbSet
                .GroupBy(a => a.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Role, x => x.Count);

            foreach (var stat in roleStats)
                stats[stat.Key] = stat.Value;

            // 分类管理员统计
            var categoryAdminCount = await _dbSet
                .CountAsync(a => a.Role == Admin.Roles.CategoryAdmin && a.AssignedCategory.HasValue);
            stats["已分配分类的管理员"] = categoryAdminCount;

            // 最近活跃的管理员（最近30天有审计日志）
            var recentActiveCount = await _dbSet
                .Include(a => a.AuditLogs)
                .Where(a => a.AuditLogs.Any(al => al.LogTime >= DateTime.Now.AddDays(-30)))
                .CountAsync();
            stats["最近活跃管理员"] = recentActiveCount;

            return stats;
        }

        public async Task<IEnumerable<Admin>> GetActiveAdminsAsync()
        {
            // 获取最近30天有活动的管理员
            var recentDate = DateTime.Now.AddDays(-30);
            return await _dbSet
                .Include(a => a.User)
                .Include(a => a.Category)
                .Include(a => a.AuditLogs)
                .Where(a => a.AuditLogs.Any(al => al.LogTime >= recentDate))
                .OrderByDescending(a => a.AuditLogs.Max(al => al.LogTime))
                .ToListAsync();
        }

        #endregion

        #region 扩展管理员方法

        /// <summary>
        /// 创建分类管理员
        /// </summary>
        public async Task<Admin> CreateCategoryAdminAsync(int userId, int categoryId)
        {
            // 检查是否已存在
            var existingAdmin = await GetByUserIdAsync(userId);
            if (existingAdmin != null)
                throw new InvalidOperationException("用户已经是管理员");

            // 检查分类是否已有管理员
            var existingCategoryAdmin = await GetCategoryAdminByCategoryIdAsync(categoryId);
            if (existingCategoryAdmin != null)
                throw new InvalidOperationException("该分类已有管理员");

            var admin = Admin.CreateCategoryAdmin(userId, categoryId);
            await AddAsync(admin);
            return admin;
        }

        /// <summary>
        /// 更新管理员角色
        /// </summary>
        public async Task<bool> UpdateAdminRoleAsync(int adminId, string newRole, int? newCategoryId = null)
        {
            var admin = await GetByPrimaryKeyAsync(adminId);
            if (admin == null)
                return false;

            if (!Admin.IsValidRole(newRole))
                return false;

            admin.Role = newRole;
            admin.AssignedCategory = newRole == Admin.Roles.CategoryAdmin ? newCategoryId : null;

            if (!admin.IsValidRoleAssignment())
                return false;

            Update(admin);
            return true;
        }

        /// <summary>
        /// 删除管理员
        /// </summary>
        public async Task<bool> RemoveAdminAsync(int userId)
        {
            var admin = await GetByUserIdAsync(userId);
            if (admin == null)
                return false;

            Delete(admin);
            return true;
        }

        /// <summary>
        /// 获取管理员的权限范围
        /// </summary>
        public async Task<List<int>> GetAdminCategoryPermissionsAsync(int userId)
        {
            var admin = await GetByUserIdAsync(userId);
            if (admin == null)
                return new List<int>();

            if (admin.IsSuperAdmin())
            {
                // 超级管理员有所有分类权限
                return await _context.Set<Category>()
                    .Select(c => c.CategoryId)
                    .ToListAsync();
            }

            if (admin.IsCategoryAdmin() && admin.AssignedCategory.HasValue)
            {
                return new List<int> { admin.AssignedCategory.Value };
            }

            return new List<int>();
        }

        #endregion
    }
}