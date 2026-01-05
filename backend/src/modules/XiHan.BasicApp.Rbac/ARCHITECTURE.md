# XiHan.BasicApp.Rbac 架构说明

## 📐 分层架构

```
┌─────────────────────────────────────────────┐
│         Controller / Application Service     │  应用层
└─────────────────────┬───────────────────────┘
                      ↓
┌─────────────────────────────────────────────┐
│            Adapters（适配器层）              │  适配层
│  - RbacAuthenticationService                │  - 实现框架接口
│  - RbacRoleManager                           │  - 数据转换
│  - RbacPermissionStore                       │  - 编排调用
│  - RbacRoleStore                             │
│  - RbacPolicyEvaluator                       │
└─────────────────────┬───────────────────────┘
                      ↓
┌─────────────────────────────────────────────┐
│          Managers（领域管理器）              │  领域层
│  - UserManager                               │  - 业务规则
│  - RoleManager                               │  - 验证逻辑
│  - PermissionManager                         │  - 领域逻辑
│  - DepartmentManager                         │
│  - TenantManager                             │
└─────────────────────┬───────────────────────┘
                      ↓
┌─────────────────────────────────────────────┐
│          Services（应用服务）                │  应用层
│  - SysUserService                            │  - 业务流程
│  - SysRoleService                            │  - 事务管理
│  - SysPermissionService                      │  - DTO 转换
└─────────────────────┬───────────────────────┘
                      ↓
┌─────────────────────────────────────────────┐
│        Repositories（仓储层）                │  数据层
│  - SysUserRepository                         │  - 数据访问
│  - SysRoleRepository                         │  - 查询构建
│  - SysPermissionRepository                   │  - 持久化
└─────────────────────────────────────────────┘
```

## 🎯 职责划分

### 1. Adapters（适配器层）

**职责**：

- 实现框架定义的接口（如 `IAuthenticationService`、`IRoleManager`）
- 进行数据转换（Entity ↔ Framework DTO）
- 编排多个服务和管理器的调用
- 作为框架和业务层之间的桥梁

**示例**：

```csharp
public class RbacRoleManager : IRoleManager  // 实现框架接口
{
    private readonly Managers.RoleManager _domainRoleManager;  // 使用领域管理器

    public async Task<RoleOperationResult> CreateRoleAsync(RoleDefinition role, ...)
    {
        // 1. 使用领域管理器验证业务规则
        if (!_domainRoleManager.IsValidRoleCode(role.Name))
            return RoleOperationResult.Failure("角色编码格式不合法");

        // 2. 使用领域管理器检查唯一性
        if (!await _domainRoleManager.IsRoleCodeUniqueAsync(role.Name))
            return RoleOperationResult.Failure("角色已存在");

        // 3. 调用存储层
        await _roleStore.CreateRoleAsync(role);
        return RoleOperationResult.Success(role);
    }
}
```

**不应该**：

- ❌ 直接包含业务规则逻辑
- ❌ 直接进行数据库操作
- ❌ 包含复杂的算法

### 2. Managers（领域管理器）

**职责**：

- 封装领域业务规则
- 数据验证和格式检查
- 领域逻辑计算
- 跨实体的业务规则

**示例**：

```csharp
public class RoleManager : DomainService
{
    // 验证业务规则
    public bool IsValidRoleCode(string roleCode)
    {
        // 业务规则：角色编码格式验证
        return Regex.IsMatch(roleCode, @"^[a-zA-Z0-9_]+$");
    }

    // 唯一性检查
    public async Task<bool> IsRoleCodeUniqueAsync(string roleCode, long? excludeId = null)
    {
        return !await _roleRepository.ExistsByRoleCodeAsync(roleCode, excludeId);
    }

    // 删除前检查
    public async Task<bool> CanDeleteAsync(long roleId)
    {
        var userCount = await _roleRepository.GetRoleUserCountAsync(roleId);
        return userCount == 0;
    }
}
```

**应该**：

- ✅ 封装业务规则
- ✅ 进行数据验证
- ✅ 调用 Repository 获取数据
- ✅ 提供可重用的领域逻辑

**不应该**：

- ❌ 直接处理 HTTP 请求/响应
- ❌ 实现框架接口
- ❌ 进行 DTO 转换

### 3. Services（应用服务）

**职责**：

- 业务流程编排
- 事务管理
- DTO 转换
- 调用多个 Manager 和 Repository

**示例**：

```csharp
public class SysRoleService : ISysRoleService
{
    public async Task<bool> AssignPermissionsAsync(AssignRolePermissionsDto input)
    {
        // 1. 使用 Manager 验证
        if (!await _roleManager.CanModifyPermissionsAsync(input.RoleId))
            throw new BusinessException("无权修改此角色权限");

        // 2. 事务处理
        using var trans = await _repository.BeginTransactionAsync();

        // 3. 业务流程
        await _repository.ClearRolePermissionsAsync(input.RoleId);
        foreach (var permissionId in input.PermissionIds)
        {
            await _repository.AddRolePermissionAsync(input.RoleId, permissionId);
        }

        await trans.CommitAsync();
        return true;
    }
}
```

