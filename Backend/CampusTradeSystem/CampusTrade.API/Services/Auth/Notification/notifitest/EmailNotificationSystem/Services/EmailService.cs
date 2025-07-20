using System.Text.RegularExpressions;
using EmailNotificationSystem.Data;
using EmailNotificationSystem.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace EmailNotificationSystem.Services
{
    /// <summary>
    /// 邮件服务实现
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            EmailDbContext context,
            IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<(bool Success, string Message, int? HistoryId)> SendEmailAsync(EmailSendRequest request)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrEmpty(request.To))
                {
                    return (false, "收件人邮箱不能为空", null);
                }

                // 验证邮箱格式
                if (!IsValidEmail(request.To))
                {
                    return (false, "收件人邮箱格式不正确", null);
                }

                string htmlBody;
                string? textBody = request.TextBody;
                string subject = request.Subject;

                // 如果指定了模板ID，使用模板
                if (request.TemplateId.HasValue && request.TemplateId.Value > 0)
                {
                    var template = await _context.EmailTemplates.FindAsync(request.TemplateId.Value);
                    if (template == null)
                    {
                        return (false, $"模板ID {request.TemplateId} 不存在", null);
                    }

                    // 使用模板的内容
                    htmlBody = template.HtmlBody;
                    textBody ??= template.TextBody;
                    subject = template.Subject;

                    // 替换模板参数
                    if (request.TemplateParameters != null && request.TemplateParameters.Count > 0)
                    {
                        htmlBody = ReplaceTemplateParameters(htmlBody, request.TemplateParameters);
                        if (textBody != null)
                        {
                            textBody = ReplaceTemplateParameters(textBody, request.TemplateParameters);
                        }
                        subject = ReplaceTemplateParameters(subject, request.TemplateParameters);
                    }
                }
                else
                {
                    // 使用请求中提供的内容
                    if (string.IsNullOrEmpty(request.HtmlBody) && string.IsNullOrEmpty(request.TextBody))
                    {
                        return (false, "邮件内容不能为空", null);
                    }

                    htmlBody = request.HtmlBody ?? string.Empty;
                }

                // 创建并发送邮件
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_configuration["Email:SenderName"], _configuration["Email:SenderEmail"]));
                message.To.Add(new MailboxAddress(request.ToName ?? request.To, request.To));
                message.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = htmlBody,
                    TextBody = textBody
                };

                message.Body = builder.ToMessageBody();

                // 发送邮件
                using var client = new SmtpClient();
                await client.ConnectAsync(
                    _configuration["Email:SmtpServer"],
                    int.Parse(_configuration["Email:SmtpPort"] ?? "587"),
                    bool.Parse(_configuration["Email:EnableSsl"] ?? "true") ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

                await client.AuthenticateAsync(_configuration["Email:Username"], _configuration["Email:Password"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                // 记录发送历史
                var history = new EmailHistory
                {
                    TemplateId = request.TemplateId,
                    RecipientEmail = request.To,
                    RecipientName = request.ToName,
                    Subject = subject,
                    SentAt = DateTime.UtcNow,
                    Status = "Success",
                    Parameters = request.TemplateParameters != null
                        ? System.Text.Json.JsonSerializer.Serialize(request.TemplateParameters)
                        : null
                };

                _context.EmailHistories.Add(history);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"邮件发送成功 - 收件人: {request.To}, 主题: {subject}");
                return (true, "邮件发送成功", history.Id);
            }
            catch (Exception ex)
            {
                // 记录失败历史
                var history = new EmailHistory
                {
                    TemplateId = request.TemplateId,
                    RecipientEmail = request.To,
                    RecipientName = request.ToName,
                    Subject = request.Subject,
                    SentAt = DateTime.UtcNow,
                    Status = "Failed",
                    ErrorMessage = ex.Message,
                    Parameters = request.TemplateParameters != null
                        ? System.Text.Json.JsonSerializer.Serialize(request.TemplateParameters)
                        : null
                };

                _context.EmailHistories.Add(history);
                await _context.SaveChangesAsync();

                _logger.LogError(ex, $"邮件发送失败 - 收件人: {request.To}, 主题: {request.Subject}");
                return (false, $"邮件发送失败: {ex.Message}", history.Id);
            }
        }

        /// <inheritdoc />
        public async Task<(bool Success, string Message, int? HistoryId)> SendEmailByTemplateAsync(
            int templateId,
            string recipientEmail,
            Dictionary<string, object> parameters)
        {
            var request = new EmailSendRequest
            {
                To = recipientEmail,
                TemplateId = templateId,
                TemplateParameters = parameters
            };

            return await SendEmailAsync(request);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<EmailHistory>> GetEmailHistoryAsync(int pageNumber = 1, int pageSize = 20)
        {
            return await _context.EmailHistories
                .Include(h => h.Template)
                .OrderByDescending(h => h.SentAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<EmailHistory>> GetEmailHistoryByRecipientAsync(
            string recipientEmail,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await _context.EmailHistories
                .Include(h => h.Template)
                .Where(h => h.RecipientEmail == recipientEmail)
                .OrderByDescending(h => h.SentAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// 替换模板参数
        /// </summary>
        /// <param name="template">模板内容</param>
        /// <param name="parameters">参数字典</param>
        /// <returns>替换后的内容</returns>
        private string ReplaceTemplateParameters(string template, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(template) || parameters == null || parameters.Count == 0)
            {
                return template;
            }

            string result = template;

            foreach (var param in parameters)
            {
                result = Regex.Replace(
                    result,
                    $@"{{\s*{param.Key}\s*}}",
                    param.Value?.ToString() ?? string.Empty,
                    RegexOptions.IgnoreCase);
            }

            return result;
        }

        /// <summary>
        /// 验证邮箱格式
        /// </summary>
        /// <param name="email">邮箱地址</param>
        /// <returns>是否有效</returns>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // 使用正则表达式验证邮箱格式
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
