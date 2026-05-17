# XiHan.BasicApp 实体-服务-页面 架构审计报告

> 审计基准: `XiHan.BasicApp.Saas\Domain\Entities` 56 个实体
> 框架参考: `XiHan.Framework\framework\src` 40+ 模块
> 审计日期: 2026/05/12
> 审计范围: 实体设计 → 服务架构 → 框架集成 → 前端页面 全链路

---

## 一、架构总览

### 1.1 框架层已有能力（XiHan.Framework）

框架层已经提供了完整的基础设施，BasicApp 不需要重复造轮子：

| 框架模块 | 提供能力 | BasicApp 集成状态 |
|----------|----------|-------------------|
| `XiHan.Framework.Authentication` | `IAuthenticationService`（登录/改密/2FA/锁定）、`IJwtTokenService`、`IPasswordHasher`、`IOtpService`、`IExternalLoginStore` | **未集成** — BasicApp 自建了 AuthAppService 未使用框架认证服务 |
| `XiHan.Framework.Authorization` | `IAuthorizationService`、`IPermissionChecker`、`IPolicyEvaluator`、`IAbacEvaluator`、`[PermissionAuthorize]` | **已集成** — 使用了 PermissionAuthorize 和动态 API |
| `XiHan.Framework.Messaging` | `IMessageDispatcher`、`IMessageSender`、`MessageEnvelope` 统一消息通道 | **未集成** — BasicApp 自建了 MessageAppService |
| `XiHan.Framework.ObjectStorage` | `IFileStorageProvider`（Local/OSS/MinIO/COS 四种实现）、分块上传、预签名 URL | **未集成** — FileAppService 未使用框架存储 |
| `XiHan.Framework.Security` | `ICurrentUser`（用户身份）、`Sm2Helper`（国密）、`BlowfishHelper`（对称加密） | **部分集成** — 使用了 ICurrentUser |
| `XiHan.Framework.Tasks` | `IJobScheduler`、`IJobWorker`、Cron/Interval/Delay 触发器、分布式锁、中间件管道 | **未集成** — BasicApp 自建了 TaskAppService |
| `XiHan.Framework.EventBus` | `ILocalEventBus`、`IDistributedEventBus`、Inbox/Outbox 可靠性投递 | **部分集成** — 定义了领域事件但发布链路不完整 |
| `XiHan.Framework.Templating` | `ITemplateService`（Scriban 引擎）、模板继承/布局/安全扫描 | **未集成** — 邮件/短信发送无模板支持 |
| `XiHan.Framework.Logging` | `IStructuredLogger`、`IPerformanceLogger`、`ILogContext`（自动携带 UserId/TenantId/TraceId） | **未集成** — 服务层直接使用 ILogger |
| `XiHan.Framework.Domain` | `AggregateRootBase`、`MultiTenantFullAuditedEntityBase`、`ISpecification<T>`、`ValueObject`、`IBusinessRule` 等完整 DDD 基类 | **已集成** — 实体继承自 BasicAppCore 的封装基类 |

### 1.2 BasicApp 服务架构现状

当前 40 个应用服务中，大部分是为**关联/从属实体的 CRUD** 创建的独立服务，这违反了聚合根设计原则：

| 服务类型 | 数量 | 举例 | 问题 |
|----------|------|------|------|
| 聚合根主服务 | 14 | UserAppService, RoleAppService, TenantAppService | 合理 |
| 从属实体独立服务 | 16 | UserRoleAppService, RolePermissionAppService, UserPermissionAppService, RoleDataScopeAppService, UserDataScopeAppService, PermissionDelegationAppService, PermissionRequestAppService, RoleHierarchyAppService, FieldLevelSecurityAppService, ConstraintRuleAppService, PermissionConditionAppService, OperationAppService, ResourceAppService, TenantEditionPermissionAppService, TenantMemberAppService, UserDepartmentAppService | **应合并入主服务** |
| 基础设施服务 | 5 | AuthAppService, CacheAppService, ServerAppService, MessageAppService, ProfileAppService | 部分合理 |
| 纯查询服务 | 5 | NotificationAppService, UserInboxAppService, UserSessionAppService, UserSecurityAppService, VersionAppService | 可合并或保留 |

---

## 二、核心架构问题：服务拆分过细

