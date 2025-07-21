using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ScheduledTaskController : ControllerBase
    {
        private readonly ILogger<ScheduledTaskController> _logger;

        public ScheduledTaskController(ILogger<ScheduledTaskController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取定时任务状态
        /// </summary>
        /// <returns>任务状态</returns>
        [HttpGet("TaskStatus")]
        public IActionResult GetTaskStatus()
        {
            // 这里可以添加获取定时任务状态的逻辑，例如从日志中读取任务执行情况

            _logger.LogInformation("获取定时任务状态");
            return Ok("定时任务监控 API 正常运行");
        }
    }
}
