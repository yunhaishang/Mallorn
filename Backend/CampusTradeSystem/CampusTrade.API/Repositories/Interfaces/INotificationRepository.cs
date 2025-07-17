using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// Notification实体的Repository接口
    /// 继承基础IRepository，提供Notification特有的查询和操作方法
    /// </summary>
    public interface INotificationRepository : IRepository<Notification>
    {
        // 通知查询相关
        Task<IEnumerable<Notification>> GetByRecipientIdAsync(int recipientId);
        Task<IEnumerable<Notification>> GetUnsentNotificationsAsync();
        Task<IEnumerable<Notification>> GetFailedNotificationsAsync();
        Task<IEnumerable<Notification>> GetPendingRetryNotificationsAsync();

        // 通知状态管理
        Task MarkAsSentAsync(int notificationId);
        Task MarkAsFailedAsync(int notificationId);
        Task IncrementRetryCountAsync(int notificationId);

        // 分页查询
        Task<(IEnumerable<Notification> Notifications, int TotalCount)> GetPagedNotificationsByUserAsync(
            int userId,
            int pageIndex,
            int pageSize,
            string? status = null,
            string? templateType = null);

        // 批量操作
        Task<IEnumerable<Notification>> CreateBatchNotificationsAsync(
            int templateId,
            List<int> recipientIds,
            int? orderId = null,
            Dictionary<string, object>? parameters = null);

        Task<IEnumerable<Notification>> CreateBatchOrderNotificationsAsync(
            int templateId,
            List<int> recipientIds,
            int orderId,
            Dictionary<string, object>? parameters = null);

        Task<IEnumerable<Notification>> CreateBatchSystemNotificationsAsync(
            int templateId,
            List<int> recipientIds,
            Dictionary<string, object>? parameters = null);

        // 统计相关
        Task<int> GetUnreadCountByUserAsync(int userId);
        Task<Dictionary<string, int>> GetNotificationStatisticsAsync();

        // 扩展查询方法
        Task<IEnumerable<Notification>> GetHighPriorityNotificationsAsync();
        Task<int> CleanupExpiredFailedNotificationsAsync(int daysOld = 30);
        Task<IEnumerable<Notification>> GetRecentNotificationsByUserAsync(int userId, int count = 10);
    }
} 