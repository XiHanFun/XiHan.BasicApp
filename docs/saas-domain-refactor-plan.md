# SaaS Domain Refactor Plan

## Background

`XiHan.BasicApp.Saas` 的 `Domain\Entities` 已完成重构，但 Application、Infrastructure、Frontend 仍大量沿用旧模型。
本次改造以 `backend/src/modules/XiHan.BasicApp.Saas/Domain/Entities` 为唯一事实源，完全按新实体语义重建，不做历史兼容层。
同时必须遵守 `E:\Repository\XiHanFun\XiHan.Framework` 作为底层框架的复用约束，优先使用框架与 `XiHan.BasicApp.Core` 既有能力，不在 SaaS 模块重复实现通用基础设施。

## Code Facts

1. `SysUser` 已明确区分“主归属租户”与“可访问租户集合”，而现有应用层和前端仍默认 `User == Tenant` 的旧模型。
2. `SysRole` 已引入 `RoleHierarchy`、`RoleDataScope`、`IsGlobal`、显式 `DataScope` 语义，现有前后端只覆盖了其中一部分。
3. `SysPermission`、`SysMenu` 已拆分为“权限原子点”和“纯 UI 菜单结构”，现有前端仍把菜单编码混用为权限表达。
4. `SysDepartment` 已要求树结构环路校验和闭包表语义，现有实现只做了普通树形 CRUD。
5. `SysConstraintRule` 与 `SysFieldLevelSecurity` 已成为正式领域对象，但现有契约、查询、前端管理能力不完整。
6. 现有前端页面和 API 类型中仍混用旧字段，如 `deptId`、`PermissionCode`、`Status`/`TenantStatus`、`RoleType`/`DataScope` 的旧数值假设。
7. 当前前端 `pnpm type-check` 还有一批基础包错误，会影响 SaaS 模块的验证闭环，需要在后续质量门禁阶段统一收敛。
8. 当前 `Infrastructure/Repositories` 基本按单实体平铺，缺少围绕聚合、租户上下文、授权上下文的数据访问边界，容易把查询拼装、租户过滤、软删过滤散落到服务层。
9. 当前 `Application/AppServices/Implementations` 数量较多且职责偏横向 CRUD，后续必须按“命令编排 + 查询服务 + 内部领域服务”重切，而不是继续在 AppService 上叠逻辑。
10. 当前 `Infrastructure/Settings` 仅看到 `RbacSettingStore`，说明配置键体系尚未系统化；缓存键、配置键、种子编码需要统一命名规范。
11. 当前 `Seeders` 以表级拆分为主，包含 `SysRolePermissionSeeder`、`SysDepartmentHierarchySeeder`、`SysConstraintRuleFeatureSeeder` 等旧组织方式；后续要按“平台基础模板 / 租户初始化模板 / 演示数据”重建。
12. `XiHan.Framework` 已提供实体基类、聚合抽象、SqlSugar、多租户、缓存、设置、事件等底层能力；后续重构必须先盘点这些能力，再决定哪些逻辑保留在 BasicApp SaaS 模块。

## Goals

1. 以后端实体和注释为准，重建 SaaS 模块的前后端契约、服务、查询和页面。
2. 严格执行多租户隔离、RBAC 为主、轻量 ABAC 补充、FLS 出口生效、审计可追踪。
3. 每一批改动必须可验证、可回滚、可独立提交。
4. 重建仓储、服务、内部服务、缓存、配置、种子数据的统一规范，移除之前不合理设计。
5. 所有通用基础能力优先复用 `XiHan.Framework`，仅当框架缺失或明确属于 SaaS 业务域时，才允许在模块内扩展。

## Non-Goals

1. 不保留旧字段兼容层。
2. 不围绕旧 API 形状做适配性补丁。
3. 不在 Controller/UI 层继续堆积业务规则。
4. 不保留旧缓存键、旧配置键、旧种子结构的过渡兼容。
5. 不在 `XiHan.BasicApp.Saas` 重复实现已经可由 `XiHan.Framework` 或 `XiHan.BasicApp.Core` 提供的通用能力。

