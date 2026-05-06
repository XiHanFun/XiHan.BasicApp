# XiHan.BasicApp 菜单收敛与页面内嵌功能开发方案

> 版本：v2.0  
> 日期：2026-05-07  
> 状态：待实施  
> 事实源：`E:\Repository\XiHanFun\开发设计\目录.md`  
> 适用范围：`XiHan.BasicApp/backend/src/modules/XiHan.BasicApp.Saas`、`XiHan.BasicApp/frontend/src`

## 1. 背景与目标

当前 SaaS 模块已经有较完整的用户、角色、权限、租户、菜单、字典、文件、任务、审批、OAuth、消息和日志实体。问题不在能力缺失，而在菜单入口过细：中间表、关联表、日志明细表、配置子表被铺成独立菜单后，用户需要理解数据库结构才能使用系统。

本方案以 `开发设计/目录.md` 为导航事实源，把后台菜单收敛为：

```text
工作台
系统管理
平台管理
日志管理
开发工具
```

目标：

- 主实体保留独立菜单，关联实体和明细实体进入主页面内的 Tab、抽屉、弹窗、选择器或按钮操作。
- 菜单结构与当前 56 个 SaaS 根实体一一有归属，避免实体漏管或重复入口。
- 后端继续使用 DynamicApi，不新增 Controller。
- 前端主要改 `frontend/src`，不改或少改 `frontend/packages`。
- 菜单权限、按钮权限、数据范围和审计保持后端可信，不依赖前端隐藏。
- 旧路径提供重定向或下线说明，避免用户收藏和动态路由缓存直接失效。

非目标：

- 不在本文中重构实体字段、聚合根、仓储基类，这些仍按 `RefactoringPlan.md` 和 `REFACTOR_OPTIMIZATION_PLAN.md` 推进。
- 不删除子实体 AppService。页面收敛不等于 API 能力删除。
- 不把 SaaS 业务概念下沉到 `XiHan.Framework`。

## 2. 当前代码事实

基于当前工作区扫描：

- `Domain/Entities` 下有 56 个 `Sys*.cs` 根实体。
- `SysMenu` 支持 `MenuType`、`PermissionId`、`Path`、`Component`、`RouteName`、`IsVisible`、`Metadata`，可表达目录、页面菜单和按钮权限。
- `SysPermission` 支持 `PermissionType`、`ModuleCode`、`PermissionCode`、`ResourceId?`、`OperationId?`、`Priority`、`IsRequireAudit`，可承载菜单、按钮、功能、资源和数据范围权限。
- `SaasMenuSeeder.BuildDefinitions()` 当前仍有大量按实体铺开的独立菜单，例如 `system.user-role`、`system.role-permission`、`system.tenant-member`、`system.diff-log`。
- `AuthAppService.BuildMenuRoutesAsync()` 当前会先过滤 `menu.IsVisible` 再构建路由。如果要保留隐藏旧路由，必须调整该逻辑；否则旧路径只能走前端静态重定向。
- 前端 `frontend/src/views/system` 已有一批旧页面，可以迁移为主页面内组件，也可以先用重定向过渡。
- 任务调度和审批流程已有后端实体与服务，但当前前端菜单和页面需要补齐或迁移。
- 开发工具不由 SaaS 实体支撑，应由 `XiHan.BasicApp.CodeGeneration` 模块接入，不应由 SaaS 菜单种子强行生成。

## 3. 需求项

| ID | 标题 | 范围 | 完成标准 | 依赖 |
|----|------|------|----------|------|
| REQ-001 | 固化目标目录树 | `SaasMenuSeeder`、动态路由、`目录.md` | 菜单顶层为工作台、系统管理、平台管理、日志管理、开发工具 | 无 |
| REQ-002 | 收敛工作台入口 | 工作台、个人中心、站内信 | `/workbench` 下有仪表盘、站内信、个人中心；`/profile` 重定向到 `/workbench/profile` | REQ-001 |
| REQ-003 | 收敛系统管理 | 用户、角色、机构、权限、消息 | 账号、角色、机构、权限中心、消息中心成为系统管理可见菜单，相关子实体均内嵌 | REQ-001 |
| REQ-004 | 收敛平台管理 | 租户、菜单、配置、字典、文件、任务、审批、OAuth、监控、缓存 | 平台管理承载平台级能力，租户版本、任务日志、审批记录、OAuth 令牌等不独立成菜单 | REQ-001 |
| REQ-005 | 收敛日志管理 | 登录、访问、操作、API、异常、差异、权限变更日志 | 日志管理保留五个主日志入口；差异日志和权限变更日志进入操作日志详情 | REQ-001 |
| REQ-006 | 隔离开发工具 | `XiHan.BasicApp.CodeGeneration` | 开发工具作为接入位，不由 SaaS 实体菜单种子生成无支撑页面 | REQ-001 |
| REQ-007 | 对齐权限与审计 | `SaasPermissionCodes`、按钮菜单、AppService | 页面内按钮绑定权限码；授权、数据范围、FLS、跨租户、导出、强制下线等操作写审计 | REQ-002 至 REQ-005 |
| REQ-008 | 提供迁移和回滚 | 旧路径、缓存、种子、前端路由 | 旧路径有重定向表；菜单缓存和授权快照失效策略明确；可回滚到旧菜单种子 | REQ-001 至 REQ-007 |
| REQ-009 | 完成验证门禁 | 后端构建、前端类型检查、权限回归 | 构建、类型检查、菜单、权限、租户隔离回归通过 | REQ-001 至 REQ-008 |

