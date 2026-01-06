# XiHan.BasicApp.Rbac 完整指南

> **标准 RBAC + 扩展能力（角色继承、DSD、约束规则）**  
> **版本**：v2.0  
> **最后更新**：2026-01-07

---

## 📖 目录

1. [核心概念](#核心概念)
2. [实体设计](#实体设计)
3. [权限模型](#权限模型)
4. [使用场景](#使用场景)
5. [种子数据示例](#种子数据示例)
6. [最佳实践](#最佳实践)

---

## 🎯 核心概念

### RBAC 标准模型

```
用户（User） → 角色（Role） → 权限（Permission）
                                    ↓
                            资源（Resource） + 操作（Operation）
```

### 核心公式

```
权限（Permission） = 资源（Resource） + 操作（Operation）
```

**示例**：

- `user:create` = 用户资源 + 创建操作
- `order:view` = 订单资源 + 查看操作
- `file:download` = 文件资源 + 下载操作

---

## 🏗️ 实体设计

### 1. 核心实体（标准 RBAC）

#### 1.1 SysResource - 资源表

**作用**：统一抽象所有可被授权的资源（菜单、API、按钮、文件等）

```csharp
public class SysResource
{
    long BasicId;                  // 资源ID
    long? ParentId;                // 父资源ID（支持树结构）
    string ResourceCode;           // 资源编码（如：user, order）
    string ResourceName;           // 资源名称
    ResourceType ResourceType;     // 资源类型（Menu/Api/Button/File等）
    string? ResourcePath;          // 资源路径
    string? Icon;                  // 资源图标
    string? Description;           // 资源描述
    string? Metadata;              // 资源元数据（JSON）
    bool RequireAuth;              // 是否需要认证
    bool IsPublic;                 // 是否公开资源
    YesOrNo Status;                // 状态
    int Sort;                      // 排序
}
```

**资源类型**：

- `Menu`：菜单资源（目录、菜单项）
- `Api`：API 接口资源
- `Button`：按钮资源（页面操作按钮）
- `File`：文件资源
- `DataTable`：数据表资源
- `Element`：页面元素资源
- `BusinessObject`：业务对象资源

---

#### 1.2 SysOperation - 操作表

**作用**：定义可对资源执行的操作类型

```csharp
public class SysOperation
{
    long BasicId;                     // 操作ID
    string OperationCode;             // 操作编码（如：create, read, update, delete）
    string OperationName;             // 操作名称
    OperationTypeCode OperationTypeCode;  // 操作类型代码
    OperationCategory Category;       // 操作分类（CRUD/Business/Admin/System）
    HttpMethodType? HttpMethod;       // HTTP方法（针对API资源）
    string? Description;              // 操作描述
    string? Icon;                     // 操作图标
    string? Color;                    // 操作颜色
    bool IsDangerous;                 // 是否危险操作
    bool RequireAudit;                // 是否需要审计
    YesOrNo Status;                   // 状态
    int Sort;                         // 排序
}
```

**标准操作**：

- `Create`：创建/新增
- `Read`：读取/查询
- `Update`：更新/修改
- `Delete`：删除
- `Execute`：执行/操作
- `Approve`：审批
- `Import`：导入
- `Export`：导出
- `Download`：下载
- `Upload`：上传
- `Grant`：授权/授予
- `Revoke`：撤销/收回

---

#### 1.3 SysPermission - 权限表

**作用**：权限 = 资源 + 操作

```csharp
public class SysPermission
{
    long BasicId;                  // 权限ID
    long ResourceId;               // 资源ID（必填）
    long OperationId;              // 操作ID（必填）
    string PermissionCode;         // 权限编码（资源编码:操作编码）
    string PermissionName;         // 权限名称
    string? PermissionDescription; // 权限描述
    string? Tags;                  // 权限标签（admin,sensitive,audit）
    bool RequireAudit;             // 是否需要审计
    int Priority;                  // 优先级
    YesOrNo Status;                // 状态
    int Sort;                      // 排序
}
```

**权限编码格式**：`{资源编码}:{操作编码}`

**示例**：

```
user:create        → 创建用户
user:read          → 查看用户
user:update        → 更新用户
user:delete        → 删除用户
order:view         → 查看订单
order:approve      → 审批订单
file:download      → 下载文件
report:export      → 导出报表
```

---

#### 1.4 SysRole - 角色表

**作用**：角色是权限的集合

```csharp
public class SysRole
{
    long BasicId;                  // 角色ID
    long? ParentRoleId;            // 父角色ID（简单继承）
    string RoleCode;               // 角色编码
    string RoleName;               // 角色名称
    string? RoleDescription;       // 角色描述
    RoleType RoleType;             // 角色类型
    DataPermissionScope DataScope; // 数据权限范围
    YesOrNo Status;                // 状态
    int Sort;                      // 排序
}
```

---

#### 1.5 SysUser - 用户表

**作用**：系统用户

```csharp
public class SysUser
{
    long BasicId;                  // 用户ID
    long? TenantId;                // 租户ID
    string UserName;               // 用户名
    string Password;               // 密码（加密）
    string? RealName;              // 真实姓名
    string? Email;                 // 邮箱
    string? Phone;                 // 手机号
    UserGender Gender;             // 性别
    YesOrNo Status;                // 状态
    DateTimeOffset? LastLoginTime; // 最后登录时间
    string? LastLoginIp;           // 最后登录IP
}
```

---

#### 1.6 关联表

```csharp
// 用户-角色映射
public class SysUserRole
{
    long UserId;    // 用户ID
    long RoleId;    // 角色ID
}

// 角色-权限映射
public class SysRolePermission
{
    long RoleId;        // 角色ID
    long PermissionId;  // 权限ID
}

// 用户-权限映射（直接授权/禁止）
public class SysUserPermission
{
    long UserId;              // 用户ID
    long PermissionId;        // 权限ID
    PermissionAction PermissionAction;  // Grant/Deny
}
```

---

### 2. 扩展实体（角色继承、DSD、约束）

#### 2.1 SysRoleHierarchy - 角色继承关系表

**作用**：支持角色多继承，子角色继承父角色的所有权限

```csharp
public class SysRoleHierarchy
{
    long BasicId;           // ID
    long ParentRoleId;      // 父角色ID
    long ChildRoleId;       // 子角色ID
    int Depth;              // 继承深度（0=直接继承）
    bool IsDirect;          // 是否直接继承
    string? InheritancePath; // 继承路径（如：1 > 3 > 5）
    YesOrNo Status;         // 状态
}
```

**使用场景**：

- 部门经理 = 经理角色 + 部门角色
- 项目负责人 = 项目成员角色 + 审批角色

---

#### 2.2 SysSessionRole - 会话角色映射表

**作用**：记录会话中激活的角色，支持动态职责分离（DSD）

```csharp
public class SysSessionRole
{
    long BasicId;               // ID
    long SessionId;             // 会话ID
    long RoleId;                // 角色ID
    DateTimeOffset ActivatedAt; // 激活时间
    DateTimeOffset? DeactivatedAt; // 停用时间
    DateTimeOffset? ExpiresAt;  // 过期时间
    SessionRoleStatus Status;   // 状态（Active/Inactive/Expired）
    string? Reason;             // 激活原因
}
```

**使用场景**：

- 用户拥有"出纳"和"审计"角色，但同一会话只能激活其中一个
- 防止角色冲突导致的安全问题

---

#### 2.3 SysConstraintRule - 约束规则表

**作用**：定义 RBAC 约束规则（SSD、DSD、互斥约束等）

```csharp
public class SysConstraintRule
{
    long BasicId;              // ID
    string RuleCode;           // 规则编码
    string RuleName;           // 规则名称
    ConstraintType ConstraintType; // 约束类型
    string TargetType;         // 约束目标类型（Role/Permission/User）
    string Parameters;         // 约束参数（JSON）
    bool IsEnabled;            // 是否启用
    ViolationAction ViolationAction; // 违规处理方式
    string? Description;       // 规则描述
    int Priority;              // 规则优先级
    DateTimeOffset? EffectiveFrom; // 生效时间
    DateTimeOffset? EffectiveTo;   // 失效时间
    YesOrNo Status;            // 状态
}
```

**约束类型**：

1. **SSD（静态职责分离）**

   ```json
   {
     "conflictRoles": [1, 2, 3],
     "maxAllowed": 1,
     "description": "用户不能同时拥有采购员和审批人角色"
   }
   ```

2. **DSD（动态职责分离）**

   ```json
   {
     "conflictRoles": [4, 5],
     "timeWindow": "8h",
     "description": "同一会话不能同时激活出纳和会计角色"
   }
   ```

3. **基数约束**

   ```json
   {
     "targetType": "Role",
     "maxCount": 5,
     "description": "一个用户最多只能拥有5个角色"
   }
   ```

4. **先决条件约束**
   ```json
   {
     "requiredRole": 1,
     "targetRole": 2,
     "description": "必须先拥有普通员工角色才能获得部门经理角色"
   }
   ```

---

### 3. 菜单与数据权限

#### 3.1 SysMenu - 菜单表

**作用**：前端菜单配置，与 SysResource 一对一关系

```csharp
public class SysMenu
{
    long BasicId;           // 菜单ID
    long? ResourceId;       // 关联资源ID
    long? ParentId;         // 父级菜单ID
    string MenuName;        // 菜单名称
    string MenuCode;        // 菜单编码
    MenuType MenuType;      // 菜单类型
    string? Path;           // 路由地址
    string? Component;      // 组件路径
    string? RouteName;      // 路由名称
    string? Redirect;       // 重定向地址
    string? Icon;           // 菜单图标
    string? Title;          // 菜单标题
    bool IsExternal;        // 是否外链
    string? ExternalUrl;    // 外链地址
    bool IsCache;           // 是否缓存
    bool IsVisible;         // 是否显示
    bool IsAffix;           // 是否固定标签
    string? Metadata;       // 菜单元数据（JSON）
    YesOrNo Status;         // 状态
    int Sort;               // 排序
}
```

**菜单 vs 资源**：

- `SysResource`：提供权限控制
- `SysMenu`：提供前端界面配置
- 关系：一个资源可以被多个菜单引用

---

#### 3.2 SysRoleDataScope - 角色数据权限范围表

**作用**：自定义数据权限规则

```csharp
public class SysRoleDataScope
{
    long BasicId;      // ID
    long RoleId;       // 角色ID
    long DepartmentId; // 部门ID
    YesOrNo Status;    // 状态
}
```

**数据权限范围**（`SysRole.DataScope`）：

- `All`：全部数据
- `DepartmentAndChildren`：本部门及子部门
- `DepartmentOnly`：仅本部门
- `SelfOnly`：仅本人
- `Custom`：自定义（通过 `SysRoleDataScope` 配置）

---

## 🎨 权限模型

### 权限判断流程

```
1. 用户登录 → 获取用户所有角色
2. 获取角色的所有权限（包括继承的权限）
3. 获取用户的直接权限（Grant/Deny）
4. 合并权限：
   - 用户直接权限（Deny）优先级最高
   - 用户直接权限（Grant）次之
   - 角色权限最低
5. 检查约束规则（SSD/DSD）
6. 返回最终权限结果
```

### 权限计算公式

```
最终权限 = 用户直接权限（Deny） OR (用户直接权限（Grant） OR 角色权限)
```

---

## 🔥 使用场景

### 场景 1：用户管理系统

#### 资源定义

```csharp
// 1. 定义资源
SysResource userResource = new()
{
    ResourceCode = "user",
    ResourceName = "用户管理",
    ResourceType = ResourceType.Menu,
    ResourcePath = "/api/users"
};

// 2. 定义操作
SysOperation[] operations = [
    new() { OperationCode = "create", OperationName = "创建用户" },
    new() { OperationCode = "read", OperationName = "查看用户" },
    new() { OperationCode = "update", OperationName = "更新用户" },
    new() { OperationCode = "delete", OperationName = "删除用户" }
];

// 3. 生成权限
SysPermission[] permissions = [
    new() { ResourceId = userResource.Id, OperationId = operations[0].Id, PermissionCode = "user:create" },
    new() { ResourceId = userResource.Id, OperationId = operations[1].Id, PermissionCode = "user:read" },
    new() { ResourceId = userResource.Id, OperationId = operations[2].Id, PermissionCode = "user:update" },
    new() { ResourceId = userResource.Id, OperationId = operations[3].Id, PermissionCode = "user:delete" }
];
```

#### 角色分配

```csharp
// 1. 创建角色
SysRole adminRole = new()
{
    RoleCode = "admin",
    RoleName = "管理员",
    DataScope = DataPermissionScope.All
};

SysRole viewerRole = new()
{
    RoleCode = "viewer",
    RoleName = "查看者",
    DataScope = DataPermissionScope.SelfOnly
};

// 2. 分配权限
// 管理员拥有所有权限
await rolePermissionService.GrantPermissionsToRoleAsync(adminRole.Id, permissions.Select(p => p.Id));

// 查看者只有查看权限
await rolePermissionService.GrantPermissionToRoleAsync(viewerRole.Id, permissions[1].Id);

// 3. 分配角色给用户
await userRoleService.AssignRoleToUserAsync(userId, adminRole.Id);
```

---

### 场景 2：职责分离（财务系统）

```csharp
// 1. 创建互斥角色
SysRole cashierRole = new() { RoleCode = "cashier", RoleName = "出纳" };
SysRole auditorRole = new() { RoleCode = "auditor", RoleName = "审计" };

// 2. 创建 SSD 约束规则
SysConstraintRule ssdRule = new()
{
    RuleCode = "ssd_cashier_auditor",
    RuleName = "出纳与审计职责分离",
    ConstraintType = ConstraintType.SSD,
    TargetType = "Role",
    Parameters = JsonSerializer.Serialize(new
    {
        conflictRoles = new[] { cashierRole.Id, auditorRole.Id },
        maxAllowed = 1
    }),
    IsEnabled = true,
    ViolationAction = ViolationAction.Deny
};

// 3. 尝试分配冲突角色时会被拒绝
try
{
    await userRoleService.AssignRoleToUserAsync(userId, cashierRole.Id);
    await userRoleService.AssignRoleToUserAsync(userId, auditorRole.Id); // 抛出异常
}
catch (ConstraintViolationException ex)
{
    Console.WriteLine(ex.Message); // "违反职责分离约束：用户不能同时拥有出纳和审计角色"
}
```

---

### 场景 3：角色继承

```csharp
// 1. 创建角色层级
SysRole employeeRole = new() { RoleCode = "employee", RoleName = "普通员工" };
SysRole managerRole = new() { RoleCode = "manager", RoleName = "部门经理", ParentRoleId = employeeRole.Id };
SysRole directorRole = new() { RoleCode = "director", RoleName = "总监", ParentRoleId = managerRole.Id };

// 2. 创建多继承关系
await roleHierarchyService.AddHierarchyAsync(new SysRoleHierarchy
{
    ParentRoleId = employeeRole.Id,
    ChildRoleId = managerRole.Id,
    Depth = 0,
    IsDirect = true
});

// 部门经理自动继承普通员工的所有权限
// 总监自动继承部门经理和普通员工的所有权限
```

---

## 💾 种子数据示例

请查看 `SEED_DATA_EXAMPLE.md` 文件获取完整的种子数据示例。

---

## ✨ 最佳实践

### 1. 权限粒度设计

**推荐**：细粒度权限 + 角色组合

```
❌ 不推荐：创建大而全的角色
  - SuperAdmin（拥有所有权限）

✅ 推荐：创建细粒度权限，通过角色组合
  - UserManager（user:create, user:read, user:update, user:delete）
  - UserViewer（user:read）
  - ReportExporter（report:export）
```

### 2. 权限命名规范

```
格式：{资源}:{操作}[:{子资源}]

示例：
  - user:create           → 创建用户
  - user:read             → 查看用户
  - user:role:assign      → 分配用户角色
  - order:approve         → 审批订单
  - report:finance:export → 导出财务报表
```

### 3. 角色设计原则

1. **按职能划分**：按用户的实际职能创建角色

   - 销售经理、财务主管、HR 专员

2. **按数据范围划分**：相同职能但不同数据权限

   - 部门经理（本部门数据）、总经理（全部数据）

3. **按临时需求划分**：临时性角色
   - 项目临时负责人、活动审批人

### 4. 约束规则使用建议

- **金融系统**：必须使用 SSD/DSD 防止舞弊
- **政府系统**：必须使用审计日志 + 约束规则
- **企业系统**：根据实际需求选择性使用

### 5. 性能优化

1. **权限缓存**：将用户权限缓存到 Redis

   ```csharp
   // 缓存Key: permission:{userId}
   // 缓存时间: 30分钟
   // 失效策略: 角色/权限变更时主动清除
   ```

2. **批量查询**：一次性加载用户所有权限

   ```csharp
   var permissions = await GetUserAllPermissionsAsync(userId);
   ```

3. **索引优化**：确保关键字段有索引
   - `SysPermission.ResourceId`
   - `SysPermission.OperationId`
   - `SysUserRole.UserId`
   - `SysRolePermission.RoleId`

---

## 📚 相关文档

- [数据库表设计](../docs/5.核心数据库表设计.md)
- [种子数据示例](./SEED_DATA_EXAMPLE.md)
- [开发计划](../docs/2.DevelopmentPlan.md)
- [架构设计](./ARCHITECTURE.md)

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

---

**最后更新**：2026-01-07  
**维护者**：XiHan Development Team
