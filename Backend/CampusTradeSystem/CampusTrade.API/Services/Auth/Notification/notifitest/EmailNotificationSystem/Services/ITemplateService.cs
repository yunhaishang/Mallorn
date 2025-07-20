using EmailNotificationSystem.Models;

namespace EmailNotificationSystem.Services
{
    /// <summary>
    /// 邮件模板管理接口
    /// </summary>
    public interface ITemplateService
    {
        /// <summary>
        /// 获取所有模板
        /// </summary>
        /// <param name="includeInactive">是否包含未激活的模板</param>
        /// <returns>模板列表</returns>
        Task<IEnumerable<EmailTemplate>> GetAllTemplatesAsync(bool includeInactive = false);
        
        /// <summary>
        /// 获取特定模板
        /// </summary>
        /// <param name="id">模板ID</param>
        /// <returns>模板</returns>
        Task<EmailTemplate?> GetTemplateByIdAsync(int id);
        
        /// <summary>
        /// 通过名称获取模板
        /// </summary>
        /// <param name="name">模板名称</param>
        /// <returns>模板</returns>
        Task<EmailTemplate?> GetTemplateByNameAsync(string name);
        
        /// <summary>
        /// 创建新模板
        /// </summary>
        /// <param name="template">模板实体</param>
        /// <returns>创建的模板</returns>
        Task<EmailTemplate> CreateTemplateAsync(EmailTemplate template);
        
        /// <summary>
        /// 更新模板
        /// </summary>
        /// <param name="template">更新后的模板实体</param>
        /// <returns>更新后的模板</returns>
        Task<EmailTemplate?> UpdateTemplateAsync(EmailTemplate template);
        
        /// <summary>
        /// 删除模板
        /// </summary>
        /// <param name="id">模板ID</param>
        /// <returns>删除结果</returns>
        Task<bool> DeleteTemplateAsync(int id);
        
        /// <summary>
        /// 启用/禁用模板
        /// </summary>
        /// <param name="id">模板ID</param>
        /// <param name="isActive">是否启用</param>
        /// <returns>更新后的模板</returns>
        Task<EmailTemplate?> SetTemplateStatusAsync(int id, bool isActive);
    }
}
