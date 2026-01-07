# XiHan.BasicApp.Rbac - Services 架构说明

## 📚 目录结构

```
Services/
├── Domain/                         # 领域服务层（Domain Layer）
│   ├── UserDomainService.cs        # 用户领域服务
│   ├── RoleDomainService.cs        # 角色领域服务
│   ├── PermissionDomainService.cs  # 权限领域服务
│   ├── AuthorizationDomainService.cs # 授权领域服务
│   └── MenuDomainService.cs        # 菜单领域服务
│
└── Application/                    # 应用服务层（Application Layer）
    ├── Commands/                   # 命令服务（写操作 - CQRS）
    │   ├── UserCommandService.cs   # 用户命令服务
    │   └── RoleCommandService.cs   # 角色命令服务
    │
    └── Queries/                    # 查询服务（读操作 - CQRS）
        ├── UserQueryService.cs     # 用户查询服务
        └── RoleQueryService.cs     # 角色查询服务
```

---

## 🏗️ 架构设计原则

### 1. DDD 分层架构

```
┌─────────────────────────────────────────┐
│   Presentation Layer (Controller)       │  ← REST API / gRPC
└─────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────┐
│   Application Layer                     │
│   ┌─────────────┐   ┌─────────────┐   │
│   │  Commands   │   │   Queries   │   │  ← CQRS 分离
│   │  (Write)    │   │   (Read)    │   │
│   └─────────────┘   └─────────────┘   │
└─────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────┐
│   Domain Layer                          │
│   ┌──────────────────────────────────┐ │
│   │   Domain Services                │ │  ← 跨聚合业务逻辑
│   │   (Cross-Aggregate Logic)        │ │
│   └──────────────────────────────────┘ │
│   ┌──────────────────────────────────┐ │
│   │   Entities & Aggregates          │ │  ← 业务实体
│   └──────────────────────────────────┘ │
│   ┌──────────────────────────────────┐ │
│   │   Repository Interfaces          │ │  ← 仓储接口
│   └──────────────────────────────────┘ │
└─────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────┐
│   Infrastructure Layer                  │  ← 数据访问实现
└─────────────────────────────────────────┘
```

---

## 📋 Domain Services（领域服务）

### 职责

- **处理跨聚合根的业务逻辑**
- **不包含基础设施依赖（如数据库操作）**
- **只依赖 Repository 接口**
- **不处理 DTO 映射**

### 1. UserDomainService（用户领域服务）

```csharp
public class UserDomainService : DomainService
{
    // 核心方法
    Task<bool> AssignRolesToUserAsync(long userId, List<long> roleIds);
    Task<bool> RemoveRolesFromUserAsync(long userId, List<long> roleIds);
    Task<bool> GrantPermissionsToUserAsync(long userId, List<long> permissionIds);

    // 验证方法
    Task<bool> IsUserNameUniqueAsync(string userName, long? excludeUserId = null);
    Task<bool> IsEmailUniqueAsync(string email, long? excludeUserId = null);
    Task<bool> IsPhoneUniqueAsync(string phone, long? excludeUserId = null);
    Task<bool> ValidateTenantLimitAsync(long tenantId);
}
```

**使用场景：**

- ✅ 用户-角色关系验证（跨聚合）
- ✅ 用户-权限关系验证（跨聚合）
- ✅ 租户限制验证（跨聚合）
- ✅ 唯一性检查

---

### 2. RoleDomainService（角色领域服务）

```csharp
public class RoleDomainService : DomainService
{
    // 权限分配
    Task<bool> AssignPermissionsToRoleAsync(long roleId, List<long> permissionIds);
    Task<bool> AssignMenusToRoleAsync(long roleId, List<long> menuIds);

    // 角色继承
    Task<List<SysPermission>> GetRolePermissionsIncludingInheritedAsync(long roleId);
    Task<List<SysMenu>> GetRoleMenusIncludingInheritedAsync(long roleId);

    // 验证
    Task<bool> IsRoleCodeUniqueAsync(string roleCode, long? excludeRoleId = null);
    Task<bool> CanDeleteRoleAsync(long roleId);
}
```

