# XiHan.BasicApp.Rbac 适配器

本目录包含了将 XiHan.BasicApp.Rbac 模块适配到 XiHan.Framework.Authentication 和 XiHan.Framework.Authorization 框架的实现。

## 架构说明

```
XiHan.Framework.Authentication/Authorization (框架层)
                    ↑
                    | 适配
                    |
        Adapters (适配器层)
                    ↑
                    | 使用
                    |
    XiHan.BasicApp.Rbac (业务层)
```

## 适配器列表

### Authentication 适配器

#### RbacAuthenticationService

实现 `IAuthenticationService` 接口，提供完整的身份认证功能。

**功能：**

- 用户名密码认证
- 密码强度验证
- 密码更改和重置
- 双因素认证（2FA）
- 账户锁定管理
- 登录失败记录

**依赖：**

- `ISysUserRepository`
- `ISysUserSecurityRepository`
- `ISysUserService`
- `IPasswordHasher`
- `IJwtTokenService`
- `IOtpService`

### Authorization 适配器

#### RbacPermissionStore

实现 `IPermissionStore` 接口，提供权限数据的存储和检索。

**功能：**

- 获取用户权限
- 获取角色权限
- 授予/撤销用户权限
- 授予/撤销角色权限
- 权限定义管理

#### RbacRoleStore

实现 `IRoleStore` 接口，提供角色数据的存储和检索。

**功能：**

- 获取用户角色
- 检查用户角色
- 添加/移除用户角色
- 角色定义管理
- 获取角色中的用户

#### RbacRoleManager

实现 `IRoleManager` 接口，提供角色管理功能。

**功能：**

- 创建/更新/删除角色
- 获取所有角色
- 角色成员管理
- 角色存在性检查

#### RbacPolicyStore

实现 `IPolicyStore` 接口，提供策略定义的存储。

**功能：**

- 策略定义管理
- 默认策略初始化
- 策略的 CRUD 操作

**默认策略：**

- `AdminPolicy` - 管理员策略
- `UserManagementPolicy` - 用户管理策略
- `RoleManagementPolicy` - 角色管理策略

#### RbacPolicyEvaluator

实现 `IPolicyEvaluator` 接口，提供策略评估功能。

**功能：**

- 单个策略评估
- 多策略评估（全部/任意）
- 角色要求检查
- 权限要求检查
- 自定义要求评估

## 使用示例

### 1. 用户认证

```csharp
public class LoginController
{
    private readonly IAuthenticationService _authenticationService;

    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authenticationService.AuthenticateAsync(
            request.Username,
            request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.ErrorMessage);
        }

        if (result.RequiresTwoFactor)
        {
            return Ok(new { requiresTwoFactor = true });
        }

        return Ok(result.TokenResult);
    }
}
```

### 2. 权限检查

```csharp
public class UserController
{
    private readonly IAuthorizationService _authorizationService;

    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 检查权限
        var authResult = await _authorizationService.AuthorizeAsync(
            userId, "User.Create");

        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        // 创建用户...
    }
}
```

### 3. 角色检查

```csharp
public class AdminController
{
    private readonly IAuthorizationService _authorizationService;

    public async Task<IActionResult> AdminAction()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 检查是否为管理员
        var authResult = await _authorizationService.AuthorizeRoleAsync(
            userId, "Admin");

        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        // 执行管理员操作...
    }
}
```

### 4. 策略授权

```csharp
public class DocumentController
{
    private readonly IAuthorizationService _authorizationService;

    public async Task<IActionResult> UpdateDocument(long id, UpdateDocumentDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var document = await _documentService.GetByIdAsync(id);

        // 使用策略检查
        var authResult = await _authorizationService.AuthorizePolicyAsync(
            userId, "DocumentOwnerPolicy", document);

        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        // 更新文档...
    }
}
```

### 5. 双因素认证

```csharp
public class TwoFactorController
{
    private readonly IAuthenticationService _authenticationService;

    // 启用 2FA
    public async Task<IActionResult> EnableTwoFactor()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _authenticationService.EnableTwoFactorAuthenticationAsync(userId);

        return Ok(new
        {
            qrCodeUri = result.QrCodeUri,
            secret = result.Secret,
            recoveryCodes = result.RecoveryCodes
        });
    }

    // 验证 2FA 代码
    public async Task<IActionResult> VerifyTwoFactor(string code)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var isValid = await _authenticationService.VerifyTwoFactorCodeAsync(userId, code);

        if (!isValid)
        {
            return BadRequest("验证码无效");
        }

        return Ok();
    }
}
```

### 6. 角色管理

```csharp
public class RoleManagementController
{
    private readonly IRoleManager _roleManager;

    // 创建角色
    public async Task<IActionResult> CreateRole(CreateRoleRequest request)
    {
        var role = new RoleDefinition
        {
            Name = request.RoleCode,
            DisplayName = request.RoleName,
            Description = request.Description
        };

        var result = await _roleManager.CreateRoleAsync(role);

        if (!result.Succeeded)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Role);
    }

    // 分配角色
    public async Task<IActionResult> AssignRole(long userId, string roleName)
    {
        var result = await _roleManager.AddUserToRoleAsync(
            userId.ToString(), roleName);

        if (!result.Succeeded)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok();
    }
}
```

## 配置说明

### appsettings.json

```json
{
  "Authentication": {
    "PasswordHasher": {
      "Iterations": 600000,
      "SaltSize": 32,
      "HashSize": 32
    },
    "PasswordPolicy": {
      "MinimumLength": 8,
      "RequireUppercase": true,
      "RequireLowercase": true,
      "RequireDigit": true,
      "RequireSpecialCharacter": true,
      "MaxFailedAccessAttempts": 5,
      "LockoutDurationMinutes": 30
    },
    "Jwt": {
      "SecretKey": "your-secret-key-at-least-32-characters-long",
      "Issuer": "XiHanBasicApp",
      "Audience": "XiHanBasicAppUsers",
      "AccessTokenExpirationMinutes": 60,
      "RefreshTokenExpirationDays": 7
    },
    "Otp": {
      "Digits": 6,
      "TimeStep": 30,
      "AllowedSkew": 1
    }
  }
}
```

## 扩展说明

### 添加自定义策略

1. 在 `RbacPolicyStore` 中添加策略定义：

```csharp
_policies["MyCustomPolicy"] = new PolicyDefinition
{
    Name = "MyCustomPolicy",
    DisplayName = "我的自定义策略",
    Description = "策略描述",
    RequiredPermissions = ["Permission1", "Permission2"],
    IsEnabled = true
};
```

### 添加自定义授权要求

1. 实现 `IAuthorizationRequirement` 接口：

```csharp
public class MyCustomRequirement : IAuthorizationRequirement
{
    public string Name => "MyCustomRequirement";

    public Task<bool> EvaluateAsync(AuthorizationContext context)
    {
        // 实现自定义逻辑
        return Task.FromResult(true);
    }
}
```

2. 在策略中使用：

```csharp
policy.CustomRequirements.Add(new MyCustomRequirement());
```

## 注意事项

1. **安全性**

   - JWT 密钥至少 32 字符
   - 使用环境变量存储敏感配置
   - 定期更新密钥

2. **性能**

   - 权限检查结果可以缓存
   - 策略评估可以异步并行

3. **可扩展性**
   - 所有适配器都可以被替换
   - 可以添加更多的策略和要求
   - 支持自定义权限检查逻辑

## 许可证

MIT License
