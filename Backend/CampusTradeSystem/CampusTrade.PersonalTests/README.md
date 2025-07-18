# CampusTrade.PersonalTests

个人功能测试项目，用于测试在 CampusTrade 系统中开发的个人功能。

## 项目结构

```
CampusTrade.PersonalTests/
├── Tests/
│   ├── Unit/                 # 单元测试
│   └── Integration/          # 集成测试
├── Fixtures/                 # 测试夹具和基类
├── Helpers/                  # 测试帮助类
├── PersonalFeatureTestRunner.cs  # 个人功能测试运行器
└── CampusTrade.PersonalTests.csproj
```

## 功能特性

- ✅ 与 CampusTrade.API 项目完全集成
- ✅ 支持内存数据库测试
- ✅ 支持真实 Oracle 数据库测试
- ✅ 包含单元测试和集成测试框架
- ✅ 预配置常用测试包 (xUnit, FluentAssertions, Moq)
- ✅ 个人功能测试运行器

## 快速开始

### 1. 运行所有测试

```bash
# 在项目根目录运行
dotnet test
```

### 2. 运行特定测试类

```bash
# 运行用户服务测试
dotnet test --filter "UserServiceTests"

# 运行集成测试
dotnet test --filter "Integration"
```

### 3. 使用个人功能测试运行器

创建一个简单的控制台程序来运行您的个人测试：

```csharp
using CampusTrade.PersonalTests;

// 快速运行所有个人功能测试
await PersonalFeatureTestRunner.QuickRun();
```

## 测试类型

### 单元测试 (Unit Tests)

位于 `Tests/Unit/` 目录，用于测试单个组件或方法：

- `UserServiceTests.cs` - 用户服务相关测试示例

### 集成测试 (Integration Tests)

位于 `Tests/Integration/` 目录，用于测试多个组件的协作：

- `ApiIntegrationTests.cs` - API端点集成测试
- `DatabaseIntegrationTests.cs` - 数据库操作集成测试

## 数据库测试

### 内存数据库

默认使用 Entity Framework 的 InMemory 数据库进行测试，快速且不需要外部依赖。

### Oracle 数据库

可以配置真实的 Oracle 数据库连接进行集成测试。在 `appsettings.Testing.json` 中配置连接字符串：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-oracle-connection-string"
  }
}
```

## 测试数据

使用 `TestDataBuilder` 类来创建测试数据：

```csharp
// 创建测试用户
var user = TestDataBuilder.CreateTestUser("username", "email@test.com");

// 种子测试数据到数据库
await TestDataBuilder.SeedTestDataAsync(context);
```

## 添加新的测试

### 1. 单元测试

在 `Tests/Unit/` 目录下创建新的测试类：

```csharp
public class MyFeatureTests : DatabaseTestBase
{
    [Fact]
    public async Task MyFeature_ShouldWork()
    {
        // Arrange
        // Act
        // Assert
    }
}
```

### 2. 集成测试

在 `Tests/Integration/` 目录下创建新的测试类：

```csharp
public class MyIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MyIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MyApi_ShouldReturnExpectedResult()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/api/my-endpoint");
        // Assert
    }
}
```

## 常用测试模式

### 1. AAA 模式

```csharp
[Fact]
public void TestMethod()
{
    // Arrange - 准备测试数据和环境
    var input = "test input";
    
    // Act - 执行被测试的操作
    var result = MethodUnderTest(input);
    
    // Assert - 验证结果
    result.Should().Be("expected output");
}
```

### 2. 使用 FluentAssertions

```csharp
// 更可读的断言
result.Should().NotBeNull();
result.Should().BeOfType<User>();
result.Count.Should().BeGreaterThan(0);
users.Should().Contain(u => u.Email == "test@test.com");
```

### 3. 使用 Moq 进行模拟

```csharp
var mockService = new Mock<IUserService>();
mockService.Setup(x => x.GetUserAsync(It.IsAny<Guid>()))
           .ReturnsAsync(new User());
```

## 注意事项

1. 测试应该是独立的，不依赖于执行顺序
2. 使用描述性的测试方法名称
3. 每个测试只验证一个具体的行为
4. 清理测试数据，避免测试之间的干扰
5. 使用适当的测试分类 (Unit, Integration, etc.)

## 依赖包

- **xUnit** - 测试框架
- **FluentAssertions** - 流畅的断言库
- **Moq** - 模拟框架
- **Microsoft.AspNetCore.Mvc.Testing** - ASP.NET Core 集成测试
- **Microsoft.EntityFrameworkCore.InMemory** - 内存数据库
- **Oracle.EntityFrameworkCore** - Oracle 数据库支持

## 贡献

在添加新功能时，请确保：

1. 为新功能编写相应的测试
2. 运行所有测试确保没有破坏现有功能
3. 更新相关文档

## 支持

如果在使用过程中遇到问题，请检查：

1. 项目引用是否正确
2. 数据库连接配置是否正确
3. 测试数据是否正确设置