## 4. 目标目录树

```text
后台目录
├── 工作台 /workbench
│   ├── 仪表盘 /workbench/dashboard -> workbench/dashboard/index
│   │   └── 主体：SysUserStatistics 聚合视图
│   ├── 站内信 /workbench/inbox -> workbench/inbox/index
│   │   └── 主体：SysUserNotification
│   └── 个人中心 /workbench/profile -> _core/profile/index
│       └── 主体：ProfileAppService、当前登录用户视图
├── 系统管理 /system
│   ├── 账号管理 /system/user -> system/user/index
│   │   └── 内嵌：租户、部门、角色、权限、数据范围、安全、会话、第三方绑定、密码历史
│   ├── 角色管理 /system/role -> system/role/index
│   │   └── 内嵌：角色层级、角色权限、角色数据范围、会话角色
│   ├── 机构管理 /system/org -> system/department/index
│   │   └── 内嵌：机构层级
│   ├── 权限中心 /system/permission -> system/permission/index
│   │   └── 内嵌：资源、操作、ABAC 条件、委托、申请、变更历史、字段级安全
│   └── 消息中心 /system/message -> system/message/index
│       └── 内嵌：通知公告、邮件记录、短信记录、用户通知状态
├── 平台管理 /platform
│   ├── 应用管理 /platform/app -> system/oauth-app/index
│   │   └── 内嵌：OAuth 授权码、OAuth 令牌
│   ├── 租户管理 /platform/tenant -> system/tenant/index
│   │   └── 内嵌：租户成员、租户版本、版本权限
│   ├── 菜单管理 /platform/menu -> system/menu/index
│   │   └── 内嵌：按钮菜单、权限绑定
│   ├── 参数配置 /platform/config -> system/config/index
│   │   └── 内嵌：约束规则、规则项、系统版本、迁移历史
│   ├── 字典管理 /platform/dict -> system/dict/index
│   │   └── 内嵌：字典项
│   ├── 文件管理 /platform/file -> system/file/index
│   │   └── 内嵌：文件存储配置
│   ├── 任务调度 /platform/job -> system/job/index
│   │   └── 内嵌：任务执行日志
│   ├── 审批流程 /platform/approval -> approvalFlow/index
│   │   └── 内嵌：审批记录
│   ├── 系统监控 /platform/server -> system/server/index
│   └── 缓存管理 /platform/cache -> system/cache/index
├── 日志管理 /log
│   ├── 登录日志 /log/login -> system/login-log/index
│   ├── 访问日志 /log/access -> system/access-log/index
│   ├── 操作日志 /log/operation -> system/operation-log/index
│   │   └── 内嵌：差异日志、权限变更日志
│   ├── API 日志 /log/api -> system/api-log/index
│   └── 异常日志 /log/exception -> system/exception-log/index
└── 开发工具 /develop
    ├── 库表管理 /develop/database -> system/database/index
    ├── 代码生成 /develop/codeGen -> system/codeGen/index
    └── 表单设计 /develop/formDes -> system/formDes/index
```

设计说明：

- `个人中心` 归入工作台，路径为 `/workbench/profile`，旧 `/profile` 做重定向。
- `开发工具` 只作为 CodeGeneration 模块接入位，SaaS 菜单种子不创建无后端支撑的菜单。
- `日志管理` 保留多个主日志入口，而不是单一日志中心，这是当前产品目录的最终取舍。
- `消息中心` 放入系统管理，聚合通知、邮件、短信；个人消息仍在工作台站内信展示。

## 5. 主菜单清单

