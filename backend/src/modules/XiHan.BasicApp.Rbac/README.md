# XiHan.BasicApp.Rbac - RBAC 权限管理模块

## 模块概述

XiHan.BasicApp.Rbac 是曦寒基础应用的 RBAC（基于角色的访问控制）模块，提供了完整的用户、角色、权限、菜单、部门和租户管理功能。

## 技术架构

本模块遵循领域驱动设计（DDD）原则，采用分层架构：

```
XiHan.BasicApp.Rbac
├── Entities/           # 实体层 - 领域实体定义
│   ├── Base/          # 基础实体
│   ├── Partials/      # 部分类（用于扩展）
│   ├── SysUser.cs     # 用户实体
│   ├── SysRole.cs     # 角色实体
│   ├── SysPermission.cs   # 权限实体
│   ├── SysMenu.cs     # 菜单实体
│   ├── SysDepartment.cs   # 部门实体
│   └── SysTenant.cs   # 租户实体
├── Dtos/              # 数据传输对象
│   ├── Base/          # 基础 DTO
│   ├── Users/         # 用户相关 DTO
│   ├── Roles/         # 角色相关 DTO
│   ├── Permissions/   # 权限相关 DTO
│   ├── Menus/         # 菜单相关 DTO
│   ├── Departments/   # 部门相关 DTO
│   └── Tenants/       # 租户相关 DTO
├── Repositories/      # 仓储接口层
│   ├── IUserRepository.cs
│   ├── IRoleRepository.cs
│   ├── IPermissionRepository.cs
│   ├── IMenuRepository.cs
│   ├── IDepartmentRepository.cs
│   └── ITenantRepository.cs
├── Services/          # 应用服务层
│   └── Abstractions/  # 服务接口
│       ├── IUserService.cs
│       ├── IRoleService.cs
│       ├── IPermissionService.cs
│       ├── IMenuService.cs
│       ├── IDepartmentService.cs
│       └── ITenantService.cs
├── Managers/          # 领域管理器
│   ├── UserManager.cs
│   ├── RoleManager.cs
│   ├── PermissionManager.cs
│   ├── MenuManager.cs
│   ├── DepartmentManager.cs
│   └── TenantManager.cs
├── Extensions/        # 扩展方法
│   ├── EntityExtensions.cs
│   └── ServiceCollectionExtensions.cs
├── Constants/         # 常量定义
│   └── RbacConstants.cs
├── Enums/            # 枚举定义
│   ├── UserEnums.cs
│   ├── RoleEnums.cs
│   ├── PermissionEnums.cs
│   ├── MenuEnums.cs
│   ├── DepartmentEnums.cs
│   ├── TenantEnums.cs
│   └── CommonEnums.cs
└── XiHanBasicAppRbacModule.cs  # 模块定义
```

## 核心功能

### 1. 用户管理

- 用户的创建、更新、删除、查询
- 密码管理（修改密码、重置密码）
- 用户角色分配
- 用户部门分配
- 用户权限查询

### 2. 角色管理

- 角色的创建、更新、删除、查询
- 角色菜单分配
- 角色权限分配
- 角色类型管理（系统角色、业务角色、自定义角色）

### 3. 权限管理

- 权限的创建、更新、删除、查询
- 权限类型（菜单权限、按钮权限、API 权限、数据权限）
- 基于权限编码的权限验证

### 4. 菜单管理

- 菜单的创建、更新、删除、查询
- 菜单树形结构
- 菜单类型（目录、菜单、按钮）
- 用户菜单树查询

### 5. 部门管理

- 部门的创建、更新、删除、查询
- 部门树形结构
- 部门类型（公司、部门、小组）
- 部门负责人管理

### 6. 租户管理

- 多租户支持
- 租户隔离模式（字段隔离、数据库隔离、Schema 隔离）
- 租户配置管理
- 租户资源限制（用户数、存储空间）

## 主要实体关系