## Framework Constraint

底层约束：

1. `XiHan.Framework` 是基础能力优先来源。
2. `XiHan.BasicApp.Core` 是 BasicApp 级复用层，优先于 `XiHan.BasicApp.Saas`。
3. `XiHan.BasicApp.Saas` 只承载 SaaS 业务语义、业务聚合、业务用例，不重复承载框架职责。

复用顺序：

1. 先找 `XiHan.Framework`
2. 再找 `XiHan.BasicApp.Core`
3. 最后才在 `XiHan.BasicApp.Saas` 新增

实施要求：

1. 新增抽象前，先确认框架是否已有等价抽象或扩展点。
2. 新增基础设施实现前，先确认是否可以继承、组合或配置框架默认实现。
3. 若发现缺口属于通用能力，应优先补到框架，而不是在 SaaS 模块长期复制。

## Design Red Lines

1. 仓储只承载聚合持久化和少量稳定查询，不承载页面拼装逻辑。
2. 应用服务只负责用例编排、事务边界、权限校验入口，不再承担大段查询拼装或跨聚合规则。
3. 查询模型与命令模型分离；列表/树/详情等读模型统一进入 `QueryServices` 或显式的 ReadModel 组件。
4. 内部服务必须显式分层：认证会话、授权决策、租户成员解析、字段脱敏、缓存失效、种子初始化均独立为内部服务，禁止继续塞进单个 AppService。
5. 缓存只加在高收益、稳定且易失效的读取路径；禁止“先全量缓存再想失效”。
6. 缓存键、配置键、种子编码、权限码、菜单码必须统一命名规则，并形成文档约束。
7. 旧种子若不符合新实体语义，直接删除重建，不做兼容性迁移。
8. 框架级能力必须优先复用，业务模块只补充 SaaS 领域约束与编排。

## Naming Standards

### Cache Keys

统一前缀：`basicapp:saas:`

建议分层：

- 聚合快照：`basicapp:saas:aggregate:{name}:{tenantId}:{id}`
- 授权结果：`basicapp:saas:authz:{tenantId}:{userId}:{sessionId}`
- 权限集合：`basicapp:saas:permission-set:{tenantId}:{userId}`
- 菜单树：`basicapp:saas:menu-tree:{tenantId}:{userId}`
- 数据范围：`basicapp:saas:data-scope:{tenantId}:{userId}`
- 字段安全：`basicapp:saas:fls:{tenantId}:{targetType}:{targetId}:{resourceId}`
- 租户解析：`basicapp:saas:tenant-resolve:{tenantCode}`
- 配置快照：`basicapp:saas:config:{scope}:{tenantId}:{key}`

约束：

1. 键名全部小写，段间使用 `:`
2. 必须显式带 `tenantId`，平台级统一使用 `0`
3. 禁止使用页面名、前端路由名作为缓存键主体

### Config Keys

统一前缀：`BasicApp:Saas:`

建议分组：

- 认证：`BasicApp:Saas:Auth:*`
- 授权：`BasicApp:Saas:Authorization:*`
- 多租户：`BasicApp:Saas:MultiTenancy:*`
- 缓存：`BasicApp:Saas:Caching:*`
- 字段安全：`BasicApp:Saas:Fls:*`
- 种子：`BasicApp:Saas:Seed:*`

约束：

1. 配置键采用 `PascalCase` 段式命名
2. 业务字典配置与系统运行配置分离，不能混在一个 store 里随意读写
3. 运行配置必须区分平台级和租户级覆盖策略

## Repository Refactor Strategy

现状问题：

1. 仓储基本按表拆分，聚合边界弱。
2. 租户过滤、软删过滤、全局记录合并规则容易泄漏到上层。
3. 授权链涉及的组合查询缺乏统一入口。
4. 需要先识别哪些仓储基础能力本就应建立在 `XiHan.Framework` 的数据访问抽象之上。

重构原则：