| 目录 | 菜单 | 主体 | 路由 | 组件 | 权限入口 |
|------|------|------|------|------|----------|
| 工作台 | 仪表盘 | `SysUserStatistics` 聚合视图 | `/workbench/dashboard` | `workbench/dashboard/index` | `saas:user-statistics:read` |
| 工作台 | 站内信 | `SysUserNotification` | `/workbench/inbox` | `workbench/inbox/index` | 用户自身消息权限 |
| 工作台 | 个人中心 | `ProfileAppService` | `/workbench/profile` | `_core/profile/index` | 当前登录用户 |
| 系统管理 | 账号管理 | `SysUser` | `/system/user` | `system/user/index` | `saas:user:read` |
| 系统管理 | 角色管理 | `SysRole` | `/system/role` | `system/role/index` | `saas:role:read` |
| 系统管理 | 机构管理 | `SysDepartment` | `/system/org` | `system/department/index` | `saas:department:read` |
| 系统管理 | 权限中心 | `SysPermission` | `/system/permission` | `system/permission/index` | `saas:permission:read` |
| 系统管理 | 消息中心 | `SysNotification`、`SysEmail`、`SysSms` | `/system/message` | `system/message/index` | `saas:message:read` |
| 平台管理 | 应用管理 | `SysOAuthApp` | `/platform/app` | `system/oauth-app/index` | `saas:oauth-app:read` |
| 平台管理 | 租户管理 | `SysTenant` | `/platform/tenant` | `system/tenant/index` | `saas:tenant:read` |
| 平台管理 | 菜单管理 | `SysMenu` | `/platform/menu` | `system/menu/index` | `saas:menu:read` |
| 平台管理 | 参数配置 | `SysConfig` | `/platform/config` | `system/config/index` | `saas:config:read` |
| 平台管理 | 字典管理 | `SysDict` | `/platform/dict` | `system/dict/index` | `saas:dict:read` |
| 平台管理 | 文件管理 | `SysFile` | `/platform/file` | `system/file/index` | `saas:file:read` |
| 平台管理 | 任务调度 | `SysTask` | `/platform/job` | `system/job/index` | `saas:task:read` |
| 平台管理 | 审批流程 | `SysReview` | `/platform/approval` | `approvalFlow/index` | `saas:review:read` |
| 平台管理 | 系统监控 | `ServerAppService` | `/platform/server` | `system/server/index` | `saas:config:read` 或运维权限 |
| 平台管理 | 缓存管理 | `CacheAppService` | `/platform/cache` | `system/cache/index` | `saas:config:read` 或运维权限 |
| 日志管理 | 登录日志 | `SysLoginLog` | `/log/login` | `system/login-log/index` | `saas:login-log:read` |
| 日志管理 | 访问日志 | `SysAccessLog` | `/log/access` | `system/access-log/index` | `saas:access-log:read` |
| 日志管理 | 操作日志 | `SysOperationLog` | `/log/operation` | `system/operation-log/index` | `saas:operation-log:read` |
| 日志管理 | API 日志 | `SysApiLog` | `/log/api` | `system/api-log/index` | `saas:api-log:read` |
| 日志管理 | 异常日志 | `SysExceptionLog` | `/log/exception` | `system/exception-log/index` | `saas:exception-log:read` |

## 6. 实体归属矩阵

### 6.1 工作台

| 实体或服务 | 呈现方式 | 说明 |
|------------|----------|------|
| `SysUserStatistics` | 仪表盘卡片、趋势图、用户详情概览 | 当前用户或授权范围内用户统计 |
| `SysUserNotification` | 站内信列表、顶部消息未读数 | 个人消息状态，不作为后台消息发布入口 |
| `ProfileAppService` | 个人中心页面 | 个人资料、密码修改、第三方绑定、个人通知设置 |

### 6.2 系统管理

| 页面 | 主体 | 页面内嵌实体 |
|------|------|--------------|
| 账号管理 | `SysUser` | `SysTenantUser`、`SysUserDepartment`、`SysUserRole`、`SysUserPermission`、`SysUserDataScope`、`SysUserSecurity`、`SysUserSession`、`SysUserStatistics`、`SysExternalLogin`、`SysPasswordHistory` |
| 角色管理 | `SysRole` | `SysRoleHierarchy`、`SysRolePermission`、`SysRoleDataScope`、`SysSessionRole` |
| 机构管理 | `SysDepartment` | `SysDepartmentHierarchy` |
| 权限中心 | `SysPermission` | `SysResource`、`SysOperation`、`SysPermissionCondition`、`SysPermissionDelegation`、`SysPermissionRequest`、`SysPermissionChangeLog`、`SysFieldLevelSecurity` |
| 消息中心 | `SysNotification`、`SysEmail`、`SysSms` | `SysUserNotification` |

