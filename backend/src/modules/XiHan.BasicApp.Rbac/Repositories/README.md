# XiHan.BasicApp.Rbac - Repositories 架构说明

## 📚 目录结构

```
Repositories/
├── Abstracts/                      # 仓储接口层（Domain层）
│   ├── IUserRepository.cs          # 用户仓储接口
│   ├── IRoleRepository.cs          # 角色仓储接口
│   ├── IPermissionRepository.cs    # 权限仓储接口
│   ├── IMenuRepository.cs          # 菜单仓储接口
│   ├── IResourceRepository.cs      # 资源仓储接口
│   ├── ITenantRepository.cs        # 租户仓储接口
│   ├── IDictRepository.cs          # 字典仓储接口
│   ├── IConfigRepository.cs        # 配置仓储接口
│   ├── IFileRepository.cs          # 文件仓储接口
│   ├── ITaskRepository.cs          # 任务仓储接口
│   ├── IOAuthAppRepository.cs      # OAuth应用仓储接口
│   ├── IOAuthCodeRepository.cs     # OAuth授权码仓储接口
│   ├── IOAuthTokenRepository.cs    # OAuth令牌仓储接口
│   ├── IAuditRepository.cs         # 审计策略仓储接口
│   ├── IAuditLogRepository.cs      # 审计日志仓储接口
│   ├── INotificationRepository.cs  # 通知仓储接口
│   ├── ISmsRepository.cs           # 短信仓储接口
│   ├── IEmailRepository.cs         # 邮件仓储接口
│   ├── IUserSessionRepository.cs   # 用户会话仓储接口
│   └── Logs/                       # 日志相关仓储
│       ├── IOperationLogRepository.cs  # 操作日志仓储接口
│       ├── ILoginLogRepository.cs      # 登录日志仓储接口
│       ├── IApiLogRepository.cs        # API日志仓储接口
│       └── IAccessLogRepository.cs     # 访问日志仓储接口
└── Implementations/                # 仓储实现层（Infrastructure层）
    └── （具体实现将在 Infrastructure 项目中）
```

---

## 🏗️ 架构设计原则

### 1. 聚合根（Aggregate Root）原则

只有**聚合根实体**才拥有专用的 Repository 接口，继承自 `IAggregateRootRepository<TEntity, TKey>`：

**核心聚合根：**

- ✅ `SysUser` - 用户聚合根
- ✅ `SysRole` - 角色聚合根
- ✅ `SysPermission` - 权限聚合根
- ✅ `SysMenu` - 菜单聚合根
- ✅ `SysResource` - 资源聚合根
- ✅ `SysTenant` - 租户聚合根
- ✅ `SysDict` - 字典聚合根
- ✅ `SysConfig` - 配置聚合根
- ✅ `SysFile` - 文件聚合根
- ✅ `SysTask` - 任务聚合根
- ✅ `SysOAuthApp` - OAuth 应用聚合根
- ✅ `SysAudit` - 审计策略聚合根
- ✅ `SysNotification` - 通知聚合根
- ✅ `SysSms` - 短信聚合根
- ✅ `SysEmail` - 邮件聚合根
- ✅ `SysUserSession` - 用户会话聚合根

**非聚合根（仅继承 `IRepositoryBase<TEntity, TKey>`）：**

- `SysOAuthCode` - OAuth 授权码（生命周期短，非聚合根）
- `SysOAuthToken` - OAuth 令牌（含刷新逻辑，非聚合根）
- 所有日志实体（`SysOperationLog`、`SysLoginLog`、`SysApiLog`、`SysAccessLog`、`SysAuditLog`）

**不建立 Repository 的实体（由聚合根维护）：**

- ❌ `SysUserRole` - 用户角色关系（由 User 聚合维护）
- ❌ `SysUserPermission` - 用户权限关系（由 User 聚合维护）
- ❌ `SysRolePermission` - 角色权限关系（由 Role 聚合维护）
- ❌ `SysRoleMenu` - 角色菜单关系（由 Role 聚合维护）
- ❌ `SysRoleHierarchy` - 角色继承关系（由 Role 聚合维护）
- ❌ `SysUserDepartment` - 用户部门关系（由 User 聚合维护）
- ❌ `SysSessionRole` - 会话角色关系（由 UserSession 聚合维护）
- ❌ `SysDictItem` - 字典项（由 Dict 聚合维护）
- ❌ `SysUserSecurity` - 用户安全信息（由 User 聚合维护）
- ❌ `SysUserStatistics` - 用户统计信息（由后台任务维护）
- ❌ `SysTaskLog` - 任务日志（只读/写日志型）

---

## 📋 仓储接口说明

### 核心仓储接口特性

#### 1. **IUserRepository** - 用户仓储

```csharp
// 业务查询方法
Task<SysUser?> GetByUserNameAsync(string userName);
Task<SysUser?> GetByEmailAsync(string email);
Task<SysUser?> GetByPhoneAsync(string phone);

// 唯一性检查
Task<bool> ExistsByUserNameAsync(string userName, long? excludeUserId = null);
Task<bool> ExistsByEmailAsync(string email, long? excludeUserId = null);
Task<bool> ExistsByPhoneAsync(string phone, long? excludeUserId = null);

// 关联查询
Task<SysUser?> GetWithRolesAsync(long userId);
Task<SysUser?> GetWithPermissionsAsync(long userId);

// 业务更新
Task<bool> UpdateLastLoginAsync(long userId, string loginIp, DateTimeOffset loginTime);
```

#### 2. **IRoleRepository** - 角色仓储

