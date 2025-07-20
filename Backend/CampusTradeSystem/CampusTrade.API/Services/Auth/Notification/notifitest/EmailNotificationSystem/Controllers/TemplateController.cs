using EmailNotificationSystem.Models;
using EmailNotificationSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmailNotificationSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TemplateController : ControllerBase
    {
        private readonly ITemplateService _templateService;
        private readonly ILogger<TemplateController> _logger;

        public TemplateController(ITemplateService templateService, ILogger<TemplateController> logger)
        {
            _templateService = templateService;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有模板
        /// </summary>
        /// <param name="includeInactive">是否包含未激活的模板</param>
        /// <returns>模板列表</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllTemplates([FromQuery] bool includeInactive = false)
        {
            try
            {
                var templates = await _templateService.GetAllTemplatesAsync(includeInactive);
                return Ok(new
                {
                    success = true,
                    data = templates
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有模板时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"获取模板失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 获取特定模板
        /// </summary>
        /// <param name="id">模板ID</param>
        /// <returns>模板</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTemplate(int id)
        {
            try
            {
                var template = await _templateService.GetTemplateByIdAsync(id);
                if (template == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"未找到ID为 {id} 的模板"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = template
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取模板 {id} 时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"获取模板失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 通过名称获取模板
        /// </summary>
        /// <param name="name">模板名称</param>
        /// <returns>模板</returns>
        [HttpGet("name/{name}")]
        public async Task<IActionResult> GetTemplateByName(string name)
        {
            try
            {
                var template = await _templateService.GetTemplateByNameAsync(name);
                if (template == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"未找到名称为 '{name}' 的模板"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = template
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取模板 '{name}' 时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"获取模板失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 创建模板
        /// </summary>
        /// <param name="template">模板实体</param>
        /// <returns>创建的模板</returns>
        [HttpPost]
        public async Task<IActionResult> CreateTemplate([FromBody] EmailTemplate template)
        {
            try
            {
                var createdTemplate = await _templateService.CreateTemplateAsync(template);
                return CreatedAtAction(
                    nameof(GetTemplate),
                    new { id = createdTemplate.Id },
                    new
                    {
                        success = true,
                        data = createdTemplate
                    });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建模板时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"创建模板失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 更新模板
        /// </summary>
        /// <param name="id">模板ID</param>
        /// <param name="template">更新后的模板实体</param>
        /// <returns>更新后的模板</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate(int id, [FromBody] EmailTemplate template)
        {
            if (id != template.Id)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "路径ID与模板ID不匹配"
                });
            }

            try
            {
                var updatedTemplate = await _templateService.UpdateTemplateAsync(template);
                if (updatedTemplate == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"未找到ID为 {id} 的模板"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = updatedTemplate
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新模板 {id} 时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"更新模板失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        /// <param name="id">模板ID</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            try
            {
                var result = await _templateService.DeleteTemplateAsync(id);
                if (!result)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"未找到ID为 {id} 的模板"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = $"成功删除ID为 {id} 的模板"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除模板 {id} 时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"删除模板失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 设置模板状态
        /// </summary>
        /// <param name="id">模板ID</param>
        /// <param name="isActive">是否启用</param>
        /// <returns>更新后的模板</returns>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> SetTemplateStatus(int id, [FromQuery] bool isActive)
        {
            try
            {
                var template = await _templateService.SetTemplateStatusAsync(id, isActive);
                if (template == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"未找到ID为 {id} 的模板"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = $"模板 '{template.Name}' 已{(isActive ? "启用" : "禁用")}",
                    data = template
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"设置模板 {id} 状态时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"设置模板状态失败: {ex.Message}"
                });
            }
        }
    }
}
