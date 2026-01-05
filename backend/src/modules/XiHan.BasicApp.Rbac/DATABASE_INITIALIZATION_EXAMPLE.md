# Rbac 模块数据库初始化使用示例

## 配置和使用

本文档展示如何在 XiHan.BasicApp.Rbac 模块中使用数据库初始化和种子数据功能。

### 1. 实体类型注册

在应用的主模块或 Program.cs 中配置：

```csharp
using XiHan.Framework.Data.SqlSugar.Extensions;
using XiHan.BasicApp.Rbac.Entities;
using SqlSugar;

// 配置 SqlSugar 数据访问
services.AddXiHanDataSqlSugar(options =>
{
    // 数据库连接
    options.ConnectionConfigs.Add(new SqlSugarConnectionConfig
    {
        ConfigId = "Default",
        ConnectionString = configuration.GetConnectionString("DefaultConnection")!,
        DbType = DbType.SqlServer,  // 或 DbType.MySQL, DbType.PostgreSQL 等
        IsAutoCloseConnection = true,
        InitKeyType = InitKeyType.Attribute
    });

    // 注册 Rbac 模块的所有实体类型
    options.EntityTypes.AddRange(new[]
    {
        // 用户相关
        typeof(SysUser),
        typeof(SysUserRole),
        typeof(SysUserPermission),
        typeof(SysUserDepartment),
        typeof(SysUserSecurity),
        typeof(SysUserSession),
        typeof(SysUserStatistics),

        // 角色相关
        typeof(SysRole),
        typeof(SysRolePermission),
        typeof(SysRoleMenu),
        typeof(SysRoleDataScope),

        // 权限和菜单
        typeof(SysPermission),
        typeof(SysMenu),

        // 组织架构
        typeof(SysDepartment),
        typeof(SysTenant),

        // 日志
        typeof(SysAccessLog),
        typeof(SysApiLog),
        typeof(SysAuditLog),
        typeof(SysLoginLog),
        typeof(SysOperationLog),
        typeof(SysTaskLog),

        // 系统配置
        typeof(SysConfig),
        typeof(SysDict),
        typeof(SysDictItem),
        typeof(SysFile),
        typeof(SysEmail),
        typeof(SysSms),
        typeof(SysNotification),

        // 任务和审计
        typeof(SysTask),
        typeof(SysAudit),

        // OAuth
        typeof(SysOAuthApp),
        typeof(SysOAuthCode),
        typeof(SysOAuthToken)
    });

    // 启用功能
    options.EnableDbInitialization = true;
    options.EnableDataSeeding = true;

    // 开发环境启用SQL日志
    if (environment.IsDevelopment())
    {
        options.EnableSqlLog = true;
        options.SqlLogAction = (sql, parameters) =>
        {
            Console.WriteLine($"[SQL] {sql}");
            if (parameters?.Length > 0)
            {
                Console.WriteLine($"[参数] {string.Join(", ", parameters.Select(p => $"{p.ParameterName}={p.Value}"))}");
            }
        };
    }
});
```

### 2. 在 Program.cs 中初始化数据库

```csharp
var builder = WebApplication.CreateBuilder(args);

// ... 服务配置 ...

var app = builder.Build();

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // 开发环境自动初始化数据库
    await app.UseDbInitializerAsync(initialize: true);
}
else
{
    // 生产环境不自动初始化，或通过配置控制
    var autoInit = builder.Configuration.GetValue<bool>("Database:AutoInitialize", false);
    if (autoInit)
    {
        await app.UseDbInitializerAsync(initialize: true);
    }
}

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 3. 通过配置文件控制

**appsettings.Development.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=XiHanBasicApp;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Database": {
    "AutoInitialize": true,
    "EnableSeeding": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "XiHan.Framework.Data.SqlSugar": "Debug"
    }
  }
}
```