```
用户 (SysUser)
  ├── 1:N → 用户角色 (SysUserRole) ← N:1 ── 角色 (SysRole)
  ├── 1:N → 用户部门 (SysUserDepartment) ← N:1 ── 部门 (SysDepartment)
  └── N:1 → 租户 (SysTenant)

角色 (SysRole)
  ├── 1:N → 角色菜单 (SysRoleMenu) ← N:1 ── 菜单 (SysMenu)
  └── 1:N → 角色权限 (SysRolePermission) ← N:1 ── 权限 (SysPermission)
```

## 使用示例

### 注册服务

模块自动注册服务，无需手动配置：

```csharp
// 在模块配置中自动注册
public override void ConfigureServices(ServiceConfigurationContext context)
{
    var services = context.Services;

    // 注册领域管理器
    services.AddScoped<UserManager>();
    services.AddScoped<RoleManager>();
    // ... 其他管理器

    // 添加 RBAC 服务和仓储
    services.AddRbacServices();
    services.AddRbacRepositories();
}
```

### 使用服务

```csharp
public class MyController
{
    private readonly IUserService _userService;

    public MyController(IUserService userService)
    {
        _userService = userService;
    }

    // 创建用户
    public async Task<UserDto> CreateUser(CreateUserDto input)
    {
        return await _userService.CreateAsync(input);
    }

    // 查询用户
    public async Task<UserDto?> GetUser(long id)
    {
        return await _userService.GetByIdAsync(id);
    }

    // 分配角色
    public async Task<bool> AssignRoles(AssignUserRolesDto input)
    {
        return await _userService.AssignRolesAsync(input);
    }
}
```

### 实体到 DTO 转换

```csharp
// 使用扩展方法转换
var user = await _userRepository.GetByIdAsync(userId);
var userDto = user.ToDto();

// 构建菜单树
var menus = await _menuRepository.GetAllAsync();
var menuDtos = menus.ToDto();
var menuTree = menuDtos.BuildTree();
```

## 权限常量

模块提供了预定义的权限常量：

```csharp
// 用户权限
PermissionConstants.UserView
PermissionConstants.UserCreate
PermissionConstants.UserEdit
PermissionConstants.UserDelete

// 角色权限
PermissionConstants.RoleView
PermissionConstants.RoleCreate
// ...
```

## 缓存键

模块提供了缓存键生成方法：

```csharp
// 用户权限缓存
var key = CacheKeyConstants.UserPermissions(userId);

// 用户角色缓存
var key = CacheKeyConstants.UserRoles(userId);

// 菜单树缓存
var key = CacheKeyConstants.MenuTree;
```

## 领域管理器

领域管理器提供了业务规则验证和领域逻辑：

```csharp
public class UserManager
{
    // 验证用户名唯一性
    public async Task<bool> IsUserNameUniqueAsync(string userName, long? excludeId = null);

    // 验证密码
    public bool VerifyPassword(SysUser user, string password);

    // 加密密码
    public string HashPassword(string password);
}
```

## 主键类型

模块使用全局类型别名 `RbacIdType` 来定义主键类型，默认为 `long`。可以在 `Entities/Base/RbacIdType.cs` 中修改：

```csharp
// 使用 long
global using RbacIdType = System.Int64;

// 使用 Guid
// global using RbacIdType = System.Guid;
```

## 数据库支持

模块基于 SqlSugar ORM，支持多种数据库：

- SQL Server
- MySQL
- PostgreSQL
- SQLite
- Oracle

## 依赖项

- XiHan.BasicApp.Core - 应用核心模块
- XiHan.Framework.\* - 框架基础库
  - Domain - 领域层基础
  - Application - 应用层基础
  - Data - 数据访问
  - Caching - 缓存
  - Authentication - 认证
  - Authorization - 授权
  - MultiTenancy - 多租户

## 后续扩展

建议实现的功能：

1. 具体的服务实现类（UserService、RoleService 等）
2. 具体的仓储实现类（UserRepository、RoleRepository 等）
3. 事件处理器（用户创建事件、角色变更事件等）
4. 数据权限过滤
5. 审计日志
6. 操作日志
7. 登录日志

## 许可证

MIT License - 详见 LICENSE 文件