### 2.1 问题描述

当前 BasicApp 为每个实体（包括关联表）创建了独立的 AppService，导致：
- 40 个服务文件，大量重复的 CRUD 模板代码
- 跨实体操作时需要注入多个服务，增加耦合
- 违反 DDD 聚合根原则——关联实体不应有独立的生命周期
- 前端需要调用多个 API 端点完成一个业务操作

### 2.2 正确的服务架构

应遵循"一个聚合根 = 一个主服务"原则：

```
聚合根主服务（需要保留）
├── TenantAppService           ← 聚合根：SysTenant
│   ├── 内部管理：SysTenantUser（成员管理）
│   └── 关联：SysTenantEditionPermission（版本权限）
├── UserAppService             ← 聚合根：SysUser
│   ├── 内部管理：SysUserSecurity（安全状态）
│   ├── 内部管理：SysUserRole（用户角色绑定）
│   ├── 内部管理：SysUserPermission（用户直授权限）
│   ├── 内部管理：SysUserDepartment（用户部门归属）
│   ├── 内部管理：SysUserDataScope（用户数据范围）
│   ├── 内部管理：SysUserSession（会话管理）
│   └── 内部管理：SysExternalLogin（第三方绑定）
├── RoleAppService             ← 聚合根：SysRole
│   ├── 内部管理：SysRolePermission（角色权限绑定）
│   ├── 内部管理：SysRoleDataScope（角色数据范围）
│   └── 内部管理：SysRoleHierarchy（角色继承）
├── PermissionAppService       ← 聚合根：SysPermission
│   ├── 内部管理：SysResource（资源定义）
│   ├── 内部管理：SysOperation（操作定义）
│   ├── 内部管理：SysPermissionCondition（ABAC 条件）
│   ├── 内部管理：SysPermissionDelegation（权限委托）
│   └── 内部管理：SysPermissionRequest（权限申请）
├── DepartmentAppService       ← 聚合根：SysDepartment
│   └── 内部管理：SysDepartmentHierarchy（闭包表）
├── FileAppService             ← 聚合根：SysFile
│   └── 内部管理：SysFileStorage（多存储副本）
├── NotificationAppService     ← 聚合根：SysNotification
│   └── 内部管理：SysUserNotification（用户通知状态）
├── ReviewAppService           ← 聚合根：SysReview
│   └── 内部管理：SysReviewLog（审查流转日志）
├── TaskAppService             ← 聚合根：SysTask
│   └── 内部管理：SysTaskLog（任务执行日志）
├── ConstraintRuleAppService   ← 聚合根：SysConstraintRule
│   └── 内部管理：SysConstraintRuleItem（约束条目）

基础设施服务（作为框架代理）
├── AuthAppService             ← 代理 IAuthenticationService + IJwtTokenService
├── MessageAppService          ← 代理 IMessageDispatcher（Email/SMS 通过框架发送）
├── MenuAppService             ← 聚合根：SysMenu
├── ConfigAppService           ← 聚合根：SysConfig
├── DictAppService             ← 聚合根：SysDict + SysDictItem
├── TenantEditionAppService    ← 聚合根：SysTenantEdition
├── VersionAppService          ← 管理：SysVersion + SysMigrationHistory
├── OAuthAppAppService         ← 聚合根：SysOAuthApp
├── FieldLevelSecurityAppService ← 聚合根：SysFieldLevelSecurity

查询专用服务（无需 CRUD 的实体）
├── UserInboxAppService        ← 查询 SysUserNotification（只读视图）
├── CacheAppService            ← 基础设施监控
├── ServerAppService           ← 基础设施监控
└── ProfileAppService          ← 组合查询（跨 User/Security/Session）
```

### 2.3 需要合并的服务清单

以下 16 个独立服务应合并入父聚合根服务：

