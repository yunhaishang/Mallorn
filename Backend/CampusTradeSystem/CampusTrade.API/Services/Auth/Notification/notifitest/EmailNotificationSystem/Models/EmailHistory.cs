using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailNotificationSystem.Models
{
    /// <summary>
    /// 邮件发送历史记录
    /// </summary>
    public class EmailHistory
    {
        /// <summary>
        /// 邮件历史ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 关联的模板ID (可选)
        /// </summary>
        public int? TemplateId { get; set; }

        /// <summary>
        /// 关联的模板 (可选)
        /// </summary>
        [ForeignKey("TemplateId")]
        public EmailTemplate? Template { get; set; }

        /// <summary>
        /// 收件人邮箱
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string RecipientEmail { get; set; } = string.Empty;

        /// <summary>
        /// 收件人姓名 (可选)
        /// </summary>
        [MaxLength(100)]
        public string? RecipientName { get; set; }

        /// <summary>
        /// 主题
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// 发送时间
        /// </summary>
        [Required]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 发送状态
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// 错误信息 (如果有)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 发送参数 (JSON格式)
        /// </summary>
        public string? Parameters { get; set; }
    }
}
