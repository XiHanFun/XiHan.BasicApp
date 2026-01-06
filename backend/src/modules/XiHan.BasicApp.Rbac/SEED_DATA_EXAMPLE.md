# RBAC 种子数据示例

> **完整的 RBAC 系统初始化数据**  
> **版本**：v2.0  
> **最后更新**：2026-01-07

---

## 📖 目录

1. [标准操作](#1-标准操作)
2. [系统资源](#2-系统资源)
3. [系统权限](#3-系统权限)
4. [系统角色](#4-系统角色)
5. [系统菜单](#5-系统菜单)
6. [约束规则](#6-约束规则)
7. [超级管理员](#7-超级管理员)

---

## 1. 标准操作

### CRUD 操作

```csharp
var operations = new List<SysOperation>
{
    new()
    {
        BasicId = 1,
        OperationCode = "create",
        OperationName = "创建",
        OperationTypeCode = OperationTypeCode.Create,
        Category = OperationCategory.Crud,
        HttpMethod = HttpMethodType.POST,
        Description = "创建新记录",
        Icon = "plus",
        Color = "success",
        IsDangerous = false,
        RequireAudit = true,
        Status = YesOrNo.Yes,
        Sort = 1
    },
    new()
    {
        BasicId = 2,
        OperationCode = "read",
        OperationName = "查看",
        OperationTypeCode = OperationTypeCode.Read,
        Category = OperationCategory.Crud,
        HttpMethod = HttpMethodType.GET,
        Description = "查看记录详情",
        Icon = "eye",
        Color = "info",
        IsDangerous = false,
        RequireAudit = false,
        Status = YesOrNo.Yes,
        Sort = 2
    },
    new()
    {
        BasicId = 3,
        OperationCode = "update",
        OperationName = "更新",
        OperationTypeCode = OperationTypeCode.Update,
        Category = OperationCategory.Crud,
        HttpMethod = HttpMethodType.PUT,
        Description = "更新记录",
        Icon = "edit",
        Color = "primary",
        IsDangerous = false,
        RequireAudit = true,
        Status = YesOrNo.Yes,
        Sort = 3
    },
    new()
    {
        BasicId = 4,
        OperationCode = "delete",
        OperationName = "删除",
        OperationTypeCode = OperationTypeCode.Delete,
        Category = OperationCategory.Crud,
        HttpMethod = HttpMethodType.DELETE,
        Description = "删除记录",
        Icon = "delete",
        Color = "danger",
        IsDangerous = true,
        RequireAudit = true,
        Status = YesOrNo.Yes,
        Sort = 4
    }
};
```

### 业务操作

```csharp
var businessOperations = new List<SysOperation>
{
    new()
    {
        BasicId = 10,
        OperationCode = "approve",
        OperationName = "审批",
        OperationTypeCode = OperationTypeCode.Approve,
        Category = OperationCategory.Business,
        HttpMethod = HttpMethodType.POST,
        Description = "审批操作",
        Icon = "check-circle",
        Color = "success",
        IsDangerous = false,
        RequireAudit = true,
        Status = YesOrNo.Yes,
        Sort = 10
    },
    new()
    {
        BasicId = 11,
        OperationCode = "execute",
        OperationName = "执行",
        OperationTypeCode = OperationTypeCode.Execute,
        Category = OperationCategory.Business,
        HttpMethod = HttpMethodType.POST,
        Description = "执行操作",
        Icon = "play-circle",
        Color = "primary",
        IsDangerous = false,
        RequireAudit = true,
        Status = YesOrNo.Yes,
        Sort = 11
    }
};
```

### 系统操作

```csharp
var systemOperations = new List<SysOperation>
{
    new()
    {
        BasicId = 20,
        OperationCode = "import",
        OperationName = "导入",
        OperationTypeCode = OperationTypeCode.Import,
        Category = OperationCategory.System,
        HttpMethod = HttpMethodType.POST,
        Description = "导入数据",
        Icon = "upload",
        Color = "info",
        IsDangerous = false,
        RequireAudit = true,
        Status = YesOrNo.Yes,
        Sort = 20
    },
    new()
    {
        BasicId = 21,
        OperationCode = "export",
        OperationName = "导出",
        OperationTypeCode = OperationTypeCode.Export,
        Category = OperationCategory.System,
        HttpMethod = HttpMethodType.GET,
        Description = "导出数据",
        Icon = "download",
        Color = "success",
        IsDangerous = false,
        RequireAudit = true,
        Status = YesOrNo.Yes,
        Sort = 21
    },
    new()
    {
        BasicId = 22,
        OperationCode = "download",
        OperationName = "下载",
        OperationTypeCode = OperationTypeCode.Download,
        Category = OperationCategory.System,
        HttpMethod = HttpMethodType.GET,
        Description = "下载文件",
        Icon = "cloud-download",
        Color = "primary",
        IsDangerous = false,
        RequireAudit = false,
        Status = YesOrNo.Yes,
        Sort = 22
    }
};
```

### 管理操作

```csharp
var adminOperations = new List<SysOperation>
{
    new()
    {
        BasicId = 30,
        OperationCode = "grant",
        OperationName = "授权",
        OperationTypeCode = OperationTypeCode.Grant,
        Category = OperationCategory.Admin,
        HttpMethod = HttpMethodType.POST,
        Description = "授予权限",
        Icon = "key",
        Color = "warning",
        IsDangerous = false,
        RequireAudit = true,
        Status = YesOrNo.Yes,
        Sort = 30
    },
    new()
    {
        BasicId = 31,
        OperationCode = "revoke",
        OperationName = "撤销",
        OperationTypeCode = OperationTypeCode.Revoke,
        Category = OperationCategory.Admin,
        HttpMethod = HttpMethodType.DELETE,
        Description = "撤销权限",
        Icon = "lock",
        Color = "danger",
        IsDangerous = true,
        RequireAudit = true,
        Status = YesOrNo.Yes,
        Sort = 31
    },
    new()
    {
        BasicId = 32,
        OperationCode = "enable",
        OperationName = "启用",
        OperationTypeCode = OperationTypeCode.Enable,
        Category = OperationCategory.Admin,
        HttpMethod = HttpMethodType.PUT,
        Description = "启用功能",
        Icon = "check",
        Color = "success",
        IsDangerous = false,
        RequireAudit = true,
        Status = YesOrNo.Yes,
        Sort = 32
    },
    new()
    {
        BasicId = 33,
        OperationCode = "disable",
        OperationName = "禁用",
        OperationTypeCode = OperationTypeCode.Disable,
        Category = OperationCategory.Admin,
        HttpMethod = HttpMethodType.PUT,
        Description = "禁用功能",
        Icon = "close",
        Color = "danger",
        IsDangerous = true,
        RequireAudit = true,
        Status = YesOrNo.Yes,
        Sort = 33
    }
};
```

---

## 2. 系统资源

### 用户管理资源

```csharp
var resources = new List<SysResource>
{
    // 系统管理
    new()
    {
        BasicId = 1,
        ParentId = null,
        ResourceCode = "system",
        ResourceName = "系统管理",
        ResourceType = ResourceType.Menu,
        ResourcePath = "/system",
        Icon = "setting",
        Description = "系统管理模块",
        RequireAuth = true,
        IsPublic = false,
        Status = YesOrNo.Yes,
        Sort = 1
    },

    // 用户管理
    new()
    {
        BasicId = 10,
        ParentId = 1,
        ResourceCode = "user",
        ResourceName = "用户管理",
        ResourceType = ResourceType.Menu,
        ResourcePath = "/system/user",
        Icon = "user",
        Description = "用户管理功能",
        RequireAuth = true,
        IsPublic = false,
        Status = YesOrNo.Yes,
        Sort = 10
    },

    // 用户列表API
    new()
    {
        BasicId = 11,
        ParentId = 10,
        ResourceCode = "user_api",
        ResourceName = "用户API",
        ResourceType = ResourceType.Api,
        ResourcePath = "/api/users",
        Description = "用户管理API接口",
        RequireAuth = true,
        IsPublic = false,
        Status = YesOrNo.Yes,
        Sort = 11
    },

    // 角色管理
    new()
    {
        BasicId = 20,
        ParentId = 1,
        ResourceCode = "role",
        ResourceName = "角色管理",
        ResourceType = ResourceType.Menu,
        ResourcePath = "/system/role",
        Icon = "team",
        Description = "角色管理功能",
        RequireAuth = true,
        IsPublic = false,
        Status = YesOrNo.Yes,
        Sort = 20
    },

    // 权限管理
    new()
    {
        BasicId = 30,
        ParentId = 1,
        ResourceCode = "permission",
        ResourceName = "权限管理",
        ResourceType = ResourceType.Menu,
        ResourcePath = "/system/permission",
        Icon = "safety",
        Description = "权限管理功能",
        RequireAuth = true,
        IsPublic = false,
        Status = YesOrNo.Yes,
        Sort = 30
    },

    // 菜单管理
    new()
    {
        BasicId = 40,
        ParentId = 1,
        ResourceCode = "menu",
        ResourceName = "菜单管理",
        ResourceType = ResourceType.Menu,
        ResourcePath = "/system/menu",
        Icon = "menu",
        Description = "菜单管理功能",
        RequireAuth = true,
        IsPublic = false,
        Status = YesOrNo.Yes,
        Sort = 40
    },

    // 部门管理
    new()
    {
        BasicId = 50,
        ParentId = 1,
        ResourceCode = "department",
        ResourceName = "部门管理",
        ResourceType = ResourceType.Menu,
        ResourcePath = "/system/department",
        Icon = "apartment",
        Description = "部门管理功能",
        RequireAuth = true,
        IsPublic = false,
        Status = YesOrNo.Yes,
        Sort = 50
    }
};
```

---

## 3. 系统权限

### 用户管理权限

```csharp
var permissions = new List<SysPermission>
{
    // 用户管理权限
    new() { BasicId = 1, ResourceId = 10, OperationId = 1, PermissionCode = "user:create", PermissionName = "创建用户", RequireAudit = true, Status = YesOrNo.Yes, Sort = 1 },
    new() { BasicId = 2, ResourceId = 10, OperationId = 2, PermissionCode = "user:read", PermissionName = "查看用户", RequireAudit = false, Status = YesOrNo.Yes, Sort = 2 },
    new() { BasicId = 3, ResourceId = 10, OperationId = 3, PermissionCode = "user:update", PermissionName = "更新用户", RequireAudit = true, Status = YesOrNo.Yes, Sort = 3 },
    new() { BasicId = 4, ResourceId = 10, OperationId = 4, PermissionCode = "user:delete", PermissionName = "删除用户", RequireAudit = true, Status = YesOrNo.Yes, Sort = 4 },
    new() { BasicId = 5, ResourceId = 10, OperationId = 32, PermissionCode = "user:enable", PermissionName = "启用用户", RequireAudit = true, Status = YesOrNo.Yes, Sort = 5 },
    new() { BasicId = 6, ResourceId = 10, OperationId = 33, PermissionCode = "user:disable", PermissionName = "禁用用户", RequireAudit = true, Status = YesOrNo.Yes, Sort = 6 },
    new() { BasicId = 7, ResourceId = 10, OperationId = 20, PermissionCode = "user:import", PermissionName = "导入用户", RequireAudit = true, Status = YesOrNo.Yes, Sort = 7 },
    new() { BasicId = 8, ResourceId = 10, OperationId = 21, PermissionCode = "user:export", PermissionName = "导出用户", RequireAudit = true, Status = YesOrNo.Yes, Sort = 8 },

    // 角色管理权限
    new() { BasicId = 20, ResourceId = 20, OperationId = 1, PermissionCode = "role:create", PermissionName = "创建角色", RequireAudit = true, Status = YesOrNo.Yes, Sort = 20 },
    new() { BasicId = 21, ResourceId = 20, OperationId = 2, PermissionCode = "role:read", PermissionName = "查看角色", RequireAudit = false, Status = YesOrNo.Yes, Sort = 21 },
    new() { BasicId = 22, ResourceId = 20, OperationId = 3, PermissionCode = "role:update", PermissionName = "更新角色", RequireAudit = true, Status = YesOrNo.Yes, Sort = 22 },
    new() { BasicId = 23, ResourceId = 20, OperationId = 4, PermissionCode = "role:delete", PermissionName = "删除角色", RequireAudit = true, Status = YesOrNo.Yes, Sort = 23 },
    new() { BasicId = 24, ResourceId = 20, OperationId = 30, PermissionCode = "role:grant", PermissionName = "角色授权", RequireAudit = true, Priority = 10, Status = YesOrNo.Yes, Sort = 24 },
    new() { BasicId = 25, ResourceId = 20, OperationId = 31, PermissionCode = "role:revoke", PermissionName = "撤销角色权限", RequireAudit = true, Priority = 10, Status = YesOrNo.Yes, Sort = 25 },

    // 权限管理权限
    new() { BasicId = 30, ResourceId = 30, OperationId = 1, PermissionCode = "permission:create", PermissionName = "创建权限", RequireAudit = true, Status = YesOrNo.Yes, Sort = 30 },
    new() { BasicId = 31, ResourceId = 30, OperationId = 2, PermissionCode = "permission:read", PermissionName = "查看权限", RequireAudit = false, Status = YesOrNo.Yes, Sort = 31 },
    new() { BasicId = 32, ResourceId = 30, OperationId = 3, PermissionCode = "permission:update", PermissionName = "更新权限", RequireAudit = true, Status = YesOrNo.Yes, Sort = 32 },
    new() { BasicId = 33, ResourceId = 30, OperationId = 4, PermissionCode = "permission:delete", PermissionName = "删除权限", RequireAudit = true, Status = YesOrNo.Yes, Sort = 33 },

    // 菜单管理权限
    new() { BasicId = 40, ResourceId = 40, OperationId = 1, PermissionCode = "menu:create", PermissionName = "创建菜单", RequireAudit = true, Status = YesOrNo.Yes, Sort = 40 },
    new() { BasicId = 41, ResourceId = 40, OperationId = 2, PermissionCode = "menu:read", PermissionName = "查看菜单", RequireAudit = false, Status = YesOrNo.Yes, Sort = 41 },
    new() { BasicId = 42, ResourceId = 40, OperationId = 3, PermissionCode = "menu:update", PermissionName = "更新菜单", RequireAudit = true, Status = YesOrNo.Yes, Sort = 42 },
    new() { BasicId = 43, ResourceId = 40, OperationId = 4, PermissionCode = "menu:delete", PermissionName = "删除菜单", RequireAudit = true, Status = YesOrNo.Yes, Sort = 43 },

    // 部门管理权限
    new() { BasicId = 50, ResourceId = 50, OperationId = 1, PermissionCode = "department:create", PermissionName = "创建部门", RequireAudit = true, Status = YesOrNo.Yes, Sort = 50 },
    new() { BasicId = 51, ResourceId = 50, OperationId = 2, PermissionCode = "department:read", PermissionName = "查看部门", RequireAudit = false, Status = YesOrNo.Yes, Sort = 51 },
    new() { BasicId = 52, ResourceId = 50, OperationId = 3, PermissionCode = "department:update", PermissionName = "更新部门", RequireAudit = true, Status = YesOrNo.Yes, Sort = 52 },
    new() { BasicId = 53, ResourceId = 50, OperationId = 4, PermissionCode = "department:delete", PermissionName = "删除部门", RequireAudit = true, Status = YesOrNo.Yes, Sort = 53 }
};
```

---

## 4. 系统角色

```csharp
var roles = new List<SysRole>
{
    // 超级管理员
    new()
    {
        BasicId = 1,
        ParentRoleId = null,
        RoleCode = "super_admin",
        RoleName = "超级管理员",
        RoleDescription = "系统最高权限角色，拥有所有功能权限",
        RoleType = RoleType.System,
        DataScope = DataPermissionScope.All,
        Status = YesOrNo.Yes,
        Sort = 1
    },

    // 系统管理员
    new()
    {
        BasicId = 2,
        ParentRoleId = null,
        RoleCode = "admin",
        RoleName = "系统管理员",
        RoleDescription = "系统管理员，拥有系统配置权限",
        RoleType = RoleType.System,
        DataScope = DataPermissionScope.All,
        Status = YesOrNo.Yes,
        Sort = 2
    },

    // 普通员工
    new()
    {
        BasicId = 10,
        ParentRoleId = null,
        RoleCode = "employee",
        RoleName = "普通员工",
        RoleDescription = "普通员工，基础查看权限",
        RoleType = RoleType.Custom,
        DataScope = DataPermissionScope.SelfOnly,
        Status = YesOrNo.Yes,
        Sort = 10
    },

    // 部门经理
    new()
    {
        BasicId = 11,
        ParentRoleId = 10,
        RoleCode = "dept_manager",
        RoleName = "部门经理",
        RoleDescription = "部门经理，管理本部门数据",
        RoleType = RoleType.Custom,
        DataScope = DataPermissionScope.DepartmentOnly,
        Status = YesOrNo.Yes,
        Sort = 11
    },

    // 总经理
    new()
    {
        BasicId = 12,
        ParentRoleId = 11,
        RoleCode = "general_manager",
        RoleName = "总经理",
        RoleDescription = "总经理，管理全公司数据",
        RoleType = RoleType.Custom,
        DataScope = DataPermissionScope.DepartmentAndChildren,
        Status = YesOrNo.Yes,
        Sort = 12
    },

    // 审计员
    new()
    {
        BasicId = 20,
        ParentRoleId = null,
        RoleCode = "auditor",
        RoleName = "审计员",
        RoleDescription = "审计员，查看审计日志",
        RoleType = RoleType.Custom,
        DataScope = DataPermissionScope.All,
        Status = YesOrNo.Yes,
        Sort = 20
    }
};
```

### 角色权限分配

```csharp
var rolePermissions = new List<SysRolePermission>
{
    // 超级管理员拥有所有权限（这里只列出部分示例）
    new() { RoleId = 1, PermissionId = 1 },  // user:create
    new() { RoleId = 1, PermissionId = 2 },  // user:read
    new() { RoleId = 1, PermissionId = 3 },  // user:update
    // ... 其他所有权限 ...

    // 系统管理员拥有系统管理权限
    new() { RoleId = 2, PermissionId = 2 },  // user:read
    new() { RoleId = 2, PermissionId = 3 },  // user:update
    new() { RoleId = 2, PermissionId = 21 }, // role:read
    new() { RoleId = 2, PermissionId = 22 }, // role:update

    // 普通员工只有查看权限
    new() { RoleId = 10, PermissionId = 2 },  // user:read
    new() { RoleId = 10, PermissionId = 21 }, // role:read
    new() { RoleId = 10, PermissionId = 41 }, // menu:read

    // 部门经理继承普通员工权限，额外拥有本部门管理权限
    // 通过角色继承自动获得
};
```

---

## 5. 系统菜单

```csharp
var menus = new List<SysMenu>
{
    // 首页
    new()
    {
        BasicId = 1,
        ResourceId = null,
        ParentId = null,
        MenuName = "首页",
        MenuCode = "dashboard",
        MenuType = MenuType.Menu,
        Path = "/dashboard",
        Component = "Dashboard",
        RouteName = "Dashboard",
        Icon = "dashboard",
        Title = "首页",
        IsExternal = false,
        IsCache = true,
        IsVisible = true,
        IsAffix = true,
        Status = YesOrNo.Yes,
        Sort = 1
    },

    // 系统管理（目录）
    new()
    {
        BasicId = 10,
        ResourceId = 1,
        ParentId = null,
        MenuName = "系统管理",
        MenuCode = "system",
        MenuType = MenuType.Directory,
        Path = "/system",
        Component = null,
        RouteName = null,
        Icon = "setting",
        Title = "系统管理",
        IsExternal = false,
        IsCache = false,
        IsVisible = true,
        IsAffix = false,
        Status = YesOrNo.Yes,
        Sort = 10
    },

    // 用户管理
    new()
    {
        BasicId = 11,
        ResourceId = 10,
        ParentId = 10,
        MenuName = "用户管理",
        MenuCode = "user",
        MenuType = MenuType.Menu,
        Path = "/system/user",
        Component = "System/User",
        RouteName = "SystemUser",
        Icon = "user",
        Title = "用户管理",
        IsExternal = false,
        IsCache = true,
        IsVisible = true,
        IsAffix = false,
        Status = YesOrNo.Yes,
        Sort = 11
    },

    // 角色管理
    new()
    {
        BasicId = 12,
        ResourceId = 20,
        ParentId = 10,
        MenuName = "角色管理",
        MenuCode = "role",
        MenuType = MenuType.Menu,
        Path = "/system/role",
        Component = "System/Role",
        RouteName = "SystemRole",
        Icon = "team",
        Title = "角色管理",
        IsExternal = false,
        IsCache = true,
        IsVisible = true,
        IsAffix = false,
        Status = YesOrNo.Yes,
        Sort = 12
    },

    // 权限管理
    new()
    {
        BasicId = 13,
        ResourceId = 30,
        ParentId = 10,
        MenuName = "权限管理",
        MenuCode = "permission",
        MenuType = MenuType.Menu,
        Path = "/system/permission",
        Component = "System/Permission",
        RouteName = "SystemPermission",
        Icon = "safety",
        Title = "权限管理",
        IsExternal = false,
        IsCache = true,
        IsVisible = true,
        IsAffix = false,
        Status = YesOrNo.Yes,
        Sort = 13
    },

    // 菜单管理
    new()
    {
        BasicId = 14,
        ResourceId = 40,
        ParentId = 10,
        MenuName = "菜单管理",
        MenuCode = "menu",
        MenuType = MenuType.Menu,
        Path = "/system/menu",
        Component = "System/Menu",
        RouteName = "SystemMenu",
        Icon = "menu",
        Title = "菜单管理",
        IsExternal = false,
        IsCache = true,
        IsVisible = true,
        IsAffix = false,
        Status = YesOrNo.Yes,
        Sort = 14
    },

    // 部门管理
    new()
    {
        BasicId = 15,
        ResourceId = 50,
        ParentId = 10,
        MenuName = "部门管理",
        MenuCode = "department",
        MenuType = MenuType.Menu,
        Path = "/system/department",
        Component = "System/Department",
        RouteName = "SystemDepartment",
        Icon = "apartment",
        Title = "部门管理",
        IsExternal = false,
        IsCache = true,
        IsVisible = true,
        IsAffix = false,
        Status = YesOrNo.Yes,
        Sort = 15
    }
};
```

---

## 6. 约束规则

### 静态职责分离（SSD）

```csharp
var ssdRules = new List<SysConstraintRule>
{
    // 出纳与审计职责分离
    new()
    {
        BasicId = 1,
        RuleCode = "ssd_cashier_auditor",
        RuleName = "出纳与审计职责分离",
        ConstraintType = ConstraintType.SSD,
        TargetType = "Role",
        Parameters = JsonSerializer.Serialize(new
        {
            conflictRoles = new[] { /* cashierRoleId, auditorRoleId */ },
            maxAllowed = 1,
            description = "用户不能同时拥有出纳和审计角色"
        }),
        IsEnabled = true,
        ViolationAction = ViolationAction.Deny,
        Description = "财务系统核心约束：出纳与审计职责必须分离",
        Priority = 100,
        Status = YesOrNo.Yes
    },

    // 采购与审批职责分离
    new()
    {
        BasicId = 2,
        RuleCode = "ssd_purchaser_approver",
        RuleName = "采购与审批职责分离",
        ConstraintType = ConstraintType.SSD,
        TargetType = "Role",
        Parameters = JsonSerializer.Serialize(new
        {
            conflictRoles = new[] { /* purchaserRoleId, approverRoleId */ },
            maxAllowed = 1,
            description = "用户不能同时拥有采购员和审批人角色"
        }),
        IsEnabled = true,
        ViolationAction = ViolationAction.Deny,
        Description = "采购流程约束：采购与审批职责必须分离",
        Priority = 90,
        Status = YesOrNo.Yes
    }
};
```

### 基数约束

```csharp
var cardinalityRules = new List<SysConstraintRule>
{
    // 用户角色数量限制
    new()
    {
        BasicId = 10,
        RuleCode = "cardinality_user_role_max",
        RuleName = "用户角色数量限制",
        ConstraintType = ConstraintType.Cardinality,
        TargetType = "User",
        Parameters = JsonSerializer.Serialize(new
        {
            targetType = "Role",
            maxCount = 5,
            description = "一个用户最多只能拥有5个角色"
        }),
        IsEnabled = true,
        ViolationAction = ViolationAction.Warning,
        Description = "防止角色滥用",
        Priority = 50,
        Status = YesOrNo.Yes
    }
};
```

---

## 7. 超级管理员

```csharp
// 创建超级管理员账号
var superAdmin = new SysUser
{
    BasicId = 1,
    TenantId = null,
    UserName = "admin",
    Password = "hashed_password_here", // 实际应该是加密后的密码
    RealName = "超级管理员",
    Email = "admin@xihanfun.com",
    Phone = "13800138000",
    Gender = UserGender.Unknown,
    Status = YesOrNo.Yes
};

// 分配超级管理员角色
var superAdminRole = new SysUserRole
{
    UserId = 1,
    RoleId = 1  // 超级管理员角色
};
```

---

## 📝 使用示例

### C# 代码示例

```csharp
public class RbacSeeder
{
    private readonly IServiceProvider _serviceProvider;

    public async Task SeedAsync()
    {
        // 1. 初始化操作
        await SeedOperationsAsync();

        // 2. 初始化资源
        await SeedResourcesAsync();

        // 3. 初始化权限
        await SeedPermissionsAsync();

        // 4. 初始化角色
        await SeedRolesAsync();

        // 5. 初始化菜单
        await SeedMenusAsync();

        // 6. 初始化约束规则
        await SeedConstraintRulesAsync();

        // 7. 创建超级管理员
        await SeedSuperAdminAsync();
    }
}
```

---

**最后更新**：2026-01-07  
**维护者**：XiHan Development Team
