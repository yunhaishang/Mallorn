using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailNotificationSystem.Models
{
    /// <summary>
    /// 邮件模板实体类
    /// </summary>
    public class EmailTemplate
    {
        /// <summary>
        /// 模板ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 模板主题
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// 模板内容 (HTML格式)
        /// </summary>
        [Required]
        public string HtmlBody { get; set; } = string.Empty;

        /// <summary>
        /// 模板内容 (纯文本格式)
        /// </summary>
        public string? TextBody { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 描述
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