| 当前独立服务 | 应合并到 | 职责说明 |
|-------------|----------|----------|
| `UserRoleAppService` | `UserAppService` | 用户-角色绑定由 UserAppService 在创建/编辑用户时管理 |
| `UserPermissionAppService` | `UserAppService` | 用户直授权限由 UserAppService 内部管理 |
| `UserDataScopeAppService` | `UserAppService` | 用户数据范围由 UserAppService 内部管理 |
| `UserDepartmentAppService` | `UserAppService` | 用户部门归属由 UserAppService 内部管理 |
| `UserSecurityAppService` | `UserAppService` | 安全状态是 User 的 1:1 扩展，不应独立 |
| `UserSessionAppService` | `UserAppService` | 会话是 User 的子属性，吊销会话是用户操作 |
| `RolePermissionAppService` | `RoleAppService` | 角色-权限绑定由 RoleAppService 在创建/编辑角色时管理 |
| `RoleDataScopeAppService` | `RoleAppService` | 角色数据范围由 RoleAppService 内部管理 |
| `RoleHierarchyAppService` | `RoleAppService` | 角色继承由 RoleAppService 在创建/编辑角色时管理 |
| `ResourceAppService` | `PermissionAppService` | 资源定义是权限的子概念 |
| `OperationAppService` | `PermissionAppService` | 操作定义是权限的子概念 |
| `PermissionConditionAppService` | `PermissionAppService` | ABAC 条件是权限绑定的子属性 |
| `PermissionDelegationAppService` | `PermissionAppService` | 权限委托由 PermissionAppService 内部管理 |
| `PermissionRequestAppService` | `PermissionAppService` | 权限申请由 PermissionAppService 内部管理 |
| `TenantMemberAppService` | `TenantAppService` | 租户成员由 TenantAppService 内部管理 |
| `TenantEditionPermissionAppService` | `TenantEditionAppService` | 版本权限由 TenantEditionAppService 内部管理 |

合并后，AppService 从 40 个精简到 **24 个**。

---

## 三、框架集成问题：重复造轮子

### 3.1 AuthAppService 未集成框架认证服务

**现状**: BasicApp 自建了 `AuthAppService` 实现登录/登出/Token 签发，但框架 `XiHan.Framework.Authentication` 已提供：

- `IAuthenticationService` — 完整认证流程（登录验证、密码修改、2FA、账号锁定/解锁、失败计数管理）
- `IJwtTokenService` — JWT 令牌生成/验证/刷新
- `IPasswordHasher` — Argon2/BCrypt 密码哈希
- `IOtpService` — TOTP/HOTP 双因素认证
- `IExternalLoginStore` — 第三方登录绑定存储接口

**优化方向**:
1. `AuthAppService` 改为实现 `IUserStore`，注入框架 `IAuthenticationService`
2. 登录流程委托给 `IAuthenticationService.AuthenticateAsync()`
3. 密码修改委托给 `IAuthenticationService.ChangePasswordAsync()`
4. 删除 `AuthAppService` 中自写的 JWT 签发/密码哈希/锁定逻辑
5. 继承框架 `PasswordPolicyOptions` 的密码策略配置

### 3.2 MessageAppService 未集成框架消息通道

**现状**: `MessageAppService` 直接操作 `SysEmail`/`SysSms` 实体做 CRUD，但框架 `XiHan.Framework.Messaging` 已提供：

- `IMessageDispatcher` — 统一消息分发（按 Channel 路由到对应 Sender）
- `IMessageSender` — 可扩展的通道实现（实现此接口对接 SMTP/阿里云短信等）
- `MessageEnvelope` + `MessageRecipient` — 标准化消息模型
- `TemplateCode` + `TemplateParams` — 对接框架 Templating 引擎

**优化方向**:
1. 新建 `EmailSender : IMessageSender`（对接 SMTP）
2. 新建 `SmsSender : IMessageSender`（对接阿里云/腾讯云短信）
3. 新建 `SiteNotificationSender : IMessageSender`（对接 SysNotification）
4. `MessageAppService` 改为注入 `IMessageDispatcher`，不直接操作实体
5. 消息发送后，由各 `IMessageSender` 实现内部记录 SysEmail/SysSms 日志

### 3.3 FileAppService 未集成框架对象存储

**现状**: `FileAppService` 自行实现文件上传，但框架 `XiHan.Framework.ObjectStorage` 已提供：

- `IFileStorageProviderManager` — 多存储提供者管理
- `IFileStorageRouter` — 按业务路由键分发到不同 Provider
- `LocalFileStorageProvider` / `AliyunOssStorageProvider` / `MinioFileStorageProvider` / `TencentCosStorageProvider`
- 分块上传、断点续传、预签名 URL

