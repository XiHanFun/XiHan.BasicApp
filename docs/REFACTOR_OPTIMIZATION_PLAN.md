# XiHan.BasicApp 全面重构优化计划

> 计划日期：2026-04-26  
> 适用范围：`XiHan.BasicApp/backend` 与 `XiHan.BasicApp/frontend/src`  
> 执行原则：严格自底向上，先修底层契约，再重构实体、领域、应用服务和前端页面。

## 1. 目标与边界

目标：

- 以 `XiHan.BasicApp.Saas/Domain/Entities` 为事实源，完成实体、仓储、领域服务、应用服务、DTO、前端 API、前端页面的系统性重构。
- 对齐 Framework 的多租户、软删除、审计、DynamicApi、RBAC、ABAC、缓存和设置能力。
- 修复当前已暴露的租户隔离、聚合根滥用、`YesOrNo` 泛化、权限模型僵硬、导航属性过重、FLS/审计/敏感字段出口不一致等问题。
- 保持 `frontend/packages` 基础框架层尽量不动；主要改造 `frontend/src`。

边界：

- 不新增业务 Controller，所有 HTTP 端点通过 AppService + `[DynamicApi]` 暴露。
- 不把 SaaS 业务概念写入 `XiHan.Framework`。
- 不做旧字段静默兼容。实体、DTO、前端类型要一次性对齐，但需要提供数据迁移脚本和回滚脚本。
- 不在前端实现真正授权，前端只做展示、交互和路由守卫。

## 2. 当前代码事实

初始扫描基于 2026-04-26 本地工作区：

- 后端解决方案：`backend/XiHan.BasicApp.slnx`。
- 应用内核：`backend/src/framework/XiHan.BasicApp.Core`，已有 `BasicAppEntity`、`BasicAppCreationEntity`、`BasicAppFullAuditedEntity`、`BasicAppAggregateRoot` 等继承链。
- SaaS 模块：`backend/src/modules/XiHan.BasicApp.Saas`，已有 Entities、Partials、Aggregates、Enums、Repositories、DomainServices、Application/AppServices、QueryServices、Caching、InternalServices、Seeders。
- 前端业务层：`frontend/src`，已有 `api/base.ts`、`api/request.ts`、`api/modules/*.ts`、`views/system/*`、`router/routes/index.ts`。
- `docs` 目录本轮之前为空。
- 业务 Controller 扫描当前无结果，说明后端基本符合 DynamicApi 方向。

已发现的高优先级问题：

- `SaasSeedDefaults.PlatformTenantId`、`RoleBasicConstants.PlatformTenantId`、`SaasTenantQueryHelper.PlatformTenantId` 当前为 `1`，与 `TenantId = 0` 平台语义冲突。
- `RbacRoleStore`、`RbacPermissionStore`、`RoleHierarchyRepository` 存在 `TenantId IS NULL` 字符串条件，违反非空 TenantId 约定。
- `YesOrNo` 在实体、DTO、Seeder、仓储、服务中大量表达业务状态，需要替换为语义明确的枚举。
- `SysUser.pl.cs` 导航属性过重，包含日志、文件、通知、OAuth、邮件、短信、统计、审计等非核心导航。
- `SysPermission.ResourceId`、`OperationId` 仍为必填 `long`，权限模型尚不支持功能权限、数据范围权限等非资源操作形态。
- `SysFieldLevelSecurity` 当前继承 `BasicAppAggregateRoot`，但按计划应降级为 `BasicAppFullAuditedEntity`。
- `SysFileStorage` 仍保存 `SignedUrl`、`SignedUrlExpiresAt`、`StorageDirectory`、`Endpoint`、`CustomDomain` 等运行时或可推导字段。

## 3. 执行顺序总览

| 阶段 | 主题 | 主要路径 | 依赖 |
|---|---|---|---|
| B0 | 基线扫描与分支保护 | 全仓 | 无 |
| B1 | Framework 依赖确认 | Framework + BasicApp.Core | Framework F1/F2 |
| B2 | 应用内核重构 | `backend/src/framework/XiHan.BasicApp.Core` | B1 |
| B3 | SaaS 实体重构 | `Domain/Entities` | B2 |
| B4 | 领域层重构 | `Domain/Repositories`, `DomainServices`, `Events`, `Specifications` | B3 |
| B5 | 基础设施重构 | `Infrastructure` | B4 |
| B6 | 应用服务重构 | `Application` | B4/B5 |
| B7 | 前端 API 重构 | `frontend/src/api` | B6 |
| B8 | 前端页面重构 | `frontend/src/views`, `router`, `stores` | B7 |
| B9 | 数据迁移、种子、回滚 | `Seeders`, migrations/scripts | B3-B8 |
| B10 | 全量验证与文档收口 | backend, frontend, docs | 全阶段 |

## 4. B0：基线扫描与保护

### 4.1 任务

- 固定当前基线编译结果：
  - `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\XiHan.BasicApp.slnx`
  - `pnpm type-check`
  - `pnpm build`
- 建立扫描清单：
  - Controller 禁止扫描。
  - `TenantId IS NULL`、`TenantId == null`、`PlatformTenantId = 1` 扫描。
  - `YesOrNo` 使用点扫描。
  - `[JsonIgnore]` 敏感字段扫描。
  - `BasicAppAggregateRoot` 使用点扫描。
- 生成实体清单：实体名称、基类、是否分表、是否软删除、索引、核心外键、是否聚合根。
- 生成前端页面/API 清单：后端 AppService、DTO、前端 API、前端页面是否一一对应。

### 4.2 验收

- 输出扫描结果作为后续任务 issue 列表。
- 对现有失败的构建或类型检查标记为环境问题或代码问题。
- 不在 B0 修改业务行为。

## 5. B1：Framework 依赖确认

本阶段不直接重构 BasicApp 业务，只确认底层契约是否可支撑后续改造。

必须确认：

- `TenantId = 0` 平台语义在 Framework 过滤器、AOP、仓储中成立。
- `CreatedTime`、`ModifiedTime`、`DeletedTime` 统一 UTC 策略。
- `CreateNoTenantQueryable()`、`CreateWithDeletedQueryable()` 可用于平台运维、审计恢复场景。
- `EnableAutoUpdateQueryFilter`、`EnableAutoDeleteQueryFilter` 对 UPDATE/DELETE 生效。
- DynamicApi 路由命名、HTTP 方法推断和 OpenAPI 分组稳定。
- `PermissionAuthorize`、`AbacAuthorize`、缓存多租户 key 可被 BasicApp 接入。

阻塞规则：

- 如果 Framework 未确认 TenantId、软删除、DynamicApi 的核心行为，不进入 B3 之后的实体和服务重构。
- 如果 Framework 需要破坏性调整，先完成 Framework 文档和迁移说明，再继续 BasicApp。

## 6. B2：应用内核重构

路径：`backend/src/framework/XiHan.BasicApp.Core`

### 6.1 实体基类

任务：

- 对齐 Framework 实体基类继承链，确认以下 BasicApp 基类只做薄封装：
  - `BasicAppEntity`
  - `BasicAppCreationEntity`
  - `BasicAppModificationEntity`
  - `BasicAppDeletionEntity`
  - `BasicAppFullAuditedEntity`
  - `BasicAppAggregateRoot`
- 删除或重写注释中与实际 Framework 行为冲突的约定。
- 明确关联表、日志表、普通实体、聚合根的基类选择规则。
- `TenantId = 0` 作为唯一平台语义，不允许再出现平台租户 `1` 的常量。

### 6.2 公共 DTO 与分页

任务：

- 确认 `BasicAppDto`、`BasicAppCDto`、`BasicAppUDto` 不暴露不该由前端写入的审计字段。
- 确认前端分页、排序、过滤结构和 Framework PageRequest 一致。
- 租户上下文不得由前端 DTO 传入决定授权租户。

### 6.3 验收

- BasicApp.Core 编译通过。
- `rg -n "PlatformTenantId\s*=\s*1|TenantId\s+IS\s+NULL" backend/src/framework backend/src/modules -g "*.cs"` 无新增。
- 基类选择规则写入文档并被实体重构引用。

## 7. B3：SaaS 实体重构

路径：`backend/src/modules/XiHan.BasicApp.Saas/Domain/Entities`

### 7.1 文件结构

目标结构：

```text
Domain/Entities/
  SysUser.cs
Domain/Entities/Expands/
  SysUser.Expand.cs
Domain/Entities/Aggregates/
  SysUser.Aggregate.cs
Domain/Entities/Enums/
  SysUser.Enum.cs
Domain/ValueObjects/
  ClientInfo.cs
  EffectivePeriod.cs
  BusinessReference.cs
  DeviceInfo.cs
  RequestContext.cs
```

任务：

- 将当前 `Partials/*.pl.cs` 迁移为 `Expands/*.Expand.cs` 或保持目录名但统一命名规范；最终以一个规范为准。
- 只有真正聚合根保留 `Aggregates/*.Aggregate.cs` 行为方法。
- 枚举按领域拆分，不再用一个 `YesOrNo` 表达所有业务状态。

### 7.2 聚合根降级和保留

降级为 `BasicAppFullAuditedEntity`：

| 实体 | 原因 |
|---|---|
| `SysFieldLevelSecurity` | 配置型策略，无独立事务不变量 |
| `SysConstraintRule` | RBAC/ABAC 策略子实体 |
| `SysNotification` | 简单消息实体 |
| `SysEmail` | 简单消息实体 |
| `SysSms` | 简单消息实体 |
| `SysFile` | 文件元数据无复杂不变量 |
| `SysDict` | 简单 CRUD |
| `SysConfig` | 简单 CRUD |
| `SysUserSession` | 会话记录本身不是聚合根，撤销通过服务和事件编排 |

保留为聚合根：

- `SysUser`
- `SysTenant`
- `SysRole`
- `SysPermission`
- `SysResource`
- `SysOperation`
- `SysDepartment`
- `SysOAuthApp`
- `SysTask`
- `SysReview`

验收：

- `rg -n ": BasicAppAggregateRoot" Domain/Entities -g "*.cs"` 只出现保留清单。
- 被降级实体的仓储接口和实现同步从 AggregateRepository 改为合适仓储。

### 7.3 TenantId 语义修复

任务：

- 将所有平台租户常量改为 `0`。
- 替换 `TenantId IS NULL`、`TenantId == null`、`TenantId = null` 逻辑。
- Seeder 写入平台级数据时统一 `TenantId = 0`。
- 业务租户写入时依赖 Framework AOP 或显式租户上下文，不在业务代码随意赋值。
- `SysTenant` 作为平台元数据，不能被业务租户过滤误隐藏；平台管理功能使用明确的跨租户查询和审计。

已知位置：

- `Seeders/SaasSeedDefaults.cs`
- `Constants/Basic/RoleBasicConstants.cs`
- `Infrastructure/Repositories/SaasTenantQueryHelper.cs`
- `Infrastructure/Authorization/RbacRoleStore.cs`
- `Infrastructure/Authorization/RbacPermissionStore.cs`
- `Infrastructure/Repositories/RoleHierarchyRepository.cs`

验收：

- `rg -n "PlatformTenantId\s*=\s*1|TenantId\s+IS\s+NULL|TenantId\s*==\s*null|TenantId\s*=\s*null" backend/src -g "*.cs"` 无违规结果。
- 跨租户负权限测试覆盖普通租户读取、更新、删除其他租户数据。

### 7.4 枚举语义化

任务：

- 替换 `YesOrNo`：
  - 启停状态：`EnableStatus`
  - 有效性：`ValidityStatus`
  - 邀请：`TenantMemberInviteStatus`
  - 授权动作：`PermissionAction`
  - 消息读写：按消息领域枚举
  - 任务状态：按任务生命周期枚举
  - 文件/存储状态：使用专用状态枚举
- 每个枚举值必须带 `[Description("中文描述")]`。
- DTO、Seeder、前端类型同步更新。

验收：

- `rg -n "YesOrNo" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"` 仅允许迁移期临时兼容文件，最终为 0。
- 前端枚举选项来自 `EnumAppService` 或明确的本地类型定义。

### 7.5 权限模型重构

任务：

- `SysPermission.ResourceId`、`OperationId` 改为 `long?`。
- 新增 `PermissionType`：
  - `ResourceBased`
  - `Functional`
  - `DataScope`
- `PermissionCode` 使用 `{模块}:{资源}:动作[:子动作]`。
- `IsGlobal` 改为计算语义：`TenantId == 0`，评估是否从数据库字段移除。
- 唯一索引从强依赖 `(ResourceId, OperationId)` 调整为 `TenantId + PermissionCode` 为主。
- `SysRolePermission` 支持：
  - `PermissionAction` Grant/Deny
  - `DataPermissionScope`
  - `Filters` JSON
  - 生效/失效时间
  - 授权原因和操作人审计

验收：

- 功能权限和数据范围权限无需绑定 Resource/Operation 也可创建。
- Deny 优先、Priority 冲突处理、过期授权均有测试。
- PermissionSeed 注册所有权限码。

### 7.6 用户导航属性精简

任务：

- `SysUser` 仅保留核心导航：
  - `Security`
  - `UserRoles`
  - `UserPermissions`
  - `UserDepartments`
- 移除日志、通知、邮件、短信、文件、OAuth、统计、审计等导航属性。
- 相关数据通过各自仓储或 QueryService 按需查询。

验收：

- `SysUser.Expand.cs` 不再包含非核心导航。
- 用户详情页需要的统计、角色、部门、权限由 QueryService 显式投影，不依赖懒加载大对象。

### 7.7 文件存储实体精简

任务：

- 从 `SysFileStorage` 移除运行时生成或可推导字段：
  - `SignedUrl`
  - `SignedUrlExpiresAt`
  - `StorageDirectory`
  - `Endpoint`
  - `CustomDomain`
- 签名 URL 由应用服务按需生成。
- Endpoint、CustomDomain 从存储配置读取。
- StorageDirectory 从 StoragePath 推导。

验收：

- 文件下载、预览、签名链接不依赖数据库持久化临时 URL。
- 迁移脚本保留历史数据备份和回滚。

### 7.8 索引与敏感字段

任务：

- 每个实体确认：
  - `(TenantId, CreatedTime)` 索引。
  - 需要软删除的实体有 `(TenantId, IsDeleted)` 索引。
  - 外键字段有普通索引。
  - 业务唯一键有 `TenantId` 前缀。
- 密码、Token、Secret、连接串字段加 `[JsonIgnore]`，且不出现在响应 DTO。

验收：

- 实体索引清单完成。
- 敏感字段 DTO 扫描无泄漏。

## 8. B4：领域层重构

路径：

- `Domain/Repositories`
- `Domain/DomainServices`
- `Domain/Events`
- `Domain/Specifications`
- `Domain/ValueObjects`

任务：

- 仓储接口只表达稳定数据访问，不做页面拼装。
- QueryService 负责列表、树、详情等读模型投影。
- DomainService 负责跨聚合规则：
  - 用户创建同步安全扩展和租户成员。
  - 角色授权、继承、数据范围。
  - 权限创建和冲突校验。
  - 租户状态、套餐、成员变更。
  - Session/Token 撤销。
- 领域事件用于缓存失效、审计、通知、Token 级联撤销。
- 规约只表达可复用业务条件，不夹带页面筛选。

验收：

- 仓储、DomainService、QueryService 职责边界清晰。
- 角色权限、用户权限、部门数据范围、FLS 配置变更能触发缓存失效事件。
- Session 撤销能级联 Token 撤销。

## 9. B5：基础设施层重构

路径：`Infrastructure`

任务：

- 所有手写租户过滤替换为 Framework 全局过滤或显式逃逸 API。
- `RbacRoleStore`、`RbacPermissionStore`、`RoleHierarchyRepository` 移除 `TenantId IS NULL`。
- 认证用户、角色、权限 Store 与新枚举、新权限模型对齐。
- ABAC evaluator 接入 Framework 通用 evaluator，业务侧只提供属性收集和策略存储。
- FLS 只在序列化出口和导出出口执行脱敏，不在前端二次裁剪敏感数据。
- 审计写入器覆盖：
  - 权限授予/撤销。
  - FLS 配置变更。
  - 敏感字段访问。
  - 跨租户操作。
  - 管理员账号操作。
- Seeder 重做为可重复执行、基于业务键 upsert，不依赖硬编码平台租户 `1`。

验收：

- 租户过滤违规扫描为 0。
- 授权 Store 对 `TenantId = 0` 平台权限和当前租户权限合并查询正确。
- 审计日志覆盖安全敏感操作。

## 10. B6：应用服务层重构

路径：`Application`

任务：