```csharp
// 业务查询
Task<SysRole?> GetByRoleCodeAsync(string roleCode);
Task<SysRole?> GetWithPermissionsAsync(long roleId);
Task<SysRole?> GetWithMenusAsync(long roleId);

// 角色继承
Task<List<SysRole>> GetParentRolesAsync(long roleId);
Task<List<SysRole>> GetChildRolesAsync(long roleId);

// 关联查询
Task<List<SysUser>> GetUsersByRoleIdAsync(long roleId);
```

#### 3. **IPermissionRepository** - 权限仓储

```csharp
// 业务查询
Task<SysPermission?> GetByPermissionCodeAsync(string permissionCode);
Task<List<SysPermission>> GetByUserIdAsync(long userId);
Task<List<SysPermission>> GetByRoleIdAsync(long roleId);
Task<List<SysPermission>> GetByResourceIdAsync(long resourceId);

// 批量查询
Task<List<SysPermission>> GetByIdsAsync(List<long> permissionIds);
Task<List<SysPermission>> GetByCodesAsync(List<string> permissionCodes);
```

#### 4. **IMenuRepository** - 菜单仓储

```csharp
// 树形结构
Task<List<SysMenu>> GetByParentIdAsync(long? parentId);
Task<List<SysMenu>> GetRootMenusAsync();
Task<List<SysMenu>> GetMenuTreeAsync(long? parentId = null);
Task<bool> HasChildrenAsync(long menuId);

// 权限相关
Task<List<SysMenu>> GetByUserIdAsync(long userId);
Task<List<SysMenu>> GetByRoleIdAsync(long roleId);
```

#### 5. **IResourceRepository** - 资源仓储

```csharp
// API资源查询
Task<SysResource?> GetByApiPathAsync(string apiPath, string? httpMethod = null);
Task<List<SysResource>> GetByResourceTypeAsync(ResourceType resourceType);

// 权限关联
Task<List<SysResource>> GetByUserIdAsync(long userId, ResourceType? resourceType = null);
Task<List<SysResource>> GetByRoleIdAsync(long roleId, ResourceType? resourceType = null);
```

---

## 🎯 使用指南

### 1. 仓储使用场景

#### ✅ 应该使用仓储的场景：

- 持久化操作（增删改查）
- 复杂查询（多表联查、条件筛选）
- 事务边界控制
- 读模型构建

#### ❌ 不应该在仓储中处理：

- 业务逻辑（应该在 Domain Service 中）
- 数据验证（应该在 Entity 或 Domain Service 中）
- 权限校验（应该在 Application Service 中）
- DTO 映射（应该在 Application Service 中）

---

### 2. 仓储实现建议

仓储的具体实现应该在 **Infrastructure 层**，使用 SqlSugar 或其他 ORM：

```csharp
// 示例：UserRepository 实现
public class UserRepository : RepositoryBase<SysUser, long>, IUserRepository
{
    public UserRepository(ISqlSugarClient db) : base(db)
    {
    }

    public async Task<SysUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        return await DbClient.Queryable<SysUser>()
            .Where(u => u.UserName == userName)
            .FirstAsync(cancellationToken);
    }

    public async Task<bool> ExistsByUserNameAsync(string userName, long? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = DbClient.Queryable<SysUser>()
            .Where(u => u.UserName == userName);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.BaseId != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<SysUser?> GetWithRolesAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await DbClient.Queryable<SysUser>()
            .Includes(u => u.UserRoles) // 通过导航属性加载关系
            .Where(u => u.BaseId == userId)
            .FirstAsync(cancellationToken);
    }

    // ... 其他方法实现
}
```

---

## 🔄 与其他层的交互

### 1. Domain Layer（领域层）

- **Repository 接口定义**在 Domain 层
- 只定义契约，不涉及具体实现
- 供 Domain Service 使用

### 2. Application Layer（应用层）

- Application Service 通过仓储接口操作数据
- 仓储提供数据访问，Application Service 提供业务编排

```csharp
public class UserCommandService
{
    private readonly IUserRepository _userRepository;
    private readonly UserDomainService _userDomainService;

    public async Task<UserDto> CreateAsync(CreateUserDto input)
    {
        // 1. 领域验证
        await _userDomainService.ValidateUserCreation(input);

        // 2. 创建实体
        var user = input.Adapt<SysUser>();

        // 3. 仓储持久化
        user = await _userRepository.AddAsync(user);

        return user.Adapt<UserDto>();
    }
}
```

### 3. Infrastructure Layer（基础设施层）

- 实现具体的数据访问逻辑
- 使用 ORM（SqlSugar）操作数据库
- 处理事务、连接池等基础设施问题

---

## ⚠️ 注意事项

### 1. 事务管理

- 聚合根的修改应该在一个事务中完成
- 跨聚合根的操作应该通过 Domain Service 协调

### 2. 性能优化

- 使用 `Includes` 或 `Select` 优化关联查询
- 避免 N+1 查询问题
- 对于大数据量查询，使用分页

### 3. 缓存策略

- 对于频繁查询且变动少的数据（如字典、配置），考虑使用缓存
- 在 Application Service 层实现缓存逻辑

---

## 📖 参考资料

- [DDD 领域驱动设计](https://docs.microsoft.com/zh-cn/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/)
- [仓储模式](https://docs.microsoft.com/zh-cn/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [聚合根设计原则](https://martinfowler.com/bliki/DDD_Aggregate.html)

---

## ✅ 总结

### 核心原则

1. **只有聚合根才有 Repository**
2. **Repository 只负责持久化和查询**
3. **业务逻辑在 Domain Service 中**
4. **用例编排在 Application Service 中**
5. **关系表由聚合根维护**

### 目录清单

- ✅ 19 个聚合根 Repository 接口
- ✅ 3 个非聚合根 Repository 接口
- ✅ 4 个日志 Repository 接口
- ✅ 清晰的职责划分
- ✅ 完整的业务方法定义