**使用场景：**

- ✅ 角色-权限关系验证
- ✅ 角色-菜单关系验证
- ✅ 角色继承逻辑
- ✅ 删除前置条件验证

---

### 3. PermissionDomainService（权限领域服务）

```csharp
public class PermissionDomainService : DomainService
{
    // 权限计算
    Task<List<SysPermission>> GetUserPermissionsAsync(long userId);
    Task<List<string>> GetUserPermissionCodesAsync(long userId);

    // 权限检查
    Task<bool> HasPermissionAsync(long userId, string permissionCode);
    Task<bool> HasAnyPermissionAsync(long userId, List<string> permissionCodes);
    Task<bool> HasAllPermissionsAsync(long userId, List<string> permissionCodes);

    // 资源权限
    Task<List<SysPermission>> GetResourcePermissionsAsync(long resourceId);
}
```

**使用场景：**

- ✅ 用户权限聚合（角色权限 + 直接权限）
- ✅ 权限检查
- ✅ 资源权限查询

---

### 4. AuthorizationDomainService（授权领域服务）

```csharp
public class AuthorizationDomainService : DomainService
{
    // 资源访问控制
    Task<bool> CanAccessResourceAsync(long userId, long resourceId);
    Task<bool> CanAccessApiAsync(long userId, string apiPath, string? httpMethod = null);

    // 用户授权信息
    Task<List<SysMenu>> GetUserMenuTreeAsync(long userId);
    Task<List<SysResource>> GetUserAccessibleResourcesAsync(long userId, ResourceType? resourceType = null);

    // 角色检查
    Task<bool> IsSuperAdminAsync(long userId);
}
```

**使用场景：**

- ✅ API 访问控制
- ✅ 资源访问控制
- ✅ 用户菜单树构建
- ✅ 超级管理员判断

---

### 5. MenuDomainService（菜单领域服务）

```csharp
public class MenuDomainService : DomainService
{
    // 菜单树构建
    Task<List<SysMenu>> BuildMenuTreeAsync(long? parentId = null);
    Task<List<SysMenu>> GetMenuPathAsync(long menuId);

    // 验证
    Task<bool> IsMenuCodeUniqueAsync(string menuCode, long? excludeMenuId = null);
    Task<bool> CanDeleteMenuAsync(long menuId);
}
```

**使用场景：**

- ✅ 菜单树递归构建
- ✅ 菜单路径获取
- ✅ 删除前置条件验证

---

## 📋 Application Services（应用服务）

### CQRS 模式

- **Command Services**：处理写操作（创建、更新、删除）
- **Query Services**：处理读操作（查询、列表）

```
           ┌─────────────────┐
           │   Controller    │
           └────────┬────────┘
                    │
        ┌───────────┴───────────┐
        ↓                       ↓
┌──────────────┐        ┌──────────────┐
│   Command    │        │    Query     │
│   Service    │        │   Service    │
└──────┬───────┘        └───────┬──────┘
       │                        │
       ↓                        ↓
┌──────────────┐        ┌──────────────┐
│   Write DB   │        │   Read DB    │
│  (写库/主库)  │        │  (读库/从库)  │
└──────────────┘        └──────────────┘
```

---

### Commands（命令服务 - 写操作）

#### UserCommandService

```csharp
public class UserCommandService : CrudApplicationServiceBase<SysUser, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    // CRUD 操作
    Task<RbacDtoBase> CreateAsync(RbacDtoBase input);
    Task<RbacDtoBase> UpdateAsync(long id, RbacDtoBase input);
    Task<bool> DeleteAsync(long id);

    // 角色管理
    Task<bool> AssignRolesToUserAsync(long userId, List<long> roleIds);
    Task<bool> RemoveRolesFromUserAsync(long userId, List<long> roleIds);

    // 权限管理
    Task<bool> GrantPermissionsToUserAsync(long userId, List<long> permissionIds);

    // 用户管理
    Task<bool> ResetPasswordAsync(long userId, string newPassword);
    Task<bool> UpdateStatusAsync(long userId, YesOrNo status);
}
```

