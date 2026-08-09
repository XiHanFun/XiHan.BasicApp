# 多租户与版本

XiHan.BasicApp 是一套 **B2B SaaS 多租户内核**：一份代码、一套部署，同时承载多个互不可见的租户（企业客户），并以**版本（Edition）**作为订阅售卖与功能门控的单元。本页讲清楚隔离怎么落地、登录后落到哪里、超级管理员如何跨租户运维、版本白名单如何在运行时门控权限，以及请求如何解析到租户上下文。

权限码、数据范围、字段脱敏等细节见 [权限模型](./permission)；框架层的租户上下文、解析链与存储见 [XiHan.Framework.MultiTenancy](https://framework.docs.xihanfun.com/packages/multitenancy)。本页聚焦 BasicApp 应用层的租户与版本机制。

## 隔离模型：字段级隔离 + `TenantId=0` 约定

BasicApp 默认走**字段级隔离（Field）**：所有业务实体继承自 `BasicApp*` 实体基类族（`BasicAppEntity` / `BasicAppCreationEntity` / `BasicAppFullAuditedEntity` / `BasicAppAggregateRoot` 等，底层是框架的 `SugarMultiTenant*<long>`），带一个 `TenantId` 列，查询按当前租户上下文自动过滤，无需业务代码手动拼条件。

隔离模式由 `SysTenant.IsolationMode`（`TenantIsolationMode`）声明，支持三种，`SysTenant` 还预留了独立库连接串等字段：

| 模式 | 含义 |
| --- | --- |
| `Field`（默认） | 同库同表，靠 `TenantId` 列区分租户数据 |
| `Database` | 每租户独立数据库（`ConnectionString` 加密存储，需 `InitializeDatabase` 建库建表种子） |
| `Schema` | 同库不同 Schema（尚未实装：`SaasTenantConnectionProvider` 解析到该模式直接抛异常 fail-closed，拒绝退化为行隔离） |

### 框架层 `long?`/null 与应用层 `TenantId=0` 的关系

这是理解本系统隔离的关键，两层用**不同的空值语义**表达"全局/无租户"：

- **框架层**：`ICurrentTenant.Id` 是 `long?`。`null`（或 `0`）表示**当前请求没有租户上下文**，即平台运维态（宿主态）。框架的租户查询过滤器在无上下文时关闭。
- **BasicApp 应用层落库约定**（见 `BasicAppEntity` 注释）：平台级/全局记录统一落 **`TenantId=0`**（"平台租户"占位），**不得用 NULL**；业务租户 Id 从 1 开始分配，0 号租户由平台保留。

二者衔接的规则：

- 平台判定统一以 `currentTenant.IsPlatformOperation()` 为准，其实现就是 `Id is null or 0`（见 `CurrentTenantPlatformExtensions`）。
- 查询"全局 + 私有"合并时用 `WHERE TenantId IN (0, {currentTenantId})`。授权快照构建即按此规则：绑定行按 `TenantId == 当前租户 || TenantId == 0` 生效，平台态（无上下文）则仅 `TenantId=0` 的全局绑定生效，防止多租户成员在平台态聚合出跨租户权限。
- 如需 `IsGlobal` 语义，实体在 Expand 里以只读派生属性 `IsGlobal => TenantId == 0` 暴露，**不落库**（避免与 `TenantId` 漂移）。
- 只有平台运维态才允许维护 `TenantId=0` 的全局模板（菜单/权限/角色/版本等）；租户态（`Id>0`）对全局模板一律拒绝写入，避免某租户改动波及所有租户。

> 服务层通过框架注入自动写入 `TenantId`，业务代码禁止直接操纵。需要临时切换上下文（如在新租户内开通、平台态全局定位账号）时用 `ICurrentTenant.Change(...)`，`using` 作用域结束自动恢复。

## 登录与落点：邮箱全局唯一，先登录后选租户

BasicApp 采用**先登录后选租户**：登录页不选择租户，统一在平台态（`_currentTenant.Change(null)`）完成身份认证，成功后按用户的成员关系自动决定落点。

- **邮箱是全平台唯一的登录身份标识**。注册、开通租户管理员、找回密码都强制校验邮箱且全局唯一（`ExistsEmailGloballyAsync`）。平台账号也可用用户名登录。
- 支持密码登录、邮箱验证码登录、第三方（OAuth）登录，均在平台态定位用户后走统一落点逻辑（`IssueLoginTokenWithLandingAsync`）。

### 落点策略

落点由 `ResolveLoginLandingAsync` 决定，返回 `null` 表示落**平台态（前端控制中心）**：

| 情形 | 落点 |
| --- | --- |
| 平台账号（`TenantId=0`，如超管） | 平台态控制中心 |
| 拥有超管角色（全局绑定） | 平台态控制中心 |
| 恰好一个可用租户成员 | 直接进入该租户（免选择） |
| 零个或多个可用租户 | 平台态控制中心（前端展示租户选择/原因） |

"唯一租户"也要确认可用（正常 / 已配置 / 未过期）才直接进入，不可用则落控制中心由前端展示原因。签发的令牌只在进入具体租户时带 `TenantId` claim，平台态不带。

### 随时切换租户

登录后可通过 `SwitchTenantAsync`（`api/Auth`）在有权限的租户之间切换（类似 GitHub 切换 Org）：

- 目标 `TenantId` 归一：`null` 或 `<=0` 视为平台运维态。
- 切到具体租户：必须是该租户的**有效成员**（普通用户），或是**超管**（可进任意租户，见下）。
- 进入平台运维态：仅超管，或拥有 `PlatformAdmin` 成员身份者。
- 切换会在目标上下文内**重建授权快照并签发新令牌**——权限随落点租户实时重算，不沿用旧令牌里的声明。

可切换的租户列表由 `GetMyAvailableTenantsAsync` 提供：普通用户取自己的有效成员关系（`GetActiveByUserIdAsync`，跨租户读取、忽略租户过滤），超管无成员关系、直接返回全部可用租户（正常态、未过期）；前端用 `TenantSwitcherDto` 渲染切换器。

## 成员关系：`SysTenantUser`

"谁能进入哪个租户"由 `SysTenantUser`（`Sys_Tenant_User`）承载——它是 用户 × 租户 的多对多成员表，是跨租户协作与平台运维统一建模的入口。语义上区分：

- `SysUser.TenantId` = 用户主账号的归属租户（注册地）
- `SysTenantUser.TenantId` = 用户拥有成员身份的租户（含主租户 + 外部协作租户）

所有租户访问（含主属）都统一走本表，鉴权路径一致。关键字段：

- `MemberType`（`TenantMemberType`）：`Owner` / `Admin` / `Member` / `External` / `Guest` / `Consultant` / `PlatformAdmin`。
  - 注意：`MemberType` 表达"成员身份类型"而非"权限级别"，**服务层禁止用它直接鉴权**（如 `if MemberType==Admin`），必须走 RBAC 权限链；它只用于成员列表分类、邀请流程控制、`PlatformAdmin` 创建校验。
- `InviteStatus`（`TenantMemberInviteStatus`）：`Pending → Accepted / Rejected / Revoked / Expired`。仅 `Accepted` 且 `Status=Valid` 且当前时间在生效/失效区间内的成员关系才可用于鉴权。
- `EffectiveTime` / `ExpirationTime`：外部协作者/访客常用的时效控制。

## 超级管理员：平台态运维 + 代管租户

超级管理员（角色编码 `super_admin`，运行时特判通配 `*`）是平台运营方账号：

- **恒落平台态**：无论有无租户成员关系，登录都落平台控制中心。
- **权限通配**：授权快照里带 `*`，放行一切，且**不受版本门控**（见下）。
- **可切入任意租户代为管理（impersonation）**：`SwitchTenantAsync` 里超管无需是目标租户成员即可进入。进入后在**目标租户上下文内重建授权快照**——即代管期间以目标租户的口径操作，行为可被审计追溯。

除超管外，拥有 `PlatformAdmin` 成员身份的账号也可进入平台运维态（用于运维/客服/风控）。

## 租户版本（Edition）：权限白名单 + 运行时门控

版本是 B2B SaaS 的**订阅售卖单元 + 功能门控单元**。三张表分工明确：

| 实体 | 表 | 职责 |
| --- | --- | --- |
| `SysTenantEdition` | `Sys_Tenant_Edition` | 版本能做什么、卖多少（`EditionCode`/`Price`/`BillingPeriodMonths`/`UserLimit`/`StorageLimit`/`IsDefault`） |
| `SysTenantEditionPermission` | `Sys_Tenant_Edition_Permission` | 版本 → 权限**白名单**映射（`EditionId` × `PermissionId`，唯一） |
| `SysTenant.EditionId` | `Sys_Tenant` | 某租户订阅了哪个版本（`null` 时取 `IsDefault=true` 的默认版本） |

内置版本种子（`SaasTenantEditionSeeder`）：`free`（免费/默认，只读+基础成员）、`basic`（+组织/用户/角色管理）、`pro`（+高级权限/审计/安全）、`enterprise`（授予除平台专属权限（`SaasPlatformPermissions.PlatformOnlyCodes`）之外的全部全局权限、不限配额）。版本记录本身是平台级（`TenantId=0`），由平台运营维护。

关键约束（见 `SysTenantEdition` 与领域服务）：

- `EditionCode` 全局唯一，如 `free`/`basic`/`pro`/`enterprise`。
- 同一时刻仅一个 `IsDefault=true`；设默认时先清其它默认（`ClearDefaultEditionsAsync`），且默认版本必须处于启用状态、不能被直接取消或禁用。
- 版本白名单只能绑定**平台级全局权限**（`permission.IsGlobal`，即 `TenantId=0`）且权限须启用——非全局权限不应被版本门控。

### 运行时门控：越白名单的权限被拒

门控在 `AuthorizationSnapshotQueryService` 构建授权快照时叠加（`ApplyEditionGatingAsync`）——用户在某租户能生效的权限，被该租户所属版本的白名单**收窄取交集**：越出白名单的权限即便被授予，也不进入生效权限集，等同被拒。

门控的**例外（不门控）**，任一命中即放行原快照：

- 超管通配 `*`；
- 平台运维态（无租户上下文）；
- 租户未绑定版本（`EditionId` 为空）；
- 版本未配置白名单（白名单为空）——避免把租户误锁死（fail-open 仅限"未启用门控"这一语义）。

性能上，门控白名单走**独立的版本门控缓存**（`SaasEditionGateCacheItem`，缓存名 `SaasCacheNames.EditionGate` = `basicapp:saas:tenancy:edition-gate`，10 分钟 TTL），在 per-user 授权快照缓存**之外**按当前租户上下文叠加，避免切换租户后缓存串味，鉴权热路径不必每请求查库。版本白名单/租户换版的写路径会调 `InvalidateEditionGateAsync` 失效缓存（事务提交后生效）。

### 开通一站式：建管理员 + 角色 + 授权

创建租户时若同时提供 `AdminUserName` + `AdminPassword`（此时 `AdminEmail` 必填且须为有效邮箱），`CreateTenantAsync` 会调 `ProvisionTenantAdminAsync` 一站式开通（`TenantProvisionDomainService`），全程在新租户上下文内进行：

1. **确保版本**：租户未指定则取默认版本并回写 `SysTenant.EditionId`；
2. **建管理员**：创建 `SysUser`（校验邮箱全局唯一）+ `SysUserSecurity`（密码哈希）+ `SysTenantUser`（`MemberType=Owner`、`InviteStatus=Accepted`）；
3. **建 Owner 角色并按白名单授权**：创建角色 `tenant_owner`（数据范围 `All`），把该版本白名单里的有效权限批量写成 `SysRolePermission`（`Grant`）；
4. **绑定**：把管理员挂到 Owner 角色（`SysUserRole`）。

于是新租户开通即"能登录、有 Owner、拥有版本范围内的全部权限"，无需人工逐项授权。

### 降级自动回收越权授权

当版本白名单**收窄**（撤销/停用某条版本权限映射），系统会**自动回收该版本下各租户存量的越权授权**（`ReconcileEditionTenantsAuthorizationAsync` → `ReconcileTenantAuthorizationWithEditionAsync`）：

- 触发点：`RevokeTenantEditionPermissionAsync`（撤销）、`UpdateTenantEditionPermissionStatusAsync`（映射被置为非 `Valid`）。恢复为有效则无需回收。
- 回收动作：把该租户下**超出白名单**的 `SysRolePermission` / `SysUserPermission` 存量行置为 `Invalid` 并记 `ExpirationTime` 与回收原因（"套餐变更回收：超出当前版本权限白名单"）。
- 边界：只回收该租户**自有**绑定行（`TenantId=本租户`）；全局行（`TenantId=0`）属平台运维资产，不在回收范围。白名单为空视为"门控未启用"，不回收以防误清。

> 运行时门控（快照收窄）+ 降级回收（存量清理）是两道并行防线：前者保证越权权限当下不生效，后者把已落库的越权授权行物理失效，避免日后升级/门控放开后旧越权授权"复活"。

## 租户解析：请求如何落到租户上下文

一次请求进入后，框架在 HTTP 管线里执行**租户解析链**（`XiHanTenantResolveMiddleware`，在 `XiHan.Framework.Web.Api`），把解析结果写入 `ICurrentTenant`。解析贡献者按顺序命中即止（`Handled=true` 短路）：

| 顺序 | 贡献者 | 来源 | 说明 |
| --- | --- | --- | --- |
| 1 | `CurrentUserTenantResolveContributor` | 已登录用户的 `TenantId`（来自 JWT claim） | 首位插入，是主路径；已认证且带租户则据此解析 |
| 2 | `HeaderTenantResolveContributor` | 请求头 `X-Tenant-Id`（默认键含 `X-Tenant-Id`/`x-tenant-id`/`TenantId`） | `EnableHeaderResolve` 默认开 |
| 3 | `QueryStringTenantResolveContributor` | 查询串 `tenantId` / `tenant` | `EnableQueryStringResolve` 默认开 |

配置节 `XiHan:MultiTenancy:Resolve`（`XiHanTenantResolveOptions`）可调头/查询串键名、开关与 `FallbackTenant`。

对 BasicApp 而言，**主路径是令牌里的 `TenantId`**：先登录后选租户，登录时按落点决定令牌是否带 `TenantId` claim（平台态不带），之后每次请求由 `CurrentUserTenantResolveContributor` 据此还原上下文；换租户则重新签发令牌。解析链的完整机制见 [XiHan.Framework.MultiTenancy](https://framework.docs.xihanfun.com/packages/multitenancy)。

## 与权限的交叉点

版本门控是租户维度对权限的**再收窄**，叠在 RBAC/ABAC 判定链之上。完整判定链（认证 → 租户解析 → RBAC → ABAC → 数据范围 → 字段脱敏）与权限码/数据范围/FLS 细节见 [权限模型](./permission)。要点回顾：

- 授权快照按 `(当前租户 OR 全局 TenantId=0)` 聚合绑定行，再被当前租户版本白名单取交集收窄。
- 超管（`*`）与平台运维态不受版本门控。
- 版本白名单只挂平台级全局权限。

## 下一步

- [权限模型](./permission)：RBAC + ABAC、权限码、数据范围、字段脱敏
- [系统架构](./introduction)：租户解析在请求管道中的位置
- [XiHan.Framework.MultiTenancy](https://framework.docs.xihanfun.com/packages/multitenancy)：框架层租户上下文、解析链与存储