1. 围绕聚合重建主仓储：`Tenant`、`User`、`Role`、`Department`、`Menu`、`Permission`
2. 关系型表仓储只保留明确职责：如 `RoleHierarchy`、`DepartmentHierarchy`、`UserSession`
3. 日志/分表仓储与业务聚合仓储严格分离
4. 所有仓储基础查询默认内置：
   - `TenantId` 过滤
   - 平台级 `TenantId = 0` 合并规则
   - 软删过滤
   - 状态过滤策略的显式参数
5. 需要复杂列表、统计、树、联表投影的读取，不继续加进仓储，统一进入 QueryService
6. 仓储基类、分页、查询扩展优先复用 `XiHan.Framework` / `XiHan.BasicApp.Core` 现有能力。

## Service Refactor Strategy

现状问题：

1. AppService 数量过多，职责边界偏 CRUD。
2. 内部规则和缓存失效逻辑容易分散在多个实现中。
3. 需要识别哪些内部服务应下沉为框架扩展点，而不是滞留在业务模块。

重构原则：

1. `AppService` 仅保留外部用例入口
2. 新增内部服务层：
   - `ITenantContextResolver`
   - `ITenantMembershipResolver`
   - `IRbacAuthorizationService`
   - `IDataScopeResolver`
   - `IFieldSecurityService`
   - `ICacheKeyNormalizer`
   - `ICacheInvalidationService`
   - `ISeedBootstrapService`
3. 命令与查询分离：
   - 写操作进入 Command/AppService
   - 读操作进入 QueryService/ReadModel
4. 认证、授权、菜单构建、数据范围、FLS 脱敏、约束规则执行都不允许散落在页面服务里直接拼
5. 通用服务外壳、事件发布、缓存访问、配置读取、租户上下文访问优先复用底层框架能力。

## Cache Strategy

框架约束：

1. 优先使用 `XiHan.Framework` 已有缓存抽象、分布式缓存接口和缓存事件能力。
2. `XiHan.BasicApp.Saas` 只定义 SaaS 领域缓存对象、缓存键规范和失效策略。
3. 不在模块内再封装一套与框架重复的缓存客户端。

只缓存以下高价值读取：

1. 租户解析结果
2. 用户权限集合与角色展开结果
3. 用户菜单树
4. 数据范围计算结果
5. 字段级安全规则聚合结果
6. 平台级基础模板数据（资源、操作、平台角色模板、平台权限模板）

不缓存以下内容：

1. 高频变更且失效复杂的编辑表单详情
2. 审计日志、操作日志、访问日志列表
3. 需要强一致的会话瞬时判断，除非已有明确失效机制

失效原则：

1. 基于领域事件失效，不基于页面刷新失效
2. 用户角色、权限、部门、租户成员变更时，必须级联失效授权缓存
3. 菜单、权限、版本套餐变更时，必须失效菜单树和权限集缓存
4. FLS 与约束规则变更时，必须失效对应目标的策略缓存

## Settings Strategy

框架约束：

1. 优先复用 `XiHan.Framework` / `XiHan.BasicApp.Core` 的配置抽象、设置存储、选项绑定能力。
2. SaaS 模块只补充本领域配置项、命名规范和覆盖规则。
3. 若 `RbacSettingStore` 不符合统一规范，应重构为基于底层统一设置模型的实现。

后续需要把设置分成三类：

1. 运行配置
   - 认证过期时间
   - 多租户隔离模式默认值
   - 缓存 TTL
   - 安全策略开关
2. 业务配置
   - 租户初始化模板选择
   - 默认角色/菜单模板
   - 审批与约束策略默认参数
3. 字典配置
   - 枚举展示与 UI 选项

约束：

1. 运行配置不能直接复用业务配置表结构乱写
2. 配置读取必须走统一 provider，不能在服务里手拼 key
3. 需要平台级默认值 + 租户级覆盖值的双层解析

## Seeder Strategy

框架约束：

1. 种子执行器、生命周期、事务控制、幂等辅助优先复用底层框架能力。
2. SaaS 模块只定义业务模板数据和初始化顺序，不重复造通用 seeder 框架。

现状问题：

1. 种子按表拆分，难以保证语义一致性
2. 旧角色、权限、菜单、部门模板可能已经不符合现实体语义