**优化方向**:
1. `FileAppService` 注入 `IFileStorageProviderManager` 和 `IFileStorageRouter`
2. 文件上传改为 `provider.UploadAsync(stream, path)`
3. 删除 BasicApp 中自写的文件存储逻辑
4. 文件 URL 生成改用 `provider.GeneratePresignedUrlAsync()`
5. `SysFileStorage` 记录在 `FileAppService` 上传成功后自动创建

### 3.4 TaskAppService 未集成框架任务调度

**现状**: `TaskAppService` 自行管理 SysTask 的生命周期，但框架 `XiHan.Framework.Tasks` 已提供：

- `IJobScheduler` — 完整的任务调度器（Cron/Interval/Delay 触发器）
- `IJobWorker` — 任务执行契约（实现此接口即可被调度）
- `IJobStore` — 任务执行历史持久化
- `IJobLockProvider` — 分布式锁防重执行
- 中间件管道（Lock → Logging → Metrics → Retry → Timeout）

**优化方向**:
1. 每个 SysTask 对应一个 `IJobWorker` 实现
2. `TaskAppService` 改为注入 `IJobScheduler`，不直接管理执行
3. 实现 `IJobStore` 对接到 `SysTaskLog` 表
4. 删除 BasicApp 中自写的调度逻辑
5. `SysTask` 实体保留作为"任务配置可视化存储"，但执行委托给框架

### 3.5 领域事件总线未充分利用

**现状**: 定义了 11 个领域事件，但发布链路不完整。框架 `XiHan.Framework.EventBus` 已提供：

- `ILocalEventBus` — 进程内事件发布/订阅
- `IDistributedEventBus` + Inbox/Outbox — 跨服务可靠投递
- `UnitOfWorkEventPublisher` — UoW 提交时自动发布聚合根的 LocalEvents/DistributedEvents
- 事件处理器自动发现和注册

**优化方向**:
1. 为每个已有领域事件实现 `ILocalEventHandler<T>`
2. 在 `AuthAppService` 登录成功后发布 `UserLoggedInEvent`
3. 在 `UserAppService` 创建用户后发布 `UserCreatedEvent` → 处理器发送欢迎通知
4. 关键事件（权限变更、租户状态变更）使用 `IDistributedEventBus` + Outbox 保证可靠投递

---

## 四、实体设计问题

### 4.1 `SysNotification` — 缺少 `TargetType` 和 `TargetValue` 字段

**问题**: 实体注释说明发布时应按 TargetType 展开生成 `SysUserNotification`，但实体上**没有这些字段**。

**优化方向**:
```csharp
// 添加字段
public virtual NotificationTargetType TargetType { get; set; } = NotificationTargetType.All;
public virtual string? TargetValue { get; set; } // JSON 数组：[101,102,103]
// 使用 TargetType=All 表示全员通知
```

### 4.2 `SysFileStorage` — `StorageConfigId` 外键指向不存在的表

**问题**: 字段注释 "关联存储配置表"，但实体模型中没有 `SysStorageConfig`。

**优化方向**: 创建 `SysStorageConfig` 实体，集中管理 S3/OSS/COS 连接参数（AccessKey/SecretKey/Endpoint/Bucket/Region），替代当前用 `SysConfig` 键值对管理存储配置的方式。

### 4.3 `SysReview` — 多级审批缺少结构化节点表

**问题**: `ReviewUserIds` JSON 字符串 + `CurrentLevel` 模拟多级审批。

**优化方向**: 新增 `SysReviewNode` 实体承载每个审批节点独立状态（ReviewId/NodeLevel/ReviewerId/NodeStatus/ReviewTime/Comment），支持会签、或签等高级审批模式。

### 4.4 `SysTask` — 双状态字段语义重叠

**问题**: 同时有 `Status`（EnableStatus 启停）和 `RunTaskStatus`（运行时状态）。

**优化方向**: 合并为 `TaskStatus` 枚举（Enabled/Disabled/Pending/Running/Success/Failed/Stopped/Paused），保证生命周期和运行时状态互斥。

### 4.5 `SysUser` — 缺少 `IsActive` 字段

**问题**: 只有 `Status` 启停，无法区分 "管理员禁用" 和 "用户未激活"。

**优化方向**: 添加 `IsActive`（bool），Status=Disabled 表示管理员禁用，IsActive=false 表示邮箱未验证。