**appsettings.Production.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=XiHanBasicApp;User Id=sa;Password=***;"
  },
  "Database": {
    "AutoInitialize": false,
    "EnableSeeding": false
  }
}
```

### 4. 种子数据执行顺序

Rbac 模块的种子数据按以下顺序执行：

1. **SysRoleSeeder** (Order: 10) - 系统角色

   - SuperAdmin (超级管理员)
   - Admin (管理员)
   - User (普通用户)

2. **SysUserSeeder** (Order: 20) - 系统用户

   - admin (管理员账号, 密码: Admin@123)
   - test (测试用户, 密码: Test@123)

3. **SysUserRoleSeeder** (Order: 30) - 用户角色关系
   - admin 用户 -> SuperAdmin 角色
   - test 用户 -> User 角色

### 5. 初始化后的默认数据

#### 系统角色

| 角色编码   | 角色名称   | 角色类型   | 数据权限范围 |
| ---------- | ---------- | ---------- | ------------ |
| SuperAdmin | 超级管理员 | 系统角色   | 全部数据     |
| Admin      | 管理员     | 系统角色   | 自定义       |
| User       | 普通用户   | 自定义角色 | 仅本人       |

#### 系统用户

| 用户名 | 密码      | 真实姓名   | 邮箱            | 角色       |
| ------ | --------- | ---------- | --------------- | ---------- |
| admin  | Admin@123 | 系统管理员 | admin@xihan.com | SuperAdmin |
| test   | Test@123  | 测试用户   | test@xihan.com  | User       |

### 6. 手动触发初始化

创建一个管理控制器用于手动初始化：

```csharp
using Microsoft.AspNetCore.Mvc;
using XiHan.Framework.Data.SqlSugar.Initializers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly IDbInitializer _dbInitializer;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(IDbInitializer dbInitializer, ILogger<DatabaseController> logger)
    {
        _dbInitializer = dbInitializer;
        _logger = logger;
    }

    /// <summary>
    /// 完整初始化数据库
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize()
    {
        try
        {
            await _dbInitializer.InitializeAsync();
            return Ok(new { success = true, message = "数据库初始化成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库初始化失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 仅创建表结构
    /// </summary>
    [HttpPost("create-tables")]
    public async Task<IActionResult> CreateTables()
    {
        try
        {
            await _dbInitializer.CreateTablesAsync();
            return Ok(new { success = true, message = "表结构创建成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "表结构创建失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 仅执行种子数据
    /// </summary>
    [HttpPost("seed-data")]
    public async Task<IActionResult> SeedData()
    {
        try
        {
            await _dbInitializer.SeedDataAsync();
            return Ok(new { success = true, message = "种子数据执行成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "种子数据执行失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
```

### 7. 查看初始化日志

运行应用后，您会看到类似的日志输出：

```
[INF] ==================== 开始数据库初始化 ====================
[INF] 数据库创建成功
[INF] 开始创建表结构，共 33 个实体
[INF] 表结构创建成功，共创建 33 个表
[INF] 开始执行种子数据，共 3 个种子数据提供者
[INF] 开始执行种子数据: 系统角色种子数据
[INF] 已插入 3 条 SysRole 数据
[INF] 种子数据执行成功: 系统角色种子数据
[INF] 开始执行种子数据: 系统用户种子数据
[INF] 已插入 2 条 SysUser 数据
[INF] 种子数据执行成功: 系统用户种子数据
[INF] 开始执行种子数据: 系统用户角色关系种子数据
[INF] 已插入 2 条 SysUserRole 数据
[INF] 种子数据执行成功: 系统用户角色关系种子数据
[INF] 种子数据执行完成
[INF] ==================== 数据库初始化完成 ====================
```

### 8. 常见问题

#### Q: 如何在已有数据库上运行？

A: 种子数据会自动检查数据是否存在，不会重复插入。表结构会自动对比差异并更新。

#### Q: 如何添加自定义种子数据？

A: 创建新的 Seeder 类继承 `DataSeederBase`，并在模块中注册：

```csharp
services.AddDataSeeder<MyCustomSeeder>();
```

#### Q: 如何禁用某些种子数据？

A: 在配置中不注册对应的 Seeder，或在 Seeder 的 `SeedInternal` 方法开始处直接返回。

#### Q: 生产环境如何管理数据库变更？

A: 建议使用专业的数据库迁移工具（如 FluentMigrator），不要在生产环境启用自动初始化。

---

**提示**: 首次运行会创建所有表结构并插入初始数据，后续运行会跳过已存在的数据。