### 4. Repositories（仓储层）

**职责**：

- 数据访问
- 查询构建
- 数据持久化

## 🔄 调用流程示例

### 示例 1：创建角色

```
Controller
    ↓ 调用
Adapter (RbacRoleManager)
    ↓ 验证规则
Domain Manager (RoleManager.IsValidRoleCode)
    ↓ 检查唯一性
Domain Manager (RoleManager.IsRoleCodeUniqueAsync)
    ↓ 查询数据
Repository (SysRoleRepository.ExistsByRoleCodeAsync)
    ↓ 创建角色
Adapter (RbacRoleManager → RoleStore.CreateRoleAsync)
    ↓ 保存数据
Repository (SysRoleRepository.InsertAsync)
```

### 示例 2：用户登录认证

```
Controller
    ↓ 调用
Adapter (RbacAuthenticationService)
    ↓ 获取用户
Repository (SysUserRepository.GetByUserNameAsync)
    ↓ 检查账户状态
Domain Manager (UserManager.IsUserActive)
    ↓ 验证密码
Domain Manager (UserManager.VerifyPassword)
    ↓ 检查密码是否需要升级
Domain Manager (UserManager.NeedsPasswordRehash)
    ↓ 生成 Token
Framework (JwtTokenService.GenerateAccessToken)
    ↓ 返回结果
Controller
```

## 💡 设计原则

### 1. 单一职责原则（SRP）

- 每一层只负责一种类型的事务
- Adapter 负责适配，Manager 负责业务规则

### 2. 依赖倒置原则（DIP）

- 高层模块（Adapter）依赖抽象（Manager 接口）
- 低层模块（Repository）被高层模块使用

### 3. 开闭原则（OCP）

- 通过添加新的 Manager 扩展业务规则
- 不修改现有的 Adapter 代码

### 4. 里氏替换原则（LSP）

- Manager 可以被不同实现替换
- Adapter 依赖抽象而非具体实现

## 📝 最佳实践

### 1. Adapter 中使用 Manager

```csharp
// ✅ 好的做法
public class RbacRoleManager : IRoleManager
{
    private readonly Managers.RoleManager _domainManager;

    public async Task<RoleOperationResult> CreateRoleAsync(...)
    {
        // 使用 Manager 验证
        if (!_domainManager.IsValidRoleCode(...))
            return Failure(...);

        // 使用 Manager 检查
        if (!await _domainManager.IsRoleCodeUniqueAsync(...))
            return Failure(...);
    }
}

// ❌ 不好的做法
public class RbacRoleManager : IRoleManager
{
    public async Task<RoleOperationResult> CreateRoleAsync(...)
    {
        // 直接在 Adapter 中写业务规则
        if (!Regex.IsMatch(role.Name, @"^[a-zA-Z0-9_]+$"))
            return Failure(...);
    }
}
```

### 2. Manager 中封装规则

```csharp
// ✅ 好的做法
public class UserManager : DomainService
{
    public bool IsValidEmail(string email)
    {
        // 封装邮箱格式验证规则
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}

// ❌ 不好的做法 - 在多个地方重复验证逻辑
```

### 3. Service 中编排流程

```csharp
// ✅ 好的做法
public class SysUserService
{
    public async Task<bool> RegisterAsync(CreateUserDto dto)
    {
        // 1. 验证（使用 Manager）
        if (!_userManager.IsValidEmail(dto.Email))
            throw new ValidationException(...);

        // 2. 业务流程
        var user = new SysUser { ... };
        user.Password = _userManager.HashPassword(dto.Password);

        // 3. 保存
        await _repository.InsertAsync(user);

        // 4. 后续操作
        await SendWelcomeEmailAsync(user);

        return true;
    }
}
```

## 🔧 依赖注入配置

```csharp
// 在 Module 中注册
public override void ConfigureServices(ServiceConfigurationContext context)
{
    var services = context.Services;

    // 1. 注册领域管理器（业务规则）
    services.AddScoped<UserManager>();
    services.AddScoped<RoleManager>();
    services.AddScoped<PermissionManager>();

    // 2. 注册适配器（框架接口实现）
    services.AddScoped<IAuthenticationService, RbacAuthenticationService>();
    services.AddScoped<IRoleManager, RbacRoleManager>();
    services.AddScoped<IPermissionStore, RbacPermissionStore>();
    services.AddScoped<IRoleStore, RbacRoleStore>();

    // 3. 注册应用服务
    services.AddRbacServices();

    // 4. 注册仓储
    services.AddRbacRepositories();
}
```

## 📚 总结

通过这种分层架构：

1. **Adapter** 专注于框架接口适配和编排
2. **Manager** 专注于领域业务规则和验证
3. **Service** 专注于业务流程和事务管理
4. **Repository** 专注于数据访问

这样既避免了重复，又保持了清晰的职责分离，使代码更易于维护和测试。
