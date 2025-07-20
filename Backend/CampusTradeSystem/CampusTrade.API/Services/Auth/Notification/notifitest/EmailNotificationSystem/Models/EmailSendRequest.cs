namespace EmailNotificationSystem.Models
{
    /// <summary>
    /// 邮件发送请求模型
    /// </summary>
    public class EmailSendRequest
    {
        /// <summary>
        /// 收件人邮箱
        /// </summary>
        public string To { get; set; } = string.Empty;

        /// <summary>
        /// 收件人姓名 (可选)
        /// </summary>
        public string? ToName { get; set; }

        /// <summary>
        /// 邮件主题
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// 邮件内容 (HTML格式)
        /// </summary>
        public string? HtmlBody { get; set; }

        /// <summary>
        /// 邮件内容 (纯文本格式)
        /// </summary>
        public string? TextBody { get; set; }

        /// <summary>
        /// 模板ID (如果使用模板)
        /// </summary>
        public int? TemplateId { get; set; }

        /// <summary>
        /// 模板参数 (键值对)
        /// </summary>
        public Dictionary<string, object>? TemplateParameters { get; set; }
    }
}