### 4.6 字段命名不一致

| 实体 | 字段 | 实体 | 字段 | 应统一为 |
|------|------|------|------|----------|
| SysEmail | `BusinessType` + `BusinessId` | SysReview | `EntityType` + `EntityId` | 统一 `EntityType` + `EntityId` |
| SysSms | `SendUserId` | SysEmail | `SendUserId` | Socket保持 |
| SysSms | `ReceiverId` | SysEmail | `ReceiveUserId` | 统一命名 |

---

## 五、前端架构问题

### 5.1 页面粒度与服务不对齐

当前 22 个前端页面中，部分页面通过"Tab 嵌套"将关联实体塞进父页面的详情抽屉（如用户详情包含角色/权限/部门/会话/统计 Tab），这一做法是对的。但缺少以下入口：

| 缺失页面 | 对应聚合根 | 优先级 |
|----------|-----------|--------|
| 约束规则管理 | ConstraintRuleAppService | HIGH |
| 字段级安全管理 | FieldLevelSecurityAppService | HIGH |
| 通知模板管理 | NotificationAppService | MEDIUM |
| 审查流程管理 | ReviewAppService | MEDIUM |
| 任务执行历史 | TaskAppService（SysTaskLog Tab） | LOW |

### 5.2 枚举值硬编码

前端 22 个页面中大量 `const statusOptions = [...]` 硬编码，与后端 37 个 `*.Enum.cs` 无自动同步。

**优化方向**: 后端动态 API 暴露 `GET /api/basicapp/enums/{entityType}/{enumType}`，前端创建 `useEnum()` composable 自动加载。

---

## 六、框架层需要补充的能力

### 6.1 `XiHan.Framework.Security` — 密码策略服务接口

**缺失**: 框架有 `PasswordPolicyOptions` 配置类，但缺少 `IPasswordPolicyService` 服务接口来统一校验密码强度和历史。

**补充内容**:
```csharp
// XiHan.Framework.Security / IPasswordPolicyService.cs
public interface IPasswordPolicyService
{
    PasswordValidationResult Validate(string password);
    Task<bool> IsPasswordReusedAsync(string newPasswordHash, long userId, int historyCount);
}
```

### 6.2 `XiHan.Framework.Messaging` — 持久化发件箱

**缺失**: `IMessageDispatcher` 是即时分发，缺少"先入库再异步发送"的发件箱模式，消息发送失败时需要重试机制。

**补充内容**: 在 `XiHan.Framework.Messaging` 中添加 `IMessageOutbox` 接口，支持：
- `EnqueueAsync(MessageEnvelope)` — 消息先入库
- `IMessageOutboxProcessor` — 后台服务轮询待发消息

### 6.3 `XiHan.Framework.Domain` — 审计拦截器

**缺失**: `SysAccessLog`/`SysOperationLog`/`SysDiffLog`/`SysLoginLog` 需业务层手动写入，框架应提供自动拦截。

**补充内容**: 在框架层添加：
- `AuditLogInterceptor` — ASP.NET Core 中间件自动写 SysAccessLog
- `CommandLogInterceptor` — CQRS Command 执行时自动写 SysOperationLog
- `EntityChangeInterceptor` — 实体更新时自动比较快照写 SysDiffLog

---

## 七、优化优先级

### P0 — 服务架构重构（影响面最大）

1. 合并 16 个从属实体服务到父聚合根服务
2. AuthAppService 集成框架 `IAuthenticationService`
3. FileAppService 集成框架 `IFileStorageProvider`

### P1 — 框架能力补充

4. MessageAppService 集成框架 `IMessageDispatcher`
5. TaskAppService 集成框架 `IJobScheduler`
6. 领域事件总线完整链路（Handler 实现 + UoW 自动发布）
7. 框架层补充密码策略服务接口、消息发件箱、审计拦截器

### P2 — 实体和前端完善

8. SysNotification 添加 TargetType/TargetValue 字段
9. 创建 SysStorageConfig 实体
10. 前端枚举自动同步
11. 补充缺失页面

---

*本文档审计了 56 个领域实体、40 个应用服务、40+ 框架模块、22 个前端页面的全链路，识别了服务架构过细拆分和框架能力未充分利用两大核心问题。*