### 6.3 平台管理

| 页面 | 主体 | 页面内嵌实体 |
|------|------|--------------|
| 应用管理 | `SysOAuthApp` | `SysOAuthCode`、`SysOAuthToken` |
| 租户管理 | `SysTenant` | `SysTenantUser`、`SysTenantEdition`、`SysTenantEditionPermission` |
| 菜单管理 | `SysMenu` | 按钮菜单、权限绑定 |
| 参数配置 | `SysConfig` | `SysConstraintRule`、`SysConstraintRuleItem`、`SysVersion`、`SysMigrationHistory` |
| 字典管理 | `SysDict` | `SysDictItem` |
| 文件管理 | `SysFile` | `SysFileStorage` |
| 任务调度 | `SysTask` | `SysTaskLog` |
| 审批流程 | `SysReview` | `SysReviewLog` |
| 系统监控 | `ServerAppService` | 无实体 |
| 缓存管理 | `CacheAppService` | 无实体 |

### 6.4 日志管理

| 页面 | 主体 | 页面内嵌实体 |
|------|------|--------------|
| 登录日志 | `SysLoginLog` | 无 |
| 访问日志 | `SysAccessLog` | 无 |
| 操作日志 | `SysOperationLog` | `SysDiffLog`、`SysPermissionChangeLog` |
| API 日志 | `SysApiLog` | 无 |
| 异常日志 | `SysExceptionLog` | 无 |

### 6.5 不作为 SaaS 菜单的实体或入口

| 对象 | 处理 |
|------|------|
| `SysDiffLog` | 并入操作日志详情 |
| `SysPermissionChangeLog` | 并入操作日志详情或权限中心详情 |
| `SysTaskLog` | 并入任务调度详情 |
| `SysReviewLog` | 并入审批流程详情 |
| `SysOAuthCode`、`SysOAuthToken` | 并入应用管理详情 |
| `SysDictItem` | 并入字典管理详情 |
| `SysFileStorage` | 并入文件管理设置抽屉 |
| `SysConstraintRuleItem` | 并入参数配置的约束规则详情 |
| 库表管理、代码生成、表单设计 | 由 `XiHan.BasicApp.CodeGeneration` 模块提供 |

## 7. 页面设计规格

### 7.1 通用规则

- 列表页使用 VxeTable，表单使用 Naive UI `NForm`。
- 主实体页面采用“列表 + 筛选 + 操作列 + 详情抽屉”。
- 子实体优先放到详情抽屉 Tab；需要复杂编辑时使用二级抽屉。
- 多对多关系使用树形选择器、多选框或穿梭框，不单独跳页面。
- 只读日志和历史记录使用表格 Tab，不提供新增入口。
- 页面内按钮通过权限码控制显示，后端 AppService 方法仍必须校验权限。
- 敏感字段只在后端序列化出口和导出出口脱敏，不依赖前端裁剪。

### 7.2 工作台

仪表盘：

- 展示当前用户统计、待办、消息、快捷入口。
- 统计数据来自 `SysUserStatistics` 或聚合查询服务。

站内信：

- 展示 `SysUserNotification` 的个人消息状态。
- 支持已读、未读、删除、跳转业务对象。

个人中心：

- 路径为 `/workbench/profile`。
- 旧路径 `/profile` 重定向到 `/workbench/profile`。
- 支持个人资料、密码修改、第三方绑定、个人通知设置。

### 7.3 系统管理

账号管理：

- 用户详情 Tab 包括基础信息、租户信息、部门分配、角色分配、额外权限、数据范围、安全设置、登录会话、第三方绑定、密码历史。
- `SysUserRole`、`SysUserPermission`、`SysUserDataScope`、`SysUserSession` 不再作为独立可见菜单。
- 重置密码、强制下线、额外授权、数据范围配置必须有独立按钮权限和审计。

角色管理：

- 角色详情 Tab 包括基础信息、角色层级、权限分配、数据范围、授权用户。
- `SysRoleHierarchy`、`SysRolePermission`、`SysRoleDataScope` 不再作为独立可见菜单。
- 权限树应展示 Grant、Deny、有效期、数据范围和优先级。

机构管理：

- 部门树本身表达层级，`SysDepartmentHierarchy` 只作为后端闭包表支撑。
- 调整父级、拖拽排序、禁用部门必须校验子节点和用户关联影响。

权限中心：