- AppService 默认 `[Authorize]`。
- 写操作 AppService 标记 `[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务")]`。
- 方法级补充 `[PermissionAuthorize("saas:resource:action")]`。
- ABAC 场景补充 `[AbacAuthorize("policy-id")]`。
- 读写分离：
  - AppServices：命令、事务、权限入口。
  - QueryServices：列表、树、详情、缓存读模型。
- DTO 与实体一一对齐：
  - 删除旧兼容字段。
  - 不暴露敏感字段。
  - 不允许前端传入 `TenantId` 参与鉴权。
- Mapster 映射集中到 `Application/Mappers`。
- 缓存失效集中到事件处理器，不在页面服务里散落删除缓存。

验收：

- `rg -n "class .*Controller" backend/src -g "*.cs"` 无业务 Controller。
- 所有 AppService 有授权策略或明确 `[AllowAnonymous]`。
- 权限码在 Seed 中登记。
- 正权限、负权限、跨租户越权三类测试覆盖核心服务。

## 11. B7：前端 API 层重构

路径：`frontend/src/api`

目标结构：

```text
api/
  request.ts
  base.ts
  helpers.ts
  modules/
    identity/
      user.ts
      types.ts
    authorization/
      role.ts
      permission.ts
      menu.ts
      field-level-security.ts
      types.ts
    organization/
      department.ts
      types.ts
    tenant/
      tenant.ts
      types.ts
```

任务：

- 按后端领域重新组织 API 模块。
- 每个 API 模块同目录维护 types，与后端 DTO 对齐。
- 移除旧兼容字段和静默字段映射。
- `useBaseApi()` 路由名与 DynamicApi ControllerName 保持一致。
- 权限、菜单、数据范围、ABAC、FLS 的类型语义与后端一致。
- 对登录、切换租户、刷新 Token、当前用户菜单/权限等核心 API 增加明确类型。

验收：

- `pnpm type-check` 通过。
- API 模块无 any 型逃逸，除非有明确泛型封装原因。
- 前端不出现以 `TenantId` 参数决定越权访问的 API。

## 12. B8：前端页面层重构

路径：

- `frontend/src/views`
- `frontend/src/router`
- 必要时 `frontend/packages/stores`、`frontend/packages/router`

页面结构规范：

```text
views/identity/user/
  index.vue
  components/
    UserForm.vue
    UserDetail.vue
    UserSearch.vue
```

任务：

- 系统管理页面从 `views/system/*` 逐步迁移到领域目录，或保留 system 入口但内部按领域组件化；最终采用一个一致规范。
- 表格统一 VxeTable，表单统一 Naive UI `NForm`。
- 列表页包含搜索、分页、排序、批量操作、权限按钮状态。
- 新增/编辑表单与后端 CreateDto/UpdateDto 一致。
- 详情抽屉/弹窗与 QueryService 读模型一致。
- FLS 只影响显示和编辑状态，不假设前端裁剪就是安全。
- 菜单和按钮权限由后端返回权限码驱动，前端只做 UX 隐藏。
- `frontend/packages` 仅在以下情况修改：
  - `useAccessStore` / `useUserStore` 需要适配新权限模型。
  - 动态路由需要适配新菜单结构。
  - 公共类型必须对齐后端 DTO。

验收：

- `pnpm type-check`
- `pnpm build`
- 仓库已有 lint 时执行 `pnpm lint`
- 移动端和桌面主要页面无布局重叠、按钮文字溢出。

## 13. B9：数据迁移、种子与回滚

任务：

- 每个实体破坏性变更都配套：
  - 前置数据扫描 SQL。
  - 迁移脚本。
  - 回滚脚本。
  - 数据校验脚本。
- 重点迁移：
  - 平台租户 `1` → `0`。
  - `TenantId IS NULL` 历史数据 → `0` 或明确租户。
  - `YesOrNo` → 新枚举值。
  - `SysPermission.ResourceId/OperationId` 可空与 `PermissionType` 填充。
  - `SysFileStorage` 移除字段前备份。
- Seeder 必须幂等：
  - 以 Code、Name、PermissionCode 等业务键 upsert。
  - 不依赖硬编码雪花 ID。
  - 不依赖租户 `1` 代表平台。

验收：

- 空库初始化成功。
- 旧库迁移后核心功能可用。
- 回滚脚本能恢复变更前字段和关键数据。

## 14. B10：全量验证门禁

后端：

```powershell
dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\XiHan.BasicApp.slnx
rg -n "class .*Controller" E:\Repository\XiHanFun\XiHan.BasicApp\backend\src -g "*.cs"
rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|TenantId\s*=\s*null|PlatformTenantId\s*=\s*1" E:\Repository\XiHanFun\XiHan.BasicApp\backend\src -g "*.cs"
rg -n "YesOrNo" E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas -g "*.cs"
```

前端：

```powershell
pnpm type-check
pnpm build
pnpm lint
```

安全测试：

- 正权限：有权限的租户成员能访问授权资源。
- 负权限：Deny 授权覆盖 Grant。
- 跨租户：普通租户无法读取、更新、删除其他租户数据。
- FLS：敏感字段在响应和导出中被脱敏，后端仍拒绝不可编辑字段写入。
- Session/Token：Session 撤销后关联 Token 全部失效。
- 审计：授权变更、敏感读取、跨租户操作、管理员操作有审计记录。

## 15. 阶段交付格式

每个阶段完成后记录：

- 已解决的设计问题。
- 修改文件。
- 行为变化。
- 迁移脚本和回滚脚本。
- 验证命令和结果。
- 剩余风险。

建议阶段提交粒度：

1. `docs`: 计划和 ADR。
2. `framework-dependency`: BasicApp.Core 与 Framework 契约对齐。
3. `tenant-semantics`: TenantId 平台语义修复。
4. `entity-bases`: 聚合根降级和实体基类调整。
5. `permission-model`: RBAC/ABAC 权限模型重构。
6. `app-services`: AppService、DTO、QueryService、缓存失效。
7. `frontend-api`: API 类型和请求模块。
8. `frontend-views`: 页面和路由。
9. `migration-seed`: 数据迁移和 Seeder。
10. `quality-gates`: 构建、测试、扫描、文档收口。

## 16. 风险与处置

- `TenantId = 0` 修复会影响 Seeder、权限 Store、缓存 key、已有数据，必须先做迁移计划。
- `YesOrNo` 替换影响面大，应按领域分批，不跨多个领域混改。
- 聚合根降级会影响仓储接口和领域事件，必须先确认事件是否仍需要由服务发布。
- `SysPermission` 模型变化会影响菜单、角色权限、用户直授权、租户版本权限、前端按钮权限。
- 前端 API 目录重组容易造成路径导入大面积变化，应与后端 DTO 分阶段对齐。
- packages 修改必须单独说明原因，避免基础框架层被业务侵入。

## 17. 当前首批建议任务

第一批只处理基础阻塞项：

1. 修复平台租户语义：`PlatformTenantId = 1` → `0`，替换 `TenantId IS NULL`。
2. 确认 Framework 多租户过滤和无租户上下文策略。
3. 生成实体基类清单，先完成聚合根降级。
4. 为 `SysPermission` 新模型建立迁移草案。
5. 将 `SysUser` 导航属性精简为核心导航。
6. 建立后端扫描命令并纳入每次重构验收。

这些任务完成后，再进入 DTO、AppService、前端 API 和页面重构。

## 18. 阶段执行记录

### 2026-04-26 B0 基线扫描与保护

本阶段只做基线验证与文档更新，不修改业务行为。

执行结果：

- `dotnet --info`：本机安装 .NET SDK `8.0.413`、`9.0.313`、`10.0.107`、`10.0.202`。BasicApp `global.json` 固定 `10.0.107` 且 `rollForward = disable`。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\XiHan.BasicApp.slnx`：失败。沙箱内 Restore 阶段无具体错误；沙箱外还原成功，但构建写入 `obj\Debug\net10.0\*.AssemblyReference.cache` 和 `WebHost\obj\Debug\net10.0\apphost.exe` 被拒绝，最终 `CreateAppHost` 抛出 `UnauthorizedAccessException`。该失败属于本地构建输出目录权限/锁定问题，非本阶段代码变更导致。
- `pnpm type-check`：通过。
- `pnpm build`：沙箱内因 Vite/esbuild 子进程 `spawn EPERM` 失败；沙箱外通过。构建产物 `frontend/dist/` 为 ignored。
- `rg -n "class .*Controller" backend/src -g "*.cs"`：0 个匹配，当前未发现业务 Controller。
- 严格租户扫描：`TenantId IS NULL` / `TenantId = null` / `TenantId == null` / `PlatformTenantId = 1` 共 9 个匹配，分布在：
  - `Constants/Basic/RoleBasicConstants.cs`
  - `Seeders/SaasSeedDefaults.cs`
  - `Infrastructure/Repositories/SaasTenantQueryHelper.cs`
  - `Infrastructure/Authorization/RbacRoleStore.cs`
  - `Infrastructure/Authorization/RbacPermissionStore.cs`
  - `Infrastructure/Repositories/RoleHierarchyRepository.cs`
- `rg -n "YesOrNo" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：301 个匹配。

协作状态：

- 本阶段未修改后端和前端业务代码。
- 构建生成的 `backend/nupkgs/`、`frontend/dist/` 为 ignored，不纳入提交。
- 后续 B1/B2 进入代码修改前，必须再次检查两个仓库 git 状态，只提交本任务修改的文件。

### 2026-04-26 B1 SaaS 枚举 Description 精简

本阶段仅优化 `XiHan.BasicApp.Saas/Domain/Entities/Enums/*.cs` 的 `[Description]` 显示标签，不修改枚举名称、枚举值和业务流程。

执行结果：

- 将带解释、示例、冒号说明、括号补充的 Description 收敛为短显示标签，例如字段脱敏策略、租户成员状态、数据权限范围、资源访问级别、操作分类、职责分离约束、审计风险等级、登录结果等。
- 修正 `SysFieldLevelSecurity.Enum.cs` 中部门目标 Description 内嵌未转义双引号的问题，统一简化为 `部门`。
- 保留 `HMAC-SHA256`、`HMAC-SHA512`、`SQL Server`、`MinIO` 等技术专有短标签。
- 未新增迁移脚本；枚举底层值未变化，数据库存量数据不受影响。

验证结果：

