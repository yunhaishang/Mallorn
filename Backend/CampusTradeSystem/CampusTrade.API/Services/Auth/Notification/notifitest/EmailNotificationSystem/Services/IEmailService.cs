using EmailNotificationSystem.Models;

namespace EmailNotificationSystem.Services
{
    /// <summary>
    /// 邮件服务接口
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="request">邮件发送请求</param>
        /// <returns>发送结果</returns>
        Task<(bool Success, string Message, int? HistoryId)> SendEmailAsync(EmailSendRequest request);
        
        /// <summary>
        /// 通过模板发送邮件
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="recipientEmail">收件人邮箱</param>
        /// <param name="parameters">模板参数</param>
        /// <returns>发送结果</returns>
        Task<(bool Success, string Message, int? HistoryId)> SendEmailByTemplateAsync(
            int templateId,
            string recipientEmail,
            Dictionary<string, object> parameters);
            
        /// <summary>
        /// 获取邮件发送历史记录
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页条数</param>
        /// <returns>邮件历史记录列表</returns>
        Task<IEnumerable<EmailHistory>> GetEmailHistoryAsync(int pageNumber = 1, int pageSize = 20);
        
        /// <summary>
        /// 获取特定收件人的邮件历史记录
        /// </summary>
        /// <param name="recipientEmail">收件人邮箱</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页条数</param>
        /// <returns>邮件历史记录列表</returns>
        Task<IEnumerable<EmailHistory>> GetEmailHistoryByRecipientAsync(
            string recipientEmail,
            int pageNumber = 1,
            int pageSize = 20);
    }
}