重建策略：

1. 删除不合理旧种子，按“模板域”重建：
   - 平台基础模板：资源、操作、平台权限、平台菜单、平台角色
   - 租户初始化模板：默认部门、默认角色、默认菜单、默认配置
   - 演示/测试数据：独立开关控制，不进入正式初始化链
2. 种子执行顺序固定：
   - 资源/操作
   - 权限
   - 菜单
   - 角色
   - 角色权限/菜单
   - 租户模板
   - 用户与租户成员
   - 约束规则/FLS 模板
3. 所有种子编码必须稳定，支持幂等更新
4. 种子不直接依赖旧主键，统一以编码或业务键关联

## Framework Reuse Matrix

### Reuse As-Is

| 能力域 | 直接复用类型 | SaaS 侧约束 |
|------|-------------|------------|
| 应用服务基类 | `XiHan.Framework.Application.Services.ApplicationServiceBase` | SaaS 的 AppService 仅作为用例编排入口，不再自建通用 service base |
| 多租户聚合/实体基类 | `XiHan.Framework.Data.SqlSugar.Aggregates.SugarMultiTenantAggregateRoot`、`XiHan.Framework.Data.SqlSugar.Entities.SugarMultiTenant*` | 通过 `XiHan.BasicApp.Core.Entities.BasicAppEntity`、`BasicAppAggregateRoot` 统一继承，继续执行 `TenantId=0` 平台记录约定 |
| SqlSugar 聚合仓储基类 | `XiHan.Framework.Data.SqlSugar.Repository.SqlSugarAggregateRepository`、`SqlSugarSoftDeleteRepository` | SaaS 仓储只补业务过滤与稳定查询，不重写通用 CRUD/UoW 行为 |
| 工作单元与事件发布 | `XiHan.Framework.Uow.UnitOfWork`、`XiHan.Framework.EventBus.Abstractions.IEventBus` | 领域事件、缓存失效、初始化链都走 UoW 提交后发布，不做手工事务拼接 |
| 多租户上下文 | `XiHan.Framework.MultiTenancy.CurrentTenant`、`XiHan.Framework.MultiTenancy.Abstractions.ICurrentTenant` | Tenant 解析、作用域切换、跨租户受控操作统一基于可信上下文，不从页面参数读鉴权租户 |
| 缓存客户端 | `XiHan.Framework.Caching.Hybrid.XiHanHybridCache` | SaaS 只定义缓存项、键规范、TTL 与失效策略，不重封装第二套缓存客户端 |
| 设置读取 | `XiHan.Framework.Settings.SettingManager`、`XiHan.Framework.Settings.Stores.ISettingStore` | 统一走 provider/store 链读取平台级与租户级配置，不在服务中散落配置访问 |
| 种子基础设施 | `XiHan.Framework.Data.SqlSugar.Seeders.IDataSeeder`、`DataSeederBase` | SaaS 仅组织模板域、顺序与业务键，不重复实现 seeder pipeline |
| 查询服务标记与实体约定 | `XiHan.BasicApp.Core.Queries.IQueryService`、`XiHan.BasicApp.Core.Entities.BasicAppEntity` | QueryService 与实体层沿用 Core 约定，SaaS 不再重复声明同类标记接口和实体基类 |

### Reuse With Extension

| 能力域 | 复用基础 | SaaS 扩展方式 | 约束 |
|------|----------|--------------|------|
| 聚合仓储 | `SqlSugarAggregateRepository` / `SqlSugarSoftDeleteRepository` | 增加平台模板合并、显式状态过滤、业务唯一键查询 | 只补稳定查询；复杂投影继续放 QueryService |
| 设置存储 | `ISettingStore` / `SettingManager` | 建立 `BasicApp:Saas:*` 配置定义、平台默认值和租户覆盖策略 | 不再保留孤立的旧 `RbacSettingStore` 语义 |
| 缓存失效 | `XiHanHybridCache` + UoW/EventBus | 基于领域事件建立 `permission/menu/data-scope/fls` 失效编排 | 失效服务只编排键，不直接承担业务授权计算 |
| 多租户解析 | `ICurrentTenant` / `ITenantStore` | 增加 SaaS 的租户成员校验、目标租户切换规则 | 解析规则属于业务域，但上下文承载仍由框架负责 |
| 数据种子 | `IDataSeeder` / `DataSeederBase` | 重建平台模板、租户模板、演示数据三层 seeder | 幂等关联统一用业务编码，不依赖历史主键 |