- 统一维护权限定义、资源定义、操作定义、ABAC 条件、权限委托、权限申请、FLS、权限变更历史。
- 平台管理员可进入资源、操作、FLS 等低频配置区。
- 普通租户管理员只看到被授权的权限配置能力。

消息中心：

- Tab 包括通知公告、邮件记录、短信记录。
- `SysUserNotification` 作为通知送达状态和个人站内信状态，不作为独立发布菜单。
- 通知撤回、邮件重发、短信重发必须写审计。

### 7.4 平台管理

应用管理：

- `SysOAuthApp` 为主页面。
- 授权码 `SysOAuthCode` 和令牌 `SysOAuthToken` 在应用详情内查看和撤销。
- `ClientSecret` 不明文回显，重置后只允许一次性展示。

租户管理：

- `SysTenant` 为主页面。
- 租户成员、版本套餐、版本权限放入租户详情 Tab。
- 跨租户查看和切换上下文只允许平台管理员，并写审计。

菜单管理：

- `SysMenu` 使用树表维护目录、菜单、按钮。
- 按钮权限使用 `MenuType.Button`，绑定 `PermissionId`。
- 菜单可见性由 `SysMenu.PermissionId` 控制，服务端鉴权仍基于 `SysPermission`。

参数配置：

- `SysConfig` 为主页面。
- 约束规则、规则项、系统版本、迁移历史作为 Tab 或详情抽屉。
- 敏感配置不明文返回 DTO。

字典管理：

- 左侧字典类型 `SysDict`，右侧字典项 `SysDictItem`。
- 字典项支持排序、启停、批量维护，不单独成菜单。

文件管理：

- `SysFile` 为主页面。
- `SysFileStorage` 放入存储配置抽屉。
- 下载、预览、外链生成必须走后端权限校验和有效期控制。

任务调度：

- `SysTask` 为主页面。
- `SysTaskLog` 放入任务详情的执行日志 Tab。
- 立即执行、启停、删除任务必须有按钮权限和审计。

审批流程：

- `SysReview` 为主页面，菜单文案使用“审批流程”。
- `SysReviewLog` 放入流程详情或审批记录 Tab。
- 审批配置变更写操作日志和差异日志。

系统监控、缓存管理：

- 由 `ServerAppService`、`CacheAppService` 提供。
- 不对应实体，但属于平台运维能力。
- 推荐使用运维权限或 `saas:config:read` 的专用派生权限，不建议长期复用配置读取权限。

### 7.5 日志管理

- 登录日志：`SysLoginLog`。
- 访问日志：`SysAccessLog`。
- 操作日志：`SysOperationLog`，详情内展示 `SysDiffLog` 和 `SysPermissionChangeLog`。
- API 日志：`SysApiLog`。
- 异常日志：`SysExceptionLog`。
- 日志导出必须使用独立 export 权限。
- 异常堆栈、请求体、响应体、Token、Cookie、Authorization 等敏感内容必须脱敏。

## 8. 后端开发方案

### 8.1 菜单种子

修改 `Infrastructure/Seeders/SaasMenuSeeder.cs`：

- 顶层目录使用 `workbench`、`system`、`platform`、`log`、`develop`。
- 可见菜单按本文“主菜单清单”生成。
- 子实体旧菜单不再作为可见菜单下发。
- 页面内按钮使用 `MenuType.Button` 并绑定 `SaasPermissionCodes`。
- `develop` 目录如果由 CodeGeneration 模块接入，SaaS Seeder 不应生成无组件或无服务支撑的菜单。

需要下线或隐藏的旧菜单：

```text
system.user-session
system.user-role
system.user-permission
system.user-data-scope
system.role-hierarchy
system.role-permission
system.role-data-scope
system.permission-condition
system.field-level-security
system.tenant-member
system.tenant-edition-permission
system.diff-log
system.permission-change-log
```

### 8.2 路由生成注意点

当前 `AuthAppService.BuildMenuRoutesAsync()` 会过滤 `menu.IsVisible`：

```csharp
.Where(menu => menu.IsVisible)
```

因此有两种实现策略：

- 策略 A：旧路由由前端静态 redirect 处理，后端菜单种子不下发隐藏旧路由。
- 策略 B：调整 `BuildMenuRoutesAsync()`，让隐藏菜单仍能作为路由下发，前端菜单渲染按 `meta.hidden` 隐藏。

推荐先采用策略 A，变更小、风险低。等菜单稳定后，再决定是否支持隐藏动态路由。

### 8.3 聚合查询服务

页面收敛后，建议增加或扩展聚合读模型，减少详情页多次请求：

