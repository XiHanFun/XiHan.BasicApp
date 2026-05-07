# XiHan.BasicApp 菜单收敛与页面内嵌功能开发方案

> 版本：v2.0  
> 日期：2026-05-08
> 状态：实施中，菜单种子、前端目录、页面级 API facade 与账号/角色/权限中心聚合读模型已落地
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
```

目标：

- 主实体保留独立菜单，关联实体和明细实体进入主页面内的 Tab、抽屉、弹窗、选择器或按钮操作。
- 菜单结构与当前 56 个 SaaS 根实体一一有归属，避免实体漏管或重复入口。
- 后端继续使用 DynamicApi，不新增 Controller。
- 前端主要改 `frontend/src`，不改或少改 `frontend/packages`。
- 菜单权限、按钮权限、数据范围和审计保持后端可信，不依赖前端隐藏。
- 重建系统只注册目标目录路径，不提供历史路径兼容层。

非目标：

- 不在本文中重构实体字段、聚合根、仓储基类，这些仍按 `RefactoringPlan.md` 和 `REFACTOR_OPTIMIZATION_PLAN.md` 推进。
- 不删除子实体 AppService。页面收敛不等于 API 能力删除。
- 不把 SaaS 业务概念下沉到 `XiHan.Framework`。

## 2. 当前代码事实

基于当前工作区扫描：

- `Domain/Entities` 下有 56 个 `Sys*.cs` 根实体。
- `SysMenu` 支持 `MenuType`、`PermissionId`、`Path`、`Component`、`RouteName`、`IsVisible`、`Metadata`，可表达目录、页面菜单和按钮权限。
- `SysPermission` 支持 `PermissionType`、`ModuleCode`、`PermissionCode`、`ResourceId?`、`OperationId?`、`Priority`、`IsRequireAudit`，可承载菜单、按钮、功能、资源和数据范围权限。
- `SaasMenuSeeder.BuildDefinitions()` 应只声明目标目录菜单，不承载历史菜单迁移、删除或兼容逻辑。
- `AuthAppService.BuildMenuRoutesAsync()` 当前会先过滤 `menu.IsVisible` 再构建路由；重建后只下发目标路径。
- 前端视图目录应按 `workbench/system/platform/log` 重建，子实体独立视图不保留为路由页面。
- 任务调度和审批流程已有后端实体与服务，但当前前端菜单和页面需要补齐或迁移。
- 开发工具不由 SaaS 实体支撑，应由 `XiHan.BasicApp.CodeGeneration` 模块接入，不应由 SaaS 菜单种子强行生成。

## 3. 需求项

| ID | 标题 | 范围 | 完成标准 | 依赖 |
|----|------|------|----------|------|
| REQ-001 | 固化目标目录树 | `SaasMenuSeeder`、动态路由、`目录.md` | SaaS 菜单顶层为工作台、系统管理、平台管理、日志管理；开发工具由 CodeGeneration 独立接入 | 无 |
| REQ-002 | 收敛工作台入口 | 工作台、个人中心、站内信 | `/workbench` 下有仪表盘、站内信、个人中心；个人中心路径为 `/workbench/profile` | REQ-001 |
| REQ-003 | 收敛系统管理 | 用户、角色、机构、权限、消息 | 账号、角色、机构、权限中心、消息中心成为系统管理可见菜单，相关子实体均内嵌 | REQ-001 |
| REQ-004 | 收敛平台管理 | 租户、菜单、配置、字典、文件、任务、审批、OAuth、监控、缓存 | 平台管理承载平台级能力，租户版本、任务日志、审批记录、OAuth 令牌等不独立成菜单 | REQ-001 |
| REQ-005 | 收敛日志管理 | 登录、访问、操作、API、异常、差异、权限变更日志 | 日志管理保留五个主日志入口；差异日志和权限变更日志进入操作日志详情 | REQ-001 |
| REQ-006 | 隔离开发工具 | `XiHan.BasicApp.CodeGeneration` | 开发工具不进入 SaaS 菜单种子；由 CodeGeneration 独立提供 `/develop/*` | REQ-001 |
| REQ-007 | 对齐权限与审计 | `SaasPermissionCodes`、按钮菜单、AppService | 页面内按钮绑定权限码；授权、数据范围、FLS、跨租户、导出、强制下线等操作写审计 | REQ-002 至 REQ-005 |
| REQ-008 | 验证目标入口 | 目标路径、视图目录、缓存、种子 | 只注册目标路径；菜单缓存和授权快照失效策略明确；回滚依赖 git diff | REQ-001 至 REQ-007 |
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
│   ├── 机构管理 /system/org -> system/org/index
│   │   └── 内嵌：机构层级
│   ├── 权限中心 /system/permission -> system/permission/index
│   │   └── 内嵌：资源、操作、ABAC 条件、委托、申请、变更历史、字段级安全
│   └── 消息中心 /system/message -> system/message/index
│       └── 内嵌：通知公告、邮件记录、短信记录、用户通知状态
├── 平台管理 /platform
│   ├── 应用管理 /platform/app -> platform/app/index
│   │   └── 内嵌：OAuth 授权码、OAuth 令牌
│   ├── 租户管理 /platform/tenant -> platform/tenant/index
│   │   └── 内嵌：租户成员、租户版本、版本权限
│   ├── 菜单管理 /platform/menu -> platform/menu/index
│   │   └── 内嵌：按钮菜单、权限绑定
│   ├── 参数配置 /platform/config -> platform/config/index
│   │   └── 内嵌：约束规则、规则项、系统版本、迁移历史
│   ├── 字典管理 /platform/dict -> platform/dict/index
│   │   └── 内嵌：字典项
│   ├── 文件管理 /platform/file -> platform/file/index
│   │   └── 内嵌：文件存储配置
│   ├── 任务调度 /platform/job -> platform/job/index
│   │   └── 内嵌：任务执行日志
│   ├── 审批流程 /platform/approval -> platform/approval/index
│   │   └── 内嵌：审批记录
│   ├── 系统监控 /platform/server -> platform/server/index
│   └── 缓存管理 /platform/cache -> platform/cache/index
├── 日志管理 /log
│   ├── 登录日志 /log/login -> log/login/index
│   ├── 访问日志 /log/access -> log/access/index
│   ├── 操作日志 /log/operation -> log/operation/index
│   │   └── 内嵌：差异日志、权限变更日志
│   ├── API 日志 /log/api -> log/api/index
│   └── 异常日志 /log/exception -> log/exception/index
```

设计说明：

- `个人中心` 归入工作台，路径为 `/workbench/profile`。
- `开发工具` 不属于 SaaS 菜单种子；如启用，由 `XiHan.BasicApp.CodeGeneration` 模块独立提供 `/develop/*`。
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
| 系统管理 | 机构管理 | `SysDepartment` | `/system/org` | `system/org/index` | `saas:department:read` |
| 系统管理 | 权限中心 | `SysPermission` | `/system/permission` | `system/permission/index` | `saas:permission:read` |
| 系统管理 | 消息中心 | `SysNotification`、`SysEmail`、`SysSms` | `/system/message` | `system/message/index` | `saas:message:read` |
| 平台管理 | 应用管理 | `SysOAuthApp` | `/platform/app` | `platform/app/index` | `saas:oauth-app:read` |
| 平台管理 | 租户管理 | `SysTenant` | `/platform/tenant` | `platform/tenant/index` | `saas:tenant:read` |
| 平台管理 | 菜单管理 | `SysMenu` | `/platform/menu` | `platform/menu/index` | `saas:menu:read` |
| 平台管理 | 参数配置 | `SysConfig` | `/platform/config` | `platform/config/index` | `saas:config:read` |
| 平台管理 | 字典管理 | `SysDict` | `/platform/dict` | `platform/dict/index` | `saas:dict:read` |
| 平台管理 | 文件管理 | `SysFile` | `/platform/file` | `platform/file/index` | `saas:file:read` |
| 平台管理 | 任务调度 | `SysTask` | `/platform/job` | `platform/job/index` | `saas:task:read` |
| 平台管理 | 审批流程 | `SysReview` | `/platform/approval` | `platform/approval/index` | `saas:review:read` |
| 平台管理 | 系统监控 | `ServerAppService` | `/platform/server` | `platform/server/index` | `saas:config:read` 或运维权限 |
| 平台管理 | 缓存管理 | `CacheAppService` | `/platform/cache` | `platform/cache/index` | `saas:config:read` 或运维权限 |
| 日志管理 | 登录日志 | `SysLoginLog` | `/log/login` | `log/login/index` | `saas:login-log:read` |
| 日志管理 | 访问日志 | `SysAccessLog` | `/log/access` | `log/access/index` | `saas:access-log:read` |
| 日志管理 | 操作日志 | `SysOperationLog` | `/log/operation` | `log/operation/index` | `saas:operation-log:read` |
| 日志管理 | API 日志 | `SysApiLog` | `/log/api` | `log/api/index` | `saas:api-log:read` |
| 日志管理 | 异常日志 | `SysExceptionLog` | `/log/exception` | `log/exception/index` | `saas:exception-log:read` |

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
- 只注册 `/workbench/profile` 路由。
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

- 顶层目录使用 `workbench`、`system`、`platform`、`log`。
- 可见菜单按本文“主菜单清单”生成。
- 子实体不生成独立可见菜单。
- 页面内按钮使用 `MenuType.Button` 并绑定 `SaasPermissionCodes`。
- `develop` 目录如果由 CodeGeneration 模块接入，SaaS Seeder 不应生成无组件或无服务支撑的菜单。

以下子实体编码只作为页面内能力归属参考，不进入 `SaasMenuSeeder.BuildDefinitions()`：

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

因此重建后只下发可见的目标菜单路由；不通过隐藏菜单、静态路由或前端兼容表承载历史入口。

### 8.3 聚合查询服务

页面收敛后，建议增加或扩展聚合读模型，减少详情页多次请求：

| 聚合读模型 | 建议位置 | 用途 |
|------------|----------|------|
| `UserManagementDetailDto` | `Application/Dtos/System` | 用户详情聚合租户成员、部门、角色、权限、数据范围、安全、会话、统计、第三方绑定、密码历史 |
| `RoleManagementDetailDto` | `Application/Dtos/System` | 角色详情聚合层级、权限、数据范围、授权用户摘要 |
| `TenantManagementDetailDto` | `Application/Dtos/Tenancy` | 租户详情聚合成员、套餐、版本权限摘要 |
| `PermissionCenterDetailDto` | `Application/Dtos/System` | 权限中心聚合资源、操作、ABAC、委托、申请、FLS、变更历史 |
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

重建后前端视图目录直接按菜单分区组织，动态路由目标只指向目标组件路径：

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
```

### 9.2 目标入口策略

运行时只注册本文目标路径；子实体能力通过主页面内嵌功能重新提供。

动态路由根据后端实际下发的可见子菜单修正目录重定向：当种子里的 `Redirect` 指向已被权限过滤掉的子菜单时，前端自动落到该目录下第一个可导航页面；首页 `homePath` 也使用同一原则，避免进入只有目录壳、没有页面组件的路径。

### 9.3 API facade

已新增页面级 facade，目标菜单页面不直接散落调用多个子实体 API：

```text
frontend/src/api/modules/workbench/index.ts
frontend/src/api/modules/system/index.ts
frontend/src/api/modules/platform/index.ts
frontend/src/api/modules/log/index.ts
```

当前 facade 先组合现有 AppService / QueryService，后端聚合 DTO 完成后替换为单次聚合请求。子实体 API 保留为页面内 Tab、抽屉、弹窗和按钮操作的能力入口，不再作为菜单级服务入口直接暴露给目标页面。

已落地的关键归口：

- `workbenchApi.dashboard.statistics` 归口 `UserStatisticsQueryService`，与仪表盘菜单权限 `saas:user-statistics:read` 一致。
- `workbenchApi.dashboard.summary` 归口 `WorkbenchQueryService.GetDashboardSummaryAsync`，一次返回当前用户今日统计和站内信摘要。
- `workbenchApi.inbox` 归口当前用户站内信能力。
- `systemApi` 聚合账号、角色、机构、权限中心、消息中心页面能力。
- `systemApi.user.detailView` 归口 `UserManagementQueryService.GetUserManagementDetailAsync`，一次返回账号管理详情抽屉所需的只读聚合数据。
- `systemApi.role.detailView` 归口 `RoleManagementQueryService.GetRoleManagementDetailAsync`，一次返回角色管理详情抽屉所需的层级、权限、数据范围和授权用户摘要。
- `systemApi.permission.detailView` 归口 `PermissionCenterQueryService.GetPermissionCenterDetailAsync`，一次返回权限中心详情抽屉所需的资源、操作、ABAC 条件、委托、申请、FLS 和变更历史。
- `systemApi.permission` 内继续暴露资源、操作、条件、委托、申请、字段级安全和权限变更日志 API，作为权限中心页面内 Tab、抽屉和按钮能力。
- `platformApi` 聚合应用、租户、菜单、参数、字典、文件、任务、审批、监控、缓存页面能力。
- `logManagementApi` 聚合登录、访问、操作、API、异常日志页面能力。

后端页面级聚合服务已开始落地：

```text
backend/src/modules/XiHan.BasicApp.Saas/Application/Contracts/Workbench/IWorkbenchQueryService.cs
backend/src/modules/XiHan.BasicApp.Saas/Application/QueryServices/Workbench/WorkbenchQueryService.cs
backend/src/modules/XiHan.BasicApp.Saas/Application/Dtos/Workbench/WorkbenchDashboardSummaryDto.cs
backend/src/modules/XiHan.BasicApp.Saas/Application/Contracts/System/IUserManagementQueryService.cs
backend/src/modules/XiHan.BasicApp.Saas/Application/QueryServices/System/UserManagementQueryService.cs
backend/src/modules/XiHan.BasicApp.Saas/Application/Dtos/System/UserManagementDetailDto.cs
backend/src/modules/XiHan.BasicApp.Saas/Application/Contracts/System/IRoleManagementQueryService.cs
backend/src/modules/XiHan.BasicApp.Saas/Application/QueryServices/System/RoleManagementQueryService.cs
backend/src/modules/XiHan.BasicApp.Saas/Application/Dtos/System/RoleManagementDetailDto.cs
backend/src/modules/XiHan.BasicApp.Saas/Application/Contracts/System/IPermissionCenterQueryService.cs
backend/src/modules/XiHan.BasicApp.Saas/Application/QueryServices/System/PermissionCenterQueryService.cs
backend/src/modules/XiHan.BasicApp.Saas/Application/Dtos/System/PermissionCenterDetailDto.cs
```

账号管理聚合接口只作为页面详情读模型，不替代子实体授权、撤销、启停、强制下线等命令接口；这些操作仍由原 AppService/QueryService 承载并按细粒度权限码校验。

角色管理聚合接口同样只作为详情读模型，不替代角色层级、角色权限、角色数据范围、用户角色绑定的授权和撤销命令接口。

权限中心聚合接口同样只作为详情读模型，不替代权限定义、资源、操作、ABAC 条件、权限委托、权限申请、FLS 的新增、更新、撤销、审批和启停命令接口。

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

### M1：菜单种子与目录重建

- 重写 `SaasMenuSeeder.BuildDefinitions()` 为本文目标目录。
- 前端视图目录按 `workbench/system/platform/log` 重建。
- 不再下发子实体可见菜单。
- 清理菜单路由缓存。

验收：

- 新登录用户侧边栏只看到目标目录。
- 只注册目标路径。

### M2：工作台收敛

- 接入个人中心到 `/workbench/profile`。
- 确认站内信只展示当前用户 `SysUserNotification`。
- 仪表盘接入 `SysUserStatistics` 聚合视图。

验收：

- 工作台三个菜单均可访问。
- 顶部用户下拉进入 `/workbench/profile`。

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
- 日志统一使用 `/log/*` 目标路径。

验收：

- 日志管理入口清晰。
- 导出和敏感详情查看受权限控制。

### M6：开发工具接入

- 明确 `XiHan.BasicApp.CodeGeneration` 对 `/develop/*` 的菜单提供方式。
- SaaS 模块不生成无支撑的开发工具菜单。

验收：

- 开发工具要么由 CodeGeneration 提供，要么不显示。

### M7：目标结构验证

- 删除未引用页面或改为子组件。
- 复核 i18n、图标、菜单编码只指向目标结构。
- 执行构建、类型检查和权限回归。

验收：

- 子实体编码不作为前端路由和菜单种子存在。
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
- 子实体路径不注册为前端独立路由。
- 缺少某目录默认子菜单权限时，目录重定向会落到第一个可见子页面，而不是 404。

权限回归：

- 正权限用户能看到对应菜单和按钮。
- 负权限用户看不到对应菜单和按钮，直接调用 API 也失败。
- 跨租户用户无法通过 URL、Query 或请求体访问其他租户数据。
- 导出、强制下线、撤销令牌、重置密钥、FLS 变更均写审计。

## 12. 风险与处理

| 风险 | 影响 | 处理 |
|------|------|------|
| 菜单路径大规模变化 | 收藏和历史标签页失效 | 重建系统不做兼容，发布说明中明确目标路径 |
| 组件路径与菜单路径不一致 | 动态路由加载失败 | 菜单种子组件路径必须与 `frontend/src/views` 新目录一致 |
| 子实体 API 仍可直接调用 | 页面隐藏被绕过 | 后端方法级权限完整校验，页面收敛不改变授权边界 |
| 个人中心进入工作台后入口变化 | 用户下拉跳转失败 | 顶部用户下拉直接跳转 `/workbench/profile` |
| 开发工具无 SaaS 实体支撑 | 菜单空页 | 由 CodeGeneration 模块提供菜单，SaaS 不生成 |
| 日志详情包含敏感信息 | 泄露风险 | 后端 DTO 脱敏，导出独立权限，敏感访问审计 |
| 菜单缓存未刷新 | 用户看不到目标菜单 | 菜单种子更新后刷新菜单路由缓存和授权快照缓存 |

## 13. 回滚策略

菜单回滚：

- 保留上一版 `SaasMenuSeeder.BuildDefinitions()` diff。
- 发布前导出平台级 `SysMenu`、`SysPermission` 数据。
- 回滚后清理菜单路由缓存和授权快照缓存。

前端回滚：

- 使用 git 回退前端视图目录重建补丁。
- 不提供历史组件路径作为运行时兜底。
- 聚合页面异常时修复对应主页面，不恢复子实体独立页面。

后端回滚：

- 聚合 QueryService 为新增接口时，可停止前端调用，不影响既有子实体服务。
- 不删除已有子实体 AppService、QueryService、DTO。
- 权限码删除另起专项评估，本轮只保证目标菜单与按钮权限可用。

## 14. Definition of Done

- SaaS 菜单顶层为工作台、系统管理、平台管理、日志管理。
- 工作台包含仪表盘、站内信、个人中心。
- 当前 56 个 SaaS 根实体均有明确归属。
- 子实体不再作为独立可见菜单。
- 只注册目标路径，独立子实体视图不保留。
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
└── views/log/
```

后续每个实施批次应在提交说明中标注 `REQ-00x` 和 `M0-M7`，便于回滚和验收追踪。
