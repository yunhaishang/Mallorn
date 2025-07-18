using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
// 添加 TestCorrelator 所在的命名空间引用
using Serilog.Sinks.TestCorrelator; 
using Xunit;
using CampusTrade.API.Middleware;

namespace CampusTrade.API.Tests
{
    public class PerformanceMiddlewareTests
    {
        [Fact]
        public async Task PerformanceMiddleware_LogsRequestDuration()
        {
            // 模拟日志记录器
            var testLogger = new LoggerConfiguration()
                .WriteTo.TestCorrelator()
                .CreateLogger();

            // 创建测试主机
            var hostBuilder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    // 替换全局日志记录器
                    Log.Logger = testLogger;
                })
                .Configure(app =>
                {
                    app.UseMiddleware<PerformanceMiddleware>();
                    app.Run(async context =>
                    {
                        // 模拟耗时操作
                        await Task.Delay(200);
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        await context.Response.WriteAsync("Hello, World!");
                    });
                });

            using var testServer = new TestServer(hostBuilder);
            using var client = testServer.CreateClient();

            // 发送请求
            var response = await client.GetAsync("/");

            // 验证响应状态码
            Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

            // 验证日志记录
            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext();
            Assert.NotEmpty(logEvents);
            var logEvent = logEvents[0];
            Assert.Contains("请求: GET / 花费", logEvent.RenderMessage());
        }

        // 其他测试方法保持不变
        // ...
    }
}