**职责：**

- ✅ 用例编排
- ✅ 权限校验
- ✅ 调用 Domain Service
- ✅ 事务管理
- ✅ DTO 映射
- ✅ 日志审计

---

#### RoleCommandService

```csharp
public class RoleCommandService : CrudApplicationServiceBase<SysRole, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    // CRUD 操作
    Task<RbacDtoBase> CreateAsync(RbacDtoBase input);
    Task<RbacDtoBase> UpdateAsync(long id, RbacDtoBase input);
    Task<bool> DeleteAsync(long id);

    // 权限管理
    Task<bool> AssignPermissionsToRoleAsync(long roleId, List<long> permissionIds);
    Task<bool> AssignMenusToRoleAsync(long roleId, List<long> menuIds);

    // 状态管理
    Task<bool> UpdateStatusAsync(long roleId, YesOrNo status);
}
```

---

### Queries（查询服务 - 读操作）

#### UserQueryService

```csharp
public class UserQueryService : ApplicationServiceBase
{
    // 基础查询
    Task<RbacDtoBase?> GetByIdAsync(long id);
    Task<RbacDtoBase?> GetByUserNameAsync(string userName);
    Task<RbacDtoBase?> GetByEmailAsync(string email);
    Task<RbacDtoBase?> GetByPhoneAsync(string phone);

    // 关联查询
    Task<RbacDtoBase?> GetUserWithRolesAsync(long userId);
    Task<List<RbacDtoBase>> GetUserRolesAsync(long userId);
    Task<List<RbacDtoBase>> GetUserPermissionsAsync(long userId);
    Task<List<string>> GetUserPermissionCodesAsync(long userId);

    // 权限检查
    Task<bool> HasPermissionAsync(long userId, string permissionCode);

    // 分页查询
    Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input);

    // 租户查询
    Task<List<RbacDtoBase>> GetByTenantIdAsync(long tenantId);
}
```

**职责：**

- ✅ 数据查询
- ✅ DTO 映射
- ✅ 分页处理
- ✅ 缓存处理（可选）

---

#### RoleQueryService

```csharp
public class RoleQueryService : ApplicationServiceBase
{
    // 基础查询
    Task<RbacDtoBase?> GetByIdAsync(long id);
    Task<RbacDtoBase?> GetByRoleCodeAsync(string roleCode);

    // 关联查询
    Task<List<RbacDtoBase>> GetRolePermissionsAsync(long roleId);
    Task<List<RbacDtoBase>> GetRoleMenusAsync(long roleId);
    Task<List<RbacDtoBase>> GetRoleUsersAsync(long roleId);

    // 角色继承
    Task<List<RbacDtoBase>> GetParentRolesAsync(long roleId);
    Task<List<RbacDtoBase>> GetChildRolesAsync(long roleId);

    // 分页查询
    Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input);
}
```

---

## 🎯 服务调用流程

### 1. 用户创建流程

```
Controller
    ↓
UserCommandService.CreateAsync(dto)
    ├─→ UserDomainService.IsUserNameUniqueAsync()  ← 验证用户名唯一性
    ├─→ UserDomainService.IsEmailUniqueAsync()     ← 验证邮箱唯一性
    ├─→ UserDomainService.ValidateTenantLimitAsync() ← 验证租户限制
    ├─→ Map DTO to Entity
    ├─→ IUserRepository.AddAsync()                 ← 持久化
    └─→ Map Entity to DTO
```

### 2. 用户角色分配流程

```
Controller
    ↓
UserCommandService.AssignRolesToUserAsync(userId, roleIds)
    ├─→ UserDomainService.AssignRolesToUserAsync()
    │       ├─→ IUserRepository.GetByIdAsync()     ← 验证用户存在
    │       ├─→ IRoleRepository.GetByIdsAsync()    ← 验证角色存在
    │       └─→ 业务规则检查（角色状态等）
    ├─→ 关系表维护（通过仓储或专用服务）
    └─→ 返回结果
```

