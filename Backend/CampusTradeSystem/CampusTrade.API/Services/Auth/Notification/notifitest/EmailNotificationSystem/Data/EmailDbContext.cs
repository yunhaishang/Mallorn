using EmailNotificationSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailNotificationSystem.Data
{
    /// <summary>
    /// 邮件通知系统数据库上下文
    /// </summary>
    public class EmailDbContext : DbContext
    {
        public EmailDbContext(DbContextOptions<EmailDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// 邮件模板
        /// </summary>
        public DbSet<EmailTemplate> EmailTemplates { get; set; } = null!;

        /// <summary>
        /// 邮件发送历史
        /// </summary>
        public DbSet<EmailHistory> EmailHistories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置邮件模板表
            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.ToTable("EmailTemplates");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // 配置邮件历史表
            modelBuilder.Entity<EmailHistory>(entity =>
            {
                entity.ToTable("EmailHistories");
                entity.HasIndex(e => e.SentAt);
            });

            // 添加示例模板数据
            modelBuilder.Entity<EmailTemplate>().HasData(
                new EmailTemplate
                {
                    Id = 1,
                    Name = "WelcomeEmail",
                    Subject = "欢迎使用校园交易系统",
                    HtmlBody = "<h1>欢迎 {{UserName}}!</h1><p>感谢您注册校园交易系统。</p><p>您的账号现在已经激活，您可以开始使用我们的服务了。</p>",
                    TextBody = "欢迎 {{UserName}}!\n\n感谢您注册校园交易系统。\n\n您的账号现在已经激活，您可以开始使用我们的服务了。",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Description = "用户注册后发送的欢迎邮件"
                },
                new EmailTemplate
                {
                    Id = 2,
                    Name = "OrderConfirmation",
                    Subject = "订单确认 - 订单号: {{OrderNumber}}",
                    HtmlBody = "<h1>订单确认</h1><p>您好 {{UserName}},</p><p>您的订单 #{{OrderNumber}} 已确认。</p><p>订单总额: ¥{{Amount}}</p><p>预计交付时间: {{DeliveryTime}}</p>",
                    TextBody = "订单确认\n\n您好 {{UserName}},\n\n您的订单 #{{OrderNumber}} 已确认。\n\n订单总额: ¥{{Amount}}\n\n预计交付时间: {{DeliveryTime}}",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Description = "订单确认邮件"
                }
            );
        }
    }
}