- `git diff --check -- backend/src/modules/XiHan.BasicApp.Saas/Domain/Entities/Enums/*.cs`：通过。
- Description 扫描：长句、括号解释、冒号说明基本清理完成；剩余 `HMAC-SHA256`、`HMAC-SHA512` 为预期保留的算法名。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --no-restore`：失败，MSBuild 摘要为 `0 个警告 / 0 个错误`；诊断日志出现本机 SDK workload resolver 的 `MSB4276` 路径解析提示，未定位到本次枚举文案变更引入的编译错误。后续全量质量门禁需继续复核本机 .NET SDK/workload 状态。

协作状态：

- 本阶段仅提交本任务修改的枚举文件和本文档。
- BasicApp 中既有未跟踪的 `Domain/Events/`、`Domain/Repositories/`、`Domain/ValueObjects/` 目录未纳入本阶段。
- Framework 仓库无改动。

### 2026-04-26 B2 SaaS Entities 编译恢复

本阶段围绕 `XiHan.BasicApp.Saas/Domain/Entities` 重构后的编译阻塞做最小恢复，不扩展业务行为。

执行结果：

- 恢复 `PermissionType` 枚举，补齐 `SysPermission` 对资源操作、功能、数据范围三类权限的类型引用。
- 重建 `XiHanBasicAppRbacModule` 模块入口，恢复 WebHost 与 CodeGeneration 对 SaaS/RBAC 模块的依赖解析。
- 重建 `BasicAppNotificationHub`、`BasicAppChatHub` 最小 Hub 类，恢复 WebHost 的 SignalR 映射引用。
- 修正 `SysOAuthCode`、`SysTask` XML 注释中的比较符描述，消除 XML 文档解析警告。
- 对齐 CodeGeneration 实体与 Seeder：补充 SaaS 实体命名空间引用，并将 SaaS 实体 `Status` 写入值改为 `EnableStatus.Enabled`。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`0` 个警告，`0` 个错误。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\XiHan.BasicApp.slnx --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`49` 个警告，`0` 个错误。
- 全量构建剩余警告均为 `NU5104`：当前稳定包依赖 `XiHan.Framework.* 2.5.0-preview.2` 预发布包，非本阶段实体代码错误。
- 常规源码输出目录构建曾因并行任务占用 `obj` 文件出现 `Access denied`，本阶段使用隔离 `--artifacts-path` 完成验证，避免影响其他同步重构任务。

协作状态：

- 阶段前后检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态；`XiHan.Framework` 无改动。
- 本阶段只提交 BasicApp 中本任务涉及的编译修复和本文档更新，不推送远端。

### 2026-04-26 D1 Domain 非实体层契约基线

本阶段开始重建 `XiHan.BasicApp.Saas/Domain` 非实体层。范围限定为领域契约与纯领域规则，不接入 Infrastructure，不修改 `Entities`。

执行结果：

- 重建 `Domain/ValueObjects`：
  - `EffectivePeriod`：统一生效/失效时间判断。
  - `BusinessReference`、`ClientInfo`、`DeviceInfo`：为日志、审计、会话、授权事件提供可复用值对象。
  - `PermissionGrantSnapshot`、`AuthorizationDecision`、`TenantMemberSnapshot`：为权限裁决和租户访问判断提供快照模型。
- 重建 `Domain/Events`：
  - `SaasDomainEventBase`：统一租户、操作人、原因上下文。
  - `AuthorizationChangedDomainEvent`、`TenantMembershipChangedDomainEvent`、`HierarchyChangedDomainEvent`：覆盖授权变更、租户成员变更、组织/角色层级变更三类后续缓存失效与审计入口。
- 重建 `Domain/Repositories` 契约：
  - `ISaasRepository<TEntity>`、`ISaasAggregateRepository<TAggregateRoot>` 统一 SaaS 仓储基线。
  - 覆盖用户、租户成员、用户角色、用户直授权限、角色、权限、资源、操作、角色权限、ABAC 条件、约束规则、租户、部门、部门闭包、用户部门、角色闭包、角色数据范围等核心 RBAC/ABAC/组织链路。
- 重建 `Domain/Specifications`：
  - `ActiveUserSpecification`、`AvailableTenantSpecification`、`ActiveTenantUserSpecification`。
  - `EnabledRoleSpecification`、`EnabledPermissionSpecification`。
  - `ValidUserRoleSpecification`、`ValidRolePermissionSpecification`、`ValidUserPermissionSpecification`。
- 重建 `Domain/DomainServices`：
  - `IPermissionDecisionDomainService` / `PermissionDecisionDomainService`：实现用户直授 Deny、用户直授 Grant、角色 Grant、角色 Deny 的裁决顺序。
  - `ITenantAccessDomainService` / `TenantAccessDomainService`：集中判断租户成员是否可进入租户、是否平台管理员身份。

设计约束：

- 本阶段不恢复旧 DomainServices / Repositories 的实现类，不从历史提交直接回滚。
- 仓储接口只表达领域查询与持久化契约，具体 SqlSugar 实现留到 Infrastructure 阶段。
- 纯规则服务只处理内存快照，不直接依赖数据库、缓存、当前用户上下文或 HTTP 上下文。

验证结果：

- `git diff --check`：通过。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配，未新增 Controller。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`0` 个警告，`0` 个错误。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\XiHan.BasicApp.slnx --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`0` 个警告，`0` 个错误。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的 Domain 非实体层重建文件和本文档，不推送远端。

### 2026-04-26 D2 Domain 仓储租户上下文修正

本阶段修正 D1 仓储契约中的租户参数设计。普通租户数据查询不得在仓储方法中显式传入 `tenantId`，应由登录/切换租户后的会话上下文决定当前租户，并由底层仓储和 SqlSugar 全局过滤器自动注入租户查询条件。

执行结果：

- 移除 `IUserRepository`、`IRoleRepository`、`IPermissionRepository`、`IResourceRepository`、`IOperationRepository` 等普通租户域仓储方法中的 `tenantId` 参数。
- 移除用户角色、用户直授权限、角色权限、ABAC 条件、约束规则、部门、部门闭包、用户部门、角色闭包、角色数据范围等仓储方法中的显式 `tenantId` 参数。
- 保留实体、事件、快照中的 `TenantId` 数据字段语义；这些字段用于审计、事件上下文和数据本身，不作为普通仓储查询入参。
- 明确跨租户/平台查询后续必须走独立平台服务或显式逃逸接口，例如 `CreateNoTenantQueryable()`，并记录审计，不混入普通仓储契约。
- 重组 `Domain/Repositories` 文件结构，按领域功能拆分目录和单接口文件：
  - `Abstractions/`：`ISaasRepository`、`ISaasAggregateRepository`。
  - `Identity/`：`IUserRepository`。
  - `Tenancy/`：`ITenantRepository`、`ITenantUserRepository`。
  - `Organization/`：`IDepartmentRepository`、`IDepartmentHierarchyRepository`、`IUserDepartmentRepository`。
  - `Authorization/`：角色、权限、资源、操作、授权关系、ABAC 条件、约束规则、角色闭包、角色数据范围等仓储契约。
- 统一 Domain 子目录的命名空间策略：子目录下的二级目录可用于物理分组，但命名空间保持在父级，例如 `Repositories/Authorization/*` 仍使用 `XiHan.BasicApp.Saas.Domain.Repositories`，`DomainServices/Implementations/*` 仍使用 `XiHan.BasicApp.Saas.Domain.DomainServices`，避免调用方引入一组细碎 `using`。

验证结果：

- `rg -n "tenantId" backend/src/modules/XiHan.BasicApp.Saas/Domain/Repositories -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Domain\.Repositories\." backend/src/modules/XiHan.BasicApp.Saas/Domain/Repositories -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Domain\.DomainServices\." backend/src/modules/XiHan.BasicApp.Saas/Domain/DomainServices -g "*.cs"`：0 个匹配。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\XiHan.BasicApp.slnx --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`0` 个警告，`0` 个错误。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态；`XiHan.Framework` 无改动。
- 本阶段只提交 BasicApp 的 Domain 仓储契约修正和本文档，不推送远端。

### 2026-04-26 D3 Domain 规约结构拆分

本阶段继续 B4 领域层重构，将 `Domain/Specifications` 从单个聚合文件拆为按领域物理分组的单规约文件。只调整文件组织，不修改规约表达式行为。

执行结果：

- 删除 `Domain/Specifications/SaasSpecifications.cs` 聚合文件。
- 新建 `Domain/Specifications/Identity/ActiveUserSpecification.cs`。
- 新建 `Domain/Specifications/Tenancy/AvailableTenantSpecification.cs`、`ActiveTenantUserSpecification.cs`。
- 新建 `Domain/Specifications/Authorization/EnabledRoleSpecification.cs`、`EnabledPermissionSpecification.cs`、`ValidUserRoleSpecification.cs`、`ValidRolePermissionSpecification.cs`、`ValidUserPermissionSpecification.cs`。
- 保持二级物理目录的父级命名空间策略：所有规约仍使用 `XiHan.BasicApp.Saas.Domain.Specifications`，不引入 `Specifications.Identity`、`Specifications.Authorization` 等细分命名空间。

设计约束：

- 规约只表达可复用领域条件，不夹带页面筛选、租户入参或读模型投影。
- 普通租户过滤仍由当前会话上下文和底层仓储/SqlSugar 过滤器处理，规约不显式接收 `tenantId`。

验证结果：

- `git diff --check`：通过。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Domain\.Specifications\." backend/src/modules/XiHan.BasicApp.Saas/Domain/Specifications -g "*.cs"`：0 个匹配。
- `rg -n "FileName:SaasSpecifications|public sealed class .*Specification" backend/src/modules/XiHan.BasicApp.Saas/Domain/Specifications -g "*.cs"`：只剩拆分后的 8 个规约类。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`49` 个 `NU5104` 预发布依赖警告，`0` 个错误。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\XiHan.BasicApp.slnx --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`0` 个警告，`0` 个错误。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的 Domain 规约拆分和本文档，不推送远端。

### 2026-04-26 D4 Domain 领域事件契约补齐

本阶段继续 B4 领域层重构，补齐安全敏感变更和会话撤销所需的领域事件契约。范围限定为事件定义与物理结构，不接入 Infrastructure 事件处理器，不修改实体行为。

执行结果：

- 重组 `Domain/Events` 物理目录：
  - `Abstractions/`：`SaasDomainEventBase`。
  - `Authorization/`：授权、数据范围、字段级安全相关事件。
  - `Identity/`：用户会话撤销事件。
  - `Organization/`：组织/角色层级关系变更事件。
  - `Tenancy/`：租户成员和租户状态变更事件。
- 保留已有事件语义并移动到领域目录：
  - `AuthorizationChangedDomainEvent`
  - `TenantMembershipChangedDomainEvent`
  - `HierarchyChangedDomainEvent`
- 新增事件契约：
  - `DataScopeChangedDomainEvent`：角色/用户等目标的数据权限范围变化，用于后续数据权限缓存失效与审计。
  - `FieldLevelSecurityChangedDomainEvent`：FLS 策略变化，用于后续字段脱敏缓存失效与审计。
  - `UserSessionRevokedDomainEvent`：会话撤销入口，用于后续 Token 级联撤销。
  - `TenantStatusChangedDomainEvent`：租户状态变化，用于后续租户访问缓存失效与通知。
- 保持二级物理目录的父级命名空间策略：所有事件仍使用 `XiHan.BasicApp.Saas.Domain.Events`，不引入 `Events.Authorization` 等细分命名空间。

设计约束：

- 事件只携带领域事实和审计上下文，不依赖数据库、缓存、HTTP、当前用户上下文或基础设施处理器。
- 普通租户隔离仍由会话上下文和底层仓储/SqlSugar 过滤器承担，事件中的 `TenantId` 是领域事实和事件上下文，不作为仓储查询入参。
- Token 级联撤销、缓存失效、审计落库留到 Application/EventHandlers 和 Infrastructure 阶段实现。

验证结果：

- `git diff --check`：通过。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Domain\.Events\." backend/src/modules/XiHan.BasicApp.Saas/Domain/Events -g "*.cs"`：0 个匹配。
- `rg -n "public sealed class .*DomainEvent|public abstract class SaasDomainEventBase|^namespace " backend/src/modules/XiHan.BasicApp.Saas/Domain/Events -g "*.cs"`：确认 1 个事件基类和 7 个事件类均使用父级命名空间。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`49` 个 `NU5104` 预发布依赖警告，`0` 个错误。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\XiHan.BasicApp.slnx --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`0` 个警告，`0` 个错误。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的 Domain 事件契约和本文档，不推送远端。

### 2026-04-26 D5 Domain 领域服务结构与数据范围裁决

本阶段继续 B4 领域层重构，规范 `Domain/DomainServices` 物理结构，并补齐数据范围合并的纯领域规则。范围限定为领域服务和值对象，不接入 Infrastructure，不生成查询表达式，不修改实体行为。

执行结果：

- 重组 `Domain/DomainServices` 物理目录：
  - `Authorization/`：权限裁决、数据范围裁决服务接口。
  - `Authorization/Implementations/`：权限裁决、数据范围裁决服务实现。
  - `Tenancy/`：租户访问服务接口。
  - `Tenancy/Implementations/`：租户访问服务实现。
- 保留已有 `IPermissionDecisionDomainService` / `PermissionDecisionDomainService` 和 `ITenantAccessDomainService` / `TenantAccessDomainService` 行为，仅移动文件位置。
- 新增 `DataScopeGrantSnapshot` 和 `DataScopeDecision`：
  - `DataScopeGrantSnapshot` 承载角色/用户等来源的数据范围事实、部门列表、生效周期和启用状态。
  - `DataScopeDecision` 表达全部数据、本人数据、直接部门、需包含子部门的部门集合。
- 新增 `IDataScopeDecisionDomainService` / `DataScopeDecisionDomainService`：
  - `All` 显式优先，不依赖 `DataPermissionScope` 枚举数值大小。
  - `DepartmentOnly` 使用调用方传入的当前用户有效部门集合。
  - `DepartmentAndChildren` 只标记需要展开的部门，子部门展开留给后续仓储/QueryService。
  - `Custom` 支持直接部门和包含子部门两种语义。
  - 无有效授权或没有可用部门时回落为 `SelfOnly`。
- 保持二级物理目录的父级命名空间策略：所有领域服务仍使用 `XiHan.BasicApp.Saas.Domain.DomainServices`。

设计约束：

- 数据范围裁决服务只处理内存快照，不依赖数据库、缓存、当前用户上下文或 HTTP 上下文。
- 普通租户隔离仍由会话上下文和底层仓储/SqlSugar 过滤器处理，领域服务不接收 `tenantId` 查询参数。
- 本阶段只定义合并规则，后续 Infrastructure/Application 阶段再把 `DataScopeDecision` 转换为具体查询条件和缓存失效逻辑。

验证结果：

- `git diff --check`：通过。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Domain\.DomainServices\." backend/src/modules/XiHan.BasicApp.Saas/Domain/DomainServices -g "*.cs"`：0 个匹配。
- `rg -n "tenantId" backend/src/modules/XiHan.BasicApp.Saas/Domain/DomainServices -g "*.cs"`：0 个匹配。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`49` 个 `NU5104` 预发布依赖警告，`0` 个错误。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\XiHan.BasicApp.slnx --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`0` 个警告，`0` 个错误。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的 Domain 领域服务和值对象调整以及本文档，不推送远端。

### 2026-04-26 D6 Domain 剩余实体仓储契约补齐

本阶段继续 B4 领域层重构，补齐 `Domain/Entities` 中剩余实体对应的 Domain 仓储契约。范围限定为接口契约与物理结构，不接入 Infrastructure 实现，不新增查询实现，不修改实体行为。

执行结果：

- 按实体领域补齐仓储接口目录：
  - `Audit/`：访问、API、审计、异常、登录、操作、权限变更日志。
  - `Authorization/`：约束规则项、字段级安全、权限委托、权限申请、用户数据范围。
  - `Configuration/`：配置、字典、字典项、迁移历史、版本。
  - `Files/`：文件元数据、文件存储配置。
  - `Identity/`：外部登录、密码历史、会话角色、用户安全、用户会话、用户统计。
  - `Messaging/`：邮件、通知、短信、用户通知。
  - `Navigation/`：菜单。
  - `OAuth/`：OAuth 应用、授权码、令牌。
  - `Tenancy/`：租户版本、租户版本权限。
  - `Workflow/`：审批、审批日志、任务、任务日志。
- 所有新增实体仓储均继承统一 SaaS 仓储基线：
  - 普通实体使用 `ISaasRepository<TEntity>`。
  - 聚合根实体使用 `ISaasAggregateRepository<TAggregateRoot>`。
- 继续保持二级物理目录的父级命名空间策略：所有仓储接口仍使用 `XiHan.BasicApp.Saas.Domain.Repositories`，不引入 `Repositories.Audit`、`Repositories.Identity` 等细分命名空间。
- 普通仓储契约不出现 `tenantId` 查询参数；租户条件仍由当前会话/租户上下文和底层仓储、SqlSugar 全局过滤器自动注入。

设计约束：

- 仓储接口只表达领域持久化入口，不做页面查询拼装、缓存策略或跨租户逃逸。
- 跨租户平台操作后续必须走明确的平台服务或基础设施逃逸接口，并记录审计，不混入普通实体仓储契约。
- 本阶段仅补齐契约覆盖面，Infrastructure 仓储实现留到下一层重建。

验证结果：

- 实体到仓储覆盖扫描：`Domain/Entities` 根目录实体均已有匹配的 `I<EntityWithoutSys>Repository`，无缺口。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Domain\.Repositories\." backend/src/modules/XiHan.BasicApp.Saas/Domain/Repositories -g "*.cs"`：0 个匹配。
- `rg -n "tenantId" backend/src/modules/XiHan.BasicApp.Saas/Domain/Repositories -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`49` 个 `NU5104` 预发布依赖警告，`0` 个错误。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\XiHan.BasicApp.slnx --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`0` 个警告，`0` 个错误。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态；`XiHan.Framework` 无改动。
- 本阶段只提交 BasicApp 的剩余实体仓储契约和本文档，不推送远端。

### 2026-04-26 I1 Infrastructure 核心仓储基线

本阶段进入第 5 层基础设施重构，先建立 SaaS 模块 `Infrastructure/Repositories` 基线，并落地核心身份、租户、组织、授权仓储实现。范围限定为 SqlSugar 仓储实现和稳定领域查询方法，不接入应用服务，不新增 Controller，不扩展页面查询。

执行结果：

- 新建 SaaS 仓储基类：
  - `SaasRepository<TEntity>`：继承 Framework `SqlSugarRepositoryBase<TEntity, long>`，实现 `ISaasRepository<TEntity>`。
  - `SaasAggregateRepository<TAggregateRoot>`：继承 Framework `SqlSugarAggregateRepository<TAggregateRoot, long>`，实现 `ISaasAggregateRepository<TAggregateRoot>`，复用聚合根领域事件入 UoW 机制。
- 新建核心仓储实现：
  - `Identity/`：`UserRepository`。
  - `Tenancy/`：`TenantRepository`、`TenantUserRepository`。
  - `Organization/`：`DepartmentRepository`、`DepartmentHierarchyRepository`、`UserDepartmentRepository`。
  - `Authorization/`：`RoleRepository`、`PermissionRepository`、`ResourceRepository`、`OperationRepository`、`UserRoleRepository`、`UserPermissionRepository`、`RolePermissionRepository`、`RoleHierarchyRepository`、`RoleDataScopeRepository`、`PermissionConditionRepository`、`ConstraintRuleRepository`。
- 所有实现保持父级命名空间策略：物理目录按领域分组，但命名空间统一为 `XiHan.BasicApp.Saas.Infrastructure.Repositories`。
- 所有普通租户业务查询继续依赖 Framework `SqlSugarReadOnlyRepository` 的 `CreateQueryable()`，租户过滤和软删除过滤由当前租户上下文与全局 QueryFilter 自动注入。
- 明确两个基础设施例外：
  - `TenantRepository` 覆盖 `CreateQueryable()` 使用 `CreateNoTenantQueryable()`，因为 `SysTenant` 是平台租户元数据，不能被当前业务租户过滤误隐藏。
  - `TenantUserRepository.GetActiveByUserIdAsync()` 使用 `CreateNoTenantQueryable()`，用于登录后/切换租户前查询用户可进入的租户成员关系；它不接收 `tenantId`，只以 `userId` 和成员状态作为边界。

设计约束：

- 本阶段不铺满所有 D6 新增仓储实现；剩余无特殊查询方法的实体仓储将在应用服务用例落地时按领域补齐，避免生成空实现噪音。
- 普通仓储实现不出现 `tenantId` 查询入参；跨租户查询仅保留在租户解析和租户成员入口这类明确基础设施边界。
- 平台级跨租户管理、审计落库、缓存失效和 Token 级联撤销仍留到后续 Infrastructure/Application 阶段。

验证结果：

- `rg -n "tenantId" backend/src/modules/XiHan.BasicApp.Saas/Infrastructure/Repositories -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Infrastructure\.Repositories\." backend/src/modules/XiHan.BasicApp.Saas/Infrastructure/Repositories -g "*.cs"`：0 个匹配。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`49` 个 `NU5104` 预发布依赖警告，`0` 个错误。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\XiHan.BasicApp.slnx --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`0` 个警告，`0` 个错误。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的核心 Infrastructure 仓储实现和本文档，不推送远端。

### 2026-04-29 I2 Infrastructure 剩余仓储实现补齐

本阶段继续第 5 层基础设施重构，将 D6 已补齐的剩余 Domain 仓储契约落到 `Infrastructure/Repositories` 实现，并同步 Framework F1 分表仓储边界。范围限定为仓储实现、分表仓储抽象和租户参数命名清理，不接入应用服务，不新增 Controller。

执行结果：

- 新增 SaaS 分表仓储基线：
  - `ISaasSplitRepository<TEntity>`：继承 Framework `ISplitRepositoryBase<TEntity>`，仅用于 `ISplitTableEntity` 分表实体。
  - `SaasSplitRepository<TEntity>`：继承 Framework `SqlSugarSplitRepository<TEntity>`，实现 SaaS 分表仓储接口并参与作用域注入。
- 将分表日志实体仓储接口从普通 `ISaasRepository<TEntity>` 切换为 `ISaasSplitRepository<TEntity>`：
  - `SysAccessLog`、`SysApiLog`、`SysAuditLog`、`SysExceptionLog`、`SysLoginLog`、`SysOperationLog`、`SysPermissionChangeLog`。
  - `SysReviewLog`、`SysTaskLog`。
- 补齐剩余仓储实现：
  - `Audit/`：访问、API、审计、异常、登录、操作、权限变更日志分表仓储。
  - `Authorization/`：约束规则项、字段级安全、权限委托、权限申请、用户数据范围仓储。
  - `Configuration/`：配置、字典、字典项、迁移历史、版本仓储。
  - `Files/`：文件、文件存储仓储。
  - `Identity/`：外部登录、密码历史、会话角色、用户安全、用户会话、用户统计仓储。
  - `Messaging/`：邮件、通知、短信、用户通知仓储。
  - `Navigation/`：菜单仓储。
  - `OAuth/`：OAuth 应用、授权码、令牌仓储。
  - `Tenancy/`：租户版本、租户版本权限仓储。
  - `Workflow/`：审批、审批日志、任务、任务日志仓储。
- 清理 `TenantRepository.ExistsTenantCodeAsync` 的排除参数命名：`excludeTenantId` 改为 `excludeId`，避免把实体主键排除语义误读为当前租户上下文入参。
- 所有新增 Infrastructure 实现继续保持父级命名空间策略：物理目录按领域分组，命名空间统一为 `XiHan.BasicApp.Saas.Infrastructure.Repositories`。

设计约束：

- 分表实体禁止再通过普通 SaaS 仓储实现承载，避免绕过 Framework `SqlSugarReadOnlyRepository` 的分表实体防呆和分表路由规则。
- 普通租户业务仓储仍不接收 `tenantId` 查询入参；租户过滤、软删除过滤、审计字段和主键注入继续由 Framework SqlSugar AOP、全局过滤器和当前租户上下文处理。
- 本阶段只补齐持久化入口，不做页面查询拼装、缓存失效、事件处理或跨租户平台管理逻辑。

验证结果：

- Domain 仓储接口到 Infrastructure 实现覆盖扫描：所有非抽象 `I*Repository` 均已有对应 `*Repository` 实现。
- `rg -n "tenantId|excludeTenantId" backend/src/modules/XiHan.BasicApp.Saas/Domain/Repositories backend/src/modules/XiHan.BasicApp.Saas/Infrastructure/Repositories -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.(Domain|Infrastructure)\.Repositories\." backend/src/modules/XiHan.BasicApp.Saas/Domain/Repositories backend/src/modules/XiHan.BasicApp.Saas/Infrastructure/Repositories -g "*.cs"`：0 个匹配。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "ISaasRepository<Sys(AccessLog|ApiLog|AuditLog|ExceptionLog|LoginLog|OperationLog|PermissionChangeLog|ReviewLog|TaskLog)>|SaasRepository<Sys(AccessLog|ApiLog|AuditLog|ExceptionLog|LoginLog|OperationLog|PermissionChangeLog|ReviewLog|TaskLog)>" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 NuGet 源/预发布依赖警告，`0` 个错误。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无源码改动；构建期间生成的本地 `nupkgs` 输出未进入 git 状态。
- 本阶段只提交 BasicApp 的仓储实现、分表仓储抽象、租户排除参数命名清理和本文档，不推送远端。

### 2026-04-29 A1 Application 基线与租户查询入口

本阶段进入第 6 层应用服务重构，先建立 SaaS 模块 `Application` 基线，并落地一个当前用户租户查询入口。范围限定为应用服务基类、查询服务契约、租户切换 DTO 和租户查询服务，不新增 Controller，不接入前端，不做写操作 AppService。

执行结果：

- 新建 `Application/` 物理结构：
  - `Abstractions/`：SaaS 应用服务基类。
  - `Contracts/Tenancy/`：租户查询服务契约。
  - `Dtos/Tenancy/`：租户切换读模型 DTO。
  - `QueryServices/Tenancy/`：租户查询服务实现。
- 新增 `SaasApplicationService`：
  - 继承 Framework `ApplicationServiceBase`。
  - 默认标记 `[Authorize]`。
  - 默认标记 `[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务")]`。
- 新增 `ITenantQueryService` / `TenantQueryService`：
  - 暴露 `GetMyAvailableTenantsAsync()`，用于获取当前登录用户可进入的租户列表。
  - 从 `ICurrentUser.UserId` 获取当前用户，不接收 `tenantId` / `userId` 查询入参。
  - 使用 `ITenantUserRepository.GetActiveByUserIdAsync()` 获取有效成员关系；该仓储方法内部通过明确基础设施边界查询登录后可进入租户。
  - 使用 `ITenantRepository.GetByIdsAsync()` 读取租户元数据，过滤 `TenantStatus.Normal`，并按租户排序和名称输出。
- 新增 `TenantSwitcherDto`：
  - 仅返回租户切换所需字段、成员类型和成员有效期。
  - 不暴露连接字符串、联系人电话、邮箱等租户敏感配置字段。

设计约束：

- 本阶段的租户标识只作为响应数据返回，不作为鉴权或查询入参；前端后续切换租户时仍必须由后端校验成员身份。
- QueryService 只做读模型组装，不承担写操作、事务编排、权限授予或缓存失效。
- 后续写操作 AppService 必须逐方法补充 `[PermissionAuthorize("saas:resource:action")]`，权限码同步进入 Seeder。

验证结果：

- `rg -n "tenantId|TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "\[Authorize\]|\[AllowAnonymous\]|public sealed class .*Service|DynamicApi" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：`TenantQueryService` 和 `SaasApplicationService` 均有授权与 DynamicApi 标记。
- `git diff --check`：通过。
- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 NuGet 源/预发布依赖警告，`0` 个错误。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的 Application 基线、租户查询入口和本文档，不推送远端。

### 2026-04-29 A2 Application 权限码基线与租户查询授权

本阶段继续第 6 层应用服务重构，补齐 SaaS 应用层权限码基线，并将 A1 的租户查询入口纳入后端方法级权限控制。范围限定为权限码常量、租户查询方法授权、SaaS 权限种子数据和模块注册，不新增 Controller，不修改 Framework。

执行结果：

- 新增 `Domain/Permissions/SaasPermissionCodes`：
  - 统一定义 SaaS 模块权限码，当前落地 `saas:tenant:read`。
  - 应用层特性和种子数据共用同一常量，避免散落硬编码权限字符串。
- 更新 `TenantQueryService.GetMyAvailableTenantsAsync()`：
  - 保留 `[Authorize]` 登录态约束。
  - 新增 `[PermissionAuthorize(SaasPermissionCodes.Tenant.Read)]` 方法级权限约束。
  - 仍只从 `ICurrentUser` 获取当前用户上下文，不接收 `tenantId` 入参。
- 新增 SaaS 权限种子数据：
  - `SaasPermissionSeeder` 初始化功能权限 `saas:tenant:read`。
  - 权限类型为 `Functional`，不绑定 `ResourceId` / `OperationId`。
  - 插入时使用当前租户上下文切换到平台上下文，让实体保持平台租户默认标识，不在业务对象中手动赋值 `TenantId`。
- 新增 `Infrastructure/Extensions/AddSaasDataSeeders()` 并在 `XiHanBasicAppRbacModule.ConfigureServices()` 注册。

设计约束：

- 权限码遵循 `{module}:{resource}:action`，后续 AppService / QueryService 必须继续复用 `SaasPermissionCodes`。
- 权限种子只登记权限定义，不在本阶段自动授予角色；角色授权、菜单绑定、缓存失效和负权限测试留到授权应用服务阶段集中处理。
- 本阶段未引入 `tenantId` 查询参数；租户隔离仍依赖当前会话、Framework 全局过滤器和仓储逃逸边界。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 NuGet 源/预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的权限码基线、租户查询授权、SaaS 权限种子注册和本文档，不推送远端。

### 2026-04-29 A3 Application 租户管理读模型

本阶段继续第 6 层应用服务重构，在 A1/A2 的租户查询服务基础上补齐租户管理读侧入口。范围限定为租户分页查询 DTO、列表/详情响应 DTO、查询契约和 QueryService 实现，不新增 Controller，不做租户写操作，不修改 Framework。

执行结果：

- 新增租户读侧 DTO：
  - `TenantPageQueryDto`：提供关键字、租户状态、配置状态、版本/套餐主键、到期时间范围等白名单查询条件。
  - `TenantListItemDto`：租户列表安全读模型。
  - `TenantDetailDto`：租户详情安全读模型。
- 扩展 `ITenantQueryService`：
  - `GetTenantPageAsync()`：租户分页列表。
  - `GetTenantDetailAsync()`：租户详情。
- 扩展 `TenantQueryService`：
  - 新增方法均使用 `[PermissionAuthorize(SaasPermissionCodes.Tenant.Read)]`。
  - 查询条件由服务端构建为 `BasicAppPRDto`，只写入白名单字段；不直接透传前端任意过滤字段。
  - 复用 `ITenantRepository` 的平台租户元数据读取边界，仍不接收 `tenantId` 作为会话/鉴权上下文。
  - 默认按 `Sort` 升序、`CreatedTime` 降序排序。
- 响应 DTO 明确不返回：
  - `ConnectionString`、`IsConnectionStringEncrypted`。
  - `DatabaseType`、`DatabaseSchema`。
  - `ContactPhone`、`ContactEmail` 等联系方式字段。

设计约束：

- 租户主键参数命名为 `id`，表示要查看的实体主键，不作为当前租户上下文来源。
- 本阶段只做安全读模型；联系方式、数据库配置、跨租户运维和敏感字段展示需在后续 FLS/审计阶段统一接入。
- 权限复用 `saas:tenant:read`，未新增权限码和种子数据项。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 NuGet 源/预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的租户管理读模型、查询契约、QueryService 实现和本文档，不推送远端。

### 2026-04-30 A4 Application 租户命令服务基线

本阶段继续第 6 层应用服务重构，补齐租户管理写侧最小闭环。范围限定为租户基础资料创建、基础资料更新、租户生命周期状态变更、方法级权限和权限种子数据，不处理连接字符串、数据库 Schema、数据库类型、联系人电话/邮箱等敏感配置，不新增 Controller，不修改 Framework。

执行结果：

- 新增租户命令 DTO：
  - `TenantCreateDto`：只接收租户编码、名称、简称、Logo、域名、套餐、隔离模式、到期时间、用户/存储限制、排序和备注。
  - `TenantUpdateDto`：只更新租户基础资料；租户编码保持不可变。
  - `TenantStatusUpdateDto`：只接收实体主键、目标状态和状态变更原因。
- 新增 `ITenantAppService` / `TenantAppService`：
  - 暴露 `CreateTenantAsync()`、`UpdateTenantAsync()`、`UpdateTenantStatusAsync()` 三个写入口。
  - 状态入口使用 `Update*` 命名，匹配 DynamicApi 的 PUT 动词和复杂命令参数识别约定。
  - 使用 `[UnitOfWork(true)]` 作为写操作事务边界，状态变更产生领域事件时由聚合仓储登记到当前工作单元。
  - 使用 `SaasPermissionCodes.Tenant.Create/Update/Status` 做方法级授权，不写硬编码权限字符串。
  - 不接收 `tenantId` 作为查询、鉴权或会话上下文；租户元数据仍通过 `ITenantRepository` 的平台读取边界处理。
- 新增 `SysTenant.Aggregate`：
  - `ChangeStatus()` 封装租户状态变更。
  - 状态实际变化时发布 `TenantStatusChangedDomainEvent`。
  - 事件租户标识取聚合自身 `TenantId`，受影响租户主键取 `BasicId`，不由应用层手动传入租户上下文。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:tenant:create`、`saas:tenant:update`、`saas:tenant:status`。
  - 种子插入前先切换平台租户上下文，再解析 `DbClient`，保证查询和插入都处于平台权限定义上下文。

设计约束：

- 本阶段只建立租户基础资料命令服务；连接字符串、数据库类型、Schema、联系人电话/邮箱、跨租户运维审计和 FLS 敏感字段展示留到后续专门阶段。
- 租户编码创建后不可变，避免后续成员关系、授权、菜单、审计日志等引用语义漂移。
- 写服务仅做用例编排、白名单字段更新、权限入口和事务边界；租户状态变更归入聚合行为。
- 权限种子只登记权限定义，不自动授予角色；角色授权、菜单按钮绑定、缓存失效和负权限测试留到授权应用服务阶段集中处理。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`102` 个既有 NuGet 漏洞源/预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的租户命令服务、命令 DTO、权限码/种子、租户聚合状态行为和本文档，不推送远端。

### 2026-04-30 A5 Application 租户映射基线

本阶段继续第 6 层应用服务重构，落实 `Application/Mappers` 映射集中化要求。范围限定为租户读写服务中重复 DTO 投影逻辑的抽取，不修改 DTO 字段、不修改权限、不修改 DynamicApi 路由、不修改仓储和 Framework。

执行结果：

- 新增 `Application/Mappers/Tenancy/TenantApplicationMapper`：
  - `ToSwitcherDto()`：集中映射租户切换读模型。
  - `ToListItemDto()`：集中映射租户列表读模型。
  - `ToDetailDto()`：集中映射租户详情读模型。
- 更新 `TenantQueryService`：
  - 删除服务内 `MapTenantSwitcherDto()`、`MapTenantListItemDto()`、`MapTenantDetailDto()` 私有映射方法。
  - 查询服务只保留读模型查询、分页条件构建和排序编排。
- 更新 `TenantAppService`：
  - 删除服务内重复的租户详情映射方法。
  - 写服务只保留命令校验、事务边界、权限入口和聚合持久化编排。

设计约束：

- 本阶段先采用显式静态映射器，不引入新的第三方映射依赖；后续如果全模块统一接入 Mapster，可在 `Application/Mappers` 内集中替换，不影响 AppService/QueryService。
- 映射器只做安全 DTO 投影，不放查询条件、权限判断、租户上下文切换或缓存失效。
- 仍不暴露 `ConnectionString`、`IsConnectionStringEncrypted`、`DatabaseType`、`DatabaseSchema`、`ContactPhone`、`ContactEmail` 等敏感字段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 NuGet 漏洞源/预发布依赖警告，`0` 个错误。
- `rg -n "private .*Map|MapTenant.*Dto|new Tenant(ListItem|Detail|Switcher)Dto" backend/src/modules/XiHan.BasicApp.Saas/Application/AppServices backend/src/modules/XiHan.BasicApp.Saas/Application/QueryServices -g "*.cs"`：0 个匹配。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的租户应用映射器、租户服务映射调用调整和本文档，不推送远端。

### 2026-04-30 A6 Application 租户版本读模型

本阶段继续第 6 层应用服务重构，补齐租户版本/套餐的读侧入口。范围限定为租户版本分页、详情、启用版本列表、读侧 DTO、映射器和查看权限，不做租户版本写操作，不修改套餐权限绑定，不新增 Controller，不修改 Framework。

执行结果：

- 新增租户版本读侧 DTO：
  - `TenantEditionPageQueryDto`：提供关键字、状态、是否免费、是否默认等白名单查询条件。
  - `TenantEditionListItemDto`：租户版本列表安全读模型。
  - `TenantEditionDetailDto`：租户版本详情安全读模型。
- 新增 `ITenantEditionQueryService` / `TenantEditionQueryService`：
  - `GetTenantEditionPageAsync()`：租户版本分页列表。
  - `GetTenantEditionDetailAsync()`：租户版本详情。
  - `GetEnabledTenantEditionsAsync()`：创建/更新租户时可选的已启用版本列表。
  - 查询条件由服务端构建为 `BasicAppPRDto`，不直接透传前端任意过滤字段。
- 新增 `TenantEditionApplicationMapper`：
  - 集中处理租户版本列表和详情映射。
  - 不在 QueryService 中直接 new DTO。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:tenant-edition:read`。
  - 权限种子登记为功能权限，不绑定资源/操作。

设计约束：

- 租户版本是平台级实体，读侧不接收 `tenantId` 作为查询或鉴权上下文。
- 本阶段只做读模型；创建/更新/删除版本、默认版本互斥、价格调整、版本权限绑定和租户引用校验留到后续命令服务阶段。
- 租户版本 DTO 不包含导航属性 `EditionPermissions`、`Permissions`、`Tenants`，避免懒加载大对象和跨领域数据外泄。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 NuGet 漏洞源/预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的租户版本读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A7 Application 租户版本命令服务基线

本阶段继续第 6 层应用服务重构，补齐租户版本/套餐的写侧最小闭环。范围限定为租户版本创建、基础资料更新、上下架状态更新、设置默认版本、命令 DTO、权限码和权限种子，不处理版本权限绑定、不删除版本、不修改租户订阅引用，不新增 Controller，不修改 Framework。

执行结果：

- 新增租户版本命令 DTO：
  - `TenantEditionCreateDto`：创建版本编码、名称、价格、计费周期、配额、免费/默认标记、状态、排序和备注。
  - `TenantEditionUpdateDto`：更新版本基础资料；版本编码保持不可变。
  - `TenantEditionStatusUpdateDto`：更新启用/禁用状态。
  - `TenantEditionDefaultUpdateDto`：设置默认版本。
- 新增 `ITenantEditionAppService` / `TenantEditionAppService`：
  - `CreateTenantEditionAsync()`：创建租户版本，校验版本编码唯一。
  - `UpdateTenantEditionAsync()`：更新租户版本基础资料，不允许直接取消当前默认版本。
  - `UpdateTenantEditionStatusAsync()`：更新版本上下架状态，不允许禁用默认版本。
  - `UpdateDefaultTenantEditionAsync()`：在事务内清理其他默认版本，再设置目标版本为默认版本。
  - 写方法均使用 `[UnitOfWork(true)]` 与方法级权限控制。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:tenant-edition:create`、`saas:tenant-edition:update`、`saas:tenant-edition:status`、`saas:tenant-edition:default`。
  - 命令权限标记为需审计功能权限。

设计约束：

- 租户版本是平台级实体，命令 DTO 不接收 `tenantId`，不由前端决定平台/租户上下文。
- 默认版本必须启用；禁用默认版本前必须先设置其他启用版本为默认。
- 版本编码创建后不可变，避免租户订阅、权限绑定和审计日志引用语义漂移。
- 本阶段不做删除：删除前必须校验无租户引用、非默认版本、权限绑定处理和审计记录，后续单独实现。
- 本阶段不做版本权限绑定：`SysTenantEditionPermission` 涉及权限缓存失效和订阅功能门控，后续单独实现。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 NuGet 漏洞源/预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的租户版本命令服务、命令 DTO、权限码/种子和本文档，不推送远端。

### 2026-04-30 A8 Application 租户版本权限绑定读模型

本阶段继续第 6 层应用服务重构，补齐租户版本权限绑定的读侧入口。范围限定为 `SysTenantEditionPermission` 列表和详情读模型、查询契约、QueryService、显式映射器和查看权限，不做绑定写操作，不处理缓存失效，不新增 Controller，不修改 Framework。

执行结果：

- 新增租户版本权限绑定读侧 DTO：
  - `TenantEditionPermissionListItemDto`：租户版本权限绑定列表安全读模型。
  - `TenantEditionPermissionDetailDto`：租户版本权限绑定详情安全读模型。
- 新增 `ITenantEditionPermissionQueryService` / `TenantEditionPermissionQueryService`：
  - `GetTenantEditionPermissionsAsync()`：按租户版本主键读取权限绑定列表，可选择仅返回有效绑定。
  - `GetTenantEditionPermissionDetailAsync()`：按绑定主键读取详情。
  - 读取列表前校验租户版本存在，避免暴露无效版本绑定查询语义。
- 新增 `TenantEditionPermissionApplicationMapper`：
  - 显式投影绑定实体和权限定义字段。
  - 不依赖导航属性，不把实体对象直接暴露到 DTO。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:tenant-edition-permission:read`。
  - 权限种子登记为功能权限，不绑定资源/操作。

设计约束：

- 本阶段只做读模型；授权、撤销、批量同步、状态调整、权限缓存失效和审计事件留到后续命令服务阶段。
- QueryService 不接收 `tenantId`，租户上下文仍由当前会话、Framework 全局过滤器和仓储边界处理。
- DTO 不暴露 `SysTenantEditionPermission` / `SysPermission` 导航实体，避免懒加载大对象和权限定义外泄。
- 权限定义通过 `IPermissionRepository.GetByIdsAsync()` 显式批量读取，避免在映射器或 DTO 中引入查询职责。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`102` 个既有 `NU1900` 漏洞源连接警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 存在本阶段未提交改动，`XiHan.Framework` git 状态干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的租户版本权限读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A9 Application 租户版本权限绑定命令服务基线

本阶段继续第 6 层应用服务重构，补齐租户版本权限绑定的写侧最小闭环。范围限定为授权、状态更新、撤销、命令 DTO、命令契约、AppService、权限码和权限种子，不新增 Controller，不修改 Framework，不处理租户订阅权限缓存和审计事件落库。

执行结果：

- 新增租户版本权限命令 DTO：
  - `TenantEditionPermissionGrantDto`：接收租户版本主键、权限主键和备注。
  - `TenantEditionPermissionStatusUpdateDto`：接收绑定主键、绑定状态和备注。
- 新增 `ITenantEditionPermissionAppService` / `TenantEditionPermissionAppService`：
  - `CreateTenantEditionPermissionAsync()`：授予租户版本权限，使用 `Create*` 命名确保 DynamicApi 映射为 POST。
  - `UpdateTenantEditionPermissionStatusAsync()`：更新绑定状态，重新启用时校验权限仍可被版本门控。
  - `DeleteTenantEditionPermissionAsync()`：撤销租户版本权限绑定，按实体设计走硬删。
  - 写方法均使用 `[UnitOfWork(true)]` 与方法级权限控制。
- 授权约束：
  - 校验租户版本存在。
  - 校验权限存在、`IsGlobal = true`、`Status = Enabled`。
  - 校验 `EditionId + PermissionId` 唯一，重复授权直接拒绝。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:tenant-edition-permission:grant`。
  - 新增 `saas:tenant-edition-permission:update`。
  - 新增 `saas:tenant-edition-permission:revoke`。
  - 写权限种子标记为需审计功能权限。

设计约束：

- 命令 DTO 不接收 `tenantId`，租户上下文仍由当前会话、Framework 全局过滤器和仓储边界处理。
- 授权入口方法名使用 `Create*` 对齐 DynamicApi HTTP 方法推断；权限码仍保留业务动作 `grant`。
- 本阶段只维护版本权限白名单本身；租户已分配角色/用户权限的降级冲突检查、权限缓存失效、审计日志和事件处理器留到后续授权闭环阶段。
- 撤销以绑定主键 `id` 表达，不把租户上下文或权限编码作为越权判断参数。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`102` 个既有 `NU1900` 漏洞源连接警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"|GrantTenantEditionPermission" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的租户版本权限命令服务、命令 DTO、权限码/种子和本文档，不推送远端。

### 2026-04-30 A10 Application 租户成员读模型

本阶段继续第 6 层应用服务重构，补齐当前租户上下文内的租户成员读侧入口。范围限定为成员分页、详情、读侧 DTO、查询契约、QueryService、显式映射器和查看权限，不做成员邀请/撤销/状态写操作，不跨租户联查用户主表资料，不新增 Controller，不修改 Framework。

执行结果：

- 新增租户成员读侧 DTO：
  - `TenantMemberPageQueryDto`：提供关键字、用户主键、成员类型、邀请状态、成员状态、失效时间范围等白名单查询条件。
  - `TenantMemberListItemDto`：租户成员列表安全读模型。
  - `TenantMemberDetailDto`：租户成员详情安全读模型。
- 新增 `ITenantMemberQueryService` / `TenantMemberQueryService`：
  - `GetTenantMemberPageAsync()`：当前租户成员分页列表。
  - `GetTenantMemberDetailAsync()`：当前租户成员详情。
  - 查询条件由服务端构建为 `BasicAppPRDto`，不直接透传前端任意过滤字段。
- 新增 `TenantMemberApplicationMapper`：
  - 集中映射成员关系字段。
  - 不依赖 `SysTenantUser.User` 导航属性，不投影 `SysUser` 主表资料。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:tenant-member:read`。
  - 权限种子登记为功能权限，不绑定资源/操作。

设计约束：

- 租户成员分页和详情不接收 `tenantId`，仅使用当前会话租户上下文与 Framework 全局过滤器。
- 本阶段只返回成员关系安全字段：`UserId`、成员类型、邀请状态、生效/失效时间、租户内显示名、成员状态等。
- 本阶段不返回用户主表的 `UserName`、`RealName`、`NickName`、`Email`、`Phone`，避免为了列表展示绕开用户主归属租户过滤；跨租户安全用户投影留到后续 FLS/审计阶段。
- 成员邀请、接受/拒绝、撤销、暂停和会话/Token 级联撤销留到租户成员命令服务阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`102` 个既有 `NU1900` 漏洞源连接警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "UserName|RealName|NickName|Email|Phone" backend/src/modules/XiHan.BasicApp.Saas/Application/Dtos/Tenancy backend/src/modules/XiHan.BasicApp.Saas/Application/Mappers/Tenancy/TenantMemberApplicationMapper.cs -g "TenantMember*.cs"`：除文件头作者邮箱注释外，0 个成员 DTO/映射字段匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的租户成员读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A11 Application 租户成员命令服务基线

本阶段继续第 6 层应用服务重构，补齐已有租户成员关系的写侧最小闭环。范围限定为成员资料/有效期更新、成员启停状态、邀请生命周期状态、撤销成员身份、命令 DTO、命令契约、AppService、权限码和权限种子，不做新增成员/跨租户邀请用户，不新增 Controller，不修改 Framework。

执行结果：

- 新增租户成员命令 DTO：
  - `TenantMemberUpdateDto`：更新成员类型、生效/失效时间、租户内显示名、邀请备注和备注。
  - `TenantMemberStatusUpdateDto`：更新成员有效性状态。
  - `TenantMemberInviteStatusUpdateDto`：更新邀请生命周期状态。
- 新增 `ITenantMemberAppService` / `TenantMemberAppService`：
  - `UpdateTenantMemberAsync()`：更新已有成员关系资料和有效期。
  - `UpdateTenantMemberStatusAsync()`：启用/停用已有成员关系。
  - `UpdateTenantMemberInviteStatusAsync()`：更新 Pending/Accepted/Rejected/Revoked/Expired 生命周期。
  - `DeleteTenantMemberAsync()`：按 DELETE 语义撤销成员身份，实际写入 `InviteStatus = Revoked` 和 `Status = Invalid`，不硬删成员关系。
  - 写方法均使用 `[UnitOfWork(true)]` 与方法级权限控制。
- 成员保护约束：
  - 租户所有者成员类型不能直接变更。
  - 租户所有者不能直接停用、撤销或置为过期。
  - 租户成员服务不能分配 `PlatformAdmin` 身份。
  - 成员失效时间必须晚于生效时间。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:tenant-member:update`。
  - 新增 `saas:tenant-member:status`。
  - 新增 `saas:tenant-member:invite-status`。
  - 新增 `saas:tenant-member:revoke`。
  - 写权限种子标记为需审计功能权限。

设计约束：

- 命令 DTO 不接收 `tenantId`，已有成员关系的读取和更新依赖当前会话租户上下文与 Framework 全局过滤器。
- 本阶段不做新增成员/邀请用户，因为跨租户邀请需要安全用户投影、FLS、审计和外部用户查找流程，后续单独实现。
- 撤销成员身份保留审计事实，不调用仓储硬删除。
- Session/Token 级联撤销、角色/部门/用户权限清理、审计日志和领域事件处理器留到租户成员授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的租户成员命令服务、命令 DTO、权限码/种子和本文档，不推送远端。

### 2026-04-30 A12 Application 权限定义读模型

本阶段继续第 6 层应用服务重构，从授权领域补齐权限定义的读侧入口。范围限定为权限分页、详情、全局权限选择器、读侧 DTO、查询契约、QueryService、显式映射器和查看权限，不做权限创建/更新/删除，不新增 Controller，不修改 Framework。

执行结果：

- 新增权限读侧 DTO：
  - `PermissionPageQueryDto`：提供关键字、模块编码、权限类型、资源、操作、全局标记、审计标记和状态等白名单查询条件。
  - `PermissionListItemDto`：权限列表安全读模型，显式展开资源/操作展示字段。
  - `PermissionDetailDto`：权限详情安全读模型，包含审计、优先级、标签、备注和基础审计字段。
  - `PermissionSelectQueryDto` / `PermissionSelectItemDto`：用于租户版本授权等场景的已启用全局权限选择项。
- 新增 `IPermissionQueryService` / `PermissionQueryService`：
  - `GetPermissionPageAsync()`：权限分页列表。
  - `GetPermissionDetailAsync()`：权限详情。
  - `GetAvailableGlobalPermissionsAsync()`：返回已启用全局权限选择项，默认最多 100 条，最大 500 条。
  - 查询条件由服务端构建为 `BasicAppPRDto`，不直接透传前端任意过滤字段。
- 新增 `PermissionApplicationMapper`：
  - 集中映射权限、资源和操作字段。
  - 不依赖 `SysPermission.Resource` / `Operation` 导航属性，不把实体对象直接暴露到 DTO。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:permission:read`。
  - 权限种子登记为功能权限，不绑定资源/操作。

设计约束：

- 权限读模型不接收 `tenantId`，全局权限和当前租户权限的合并仍由当前会话、Framework 全局过滤器和仓储边界处理。
- 选择器只返回已启用全局权限，用于租户版本权限白名单等平台门控场景，不暴露跨租户写入口。
- DTO 不暴露权限实体导航集合、角色/用户授权关系、菜单绑定集合或任何连接/密钥类敏感字段。
- 本阶段不修正 `SysPermission` 的命令侧验证与生命周期规则；权限创建、资源/操作必填规则、缓存失效和审计事件留到后续命令服务阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的权限定义读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A13 Application 资源定义读模型

本阶段继续第 6 层应用服务重构，补齐授权资源定义的读侧入口。范围限定为资源分页、详情、全局资源选择器、读侧 DTO、查询契约、QueryService、显式映射器和查看权限，不做资源创建/更新/删除，不新增 Controller，不修改 Framework。

执行结果：

- 新增资源读侧 DTO：
  - `ResourcePageQueryDto`：提供关键字、资源类型、访问级别、全局标记和状态等白名单查询条件。
  - `ResourceListItemDto`：资源列表安全读模型。
  - `ResourceDetailDto`：资源详情安全读模型，包含资源元数据、备注和基础审计字段。
  - `ResourceSelectQueryDto` / `ResourceSelectItemDto`：用于权限定义等场景的已启用全局资源选择项。
- 新增 `IResourceQueryService` / `ResourceQueryService`：
  - `GetResourcePageAsync()`：资源分页列表。
  - `GetResourceDetailAsync()`：资源详情。
  - `GetAvailableGlobalResourcesAsync()`：返回已启用全局资源选择项，默认最多 100 条，最大 500 条。
  - 查询条件由服务端构建为 `BasicAppPRDto`，不直接透传前端任意过滤字段。
- 新增 `ResourceApplicationMapper`：
  - 集中映射资源定义字段。
  - 不依赖 `SysResource.Permissions` 导航集合，不把实体对象直接暴露到 DTO。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:resource:read`。
  - 权限种子登记为功能权限，不绑定资源/操作。

设计约束：

- 资源读模型不接收 `tenantId`，全局资源和当前租户资源的合并仍由当前会话、Framework 全局过滤器和仓储边界处理。
- 选择器只返回已启用全局资源，用于权限资源绑定等平台模板场景，不暴露跨租户写入口。
- DTO 不暴露权限导航集合、字段级安全策略集合或任何连接/密钥类敏感字段。
- 本阶段不实现资源注册、删除前引用校验、权限缓存失效和审计事件；这些留到资源命令服务和授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的资源定义读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A14 Application 操作定义读模型

本阶段继续第 6 层应用服务重构，补齐授权操作定义的读侧入口。范围限定为操作分页、详情、全局操作选择器、读侧 DTO、查询契约、QueryService、显式映射器和查看权限，不做操作创建/更新/删除，不新增 Controller，不修改 Framework。

执行结果：

- 新增操作读侧 DTO：
  - `OperationPageQueryDto`：提供关键字、操作类型、操作分类、HTTP 方法、危险标记、审计标记、全局标记和状态等白名单查询条件。
  - `OperationListItemDto`：操作列表安全读模型。
  - `OperationDetailDto`：操作详情安全读模型，包含图标、颜色、备注和基础审计字段。
  - `OperationSelectQueryDto` / `OperationSelectItemDto`：用于权限定义等场景的已启用全局操作选择项。
- 新增 `IOperationQueryService` / `OperationQueryService`：
  - `GetOperationPageAsync()`：操作分页列表。
  - `GetOperationDetailAsync()`：操作详情。
  - `GetAvailableGlobalOperationsAsync()`：返回已启用全局操作选择项，默认最多 100 条，最大 500 条。
  - 查询条件由服务端构建为 `BasicAppPRDto`，不直接透传前端任意过滤字段。
- 新增 `OperationApplicationMapper`：
  - 集中映射操作定义字段。
  - 不依赖 `SysOperation.Permissions` 导航集合，不把实体对象直接暴露到 DTO。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:operation:read`。
  - 权限种子登记为功能权限，不绑定资源/操作。

设计约束：

- 操作读模型不接收 `tenantId`，全局操作和当前租户操作的合并仍由当前会话、Framework 全局过滤器和仓储边界处理。
- 选择器只返回已启用全局操作，用于权限资源操作组合等平台模板场景，不暴露跨租户写入口。
- DTO 不暴露权限导航集合、授权关系或任何连接/密钥类敏感字段。
- 本阶段不实现操作模板生成、删除前引用校验、权限缓存失效和审计事件；这些留到操作命令服务和授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的操作定义读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A15 Application 角色定义读模型

本阶段继续第 6 层应用服务重构，补齐授权角色定义的读侧入口。范围限定为角色分页、详情、已启用角色选择器、读侧 DTO、查询契约、QueryService、显式映射器和查看权限，不做角色创建/更新/删除，不做角色权限绑定，不新增 Controller，不修改 Framework。

执行结果：

- 新增角色读侧 DTO：
  - `RolePageQueryDto`：提供关键字、角色类型、数据权限范围、全局标记和状态等白名单查询条件。
  - `RoleListItemDto`：角色列表安全读模型。
  - `RoleDetailDto`：角色详情安全读模型，包含数据范围、最大成员数、备注和基础审计字段。
  - `RoleSelectQueryDto` / `RoleSelectItemDto`：用于用户授权、成员角色分配等场景的已启用角色选择项。
- 新增 `IRoleQueryService` / `RoleQueryService`：
  - `GetRolePageAsync()`：角色分页列表。
  - `GetRoleDetailAsync()`：角色详情。
  - `GetEnabledRolesAsync()`：返回已启用角色选择项，默认最多 100 条，最大 500 条。
  - 查询条件由服务端构建为 `BasicAppPRDto`，不直接透传前端任意过滤字段。
- 新增 `RoleApplicationMapper`：
  - 集中映射角色定义字段。
  - 不依赖 `SysRole.UserRoles`、`RolePermissions`、`Users`、`Permissions`、`DataScopes`、层级关系和会话角色导航集合。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:role:read`。
  - 权限种子登记为功能权限，不绑定资源/操作。

设计约束：

- 角色读模型不接收 `tenantId`，全局角色和当前租户角色的合并仍由当前会话、Framework 全局过滤器和仓储边界处理。
- 选择器只返回已启用角色，不承担用户角色分配、角色继承展开或权限合并计算。
- DTO 不暴露用户、权限、数据范围明细、角色层级和会话授权导航集合，避免一次读模型承载过多授权拼装。
- 角色权限绑定、角色继承、数据范围明细、缓存失效和审计事件留到后续授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的角色定义读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A16 Application 角色权限绑定读模型

本阶段继续第 6 层应用服务重构，补齐角色权限绑定的读侧入口。范围限定为角色下权限绑定列表、绑定详情、读侧 DTO、查询契约、QueryService、显式映射器和查看权限，不做角色授权/撤销/状态写操作，不做角色继承权限展开，不新增 Controller，不修改 Framework。

执行结果：

- 新增角色权限读侧 DTO：
  - `RolePermissionListItemDto`：角色权限绑定列表安全读模型，显式展开权限定义展示字段。
  - `RolePermissionDetailDto`：角色权限绑定详情安全读模型，包含权限描述、标签、优先级、授权原因、生效/失效时间和基础审计字段。
- 新增 `IRolePermissionQueryService` / `RolePermissionQueryService`：
  - `GetRolePermissionsAsync()`：按角色主键读取权限绑定列表，可选择仅返回当前有效授权。
  - `GetRolePermissionDetailAsync()`：按绑定主键读取详情。
  - 读取列表前校验角色存在，避免暴露无效角色绑定查询语义。
- 新增 `RolePermissionApplicationMapper`：
  - 集中映射角色权限绑定和权限定义字段。
  - 不依赖 `SysRolePermission.Permission` 导航，不把实体对象直接暴露到 DTO。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:role-permission:read`。
  - 权限种子登记为功能权限，不绑定资源/操作。

设计约束：

- 角色权限读模型不接收 `tenantId`，角色和权限绑定的读取仍由当前会话、Framework 全局过滤器和仓储边界处理。
- `onlyValid` 使用 `IRolePermissionRepository.GetValidByRoleIdsAsync()`，复用领域仓储对状态、生效时间和失效时间的统一判断。
- DTO 不展开角色继承链、不合并父角色权限、不计算最终授权结果；最终授权裁决留给授权 Store 和后续授权闭环阶段。
- 授权、拒绝、撤销、权限缓存失效、审计事件和角色继承展开留到后续命令服务/基础设施阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的角色权限读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A17 Application 角色权限绑定命令服务基线

本阶段继续第 6 层应用服务重构，补齐角色权限绑定的写侧最小闭环。范围限定为授予/拒绝权限、更新权限操作和有效期、更新绑定状态、撤销绑定、命令 DTO、命令契约、AppService、权限码和权限种子，不新增 Controller，不修改 Framework，不处理权限缓存失效和审计事件落库。

执行结果：

- 新增角色权限命令 DTO：
  - `RolePermissionGrantDto`：接收角色主键、权限主键、授权动作、生效/失效时间、授权原因和备注。
  - `RolePermissionUpdateDto`：更新已有绑定的授权动作、生效/失效时间、授权原因和备注；不允许换绑角色或权限。
  - `RolePermissionStatusUpdateDto`：更新绑定有效性状态。
- 新增 `IRolePermissionAppService` / `RolePermissionAppService`：
  - `CreateRolePermissionAsync()`：授予或拒绝角色权限，使用 `Create*` 命名确保 DynamicApi 映射为 POST。
  - `UpdateRolePermissionAsync()`：更新授权动作、有效期和授权原因。
  - `UpdateRolePermissionStatusAsync()`：更新 Valid/Invalid 状态，重新启用时校验角色和权限仍可维护。
  - `DeleteRolePermissionAsync()`：按 DELETE 语义撤销角色权限，实际写入 `Status = Invalid`，不硬删授权事实。
  - 写方法均使用 `[UnitOfWork(true)]` 与方法级权限控制。
- 授权约束：
  - 校验角色存在且启用。
  - 校验权限存在且启用。
  - 校验 `RoleId + PermissionId` 唯一，重复绑定直接拒绝。
  - 校验失效时间必须晚于生效时间。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:role-permission:grant`。
  - 新增 `saas:role-permission:update`。
  - 新增 `saas:role-permission:status`。
  - 新增 `saas:role-permission:revoke`。
  - 写权限种子标记为需审计功能权限。

设计约束：

- 命令 DTO 不接收 `tenantId`，角色和权限绑定的读取和更新依赖当前会话租户上下文与 Framework 全局过滤器。
- 撤销角色权限保留授权事实，不调用仓储硬删除。
- 本阶段只维护角色权限绑定本身；角色继承权限展开、最终授权裁决、权限缓存失效、审计日志和领域事件处理器留到后续授权闭环阶段。
- 更新绑定不允许更换角色或权限，避免授权事实身份漂移；换绑应撤销后重新授权。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的角色权限命令服务、命令 DTO、权限码/种子和本文档，不推送远端。

### 2026-04-30 A18 Application 角色定义命令服务基线

本阶段继续第 6 层应用服务重构，补齐角色定义写侧最小闭环。范围限定为当前租户角色创建、基础资料更新、启停状态更新、未引用角色删除、命令 DTO、命令契约、AppService、权限码和权限种子，不新增 Controller，不修改 Framework，不处理平台全局/系统角色运维、角色继承维护、角色数据范围绑定、权限缓存失效和审计事件落库。

执行结果：

- 新增角色命令 DTO：
  - `RoleCreateDto`：接收角色编码、名称、描述、角色类型、数据权限范围、最大成员数、状态、排序和备注。
  - `RoleUpdateDto`：更新角色名称、描述、类型、数据权限范围、最大成员数、排序和备注；不允许更新角色编码。
  - `RoleStatusUpdateDto`：更新角色启停状态和备注。
- 新增 `IRoleAppService` / `RoleAppService`：
  - `CreateRoleAsync()`：创建当前租户角色定义，角色编码在当前会话可见范围内唯一。
  - `UpdateRoleAsync()`：更新普通租户角色基础资料。
  - `UpdateRoleStatusAsync()`：更新普通租户角色启停状态。
  - `DeleteRoleAsync()`：删除未被用户角色、角色权限、角色继承和角色数据范围引用的普通租户角色。
  - 写方法均使用 `[UnitOfWork(true)]` 与方法级权限控制。
- 授权约束：
  - 普通角色服务不维护 `IsGlobal = true` 的平台全局角色。
  - 普通角色服务不创建或维护 `RoleType.System` 系统角色。
  - 删除前检查角色未被用户、权限、继承关系和数据范围引用，避免产生孤立授权事实。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:role:create`。
  - 新增 `saas:role:update`。
  - 新增 `saas:role:status`。
  - 新增 `saas:role:delete`。
  - 写权限种子标记为需审计功能权限。

设计约束：

- 命令 DTO 不接收 `tenantId`，角色写入和查询依赖当前会话租户上下文与 Framework 全局过滤器。
- 本阶段创建的角色固定为当前租户普通角色，不通过业务参数伪造平台全局角色。
- 角色编码作为授权事实身份保持创建后不可变；需要换编码时应新建角色并迁移授权关系。
- `DataScope = Custom` 的具体部门范围不在角色定义命令里内联维护，后续通过角色数据范围服务单独闭环。
- 角色继承、数据范围、用户角色分配、权限缓存失效、审计日志和领域事件处理器留到后续授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的角色命令服务、命令 DTO、权限码/种子和本文档，不推送远端。

### 2026-04-30 A19 Application 角色继承读模型

本阶段继续第 6 层应用服务重构，补齐角色继承闭包表的读侧入口。范围限定为角色祖先链、后代链、继承详情、读侧 DTO、查询契约、QueryService、显式映射器和查看权限，不做继承关系写入、闭包表重建、环路检测、权限继承合并和缓存失效，不新增 Controller，不修改 Framework。

执行结果：

- 新增角色继承读侧 DTO：
  - `RoleHierarchyListItemDto`：角色继承列表安全读模型，展示祖先/后代角色摘要、深度、路径、备注和创建时间。
  - `RoleHierarchyDetailDto`：角色继承详情安全读模型，补充创建人审计字段。
- 新增 `IRoleHierarchyQueryService` / `RoleHierarchyQueryService`：
  - `GetRoleAncestorsAsync()`：按角色主键读取祖先链，可选择是否包含自身闭包记录。
  - `GetRoleDescendantsAsync()`：按角色主键读取后代链，可选择是否包含自身闭包记录。
  - `GetRoleHierarchyDetailAsync()`：按闭包记录主键读取详情。
  - 读取链路前校验角色存在，避免暴露无效角色的继承查询语义。
- 新增 `RoleHierarchyApplicationMapper`：
  - 集中映射闭包记录和两端角色摘要字段。
  - 不依赖实体导航，不把 `SysRole` 对象直接暴露到 DTO。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:role-hierarchy:read`。
  - 权限种子登记为功能权限，不标记审计。

设计约束：

- 角色继承读模型不接收 `tenantId`，闭包记录和两端角色读取依赖当前会话租户上下文与 Framework 全局过滤器。
- 读模型只展示闭包事实，不在应用层按读请求临时重算继承关系。
- DTO 不计算继承后的最终权限、不合并角色权限、不展开 SSD/DSD 约束；这些留给授权裁决服务和后续授权闭环阶段。
- 继承关系写入、环路检测、闭包表 diff 重建、缓存失效和审计事件留到后续命令服务阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的角色继承读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A20 Application 角色继承命令服务基线

本阶段继续第 6 层应用服务重构，补齐角色继承闭包表的写侧基线。范围限定为创建直接继承边、补齐自身与派生闭包记录、删除直接继承边并清理不再可达的派生闭包记录、命令 DTO、命令契约、AppService、权限码和权限种子，不新增 Controller，不修改 Framework，不处理 SSD/DSD 约束、权限缓存失效和审计事件落库。

执行结果：

- 新增 `RoleHierarchyCreateDto`：
  - 接收祖先角色主键、后代角色主键和备注。
  - 不接收 `tenantId`，不允许角色继承自己。
- 新增 `IRoleHierarchyAppService` / `RoleHierarchyAppService`：
  - `CreateRoleHierarchyAsync()`：创建直接继承关系，校验重复关系和环路，补齐自身闭包与祖先 × 后代派生闭包记录。
  - `DeleteRoleHierarchyAsync()`：只允许删除 `Depth = 1` 的直接继承关系，按剩余直接边计算可达闭包，并删除不再可达的派生闭包记录。
  - 写方法均使用 `[UnitOfWork(true)]` 与方法级权限控制。
- 写入约束：
  - 后代角色不能是平台全局角色或系统角色；这些角色必须走平台运维流程维护。
  - 不允许创建已存在的祖先 → 后代闭包关系，避免把冗余直接边写进只有闭包表、没有边表的模型。
  - 删除直接边时，如果剩余直接边仍能形成替代路径，则拒绝删除，避免在唯一闭包记录上丢失直接边和传递边的区别。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:role-hierarchy:create`。
  - 新增 `saas:role-hierarchy:delete`。
  - 写权限种子标记为需审计功能权限。

设计约束：

- 命令 DTO 不接收 `tenantId`，角色与闭包记录的读取、写入和删除依赖当前会话租户上下文与 Framework 全局过滤器。
- 由于当前模型只有闭包表而没有独立直接边表，本阶段显式禁止冗余直接边，降低删除时无法区分替代路径的风险。
- 全局角色可作为祖先被当前租户普通角色继承，但普通角色服务不维护全局角色作为后代的继承关系。
- 本阶段只维护继承结构事实；继承后的权限合并、SSD/DSD 约束检查、缓存失效、审计日志和领域事件处理器留到后续授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`102` 个既有 `NU1900` 包源警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的角色继承命令服务、命令 DTO、权限码/种子和本文档，不推送远端。

### 2026-04-30 A21 Application 角色数据范围读模型

本阶段继续第 6 层应用服务重构，补齐角色自定义数据范围的读侧入口。范围限定为角色数据范围列表、详情、读侧 DTO、查询契约、QueryService、显式映射器和查看权限，不做数据范围绑定写操作，不展开部门闭包，不生成最终数据过滤表达式，不新增 Controller，不修改 Framework。

执行结果：

- 新增角色数据范围读侧 DTO：
  - `RoleDataScopeListItemDto`：角色自定义数据范围列表安全读模型，展示部门摘要、是否包含子部门、生效/失效时间、状态和备注。
  - `RoleDataScopeDetailDto`：角色自定义数据范围详情安全读模型，补充创建人审计字段。
- 新增 `IRoleDataScopeQueryService` / `RoleDataScopeQueryService`：
  - `GetRoleDataScopesAsync()`：按角色主键读取自定义数据范围列表，可选择仅返回当前有效范围。
  - `GetRoleDataScopeDetailAsync()`：按绑定主键读取详情。
  - 读取列表前校验角色存在，避免暴露无效角色的数据范围查询语义。
- 新增 `RoleDataScopeApplicationMapper`：
  - 集中映射角色数据范围和部门摘要字段。
  - 不依赖实体导航，不把 `SysDepartment` 对象直接暴露到 DTO。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:role-data-scope:read`。
  - 权限种子登记为功能权限，不标记审计。
  - 顺手将角色权限种子的排序号后移到 `280` 起，避免与角色继承/角色数据范围权限排序冲突。

设计约束：

- 角色数据范围读模型不接收 `tenantId`，范围和部门读取依赖当前会话租户上下文与 Framework 全局过滤器。
- DTO 只展示自定义数据范围事实，不展开 `IncludeChildren` 对应的部门后代集合。
- 本阶段不判断角色 `DataScope` 是否为 `Custom`，避免读侧隐藏历史配置；写侧维护时再约束。
- 数据范围绑定、状态更新、部门有效性校验、部门闭包展开、缓存失效和审计事件留到后续命令服务阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`102` 个既有 `NU1900` 包源警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的角色数据范围读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A22 Application 角色数据范围命令服务基线

本阶段继续第 6 层应用服务重构，补齐角色自定义数据范围的写侧入口。范围限定为数据范围授权、有效期/包含子部门更新、状态更新、撤销、命令 DTO、命令契约、AppService、权限码和权限种子，不新增 Controller，不修改 Framework，不展开部门闭包，不生成最终数据过滤表达式，不处理缓存失效和审计事件落库。

执行结果：

- 新增角色数据范围命令 DTO：
  - `RoleDataScopeGrantDto`：接收角色主键、部门主键、是否包含子部门、生效/失效时间和备注。
  - `RoleDataScopeUpdateDto`：更新绑定的包含子部门、有效期和备注。
  - `RoleDataScopeStatusUpdateDto`：更新绑定有效性状态和备注。
- 新增 `IRoleDataScopeAppService` / `RoleDataScopeAppService`：
  - `CreateRoleDataScopeAsync()`：授予角色自定义数据范围，校验角色、部门、重复绑定和有效期。
  - `UpdateRoleDataScopeAsync()`：更新已有绑定的包含子部门和有效期配置。
  - `UpdateRoleDataScopeStatusAsync()`：更新绑定状态，恢复有效时重新校验角色和部门可维护性。
  - `DeleteRoleDataScopeAsync()`：撤销绑定时标记 `ValidityStatus.Invalid`，保留授权事实以便追溯。
- 写入约束：
  - 角色必须存在、启用、非平台全局/系统角色，并且 `DataScope = Custom`。
  - 部门必须存在且启用。
  - 禁止重复写入同一角色和同一部门的数据范围绑定。
  - 生效时间和失效时间同时存在时，失效时间必须晚于生效时间。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:role-data-scope:grant`。
  - 新增 `saas:role-data-scope:update`。
  - 新增 `saas:role-data-scope:status`。
  - 新增 `saas:role-data-scope:revoke`。
  - 写权限种子标记为需审计功能权限。

设计约束：

- 命令 DTO 不接收 `tenantId`，角色、部门和数据范围绑定的读取与写入依赖当前会话租户上下文与 Framework 全局过滤器。
- 数据范围撤销不硬删，统一按授权事实生命周期处理，避免丢失历史绑定信息。
- 本阶段只维护 `SysRoleDataScope` 绑定事实，不展开 `IncludeChildren` 对应的部门后代集合。
- 部门闭包展开、最终数据范围合并、权限缓存失效、审计日志和领域事件处理器留到后续授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的角色数据范围命令服务、命令 DTO、权限码/种子和本文档，不推送远端。

### 2026-04-30 A23 Application 用户角色绑定读模型

本阶段继续第 6 层应用服务重构，补齐当前租户用户角色绑定的读侧入口。范围限定为用户角色列表、详情、读侧 DTO、查询契约、QueryService、显式映射器和查看权限，不做用户角色授予、状态更新、撤销、SSD/DSD 约束检查、会话角色同步、缓存失效和审计事件落库，不新增 Controller，不修改 Framework。

执行结果：

- 新增用户角色读侧 DTO：
  - `UserRoleListItemDto`：用户角色绑定列表安全读模型，展示租户成员摘要、角色摘要、有效期、授权原因、状态和创建时间。
  - `UserRoleDetailDto`：用户角色绑定详情安全读模型，补充角色描述和创建人审计字段。
- 新增 `IUserRoleQueryService` / `UserRoleQueryService`：
  - `GetUserRolesAsync()`：按用户主键读取当前租户用户角色绑定列表，可选择仅返回当前有效授权。
  - `GetUserRoleDetailAsync()`：按绑定主键读取详情。
  - 读取列表前校验当前租户成员关系存在，避免把 `SysUser.TenantId` 误当作当前租户身份。
- 新增 `UserRoleApplicationMapper`：
  - 集中映射用户角色绑定、角色摘要和当前租户成员摘要。
  - 不读取 `SysUser` 主表资料，不暴露邮箱、手机号等身份敏感字段。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:user-role:read`。
  - 权限种子登记为功能权限，不标记审计。

设计约束：

- 用户角色读模型不接收 `tenantId`，绑定、角色和租户成员关系读取依赖当前会话租户上下文与 Framework 全局过滤器。
- 因 `SysUser.TenantId` 表达主账号归属租户，不等同于当前生效租户，本阶段不通过 `IUserRepository` 读取用户资料；当前租户内展示信息来自 `SysTenantUser.DisplayName`。
- DTO 只展示用户角色绑定事实，不展开角色继承链、不合并角色权限、不计算最终授权结果。
- 用户角色授予/撤销、SSD/DSD 约束、会话角色同步、权限缓存失效、审计日志和领域事件处理器留到后续授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的用户角色读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A24 Application 用户角色绑定命令服务基线

本阶段继续第 6 层应用服务重构，补齐当前租户用户角色绑定的写侧入口。范围限定为用户角色授权、有效期/授权原因更新、状态更新、撤销、命令 DTO、命令契约、AppService、权限码和权限种子，不新增 Controller，不修改 Framework，不处理 SSD/DSD 约束、会话角色同步、缓存失效和审计事件落库。

执行结果：

- 新增用户角色命令 DTO：
  - `UserRoleGrantDto`：接收用户主键、角色主键、生效/失效时间、授权原因和备注。
  - `UserRoleUpdateDto`：更新已有绑定的生效/失效时间、授权原因和备注。
  - `UserRoleStatusUpdateDto`：更新绑定有效性状态和备注。
- 新增 `IUserRoleAppService` / `UserRoleAppService`：
  - `CreateUserRoleAsync()`：授予当前租户成员角色，校验成员、角色、重复绑定和有效期。
  - `UpdateUserRoleAsync()`：更新用户角色绑定的授权原因和有效期。
  - `UpdateUserRoleStatusAsync()`：更新绑定状态，恢复有效时重新校验成员和角色可授权性。
  - `DeleteUserRoleAsync()`：撤销绑定时标记 `ValidityStatus.Invalid`，保留授权事实以便追溯。
- 写入约束：
  - 用户必须是当前租户成员，且邀请状态为已接受、成员状态有效、成员有效期当前可用。
  - 平台管理员成员身份的角色维护必须通过平台运维流程，不通过普通租户用户角色服务处理。
  - 角色必须存在且启用；系统角色必须通过平台运维流程分配。
  - 禁止重复写入同一用户和同一角色的绑定。
  - 生效时间和失效时间同时存在时，失效时间必须晚于生效时间。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:user-role:grant`。
  - 新增 `saas:user-role:update`。
  - 新增 `saas:user-role:status`。
  - 新增 `saas:user-role:revoke`。
  - 写权限种子标记为需审计功能权限。

设计约束：

- 命令 DTO 不接收 `tenantId`，用户角色绑定、角色和租户成员关系读取与写入依赖当前会话租户上下文与 Framework 全局过滤器。
- 用户身份校验继续通过 `SysTenantUser` 当前租户成员关系完成，不读取 `SysUser` 主表资料作为当前租户身份依据。
- 用户角色撤销不硬删，统一按授权事实生命周期处理，避免丢失历史授权信息。
- 本阶段只维护 `SysUserRole` 绑定事实；SSD/DSD 职责分离约束、会话角色同步、权限缓存失效、审计日志和领域事件处理器留到后续授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的用户角色命令服务、命令 DTO、权限码/种子和本文档，不推送远端。

### 2026-04-30 A25 Application 用户直授权限读模型

本阶段继续第 6 层应用服务重构，补齐当前租户用户直授权限的读侧入口。范围限定为用户直授权限列表、详情、读侧 DTO、查询契约、QueryService、显式映射器和查看权限，不做用户直授权限授予、撤销、最终权限裁决、缓存失效和审计事件落库，不新增 Controller，不修改 Framework。

执行结果：

- 新增用户直授权限读侧 DTO：
  - `UserPermissionListItemDto`：用户直授权限列表安全读模型，展示租户成员摘要、权限摘要、Grant/Deny 操作、有效期、授权原因、状态和创建时间。
  - `UserPermissionDetailDto`：用户直授权限详情安全读模型，补充权限描述、标签、优先级和创建人审计字段。
- 新增 `IUserPermissionQueryService` / `UserPermissionQueryService`：
  - `GetUserPermissionsAsync()`：按用户主键读取当前租户用户直授权限列表，可选择仅返回当前有效直授权限。
  - `GetUserPermissionDetailAsync()`：按绑定主键读取详情。
  - 读取列表前校验当前租户成员关系存在，避免把 `SysUser.TenantId` 误当作当前租户身份。
- 新增 `UserPermissionApplicationMapper`：
  - 集中映射用户直授权限绑定、权限摘要和当前租户成员摘要。
  - 不读取 `SysUser` 主表资料，不暴露邮箱、手机号等身份敏感字段。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:user-permission:read`。
  - 权限种子登记为功能权限，不标记审计。

设计约束：

- 用户直授权限读模型不接收 `tenantId`，绑定、权限和租户成员关系读取依赖当前会话租户上下文与 Framework 全局过滤器。
- 因 `SysUser.TenantId` 表达主账号归属租户，不等同于当前生效租户，本阶段不通过 `IUserRepository` 读取用户资料；当前租户内展示信息来自 `SysTenantUser.DisplayName`。
- DTO 只展示用户直授权限事实，不合并角色权限、不展开角色继承链、不计算最终授权结果。
- 用户直授权限授权/撤销、Deny-overrides 最终裁决、权限缓存失效、审计日志和领域事件处理器留到后续授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的用户直授权限读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A26 Application 用户直授权限命令服务基线

本阶段继续第 6 层应用服务重构，补齐当前租户用户直授权限的写侧入口。范围限定为用户直授权限授权、权限操作/有效期/授权原因更新、状态更新、撤销、命令 DTO、命令契约、AppService、权限码和权限种子，不新增 Controller，不修改 Framework，不处理最终权限裁决、缓存失效和审计事件落库。

执行结果：

- 新增用户直授权限命令 DTO：
  - `UserPermissionGrantDto`：接收用户主键、权限主键、Grant/Deny 操作、生效/失效时间、授权原因和备注。
  - `UserPermissionUpdateDto`：更新已有绑定的 Grant/Deny 操作、生效/失效时间、授权原因和备注。
  - `UserPermissionStatusUpdateDto`：更新绑定有效性状态和备注。
- 新增 `IUserPermissionAppService` / `UserPermissionAppService`：
  - `CreateUserPermissionAsync()`：授予或拒绝当前租户成员的直授权限，校验成员、权限、重复绑定和有效期。
  - `UpdateUserPermissionAsync()`：更新用户直授权限操作、授权原因和有效期。
  - `UpdateUserPermissionStatusAsync()`：更新绑定状态，恢复有效时重新校验成员和权限可授权性。
  - `DeleteUserPermissionAsync()`：撤销绑定时标记 `ValidityStatus.Invalid`，保留授权事实以便追溯。
- 写入约束：
  - 用户必须是当前租户成员，且邀请状态为已接受、成员状态有效、成员有效期当前可用。
  - 平台管理员成员身份的权限维护必须通过平台运维流程，不通过普通租户用户直授权限服务处理。
  - 权限必须存在且启用。
  - 禁止重复写入同一用户和同一权限的绑定。
  - 生效时间和失效时间同时存在时，失效时间必须晚于生效时间。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:user-permission:grant`。
  - 新增 `saas:user-permission:update`。
  - 新增 `saas:user-permission:status`。
  - 新增 `saas:user-permission:revoke`。
  - 写权限种子标记为需审计功能权限。

设计约束：

- 命令 DTO 不接收 `tenantId`，用户直授权限、权限和租户成员关系读取与写入依赖当前会话租户上下文与 Framework 全局过滤器。
- 用户身份校验继续通过 `SysTenantUser` 当前租户成员关系完成，不读取 `SysUser` 主表资料作为当前租户身份依据。
- 用户直授权限撤销不硬删，统一按授权事实生命周期处理，避免丢失历史授权信息。
- 本阶段只维护 `SysUserPermission` 绑定事实；Deny-overrides 最终裁决、权限缓存失效、审计日志和领域事件处理器留到后续授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的用户直授权限命令服务、命令 DTO、权限码/种子和本文档，不推送远端。

### 2026-04-30 A27 Application 用户数据范围读模型

本阶段继续第 6 层应用服务重构，补齐当前租户用户数据范围覆盖的读侧入口。范围限定为用户数据范围列表、详情、读侧 DTO、查询契约、QueryService、显式映射器、仓储有效覆盖查询和查看权限，不做用户数据范围授予、撤销、最终数据范围合并、缓存失效和审计事件落库，不新增 Controller，不修改 Framework。

执行结果：

- 新增用户数据范围读侧 DTO：
  - `UserDataScopeListItemDto`：展示当前租户成员摘要、数据范围、部门摘要、包含子部门、状态、备注和创建时间。
  - `UserDataScopeDetailDto`：在列表字段基础上补充创建人审计字段。
- 新增 `IUserDataScopeQueryService` / `UserDataScopeQueryService`：
  - `GetUserDataScopesAsync()`：按用户主键读取当前租户用户数据范围覆盖，可选择仅返回有效覆盖。
  - `GetUserDataScopeDetailAsync()`：按绑定主键读取详情。
  - 读取列表前校验当前租户成员关系存在，避免把 `SysUser.TenantId` 误当作当前租户身份。
- 新增 `UserDataScopeApplicationMapper`：
  - 集中映射用户数据范围覆盖、部门摘要和当前租户成员摘要。
  - 不读取 `SysUser` 主表资料，不暴露邮箱、手机号等身份敏感字段。
- 扩展 `IUserDataScopeRepository` / `UserDataScopeRepository`：
  - 新增 `GetValidByUserIdAsync()`，按当前租户过滤上下文读取有效用户级数据范围覆盖。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:user-data-scope:read`。
  - 权限种子登记为功能权限，不标记审计。

设计约束：

- 用户数据范围读模型不接收 `tenantId`，覆盖记录、部门和租户成员关系读取依赖当前会话租户上下文与 Framework 全局过滤器。
- 因 `SysUser.TenantId` 表达主账号归属租户，不等同于当前生效租户，本阶段不通过 `IUserRepository` 读取用户资料；当前租户内展示信息来自 `SysTenantUser.DisplayName`。
- DTO 只展示用户级数据范围覆盖事实，不合并角色级数据范围、不展开部门树、不计算最终数据范围。
- 用户数据范围授予/撤销、用户覆盖优先级合并、部门层级展开、数据范围缓存失效、审计日志和领域事件处理器留到后续授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的用户数据范围读模型、QueryService、Mapper、仓储有效查询、权限码/种子和本文档，不推送远端。

### 2026-04-30 A28 Application 用户数据范围命令服务基线

本阶段继续第 6 层应用服务重构，补齐当前租户用户数据范围覆盖的写侧入口。范围限定为用户数据范围授权、覆盖模式/部门设置更新、状态更新、撤销、命令 DTO、命令契约、AppService、权限码和权限种子，不新增 Controller，不修改 Framework，不处理最终数据范围合并、缓存失效和审计事件落库。

执行结果：

- 新增用户数据范围命令 DTO：
  - `UserDataScopeGrantDto`：接收用户主键、数据权限范围、自定义部门、是否包含子部门和备注。
  - `UserDataScopeUpdateDto`：更新已有覆盖记录的数据权限范围、自定义部门、是否包含子部门和备注。
  - `UserDataScopeStatusUpdateDto`：更新覆盖记录有效性状态和备注。
- 新增 `IUserDataScopeAppService` / `UserDataScopeAppService`：
  - `CreateUserDataScopeAsync()`：授予当前租户成员用户级数据范围覆盖，校验成员、覆盖模式、部门和重复绑定。
  - `UpdateUserDataScopeAsync()`：更新用户级数据范围覆盖模式和部门设置。
  - `UpdateUserDataScopeStatusAsync()`：更新绑定状态，恢复有效时重新校验成员和自定义部门可用性。
  - `DeleteUserDataScopeAsync()`：撤销覆盖时标记 `ValidityStatus.Invalid`，保留授权事实以便追溯。
- 写入约束：
  - 用户必须是当前租户成员，且邀请状态为已接受、成员状态有效、成员有效期当前可用。
  - 平台管理员成员身份的数据范围维护必须通过平台运维流程，不通过普通租户用户数据范围服务处理。
  - `DataPermissionScope.Custom` 必须指定已启用部门；非自定义范围不能指定部门。
  - 非自定义用户覆盖每个用户只允许一条；自定义用户覆盖允许同一用户绑定多个部门，但不能与非自定义覆盖并存。
  - 撤销不硬删，统一按授权事实生命周期处理。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:user-data-scope:grant`。
  - 新增 `saas:user-data-scope:update`。
  - 新增 `saas:user-data-scope:status`。
  - 新增 `saas:user-data-scope:revoke`。
  - 写权限种子标记为需审计功能权限。

设计约束：

- 命令 DTO 不接收 `tenantId`，用户数据范围覆盖、部门和租户成员关系读取与写入依赖当前会话租户上下文与 Framework 全局过滤器。
- 用户身份校验继续通过 `SysTenantUser` 当前租户成员关系完成，不读取 `SysUser` 主表资料作为当前租户身份依据。
- 本阶段只维护 `SysUserDataScope` 覆盖事实；用户覆盖优先级合并、部门层级展开、数据范围缓存失效、审计日志和领域事件处理器留到后续授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的用户数据范围命令服务、命令 DTO、权限码/种子和本文档，不推送远端。

### 2026-04-30 A29 Application 字段级安全读模型

本阶段继续第 6 层应用服务重构，补齐 FLS 字段级安全策略的读侧入口。范围限定为策略分页、详情、读侧 DTO、查询契约、QueryService、显式映射器和查看权限，不执行真实字段脱敏、不处理策略写入、不接入缓存失效和审计事件落库，不新增 Controller，不修改 Framework。

执行结果：

- 新增字段级安全读侧 DTO：
  - `FieldLevelSecurityPageQueryDto`：支持关键字、目标类型、目标主键、资源主键、脱敏策略和状态筛选。
  - `FieldLevelSecurityListItemDto`：展示目标摘要、资源摘要、字段名、读写能力、脱敏策略、优先级、策略描述、状态和审计时间。
  - `FieldLevelSecurityDetailDto`：在列表字段基础上补充脱敏模式、备注和创建/修改审计字段。
- 新增 `IFieldLevelSecurityQueryService` / `FieldLevelSecurityQueryService`：
  - `GetFieldLevelSecurityPageAsync()`：分页读取当前租户上下文内的 FLS 策略，并汇总资源与目标摘要。
  - `GetFieldLevelSecurityDetailAsync()`：按策略主键读取详情。
  - `TargetType=User` 时只读取当前租户成员 `SysTenantUser` 摘要，不读取 `SysUser` 主表资料。
- 新增 `FieldLevelSecurityApplicationMapper`：
  - 集中映射 FLS 策略、资源摘要和目标摘要。
  - 只返回策略定义，不返回任何真实业务字段值。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:field-level-security:read`。
  - 权限种子登记为功能权限，不标记审计。

设计约束：

- FLS 读模型不接收 `tenantId`，策略、资源、角色、权限、部门和租户成员读取依赖当前会话租户上下文与 Framework 全局过滤器。
- 本阶段仅展示策略元数据；真实字段脱敏、不可编辑字段写入拒绝、冲突合并、缓存失效、审计日志和领域事件处理器留到后续 FLS 闭环阶段。
- 目标为用户时，不把 `SysUser.TenantId` 当作当前租户身份依据，避免跨租户用户资料泄露。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的字段级安全读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A30 Application 字段级安全命令服务基线

本阶段继续第 6 层应用服务重构，补齐 FLS 字段级安全策略的写侧入口。范围限定为策略创建、策略更新、状态更新、删除、命令 DTO、命令契约、AppService、权限码和权限种子，不执行真实字段脱敏、不接入缓存失效和审计事件落库，不新增 Controller，不修改 Framework。

执行结果：

- 新增字段级安全命令 DTO：
  - `FieldLevelSecurityCreateDto`：接收目标类型、目标主键、资源主键、字段名、读写能力、脱敏策略、脱敏模式、优先级、策略描述、状态和备注。
  - `FieldLevelSecurityUpdateDto`：更新已有策略的目标、资源、字段、读写能力、脱敏策略、优先级、描述和备注。
  - `FieldLevelSecurityStatusUpdateDto`：更新策略启停状态和备注。
- 新增 `IFieldLevelSecurityAppService` / `FieldLevelSecurityAppService`：
  - `CreateFieldLevelSecurityAsync()`：创建当前租户上下文内的 FLS 策略，校验资源、目标主体、字段名、读写能力、脱敏策略和唯一约束。
  - `UpdateFieldLevelSecurityAsync()`：更新 FLS 策略定义，支持调整目标、资源和字段。
  - `UpdateFieldLevelSecurityStatusAsync()`：更新策略状态，恢复启用时重新校验资源和目标主体可用性。
  - `DeleteFieldLevelSecurityAsync()`：删除策略，依赖实体软删除能力保留审计字段。
- 写入约束：
  - 资源必须存在且启用。
  - 角色目标必须是当前租户可维护角色，停用、平台全局或系统角色不能通过普通租户 FLS 服务维护。
  - 用户目标必须是当前租户成员，且邀请状态已接受、成员状态有效、成员有效期当前可用。
  - 权限和部门目标必须存在且启用。
  - 不可读字段不能设置为可编辑；不可读字段必须指定非 `None` 的脱敏策略。
  - 同一目标类型、目标主键、资源主键和字段名不能重复配置。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:field-level-security:create`。
  - 新增 `saas:field-level-security:update`。
  - 新增 `saas:field-level-security:status`。
  - 新增 `saas:field-level-security:delete`。
  - 写权限种子标记为需审计功能权限。

设计约束：

- 命令 DTO 不接收 `tenantId`，FLS 策略、资源、角色、权限、部门和租户成员读取与写入依赖当前会话租户上下文与 Framework 全局过滤器。
- 用户目标继续通过 `SysTenantUser` 当前租户成员关系校验，不读取 `SysUser` 主表资料作为当前租户身份依据。
- 本阶段只维护 `SysFieldLevelSecurity` 策略定义；真实字段脱敏、不可编辑字段写入拒绝、冲突合并、缓存失效、审计日志和领域事件处理器留到后续 FLS 闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的字段级安全命令服务、命令 DTO、权限码/种子和本文档，不推送远端。

### 2026-04-30 A31 Application 权限委托读模型

本阶段继续第 6 层应用服务重构，补齐权限委托的读侧入口。范围限定为权限委托分页、详情、读侧 DTO、查询契约、QueryService、显式映射器和查看权限，不处理委托创建、更新、撤销、缓存失效、审计日志和领域事件落库，不新增 Controller，不修改 Framework。

执行结果：

- 新增权限委托读侧 DTO：
  - `PermissionDelegationPageQueryDto`：支持关键字、委托人、被委托人、权限、角色和委托状态筛选。
  - `PermissionDelegationListItemDto`：展示委托人/被委托人租户成员摘要、权限摘要、角色摘要、状态、生效失效时间、过期标记和审计时间。
  - `PermissionDelegationDetailDto`：在列表字段基础上补充权限/角色描述、备注和创建/修改审计字段。
- 新增 `IPermissionDelegationQueryService` / `PermissionDelegationQueryService`：
  - `GetPermissionDelegationPageAsync()`：分页读取当前租户上下文内的权限委托事实，并批量汇总租户成员、权限和角色摘要。
  - `GetPermissionDelegationDetailAsync()`：按委托主键读取详情。
  - 委托人和被委托人摘要统一通过 `SysTenantUser` 当前租户成员关系读取，不读取 `SysUser` 主表资料作为当前租户身份依据。
- 新增 `PermissionDelegationApplicationMapper`：
  - 集中映射权限委托列表项和详情。
  - 只返回委托事实和授权对象摘要，不返回租户连接串、联系人等敏感租户配置。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:permission-delegation:read`。
  - 权限种子登记为功能权限，不标记审计。

设计约束：

- 权限委托读模型不接收 `tenantId`，委托、租户成员、权限和角色读取依赖当前会话租户上下文与 Framework 全局过滤器。
- 本阶段只展示权限委托事实；委托创建、更新、状态变更、撤销、有效期冲突处理、授权合并、缓存失效、审计日志和领域事件处理器留到后续授权闭环阶段。
- 用户相关摘要继续通过 `SysTenantUser` 当前租户成员关系完成，避免把 `SysUser.TenantId` 误当作当前租户身份依据。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`102` 个既有 `NU1900` 包漏洞数据源连接警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 存在本阶段未提交改动，`XiHan.Framework` git 状态干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的权限委托读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A32 Application 权限委托命令服务基线

本阶段继续第 6 层应用服务重构，补齐权限委托的写侧入口。范围限定为委托创建、委托更新、委托状态更新、撤销、命令 DTO、命令契约、AppService、权限码和权限种子，不处理授权合并、缓存失效、审计日志和领域事件落库，不新增 Controller，不修改 Framework。

执行结果：

- 新增权限委托命令 DTO：
  - `PermissionDelegationCreateDto`：接收委托人、被委托人、可选权限、可选角色、生效时间、失效时间、委托原因和备注。
  - `PermissionDelegationUpdateDto`：更新已有权限委托的主体、授权范围、有效期、原因和备注。
  - `PermissionDelegationStatusUpdateDto`：更新委托状态和备注。
- 新增 `IPermissionDelegationAppService` / `PermissionDelegationAppService`：
  - `CreatePermissionDelegationAsync()`：创建当前租户上下文内的权限委托，按生效时间自动写入 `Pending` 或 `Active`。
  - `UpdatePermissionDelegationAsync()`：更新未撤销的权限委托，重新校验主体、授权对象、有效期和唯一约束。
  - `UpdatePermissionDelegationStatusAsync()`：更新状态，恢复到待生效/生效中时重新校验租户成员、权限和角色可用性。
  - `DeletePermissionDelegationAsync()`：按动态 API 删除语义暴露撤销入口，但只将委托状态更新为 `Revoked`，不硬删事实。
- 写入约束：
  - 委托人和被委托人必须是当前租户成员，邀请状态已接受、成员状态有效、成员有效期当前可用。
  - 委托人和被委托人不能相同。
  - 可选权限必须存在且启用。
  - 可选角色必须存在且启用，平台全局角色或系统角色不能通过普通租户权限委托服务维护。
  - 失效时间必填，且必须晚于当前时间和生效时间。
  - 同一委托人、被委托人和权限主键不能重复配置。
  - 已撤销委托不能更新或重新生效。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:permission-delegation:create`。
  - 新增 `saas:permission-delegation:update`。
  - 新增 `saas:permission-delegation:status`。
  - 新增 `saas:permission-delegation:revoke`。
  - 写权限种子标记为需审计功能权限。

设计约束：

- 命令 DTO 不接收 `tenantId`，权限委托、租户成员、权限和角色读取与写入依赖当前会话租户上下文与 Framework 全局过滤器。
- 用户相关校验继续通过 `SysTenantUser` 当前租户成员关系完成，不读取 `SysUser` 主表资料作为当前租户身份依据。
- 本阶段只维护 `SysPermissionDelegation` 委托事实；委托权限运行时合并、过期扫描、撤销事件、缓存失效、审计日志和领域事件处理器留到后续授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的权限委托命令服务、命令 DTO、权限码/种子和本文档，不推送远端。

### 2026-04-30 A33 Application 权限申请读模型

本阶段继续第 6 层应用服务重构，补齐权限申请的读侧入口。范围限定为权限申请分页、详情、读侧 DTO、查询契约、QueryService、显式映射器和查看权限，不处理申请提交、审批流创建、审批通过后的自动授权、缓存失效、审计日志和领域事件落库，不新增 Controller，不修改 Framework。

执行结果：

- 新增权限申请读侧 DTO：
  - `PermissionRequestPageQueryDto`：支持关键字、申请人、权限、角色、审批单和申请状态筛选。
  - `PermissionRequestListItemDto`：展示申请人租户成员摘要、权限摘要、角色摘要、申请状态、期望有效期、审批单摘要和审计时间。
  - `PermissionRequestDetailDto`：在列表字段基础上补充权限/角色描述、审批单描述、备注和创建/修改审计字段。
- 新增 `IPermissionRequestQueryService` / `PermissionRequestQueryService`：
  - `GetPermissionRequestPageAsync()`：分页读取当前租户上下文内的权限申请，并批量汇总租户成员、权限、角色和审批单摘要。
  - `GetPermissionRequestDetailAsync()`：按申请主键读取详情。
  - 申请人摘要统一通过 `SysTenantUser` 当前租户成员关系读取，不读取 `SysUser` 主表资料作为当前租户身份依据。
- 新增 `PermissionRequestApplicationMapper`：
  - 集中映射权限申请列表项和详情。
  - 只返回申请事实和审批摘要，不返回审批内容 JSON、业务数据 JSON、附件 JSON 等潜在敏感流程载荷。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:permission-request:read`。
  - 权限种子登记为功能权限，不标记审计。

设计约束：

- 权限申请读模型不接收 `tenantId`，申请、租户成员、权限、角色和审批单读取依赖当前会话租户上下文与 Framework 全局过滤器。
- 本阶段只展示权限申请事实；申请提交、审批流创建、审批状态流转、审批通过后的用户角色/权限写入、缓存失效、审计日志和领域事件处理器留到后续授权闭环阶段。
- 用户相关摘要继续通过 `SysTenantUser` 当前租户成员关系完成，避免把 `SysUser.TenantId` 误当作当前租户身份依据。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的权限申请读模型、QueryService、Mapper、权限码/种子和本文档，不推送远端。

### 2026-04-30 A34 Application 权限申请命令服务基线

本阶段继续第 6 层应用服务重构，补齐权限申请的写侧入口。范围限定为当前登录用户提交申请、更新自己的待审批申请、状态更新、撤回、命令 DTO、命令契约、AppService、权限码和权限种子，不处理审批流创建、审批通过后的自动授权、缓存失效、审计日志和领域事件落库，不新增 Controller，不修改 Framework。

执行结果：

- 新增权限申请命令 DTO：
  - `PermissionRequestCreateDto`：接收可选权限、可选角色、申请原因、期望有效期和备注；申请人来自当前会话用户。
  - `PermissionRequestUpdateDto`：更新待审批申请的授权目标、原因、期望有效期和备注。
  - `PermissionRequestStatusUpdateDto`：更新申请状态，可按需关联审批单。
- 新增 `IPermissionRequestAppService` / `PermissionRequestAppService`：
  - `CreatePermissionRequestAsync()`：使用 `ICurrentUser.UserId` 作为申请人，创建当前租户上下文内的待审批权限申请。
  - `UpdatePermissionRequestAsync()`：仅允许申请人更新自己的待审批申请。
  - `UpdatePermissionRequestStatusAsync()`：允许授权服务更新申请状态，但禁止直接更新为 `Approved`，避免绕过审批流和自动授权闭环。
  - `DeletePermissionRequestAsync()`：按动态 API 删除语义暴露撤回入口，但只将申请状态更新为 `Withdrawn`，不硬删事实。
- 写入约束：
  - 创建和更新 DTO 不接收 `RequestUserId` 或 `tenantId`。
  - 当前用户必须是当前租户成员，邀请状态已接受、成员状态有效、成员有效期当前可用。
  - 申请必须指定权限或角色。
  - 可选权限必须存在且启用。
  - 可选角色必须存在且启用，平台全局角色或系统角色不能通过普通租户权限申请服务维护。
  - 期望失效时间必须晚于当前时间和期望生效时间。
  - 同一申请人、权限和角色组合不能存在重复待审批申请。
  - 已完结申请不能变更状态；审批通过必须留给审批流自动授权流程。
- 扩展 `SaasPermissionCodes` 与 `SaasPermissionSeeder`：
  - 新增 `saas:permission-request:create`。
  - 新增 `saas:permission-request:update`。
  - 新增 `saas:permission-request:status`。
  - 新增 `saas:permission-request:withdraw`。
  - 写权限种子标记为需审计功能权限。

设计约束：

- 申请人身份来自当前认证会话，不由前端传入，租户隔离依赖当前会话租户上下文与 Framework 全局过滤器。
- 用户相关校验继续通过 `SysTenantUser` 当前租户成员关系完成，不读取 `SysUser` 主表资料作为当前租户身份依据。
- 本阶段只维护 `SysPermissionRequest` 申请事实；审批单创建、审批通过后的 `SysUserRole` / `SysUserPermission` 自动写入、缓存失效、审计日志和领域事件处理器留到后续授权闭环阶段。

验证结果：

- `dotnet build E:\Repository\XiHanFun\XiHan.BasicApp\backend\src\modules\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj --artifacts-path C:\Users\zhaifanhua\AppData\Local\Temp\XiHanBasicAppCodexArtifacts -m:1 -p:UseSharedCompilation=false --no-restore`：通过，`151` 个既有 `NU1900`/`NU5104` 包源和预发布依赖警告，`0` 个错误。
- `rg -n "class .*Controller" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "TenantId\s*==\s*null|TenantId\s+IS\s+NULL|PlatformTenantId\s*=\s*1" backend/src/modules/XiHan.BasicApp.Saas -g "*.cs"`：0 个匹配。
- `rg -n "\btenantId\b" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "ConnectionString|ContactPhone|ContactEmail|DatabaseSchema|DatabaseType|IsConnectionStringEncrypted" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "namespace XiHan\.BasicApp\.Saas\.Application\.(Dtos|Contracts|QueryServices|AppServices|Mappers)\." backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "PermissionAuthorize\(\"" backend/src/modules/XiHan.BasicApp.Saas/Application -g "*.cs"`：0 个匹配。
- `rg -n "RequestUserId\s*\{\s*get;\s*set;\s*\}" backend/src/modules/XiHan.BasicApp.Saas/Application/Dtos/Authorization/PermissionRequestCreateDto.cs backend/src/modules/XiHan.BasicApp.Saas/Application/Dtos/Authorization/PermissionRequestUpdateDto.cs -g "*.cs"`：0 个匹配。
- `git diff --check`：通过。

协作状态：

- 阶段前检查 `XiHan.BasicApp` 与 `XiHan.Framework` git 状态均干净。
- `XiHan.Framework` 本阶段无改动。
- 本阶段只提交 BasicApp 的权限申请命令服务、命令 DTO、权限码/种子和本文档，不推送远端。
