using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// Notification实体的Repository实现类
    /// 继承基础Repository，提供Notification特有的查询和操作方法
    /// </summary>
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        public NotificationRepository(CampusTradeDbContext context) : base(context)
        {
        }

        #region 通知查询相关

        public async Task<IEnumerable<Notification>> GetByRecipientIdAsync(int recipientId)
        {
            return await _dbSet
                .Where(n => n.RecipientId == recipientId)
                .Include(n => n.Template)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUnsentNotificationsAsync()
        {
            return await _dbSet
                .Where(n => n.SendStatus == Notification.SendStatuses.Pending)
                .Include(n => n.Template)
                .Include(n => n.Recipient)
                .OrderBy(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetFailedNotificationsAsync()
        {
            return await _dbSet
                .Where(n => n.SendStatus == Notification.SendStatuses.Failed)
                .Include(n => n.Template)
                .Include(n => n.Recipient)
                .OrderByDescending(n => n.LastAttemptTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetPendingRetryNotificationsAsync()
        {
            var now = DateTime.Now;
            return await _dbSet
                .Where(n => n.SendStatus == Notification.SendStatuses.Failed
                           && n.RetryCount < Notification.MaxRetryCount
                           && n.LastAttemptTime.AddMinutes(Notification.DefaultRetryIntervalMinutes) <= now)
                .Include(n => n.Template)
                .Include(n => n.Recipient)
                .OrderBy(n => n.LastAttemptTime)
                .ToListAsync();
        }

        #endregion

        #region 通知状态管理

        public async Task MarkAsSentAsync(int notificationId)
        {
            var notification = await GetByPrimaryKeyAsync(notificationId);
            if (notification != null)
            {
                notification.MarkAsSuccessful();
                Update(notification);
            }
        }

        public async Task MarkAsFailedAsync(int notificationId)
        {
            var notification = await GetByPrimaryKeyAsync(notificationId);
            if (notification != null)
            {
                notification.MarkAsFailed();
                Update(notification);
            }
        }

        public async Task IncrementRetryCountAsync(int notificationId)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE NOTIFICATIONS 
                  SET RETRY_COUNT = RETRY_COUNT + 1, 
                      LAST_ATTEMPT_TIME = CURRENT_TIMESTAMP
                  WHERE NOTIFICATION_ID = {0}",
                notificationId);
        }

        #endregion

        #region 分页查询

        public async Task<(IEnumerable<Notification> Notifications, int TotalCount)> GetPagedNotificationsByUserAsync(
            int userId,
            int pageIndex,
            int pageSize,
            string? status = null,
            string? templateType = null)
        {
            var query = _dbSet
                .Where(n => n.RecipientId == userId)
                .AsQueryable();

            // 应用过滤条件
            if (!string.IsNullOrEmpty(status))
                query = query.Where(n => n.SendStatus == status);

            if (!string.IsNullOrEmpty(templateType))
                query = query.Where(n => n.Template.TemplateType == templateType);

            var totalCount = await query.CountAsync();
            var notifications = await query
                .Include(n => n.Template)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (notifications, totalCount);
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量创建通知
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="recipientIds">接收者ID列表</param>
        /// <param name="orderId">订单ID（可选）</param>
        /// <param name="parameters">模板参数（可选）</param>
        /// <returns>创建的通知实体列表</returns>
        public async Task<IEnumerable<Notification>> CreateBatchNotificationsAsync(
            int templateId,
            List<int> recipientIds,
            int? orderId = null,
            Dictionary<string, object>? parameters = null)
        {
            if (!recipientIds.Any())
                return new List<Notification>();

            var notifications = new List<Notification>();
            var serializedParams = parameters != null ?
                JsonSerializer.Serialize(parameters) : null;

            foreach (var recipientId in recipientIds)
            {
                var notification = new Notification
                {
                    TemplateId = templateId,
                    RecipientId = recipientId,
                    OrderId = orderId,
                    SendStatus = Notification.SendStatuses.Pending,
                    TemplateParams = serializedParams
                    // NotificationId, CreatedAt, LastAttemptTime, RetryCount 等由Oracle处理
                };

                notifications.Add(notification);
            }

            await AddRangeAsync(notifications);
            return notifications;
        }

        /// <summary>
        /// 批量创建订单相关通知
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="recipientIds">接收者ID列表</param>
        /// <param name="orderId">订单ID</param>
        /// <param name="parameters">模板参数（可选）</param>
        /// <returns>创建的通知实体列表</returns>
        public async Task<IEnumerable<Notification>> CreateBatchOrderNotificationsAsync(
            int templateId,
            List<int> recipientIds,
            int orderId,
            Dictionary<string, object>? parameters = null)
        {
            return await CreateBatchNotificationsAsync(templateId, recipientIds, orderId, parameters);
        }

        /// <summary>
        /// 批量创建系统通知
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="recipientIds">接收者ID列表</param>
        /// <param name="parameters">模板参数（可选）</param>
        /// <returns>创建的通知实体列表</returns>
        public async Task<IEnumerable<Notification>> CreateBatchSystemNotificationsAsync(
            int templateId,
            List<int> recipientIds,
            Dictionary<string, object>? parameters = null)
        {
            return await CreateBatchNotificationsAsync(templateId, recipientIds, null, parameters);
        }

        #endregion

        #region 统计相关

        public async Task<int> GetUnreadCountByUserAsync(int userId)
        {
            return await _dbSet
                .CountAsync(n => n.RecipientId == userId
                               && n.SendStatus == Notification.SendStatuses.Success);
        }

        public async Task<Dictionary<string, int>> GetNotificationStatisticsAsync()
        {
            var stats = new Dictionary<string, int>();

            // 按状态统计
            var statusStats = await _dbSet
                .GroupBy(n => n.SendStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            foreach (var stat in statusStats)
                stats[stat.Key] = stat.Value;

            // 按模板类型统计
            var typeStats = await _dbSet
                .Include(n => n.Template)
                .GroupBy(n => n.Template.TemplateType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);

            foreach (var stat in typeStats)
                stats[$"类型_{stat.Key}"] = stat.Value;

            // 重试次数统计
            var retryStats = await _dbSet
                .Where(n => n.RetryCount > 0)
                .CountAsync();
            stats["需要重试"] = retryStats;

            return stats;
        }

        #endregion

        #region 扩展查询方法

        /// <summary>
        /// 获取高优先级通知
        /// </summary>
        public async Task<IEnumerable<Notification>> GetHighPriorityNotificationsAsync()
        {
            return await _dbSet
                .Include(n => n.Template)
                .Where(n => n.Template.Priority >= NotificationTemplate.PriorityLevels.High
                           && n.SendStatus == Notification.SendStatuses.Pending)
                .OrderByDescending(n => n.Template.Priority)
                .ThenBy(n => n.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 清理过期的失败通知
        /// </summary>
        public async Task<int> CleanupExpiredFailedNotificationsAsync(int daysOld = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysOld);
            var expiredNotifications = await _dbSet
                .Where(n => n.SendStatus == Notification.SendStatuses.Failed
                           && n.RetryCount >= Notification.MaxRetryCount
                           && n.CreatedAt < cutoffDate)
                .ToListAsync();

            if (expiredNotifications.Any())
            {
                DeleteRange(expiredNotifications);
            }

            return expiredNotifications.Count;
        }

        /// <summary>
        /// 获取用户最近的通知
        /// </summary>
        public async Task<IEnumerable<Notification>> GetRecentNotificationsByUserAsync(int userId, int count = 10)
        {
            return await _dbSet
                .Where(n => n.RecipientId == userId)
                .Include(n => n.Template)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        #endregion
    }
}