### Must Stay In SaaS Domain

| 领域能力 | 保留在 SaaS 的原因 |
|---------|------------------|
| `ITenantMembershipResolver`、用户可访问租户集合解析 | 属于 `SysUser` 与租户成员模型的业务语义，不是通用框架能力 |
| `IRbacAuthorizationService`、角色继承展开、权限集归并 | 与 `SysRole`、`SysPermission`、`SysMenu` 的领域模型强绑定 |
| `IDataScopeResolver`、部门闭包/自定义范围计算 | 与 `SysDepartment`、`RoleDataScope` 直接相关，不能下沉为通用框架默认实现 |
| `IFieldSecurityService`、FLS 出口脱敏策略 | 依赖 `SysFieldLevelSecurity`、目标类型和资源模型，是业务级安全策略 |
| `IConstraintRuleExecutor` 或等价规则执行服务 | 约束规则算子、资源绑定和冲突合并属于 SaaS ABAC 语义 |
| SaaS 平台模板/租户模板 seed 数据 | 这是业务初始化内容，不属于框架通用数据 |

### Explicit No-Go

1. 不在 `XiHan.BasicApp.Saas` 再新增通用 `UnitOfWork`、`RepositoryBase`、`CacheManager`、`SettingManager` 平替。
2. 不把 `Role`、`Permission`、`Department`、`ConstraintRule` 等业务聚合硬下沉到 `XiHan.Framework`。
3. 不在前后端继续硬编码租户隔离、权限拼装、字段脱敏规则来绕开底层抽象。
4. 不围绕旧表级 Seeder、旧缓存键、旧配置键保留兼容层。

## Batch-03 Deliverables

1. 建立框架与 Core 复用矩阵，明确“直接复用 / 扩展复用 / 必须留在 SaaS 域”的边界。
2. 明确后续 Batch-04 至 Batch-07 的仓储、服务、缓存、设置、种子设计必须依附的底层抽象。
3. 明确禁止重复实现清单，作为后续代码评审红线。
4. 将 Batch-03 的验证方式限定为“方案自检 + 真实类型存在性核对”，避免在未进入实现阶段前伪造构建完成状态。

## Batches