### 3. 用户权限查询流程

```
Controller
    ↓
UserQueryService.GetUserPermissionsAsync(userId)
    ↓
PermissionDomainService.GetUserPermissionsAsync(userId)
    ├─→ IPermissionRepository.GetByUserIdAsync()   ← 获取直接权限
    ├─→ IUserRepository.GetWithRolesAsync()        ← 获取用户角色
    ├─→ RoleDomainService.GetRolePermissionsIncludingInheritedAsync() ← 获取角色权限（含继承）
    ├─→ 权限聚合去重
    └─→ Map Entity to DTO
```

---

## 📐 设计原则

### 1. 单一职责原则（SRP）

- **Domain Service**：只处理业务规则
- **Command Service**：只处理写操作
- **Query Service**：只处理读操作

### 2. 依赖倒置原则（DIP）

- 依赖接口而非实现
- Repository 接口在 Domain 层定义

### 3. 关注点分离（SoC）

- 业务逻辑 vs 数据访问
- 命令 vs 查询（CQRS）
- 领域逻辑 vs 应用逻辑

---

## ⚠️ 注意事项

### 1. 事务管理

```csharp
// ✅ 正确：在 Application Service 中管理事务
public async Task<UserDto> CreateUserWithRolesAsync(CreateUserDto input)
{
    using var transaction = await _unitOfWork.BeginTransactionAsync();
    try
    {
        // 1. 创建用户
        var user = await _userRepository.AddAsync(input.Adapt<SysUser>());

        // 2. 分配角色
        await AssignRolesToUserAsync(user.BaseId, input.RoleIds);

        await transaction.CommitAsync();
        return user.Adapt<UserDto>();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 2. 避免循环依赖

```csharp
// ❌ 错误：Domain Service 之间循环依赖
public class UserDomainService
{
    private readonly RoleDomainService _roleDomainService; // ❌
}

public class RoleDomainService
{
    private readonly UserDomainService _userDomainService; // ❌
}

// ✅ 正确：通过仓储接口协调
public class UserDomainService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository; // ✅
}
```

### 3. DTO 映射

```csharp
// ✅ 正确：在 Application Service 中映射
public async Task<UserDto> GetByIdAsync(long id)
{
    var user = await _userRepository.GetByIdAsync(id);
    return user?.Adapt<UserDto>(); // ✅ 在 Application 层映射
}

// ❌ 错误：在 Domain Service 中映射
public class UserDomainService
{
    public async Task<UserDto> GetUserAsync(long id) // ❌ Domain Service 不应该返回 DTO
    {
        // ...
    }
}
```

---

## 📖 参考资料

- [DDD 领域驱动设计](https://docs.microsoft.com/zh-cn/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/)
- [CQRS 模式](https://docs.microsoft.com/zh-cn/azure/architecture/patterns/cqrs)
- [应用服务层设计](https://docs.microsoft.com/zh-cn/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api)

---

## ✅ 总结

### 核心职责划分

| 层级                | 职责                           | 示例                                         |
| ------------------- | ------------------------------ | -------------------------------------------- |
| **Domain Service**  | 跨聚合业务逻辑、业务规则验证   | `UserDomainService.AssignRolesToUserAsync()` |
| **Command Service** | 写操作编排、事务管理、DTO 映射 | `UserCommandService.CreateAsync()`           |
| **Query Service**   | 读操作、数据查询、DTO 映射     | `UserQueryService.GetPagedAsync()`           |
| **Repository**      | 数据持久化、数据查询           | `IUserRepository.GetByIdAsync()`             |

### 设计原则

✅ DDD 分层架构
✅ CQRS 读写分离
✅ 单一职责原则
✅ 依赖倒置原则
✅ 关注点分离

### 已实现服务

- ✅ 5 个 Domain Services
- ✅ 2 个 Command Services（示例）
- ✅ 2 个 Query Services（示例）
- ✅ 完整的业务逻辑封装
- ✅ 清晰的调用流程
