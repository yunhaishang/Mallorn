using EmailNotificationSystem.Data;
using EmailNotificationSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailNotificationSystem.Services
{
    /// <summary>
    /// 邮件模板管理服务实现
    /// </summary>
    public class TemplateService : ITemplateService
    {
        private readonly EmailDbContext _context;
        private readonly ILogger<TemplateService> _logger;

        public TemplateService(EmailDbContext context, ILogger<TemplateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<EmailTemplate>> GetAllTemplatesAsync(bool includeInactive = false)
        {
            if (includeInactive)
            {
                return await _context.EmailTemplates
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            else
            {
                return await _context.EmailTemplates
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
        }

        /// <inheritdoc />
        public async Task<EmailTemplate?> GetTemplateByIdAsync(int id)
        {
            return await _context.EmailTemplates.FindAsync(id);
        }

        /// <inheritdoc />
        public async Task<EmailTemplate?> GetTemplateByNameAsync(string name)
        {
            return await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.Name == name);
        }

        /// <inheritdoc />
        public async Task<EmailTemplate> CreateTemplateAsync(EmailTemplate template)
        {
            // 检查名称是否已存在
            var existing = await _context.EmailTemplates
                .AnyAsync(t => t.Name == template.Name);
                
            if (existing)
            {
                throw new InvalidOperationException($"模板名称 '{template.Name}' 已存在");
            }

            template.CreatedAt = DateTime.UtcNow;
            _context.EmailTemplates.Add(template);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"创建了新模板: {template.Name} (ID: {template.Id})");
            
            return template;
        }

        /// <inheritdoc />
        public async Task<EmailTemplate?> UpdateTemplateAsync(EmailTemplate template)
        {
            var existingTemplate = await _context.EmailTemplates.FindAsync(template.Id);
            if (existingTemplate == null)
            {
                return null;
            }

            // 检查名称是否与其他模板冲突
            var nameConflict = await _context.EmailTemplates
                .AnyAsync(t => t.Name == template.Name && t.Id != template.Id);
                
            if (nameConflict)
            {
                throw new InvalidOperationException($"模板名称 '{template.Name}' 已被其他模板使用");
            }

            existingTemplate.Name = template.Name;
            existingTemplate.Subject = template.Subject;
            existingTemplate.HtmlBody = template.HtmlBody;
            existingTemplate.TextBody = template.TextBody;
            existingTemplate.IsActive = template.IsActive;
            existingTemplate.Description = template.Description;
            existingTemplate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"更新了模板: {template.Name} (ID: {template.Id})");
            
            return existingTemplate;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteTemplateAsync(int id)
        {
            var template = await _context.EmailTemplates.FindAsync(id);
            if (template == null)
            {
                return false;
            }

            _context.EmailTemplates.Remove(template);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"删除了模板: {template.Name} (ID: {template.Id})");
            
            return true;
        }

        /// <inheritdoc />
        public async Task<EmailTemplate?> SetTemplateStatusAsync(int id, bool isActive)
        {
            var template = await _context.EmailTemplates.FindAsync(id);
            if (template == null)
            {
                return null;
            }

            template.IsActive = isActive;
            template.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"{(isActive ? "启用" : "禁用")}了模板: {template.Name} (ID: {template.Id})");
            
            return template;
        }
    }
}