| Batch | 标题 | 范围 | 完成标准 | 风险重点 |
|------|------|------|----------|----------|
| Batch-01 | 方案与第一批契约基线 | 方案文档、`User/Tenant/Menu/ConstraintRule` DTO 与前端 API、受影响页面 | 建立可追踪重构面板；首批核心契约不再依赖旧字段语义 | 字段命名漂移、枚举值误配、前端归一化逻辑失真 |
| Batch-02 | 第二批契约基线 | `Role/Permission/Department` DTO、前端 API、枚举映射 | RBAC 主链剩余契约按实体语义统一 | 旧数值语义残留、树形字段含义不一致 |
| Batch-03 | 框架能力盘点与复用基线 | `XiHan.Framework`、`XiHan.BasicApp.Core`、SaaS 依赖点 | 明确哪些基础能力必须复用、哪些缺口需要补到底层 | 错把业务逻辑下沉到底层，或继续重复造轮子 |
| Batch-04 | 仓储与查询层重建 | `Infrastructure/Repositories`、`QueryServices`、ReadModel | 聚合仓储、关系仓储、查询服务边界清晰，租户/软删/平台模板规则统一下沉 | 查询边界错划、越权过滤缺失 |
| Batch-05 | 服务与内部服务重建 | `AppServices`、内部领域服务、授权/租户/缓存内部服务 | AppService 只负责编排，内部服务职责稳定可复用 | 逻辑迁移不完整、事务边界破碎 |
| Batch-06 | 缓存与配置体系重建 | cache services、cache items、settings、cache/config key 规范 | 缓存键、配置键、TTL、失效链统一，移除旧键 | 脏缓存、错失效、配置漂移 |
| Batch-07 | 种子数据与初始化链重建 | `Seeders`、模板数据、初始化流程 | 旧不合理种子移除，平台模板与租户模板重建 | 旧主键依赖、初始化非幂等 |
| Batch-08 | 用户/租户主链重建 | `User`、`Tenant`、`Auth`、`UserSession`、相关页面 | 登录/会话支持目标租户上下文，用户维护支持成员租户、角色、部门、权限 | 越权访问、租户上下文错绑、删除/禁用状态遗漏 |
| Batch-09 | RBAC 主链重建 | `Role`、`Permission`、`Menu`、`Department`、系统页 | 角色继承、菜单绑定权限、数据范围、部门树闭包语义前后端一致 | 菜单权限错绑、数据范围放大、树结构坏链 |
| Batch-10 | ABAC/FLS 重建 | `ConstraintRule`、`FieldLevelSecurity`、授权出口、页面 | 约束规则、字段脱敏、可编辑性规则能被完整维护和执行 | 脱敏下沉位置错误、规则冲突合并错误 |
| Batch-11 | 前端整体验证与交互重建 | `frontend/src/views/system/**`、路由与交互 | 系统管理页面按新模型可用，页面不再依赖旧字段 | 页面状态错乱、批量编辑丢字段、树与列表联动错误 |
| Batch-12 | 质量门禁与收尾 | 后端构建、前端 type-check、回归清单、文档 | 关键构建通过，输出验证结果、风险余项、回滚建议 | 全局类型错误挡住 SaaS 验证、未覆盖的边界场景 |

## Requirement Table

| ID | 标题 | 范围 | 完成标准 | 依赖 |
|----|------|------|----------|------|
| REQ-001 | 重建核心契约层 | Dto、API modules、枚举映射 | DTO 与前端模型准确反映实体含义 | — |
| REQ-002 | 建立底层框架复用基线 | `XiHan.Framework`、`XiHan.BasicApp.Core`、SaaS 依赖点 | 明确复用清单、扩展点清单、禁止重复实现清单 | REQ-001 |
| REQ-003 | 重建仓储与查询边界 | Repositories、QueryServices、ReadModel | 聚合边界、租户过滤、平台模板规则统一下沉 | REQ-001, REQ-002 |
| REQ-004 | 重建服务与内部服务层 | AppServices、内部服务、事务边界 | AppService 编排化、内部能力服务化 | REQ-001, REQ-002, REQ-003 |
| REQ-005 | 重建缓存与配置体系 | Cache、Settings、统一 key 规范 | 缓存策略、缓存键、配置键、失效链统一 | REQ-002, REQ-003, REQ-004 |
| REQ-006 | 重建种子数据与初始化模板 | Seeders、模板数据、初始化流程 | 旧种子移除，新模板幂等且符合实体语义 | REQ-001, REQ-002, REQ-003, REQ-004 |
| REQ-007 | 重建用户与租户主链 | Auth/User/Tenant/UserSession | 用户和租户上下文语义正确落地 | REQ-001, REQ-002, REQ-003, REQ-004, REQ-005, REQ-006 |
| REQ-008 | 重建 RBAC 主链 | Role/Permission/Menu/Department | 授权链、菜单树、数据范围统一 | REQ-001, REQ-002, REQ-003, REQ-004, REQ-005, REQ-006 |
| REQ-009 | 重建轻量 ABAC 与 FLS | ConstraintRule/FLS/授权出口 | ABAC 与字段级安全具备完整能力 | REQ-001, REQ-002, REQ-003, REQ-004, REQ-005, REQ-006, REQ-008 |
| REQ-010 | 重建前端系统页 | `frontend/src/views/system/**` | 管理页面按新模型完成联调 | REQ-001, REQ-007, REQ-008, REQ-009 |
| REQ-011 | 完成质量门禁与交付 | 构建、类型检查、回归、文档 | 可验证、可回滚、结果可追踪 | REQ-001, REQ-002, REQ-003, REQ-004, REQ-005, REQ-006, REQ-007, REQ-008, REQ-009, REQ-010 |

