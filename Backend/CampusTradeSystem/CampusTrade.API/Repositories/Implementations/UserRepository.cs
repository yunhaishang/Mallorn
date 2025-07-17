using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// User实体的Repository实现类
    /// 继承基础Repository，提供User特有的查询和操作方法
    /// </summary>
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(CampusTradeDbContext context) : base(context)
        {
        }

        #region 用户认证相关

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByStudentIdAsync(string studentId)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.StudentId == studentId);
        }

        public async Task<User?> ValidateUserAsync(string email, string passwordHash)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == passwordHash);
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsStudentIdExistsAsync(string studentId)
        {
            return await _dbSet.AnyAsync(u => u.StudentId == studentId);
        }

        #endregion

        #region 用户状态管理

        public async Task<User?> GetActiveUserByIdAsync(int userId)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive == 1);
        }

        public async Task<User?> GetUserWithDetailsAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.Student)
                .Include(u => u.VirtualAccount)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task SetUserActiveStatusAsync(int userId, bool isActive)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.IsActive = isActive ? 1 : 0;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        public async Task UpdateLastLoginAsync(int userId, string ipAddress)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                user.LastLoginIp = ipAddress;
                user.LoginCount++;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        #endregion

        #region 安全相关

        public async Task<User?> GetUserBySecurityStampAsync(string securityStamp)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.SecurityStamp == securityStamp);
        }

        public async Task UpdateSecurityStampAsync(int userId, string newSecurityStamp)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.SecurityStamp = newSecurityStamp;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        public async Task<bool> IsUserLockedAsync(int userId)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            return user != null && user.IsLocked == 1 &&
                   (user.LockoutEnd == null || user.LockoutEnd > DateTime.UtcNow);
        }

        public async Task LockUserAsync(int userId, DateTime? lockoutEnd = null)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.IsLocked = 1;
                user.LockoutEnd = lockoutEnd;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        public async Task UnlockUserAsync(int userId)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.IsLocked = 0;
                user.LockoutEnd = null;
                user.FailedLoginAttempts = 0;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        public async Task IncrementFailedLoginAttemptsAsync(int userId)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.FailedLoginAttempts++;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        public async Task ResetFailedLoginAttemptsAsync(int userId)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.FailedLoginAttempts = 0;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        #endregion

        #region 密码相关

        public async Task UpdatePasswordAsync(int userId, string newPasswordHash)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.PasswordHash = newPasswordHash;
                user.PasswordChangedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        public async Task<DateTime?> GetPasswordChangedAtAsync(int userId)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            return user?.PasswordChangedAt;
        }

        public async Task UpdatePasswordChangedAtAsync(int userId, DateTime changedAt)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.PasswordChangedAt = changedAt;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        #endregion

        #region 邮箱验证

        public async Task<bool> IsEmailVerifiedAsync(int userId)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            return user != null && user.EmailVerified == 1;
        }

        public async Task SetEmailVerifiedAsync(int userId, bool isVerified)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.EmailVerified = isVerified ? 1 : 0;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        public async Task UpdateEmailVerificationTokenAsync(int userId, string? token)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.EmailVerificationToken = token;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        #endregion

        #region 双因子认证

        public async Task<bool> IsTwoFactorEnabledAsync(int userId)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            return user != null && user.TwoFactorEnabled == 1;
        }

        public async Task SetTwoFactorEnabledAsync(int userId, bool enabled)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.TwoFactorEnabled = enabled ? 1 : 0;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        #endregion

        #region 用户统计和查询

        public async Task<int> GetUserCountAsync()
        {
            return await _dbSet.CountAsync();
        }

        public async Task<int> GetActiveUserCountAsync()
        {
            return await _dbSet.CountAsync(u => u.IsActive == 1);
        }

        public async Task<IEnumerable<User>> GetUsersByDepartmentAsync(string department)
        {
            return await _dbSet
                .Include(u => u.Student)
                .Where(u => u.Student != null && u.Student.Department == department)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersByCreditRangeAsync(decimal minCredit, decimal maxCredit)
        {
            return await _dbSet
                .Where(u => u.CreditScore >= minCredit && u.CreditScore <= maxCredit)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetRecentRegisteredUsersAsync(int days)
        {
            var sinceDate = DateTime.UtcNow.AddDays(-days);
            return await _dbSet
                .Where(u => u.CreatedAt >= sinceDate)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersWithLowCreditAsync(decimal threshold)
        {
            return await _dbSet
                .Where(u => u.CreditScore < threshold)
                .OrderBy(u => u.CreditScore)
                .ToListAsync();
        }

        #endregion

        #region 用户关系查询

        public async Task<User?> GetUserWithStudentAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User?> GetUserWithVirtualAccountAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.VirtualAccount)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User?> GetUserWithRefreshTokensAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User?> GetUserWithOrdersAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.BuyerOrders)
                .Include(u => u.SellerOrders)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User?> GetUserWithProductsAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.Products)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User?> GetUserWithNotificationsAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.ReceivedNotifications)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        #endregion

        #region 批量操作

        public async Task<IEnumerable<User>> GetUsersByIdsAsync(IEnumerable<int> userIds)
        {
            return await _dbSet
                .Where(u => userIds.Contains(u.UserId))
                .ToListAsync();
        }

        public async Task BulkUpdateLastLoginAsync(IEnumerable<int> userIds, DateTime loginTime)
        {
            var users = await GetUsersByIdsAsync(userIds);
            foreach (var user in users)
            {
                user.LastLoginAt = loginTime;
                user.LoginCount++;
                user.UpdatedAt = DateTime.UtcNow;
            }
            UpdateRange(users);
        }

        public async Task BulkLockUsersAsync(IEnumerable<int> userIds, DateTime? lockoutEnd = null)
        {
            var users = await GetUsersByIdsAsync(userIds);
            foreach (var user in users)
            {
                user.IsLocked = 1;
                user.LockoutEnd = lockoutEnd;
                user.UpdatedAt = DateTime.UtcNow;
            }
            UpdateRange(users);
        }

        public async Task BulkUnlockUsersAsync(IEnumerable<int> userIds)
        {
            var users = await GetUsersByIdsAsync(userIds);
            foreach (var user in users)
            {
                user.IsLocked = 0;
                user.LockoutEnd = null;
                user.FailedLoginAttempts = 0;
                user.UpdatedAt = DateTime.UtcNow;
            }
            UpdateRange(users);
        }

        #endregion

        #region 高级查询

        public async Task<(IEnumerable<User> Users, int TotalCount)> SearchUsersAsync(
            string? keyword = null,
            string? department = null,
            decimal? minCredit = null,
            decimal? maxCredit = null,
            bool? isActive = null,
            bool? isLocked = null,
            DateTime? registeredAfter = null,
            DateTime? registeredBefore = null,
            int pageNumber = 1,
            int pageSize = 20)
        {
            var query = _dbSet.Include(u => u.Student).AsQueryable();

            // 关键词搜索
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(u => u.Email.Contains(keyword) ||
                                        u.Username!.Contains(keyword) ||
                                        u.FullName!.Contains(keyword) ||
                                        u.StudentId.Contains(keyword));
            }

            // 院系过滤
            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(u => u.Student != null && u.Student.Department == department);
            }

            // 信用分数范围
            if (minCredit.HasValue)
            {
                query = query.Where(u => u.CreditScore >= minCredit.Value);
            }
            if (maxCredit.HasValue)
            {
                query = query.Where(u => u.CreditScore <= maxCredit.Value);
            }

            // 激活状态
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == (isActive.Value ? 1 : 0));
            }

            // 锁定状态
            if (isLocked.HasValue)
            {
                query = query.Where(u => u.IsLocked == (isLocked.Value ? 1 : 0));
            }

            // 注册时间范围
            if (registeredAfter.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= registeredAfter.Value);
            }
            if (registeredBefore.HasValue)
            {
                query = query.Where(u => u.CreatedAt <= registeredBefore.Value);
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }

        #endregion

        #region 用户数据统计相关

        public async Task<Dictionary<string, int>> GetUserCountByDepartmentAsync()
        {
            return await _dbSet
                .Include(u => u.Student)
                .Where(u => u.Student != null && u.Student.Department != null)
                .GroupBy(u => u.Student!.Department!)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Department, x => x.Count);
        }

        public async Task<Dictionary<DateTime, int>> GetUserRegistrationTrendAsync(int days)
        {
            var startDate = DateTime.UtcNow.AddDays(-days).Date;
            return await _dbSet
                .Where(u => u.CreatedAt >= startDate)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count);
        }

        public async Task<IEnumerable<User>> GetTopUsersByCreditAsync(int count)
        {
            return await _dbSet
                .OrderByDescending(u => u.CreditScore)
                .Take(count)
                .ToListAsync();
        }

        #endregion
    }
}