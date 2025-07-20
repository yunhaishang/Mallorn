using EmailNotificationSystem.Models;
using EmailNotificationSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmailNotificationSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(IEmailService emailService, ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="request">邮件发送请求</param>
        /// <returns>发送结果</returns>
        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] EmailSendRequest request)
        {
            try
            {
                var result = await _emailService.SendEmailAsync(request);
                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        historyId = result.HistoryId
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        historyId = result.HistoryId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送邮件时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"发送邮件失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 使用模板发送邮件
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="request">邮件发送请求</param>
        /// <returns>发送结果</returns>
        [HttpPost("template/{templateId}")]
        public async Task<IActionResult> SendEmailByTemplate(
            int templateId, 
            [FromBody] EmailSendRequest request)
        {
            try
            {
                // 确保使用指定的模板ID
                request.TemplateId = templateId;
                
                var result = await _emailService.SendEmailAsync(request);
                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        historyId = result.HistoryId
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        historyId = result.HistoryId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"使用模板 {templateId} 发送邮件时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"发送邮件失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 获取邮件发送历史
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页条数</param>
        /// <returns>历史记录列表</returns>
        [HttpGet("history")]
        public async Task<IActionResult> GetEmailHistory(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var history = await _emailService.GetEmailHistoryAsync(pageNumber, pageSize);
                return Ok(new
                {
                    success = true,
                    data = history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取邮件历史记录时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"获取历史记录失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 获取特定收件人的邮件历史
        /// </summary>
        /// <param name="email">收件人邮箱</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页条数</param>
        /// <returns>历史记录列表</returns>
        [HttpGet("history/recipient/{email}")]
        public async Task<IActionResult> GetEmailHistoryByRecipient(
            string email,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var history = await _emailService.GetEmailHistoryByRecipientAsync(email, pageNumber, pageSize);
                return Ok(new
                {
                    success = true,
                    data = history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取收件人 {email} 的邮件历史记录时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"获取历史记录失败: {ex.Message}"
                });
            }
        }
    }
}