## Batch-04 Breakdown

| Sub-Batch | 标题 | 范围 | 完成标准 |
|-----------|------|------|----------|
| Batch-04A | 核心授权聚合读写边界基线 | `Role/Permission/Department` 仓储、QueryService、ReadModel mapper | 多租户过滤规则统一，AppService 的只读查询下沉到 QueryService，读模型映射从服务层拼装中抽离 |
| Batch-04B | 剩余主链查询与仓储收口 | `User/Tenant/Menu/ConstraintRule` 等剩余主链 | 剩余主链完成同样的边界收敛，避免继续在 AppService 里直连仓储拼读模型 |

## Batch-05 Breakdown

| Sub-Batch | 标题 | 范围 | 完成标准 |
|-----------|------|------|----------|
| Batch-05A | RBAC 变更通知与超管保护收敛 | `User/Role/Permission/Menu/Department` AppService、`Application/InternalServices` | 授权变更通知统一走内部服务，超管保护规则从用户服务抽离，AppService 只保留事务编排 |
| Batch-05B | 授权编排与租户成员解析收敛 | `AuthAppService`、授权/租户内部服务 | 登录后授权装配、菜单/权限/数据范围解析从 AppService 继续拆出稳定内部服务 |

## Execution Rules

1. 每个 Batch 完成后立即更新本文件状态。
2. 每个 Batch 完成后执行本地验证，并记录结果。
3. 每个 Batch 完成后执行一次本地 `git commit`，不 push。
4. 若出现跨 Batch 的阻塞问题，先在当前 Batch 标记风险，再拆出后续修复，不把无关改动混入同一提交。

## Batch Status

- [x] Batch-01 方案与第一批契约基线
- [x] Batch-02 第二批契约基线
- [x] Batch-03 框架能力盘点与复用基线
- [x] Batch-04 仓储与查询层重建
- [x] Batch-05 服务与内部服务重建
- [ ] Batch-06 缓存与配置体系重建
- [ ] Batch-07 种子数据与初始化链重建
- [ ] Batch-08 用户/租户主链重建
- [ ] Batch-09 RBAC 主链重建
- [ ] Batch-10 ABAC/FLS 重建
- [ ] Batch-11 前端系统页重建与联调
- [ ] Batch-12 质量门禁与收尾

### Batch-04 Sub-Status

- [x] Batch-04A 核心授权聚合读写边界基线
- [x] Batch-04B 剩余主链查询与仓储收口

### Batch-05 Sub-Status

- [x] Batch-05A RBAC 变更通知与超管保护收敛
- [x] Batch-05B 授权编排与租户成员解析收敛

## Validation Template