| 聚合读模型 | 建议位置 | 用途 |
|------------|----------|------|
| `UserManagementDetailDto` | `Application/Dtos/Identity` | 用户详情聚合用户、部门、角色、安全、会话摘要 |
| `RoleManagementDetailDto` | `Application/Dtos/Authorization` | 角色详情聚合层级、权限、数据范围 |
| `TenantManagementDetailDto` | `Application/Dtos/Tenancy` | 租户详情聚合成员、套餐、版本权限摘要 |
| `PermissionCenterDetailDto` | `Application/Dtos/Authorization` | 权限中心聚合资源、操作、ABAC、FLS |
| `OperationLogDetailDto` | `Application/Dtos/Logging` | 操作日志详情聚合差异和权限变更 |
| `MessageCenterSummaryDto` | `Application/Dtos/Messaging` | 消息中心通知、邮件、短信统计 |

子实体 AppService 可以保留，用于 Tab 内分页、授权、撤销、启停、重发等具体操作。

### 8.4 权限码

权限码继续以 `SaasPermissionCodes` 为唯一来源。页面内按钮优先复用已有细粒度权限码：

```text
saas:user-role:grant
saas:user-permission:grant
saas:user-data-scope:update
saas:user-session:revoke
saas:role-permission:grant
saas:role-data-scope:update
saas:tenant-edition-permission:update
saas:oauth-token:read
saas:task:status
saas:review:update
```

需要新增派生权限的场景：

- 导出敏感日志。
- 重置 OAuth 密钥。
- 强制下线。
- 跨租户查看。
- 敏感字段导出。
- 高风险缓存清理。

### 8.5 审计和缓存

必须写审计的操作：

- 用户角色、用户权限、用户数据范围变更。
- 角色权限、角色层级、角色数据范围变更。
- 权限定义、ABAC 条件、FLS、权限委托、权限申请审批。
- 租户套餐、版本权限、租户成员变更。
- OAuth 密钥重置、令牌撤销、授权码撤销。
- 任务启停、立即执行、审批流程变更。
- 跨租户查看、导出、敏感数据访问。

必须失效的缓存：

- 用户授权快照缓存。
- 角色权限和角色层级缓存。
- 菜单路由缓存。
- 租户版本权限缓存。
- 字典缓存。
- 配置缓存。

## 9. 前端开发方案

### 9.1 路由和视图目录

短期建议复用现有 `frontend/src/views/system/*` 组件，先完成菜单和重定向；中期再重组目录。

建议最终目录：

```text
frontend/src/views/
├── workbench/
│   ├── dashboard/
│   ├── inbox/
│   └── profile/
├── system/
│   ├── user/
│   ├── role/
│   ├── org/
│   ├── permission/
│   └── message/
├── platform/
│   ├── app/
│   ├── tenant/
│   ├── menu/
│   ├── config/
│   ├── dict/
│   ├── file/
│   ├── job/
│   ├── approval/
│   ├── server/
│   └── cache/
├── log/
│   ├── login/
│   ├── access/
│   ├── operation/
│   ├── api/
│   └── exception/
└── develop/
```

为了降低风险，第一阶段组件仍可指向旧路径，例如 `/platform/tenant -> system/tenant/index`。

### 9.2 旧路径重定向

| 旧路径 | 新路径 |
|--------|--------|
| `/profile` | `/workbench/profile` |
| `/system/department` | `/system/org` |
| `/system/oauth-app` | `/platform/app` |
| `/system/tenant` | `/platform/tenant` |
| `/system/tenant-member` | `/platform/tenant?tab=members` |
| `/system/tenant-edition` | `/platform/tenant?tab=edition` |
| `/system/tenant-edition-permission` | `/platform/tenant?tab=edition-permission` |
| `/system/menu` | `/platform/menu` |
| `/system/config` | `/platform/config` |
| `/system/constraint-rule` | `/platform/config?tab=constraint-rule` |
| `/system/dict` | `/platform/dict` |
| `/system/file` | `/platform/file` |
| `/system/cache` | `/platform/cache` |
| `/system/server` | `/platform/server` |
| `/system/user-session` | `/system/user?tab=session` |
| `/system/user-role` | `/system/user?tab=role` |
| `/system/user-permission` | `/system/user?tab=permission` |
| `/system/user-data-scope` | `/system/user?tab=data-scope` |
| `/system/role-hierarchy` | `/system/role?tab=hierarchy` |
| `/system/role-permission` | `/system/role?tab=permission` |
| `/system/role-data-scope` | `/system/role?tab=data-scope` |
| `/system/permission-condition` | `/system/permission?tab=condition` |
| `/system/field-level-security` | `/system/permission?tab=fls` |
| `/system/message` | `/system/message` |
| `/system/notification` | `/system/message?tab=notification` |
| `/system/login-log` | `/log/login` |
| `/system/access-log` | `/log/access` |
| `/system/operation-log` | `/log/operation` |
| `/system/api-log` | `/log/api` |
| `/system/exception-log` | `/log/exception` |
| `/system/diff-log` | `/log/operation?drawer=diff` |
| `/system/permission-change-log` | `/log/operation?tab=permission-change` |

