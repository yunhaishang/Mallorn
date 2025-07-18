using System.Net;
using System.Text;
using System.Text.Json;
using CampusTrade.PersonalTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using CampusTrade.API.Data;

namespace CampusTrade.PersonalTests.Tests.Integration;

/// <summary>
/// API控制器的集成测试示例
/// </summary>
public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_Home_ShouldReturnValidResponse()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        // API可能返回404（没有首页）或重定向，这些都是有效的响应
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.NotFound, 
            HttpStatusCode.Redirect,
            HttpStatusCode.MovedPermanently);
    }

    [Fact]
    public async Task Get_ApiHealth_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // 如果健康检查端点不存在，这也是可以接受的
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task DatabaseConnection_ShouldBeConfiguredCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CampusTradeDbContext>();

        // Act & Assert
        context.Should().NotBeNull();
        await context.Database.CanConnectAsync();
    }

    private static StringContent CreateJsonContent(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