| Batch | Validation | Result | Notes |
|------|------------|--------|-------|
| Batch-01 | `dotnet build` / `pnpm type-check` / 契约自检 | Partial | `dotnet build` 当前只返回“生成失败，0 错误”；`pnpm type-check` 仍被全局历史错误阻塞，但本批引入的 `tenant/constraint-rule` 页面类型错误已收口 |
| Batch-02 | 前端定向 `eslint` / 契约自检 | Partial | `Role/Permission/Department` DTO、前端 API 与系统页已按实体语义对齐；已消除本批新增静态错误。定向 `eslint` 仅剩这些文件中已有的 `ts/no-explicit-any` warning；未执行全量 `pnpm type-check`，因仓库其他历史错误仍会阻塞结论 |
| Batch-03 | 方案自检 / 底层类型存在性核对 | Passed | 已核对 `ApplicationServiceBase`、`SugarMultiTenantAggregateRoot`、`SqlSugarAggregateRepository`、`XiHanHybridCache`、`SettingManager`、`IDataSeeder`、`CurrentTenant`、`ITenantStore` 以及 `BasicAppEntity`、`IQueryService` 的真实路径，复用矩阵已落入方案 |
| Batch-04 | 代码事实检索 / `dotnet build` 定向验证 | Partial | `Role/Permission/Department/User/Tenant/Menu/ConstraintRule` 的对外只读查询已下沉到 QueryService，核心仓储租户过滤规则已统一收敛；`dotnet build` 仍被本机 .NET SDK workload 解析异常阻塞，未获得可信的业务编译成功信号 |
| Batch-05 | 代码事实检索 / `dotnet build` 定向验证 | Partial | Batch-05 已完成两段收敛：05A 将 `User/Role/Permission/Menu/Department` 的授权变更通知统一收敛到 `IRbacChangeNotifier`，并将 superadmin 保护规则收敛到 `ISuperAdminGuard`；05B 进一步将 `AuthAppService` 的当前用户解析、角色码展开、权限集/菜单树/数据范围装配收敛到 `IAuthorizationContextService`，同时复用既有 `IUserManager.ResolveDefaultRoleIdAsync`。`dotnet build` 仍被本机 .NET SDK workload 解析异常阻塞，仅输出“生成失败，0 个警告 0 个错误”，暂不能作为可信业务编译结论 |
| Batch-06 | `dotnet build` / 种子初始化自检 | Pending |  |
| Batch-07 | `dotnet build` / 认证链自检 | Pending |  |
| Batch-08 | `dotnet build` / 授权链自检 | Pending |  |
| Batch-09 | `dotnet build` / 安全出口自检 | Pending |  |
| Batch-10 | `pnpm type-check` / 页面联调自检 | Pending |  |
| Batch-11 | 汇总验证 | Pending |  |

### Batch-04 Sub-Validation

| Sub-Batch | Validation | Result | Notes |
|-----------|------------|--------|-------|
| Batch-04A | 代码事实检索 / AppService 读仓储残留扫描 / `dotnet build` 定向验证 | Partial | `Role/Permission/Department` 已建立统一租户过滤辅助器、读模型 mapper，并把只读查询下沉到 QueryService；针对性检索已确认三个 AppService 不再直连对应只读仓储方法。`dotnet build` 仍被本机 .NET SDK workload 解析异常阻塞，失败原因为 `MSB4276` 解析 `Microsoft.NET.SDK.WorkloadAutoImportPropsLocator` 和 `Microsoft.NET.SDK.WorkloadManifestTargetsLocator` 时目录缺失，不属于本批业务代码编译错误信号 |
| Batch-04B | 代码事实检索 / AppService 读仓储残留扫描 / `dotnet build` 定向验证 | Partial | `User/Tenant/Menu/ConstraintRule` 的对外只读入口已下沉到 QueryService；AppService 残留的仓储读取仅用于写路径内部校验与映射。`dotnet build` 仍受同一 `MSB4276` SDK workload 环境问题阻塞，未暴露新的业务代码编译错误信息 |

### Batch-05 Sub-Validation

| Sub-Batch | Validation | Result | Notes |
|-----------|------------|--------|-------|
| Batch-05A | 代码事实检索 / 重复逻辑残留扫描 / `dotnet build` 定向验证 | Partial | `User/Role/Permission/Menu/Department` 已不再保留本地 `PublishAuthorizationChangedEventAsync` 和用户服务内联的 superadmin 判定逻辑，统一改为调用 `IRbacChangeNotifier` 与 `ISuperAdminGuard`；新增内部服务实现遵循 `XiHan.Framework` 的 scoped dependency 注册约定。`dotnet build` 仍受本机 SDK workload 异常影响，只输出“生成失败，0 个警告 0 个错误”，未提供可信业务编译结论 |
| Batch-05B | 代码事实检索 / 授权装配残留扫描 / `dotnet build` 定向验证 | Partial | `AuthAppService` 已不再直连角色层级仓储、菜单仓储和授权缓存细节；当前用户解析、角色码展开、权限集/菜单树/数据范围装配统一收敛到 `IAuthorizationContextService`，注册默认角色解析复用 `IUserManager.ResolveDefaultRoleIdAsync`。`dotnet build` 仍受本机 SDK workload 异常影响，只输出“生成失败，0 个警告 0 个错误”，未提供可信业务编译结论 |