### 9.3 API facade

建议新增页面级 facade，避免页面直接散落调用多个子实体 API：

```text
frontend/src/api/modules/workbench/
frontend/src/api/modules/system/user-management.ts
frontend/src/api/modules/system/role-management.ts
frontend/src/api/modules/system/permission-center.ts
frontend/src/api/modules/platform/tenant-management.ts
frontend/src/api/modules/platform/job-management.ts
frontend/src/api/modules/platform/approval-management.ts
frontend/src/api/modules/log/log-management.ts
frontend/src/api/modules/messaging/message-center.ts
```

后端聚合 DTO 未完成前，facade 可以短期并行调用现有 API；后端聚合接口完成后替换为单次请求。

### 9.4 权限控制

前端权限控制分三层：

- 路由层：后端下发菜单和权限码决定可见路由。
- 页面层：Tab 是否展示由权限集合控制。
- 操作层：按钮是否展示由按钮权限码控制。

注意：

- 前端隐藏不是安全边界。
- 无权限 Tab 不发起请求。
- 导出、重置密钥、强制下线、跨租户查看等按钮必须有独立权限码。

## 10. 实施批次

### M0：基线冻结

- 记录后端构建、前端类型检查、前端构建当前状态。
- 导出当前 `SaasMenuSeeder.BuildDefinitions()` 菜单清单。
- 记录当前登录后动态路由返回结构。

验收：

- 形成基线记录。
- 不修改业务行为。

### M1：菜单种子与重定向

- 重写 `SaasMenuSeeder.BuildDefinitions()` 为本文目标目录。
- 添加 `/profile -> /workbench/profile` 和旧系统路径到新路径的前端 redirect。
- 不再下发子实体可见菜单。
- 清理菜单路由缓存。

验收：

- 新登录用户侧边栏只看到目标目录。
- 旧路径可跳转到新位置。

### M2：工作台收敛

- 接入个人中心到 `/workbench/profile`。
- 确认站内信只展示当前用户 `SysUserNotification`。
- 仪表盘接入 `SysUserStatistics` 聚合视图。

验收：

- 工作台三个菜单均可访问。
- `/profile` 重定向生效。

### M3：系统管理收敛

- 账号管理迁入用户角色、用户权限、用户数据范围、用户会话、安全设置、第三方绑定、密码历史。
- 角色管理迁入角色层级、角色权限、角色数据范围。
- 权限中心迁入资源、操作、ABAC、委托、申请、FLS、变更历史。
- 消息中心迁入通知、邮件、短信。

验收：

- 系统管理下不再展示子实体菜单。
- 授权、强制下线、数据范围、FLS 操作有权限和审计。

### M4：平台管理收敛

- 租户详情迁入租户成员、租户版本、版本权限。
- 参数配置迁入约束规则、规则项、版本、迁移历史。
- 文件管理迁入文件存储配置。
- 任务调度和审批流程补齐页面。
- 应用管理迁入 OAuth 授权码和令牌。

验收：

- 平台管理目录完整可访问。
- 子实体只在主页面内出现。

### M5：日志管理收敛

- 登录、访问、操作、API、异常日志迁移到 `/log/*`。
- 操作日志详情接入差异日志和权限变更日志。
- 旧日志路径完成重定向。

验收：

- 日志管理入口清晰。
- 导出和敏感详情查看受权限控制。

### M6：开发工具接入

- 明确 `XiHan.BasicApp.CodeGeneration` 对 `/develop/*` 的菜单提供方式。
- SaaS 模块不生成无支撑的开发工具菜单。

验收：

- 开发工具要么由 CodeGeneration 提供，要么不显示。

### M7：清理与验证

- 清理未引用页面或改为子组件。
- 清理旧 i18n、旧图标、旧菜单编码引用。
- 执行构建、类型检查和权限回归。

验收：

- 旧菜单编码只剩重定向或迁移说明。
- 构建和权限回归通过。

## 11. 验证门禁

后端：

