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
