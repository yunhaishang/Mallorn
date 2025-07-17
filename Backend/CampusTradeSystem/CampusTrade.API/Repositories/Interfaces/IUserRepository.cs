using CampusTrade.API.Models.Entities;
using System.Linq.Expressions;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// User实体的Repository接口
    /// 继承基础IRepository，提供User特有的查询和操作方法
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        #region 读取操作
        // 根据邮箱查询用户
        Task<User?> GetByEmailAsync(string email);
        // 根据学号查询用户
        Task<User?> GetByStudentIdAsync(string studentId);
        // 根据用户名获取用户
        Task<User?> GetByUsernameAsync(string username);
        // 获取所有活跃用户
        Task<IEnumerable<User>> GetActiveUsersAsync();
        // 获取用户详细信息
        Task<User?> GetUserWithDetailsAsync(int userId);
        // 根据安全戳获取用户
        Task<User?> GetUserBySecurityStampAsync(string securityStamp);
        // 获取密码修改时间
        Task<DateTime?> GetPasswordChangedAtAsync(int userId);
        #endregion

        #region 更新操作
        // 更新用户账号状态
        Task SetUserActiveStatusAsync(int userId, bool isActive);
        // 更新用户最后登录信息
        Task UpdateLastLoginAsync(int userId, string ipAddress);
        // 更新用户安全戳
        Task UpdateSecurityStampAsync(int userId, string newSecurityStamp);
        // 锁定用户
        Task LockUserAsync(int userId, DateTime? lockoutEnd = null);
        // 解锁用户
        Task UnlockUserAsync(int userId);
        // 增加登录失败次数
        Task IncrementFailedLoginAttemptsAsync(int userId);
        // 重置登录失败次数
        Task ResetFailedLoginAttemptsAsync(int userId);
        // 更新用户密码
        Task UpdatePasswordAsync(int userId, string newPasswordHash);


        Task SetEmailVerifiedAsync(int userId, bool isVerified);
        Task UpdateEmailVerificationTokenAsync(int userId, string? token);

        // 双因子认证
        Task<bool> IsTwoFactorEnabledAsync(int userId);
        Task SetTwoFactorEnabledAsync(int userId, bool enabled);

        // 用户统计和查询
        Task<int> GetUserCountAsync();
        Task<int> GetActiveUserCountAsync();
        Task<IEnumerable<User>> GetUsersByDepartmentAsync(string department);
        Task<IEnumerable<User>> GetUsersByCreditRangeAsync(decimal minCredit, decimal maxCredit);
        Task<IEnumerable<User>> GetRecentRegisteredUsersAsync(int days);
        Task<IEnumerable<User>> GetUsersWithLowCreditAsync(decimal threshold);

        // 用户关系查询
        Task<User?> GetUserWithStudentAsync(int userId);
        Task<User?> GetUserWithVirtualAccountAsync(int userId);
        Task<User?> GetUserWithRefreshTokensAsync(int userId);
        Task<User?> GetUserWithOrdersAsync(int userId);
        Task<User?> GetUserWithProductsAsync(int userId);
        Task<User?> GetUserWithNotificationsAsync(int userId);

        // 批量操作
        Task<IEnumerable<User>> GetUsersByIdsAsync(IEnumerable<int> userIds);
        Task BulkUpdateLastLoginAsync(IEnumerable<int> userIds, DateTime loginTime);
        Task BulkLockUsersAsync(IEnumerable<int> userIds, DateTime? lockoutEnd = null);
        Task BulkUnlockUsersAsync(IEnumerable<int> userIds);

        // 高级查询
        Task<(IEnumerable<User> Users, int TotalCount)> SearchUsersAsync(
            string? keyword = null,
            string? department = null,
            decimal? minCredit = null,
            decimal? maxCredit = null,
            bool? isActive = null,
            bool? isLocked = null,
            DateTime? registeredAfter = null,
            DateTime? registeredBefore = null,
            int pageNumber = 1,
            int pageSize = 20);

        // 用户数据统计相关
        Task<Dictionary<string, int>> GetUserCountByDepartmentAsync();
        Task<Dictionary<DateTime, int>> GetUserRegistrationTrendAsync(int days);
        Task<IEnumerable<User>> GetTopUsersByCreditAsync(int count);
    }
}