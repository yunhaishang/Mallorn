using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CampusTrade.API.Services.ScheduledTasks
{
    public abstract class ScheduledService : IHostedService, IDisposable
    {
        protected readonly ILogger<ScheduledService> _logger;
        private Timer _timer;
        protected abstract TimeSpan Interval { get; }

        public ScheduledService(ILogger<ScheduledService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("定时任务开始启动");
            _timer = new Timer(ExecuteTask, null, TimeSpan.Zero, Interval);
            return Task.CompletedTask;
        }

        private void ExecuteTask(object state)
        {
            try
            {
                _logger.LogInformation("定时任务开始执行");
                ExecuteTaskAsync().GetAwaiter().GetResult();
                _logger.LogInformation("定时任务执行完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "定时任务执行出错");
            }
        }

        protected abstract Task ExecuteTaskAsync();

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("定时任务开始停止");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