```powershell
dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\XiHan.BasicApp.slnx
rg -n "class .*Controller" E:\Repository\XiHanFun\XiHan.BasicApp\backend\src -g "*.cs"
rg -n "TenantId\s+IS\s+NULL|TenantId\s*==\s*null|PlatformTenantId\s*=\s*1" E:\Repository\XiHanFun\XiHan.BasicApp\backend\src -g "*.cs"
```

前端：

```powershell
pnpm type-check
pnpm build
rg -n "system/user-role|system/role-permission|system/tenant-member|system/diff-log" E:\Repository\XiHanFun\XiHan.BasicApp\frontend\src
```

菜单验证：

- `/workbench/dashboard`、`/workbench/inbox`、`/workbench/profile` 可访问。
- `/system/user`、`/system/role`、`/system/org`、`/system/permission`、`/system/message` 可访问。
- `/platform/app`、`/platform/tenant`、`/platform/menu`、`/platform/config`、`/platform/dict`、`/platform/file`、`/platform/job`、`/platform/approval`、`/platform/server`、`/platform/cache` 可访问。
- `/log/login`、`/log/access`、`/log/operation`、`/log/api`、`/log/exception` 可访问。
- 子实体旧路径能重定向或明确返回迁移提示。

权限回归：

- 正权限用户能看到对应菜单和按钮。
- 负权限用户看不到对应菜单和按钮，直接调用 API 也失败。
- 跨租户用户无法通过 URL、Query 或请求体访问其他租户数据。
- 导出、强制下线、撤销令牌、重置密钥、FLS 变更均写审计。

## 12. 风险与处理

| 风险 | 影响 | 处理 |
|------|------|------|
| 菜单路径大规模变化 | 收藏和旧标签页失效 | 前端提供 redirect 表，至少保留一个版本 |
| 组件路径与菜单路径不一致 | 动态路由加载失败 | 第一阶段允许新路由指向旧组件路径，例如 `/platform/tenant -> system/tenant/index` |
| 子实体 API 仍可直接调用 | 页面隐藏被绕过 | 后端方法级权限完整校验，页面收敛不改变授权边界 |
| 个人中心进入工作台后旧 `/profile` 失效 | 用户下拉跳转失败 | `/profile` 重定向到 `/workbench/profile` |
| 开发工具无 SaaS 实体支撑 | 菜单空页 | 由 CodeGeneration 模块提供菜单，SaaS 不生成 |
| 日志详情包含敏感信息 | 泄露风险 | 后端 DTO 脱敏，导出独立权限，敏感访问审计 |
| 菜单缓存未清理 | 用户看到旧菜单 | 菜单种子更新后清理菜单路由缓存和授权快照缓存 |

## 13. 回滚策略

菜单回滚：

- 保留上一版 `SaasMenuSeeder.BuildDefinitions()` diff。
- 发布前导出平台级 `SysMenu`、`SysPermission` 数据。
- 回滚后清理菜单路由缓存和授权快照缓存。

前端回滚：

- 新路径上线初期保留旧组件和重定向。
- 聚合页面异常时，临时将菜单指回旧组件路径。
- 不在第一阶段删除旧页面文件。

后端回滚：

- 聚合 QueryService 为新增接口时，可停止前端调用，不影响旧子实体服务。
- 不删除已有子实体 AppService、QueryService、DTO。
- 新增权限码不立即删除旧权限码，确认稳定后再清理。

## 14. Definition of Done

- 菜单顶层为工作台、系统管理、平台管理、日志管理、开发工具。
- 工作台包含仪表盘、站内信、个人中心。
- 当前 56 个 SaaS 根实体均有明确归属。
- 子实体不再作为独立可见菜单。
- 旧路径有重定向或明确迁移说明。
- 页面内按钮绑定 `SaasPermissionCodes`，后端 AppService 方法校验权限。
- 授权、数据范围、FLS、跨租户、高风险操作均写审计。
- 后端构建、前端类型检查、前端构建通过。
- 正权限、负权限、跨租户越权三类测试通过。

## 15. 影响文件索引

预计主要修改区域：

```text
backend/src/modules/XiHan.BasicApp.Saas/
├── Infrastructure/Seeders/SaasMenuSeeder.cs
├── Domain/Permissions/SaasPermissionCodes.cs
├── Application/AppServices/Auth/AuthAppService.cs
├── Application/QueryServices/
├── Application/Dtos/
└── Application/Mappers/

frontend/src/
├── api/modules/
├── router/
├── views/workbench/
├── views/system/
├── views/platform/
├── views/log/
└── views/develop/
```

后续每个实施批次应在提交说明中标注 `REQ-00x` 和 `M0-M7`，便于回滚和验收追踪。
