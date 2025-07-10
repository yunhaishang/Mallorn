using Microsoft.EntityFrameworkCore;
using CampusTrade.API.Data;
using CampusTrade.API.Extensions;
using CampusTrade.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Campus Trade API",
        Version = "v1.0",
        Description = "校园交易平台后端API文档",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Campus Trade Team",
            Email = "admin@campustrade.com"
        }
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Include XML comments (optional)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// 添加 Oracle 数据库连接
builder.Services.AddDbContext<CampusTradeDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("DefaultConnection")));

// 添加JWT认证和Token服务（使用扩展方法）
builder.Services.AddJwtAuthentication(builder.Configuration);

// 添加认证相关服务
builder.Services.AddAuthenticationServices();

// 配置 CORS（使用扩展方法）
builder.Services.AddCorsPolicy(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // 在开发环境下，先配置Swagger，避免被其他中间件影响
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Campus Trade API v1.0");
        c.RoutePrefix = string.Empty; // 将Swagger UI设置为根路径
        c.DocumentTitle = "Campus Trade API Documentation";
        c.DefaultModelsExpandDepth(-1); // 隐藏模型
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None); // 默认折叠所有操作
        
        // 自定义HTML模板
        c.IndexStream = () => {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Campus Trade API Documentation</title>
    <link rel='stylesheet' type='text/css' href='./swagger-ui.css' />
    <style>
        html { box-sizing: border-box; overflow: -moz-scrollbars-vertical; overflow-y: scroll; }
        *, *:before, *:after { box-sizing: inherit; }
        body { margin:0; background: #fafafa; }
    </style>
</head>
<body>
    <div id='swagger-ui'></div>
    <script src='./swagger-ui-bundle.js'></script>
    <script src='./swagger-ui-standalone-preset.js'></script>
    <script>
        window.onload = function() {
            const ui = SwaggerUIBundle({
                url: '/swagger/v1/swagger.json',
                dom_id: '#swagger-ui',
                deepLinking: true,
                presets: [
                    SwaggerUIBundle.presets.apis,
                    SwaggerUIStandalonePreset
                ],
                plugins: [
                    SwaggerUIBundle.plugins.DownloadUrl
                ],
                layout: 'StandaloneLayout'
            });
        }
    </script>
</body>
</html>"));
        };
    });
}
else
{
    app.UseHsts();
}

// 使用全局异常处理中间件
app.UseGlobalExceptionHandler();

// 使用安全检查中间件
app.UseSecurity();

// 在开发环境下禁用HTTPS重定向，避免影响Swagger
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

// 启用 CORS
app.UseCors("CampusTradeCors");

// 启用JWT验证中间件（在认证之前）
app.UseJwtValidation();

// 启用认证和授权
app.UseAuthentication();
app.UseAuthorization();

// 配置API路由
app.MapControllers();

// 自动创建数据库
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CampusTradeDbContext>();
    try
    {
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "创建数据库时发生错误");
    }
}

app.Run();

// 使Program类可供测试访问
public partial class Program { }
