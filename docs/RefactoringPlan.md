# XiHan.BasicApp 全栈重构优化计划

> 版本：v1.0 | 日期：2026-04-26 | 状态：待实施
>
> 本计划基于 XiHan.Framework 底层修复完成后，对 BasicApp 进行自底向上的全面重构。
> 严格遵循 skill 规范中的 8 层执行顺序。

---

## 一、现状分析

### 1.1 项目规模

| 维度 | 数量 |
|------|------|
| 后端实体 | 56 个 |
| 聚合根实体 | 19 个（其中 9 个需降级） |
| FullAudited 实体 | 11 个 |
| CreationEntity 实体 | 26 个 |
| 领域枚举 | 54 个（含 1 个需替换的 YesOrNo） |
| 仓储接口 | 27 个 |
| 应用服务 | 34 个 |
| 领域服务 | 15 个 |
| 领域事件 | 20 个 |
| 值对象 | 8 个 |
| 规约 | 7 个 |
| 前端 API 模块 | 33 个 |
| 前端页面 | 30 个 |

### 1.2 已识别的关键问题

#### 聚合根滥用（9 个实体需降级）

| 实体 | 当前基类 | 应降级为 | 原因 |
|------|----------|----------|------|
| `SysFieldLevelSecurity` | `BasicAppAggregateRoot` | `BasicAppFullAuditedEntity` | 无独立不变量，RBAC 策略子实体 |
| `SysConstraintRule` | `BasicAppAggregateRoot` | `BasicAppFullAuditedEntity` | RBAC 策略子实体 |
| `SysNotification` | `BasicAppAggregateRoot` | `BasicAppFullAuditedEntity` | 简单消息实体 |
| `SysEmail` | `BasicAppAggregateRoot` | `BasicAppCreationEntity` | 只写不改的发送记录 |
| `SysSms` | `BasicAppAggregateRoot` | `BasicAppCreationEntity` | 只写不改的发送记录 |
| `SysFile` | `BasicAppAggregateRoot` | `BasicAppFullAuditedEntity` | 文件元数据无复杂不变量 |
| `SysDict` | `BasicAppAggregateRoot` | `BasicAppFullAuditedEntity` | 简单 CRUD |
| `SysConfig` | `BasicAppAggregateRoot` | `BasicAppFullAuditedEntity` | 简单 CRUD |
| `SysUserSession` | `BasicAppAggregateRoot` | `BasicAppFullAuditedEntity` | 会话记录（有 Aggregate 行为但无领域事件需求） |

#### 保留为聚合根的实体（10 个）

`SysUser`、`SysTenant`、`SysRole`、`SysPermission`、`SysResource`、`SysOperation`、`SysDepartment`、`SysOAuthApp`、`SysTask`、`SysReview`

#### YesOrNo 枚举泛滥

`YesOrNo` 枚举在 40+ 个实体的 `Status` 字段中使用，语义模糊。需按实体场景替换为独立枚举。

#### SysUser 导航属性过多

`SysUser.pl.cs` 包含 24 个导航属性，包括日志、通知、邮件、短信、OAuth、统计等。
需精简为核心导航：`Security`（1:1）、`UserRoles`、`UserPermissions`、`UserDepartments`。

---

## 二、前置条件

本计划依赖 XiHan.Framework 以下修复完成：

- [F-0.1] MultiTenantAggregateRootBase TenantId 可空修复
- [F-0.2] CrudApplicationServiceBase 硬删除修复
- [F-0.3] SqlSugarAggregateRepository 无 UoW 崩溃修复
- [F-1.2] ISoftDelete 接口链验证
- [F-1.3] IMultiTenant TenantId=0 平台语义

---

## 三、重构任务清单（8 层自底向上）

### 第 2 层：应用内核优化（XiHan.BasicApp.Core）

#### 任务 B-2.1：BasicAppEntity 基类注释完善

- **涉及文件**：`backend/src/framework/XiHan.BasicApp.Core/Entities/BasicAppEntity.cs`
- **具体工作**：
  1. 为每个基类添加适用场景的 `<remarks>` 注释
  2. 明确聚合根判定三条件（不变量、事务边界、领域事件）
  3. 添加示例：关联表用 `BasicAppCreationEntity`，日志用 `BasicAppCreationEntity + ISplitTableEntity`
- **验证标准**：XML 文档生成无警告

#### 任务 B-2.2：公共值对象定义

- **涉及文件**：`backend/src/modules/XiHan.BasicApp.Saas/Domain/ValueObjects/`
- **具体工作**：
  1. 审查现有 8 个值对象（EmailValueObject、IpAddressValueObject 等）
  2. 补充 skill 规范中定义的公共值对象：
     - `ClientInfo(Ip, Location, UserAgent, Browser, Os)` — 替代日志实体中重复的客户端字段
     - `EffectivePeriod(EffectiveTime, ExpirationTime)` — 替代权限/委托中的生效时间对
     - `BusinessReference(BusinessType, BusinessId)` — 替代通知等实体中的业务关联字段
     - `DeviceInfo(DeviceType, DeviceName, DeviceId, Os, Browser)` — 替代会话中的设备字段
  3. 使用 `record` 类型定义，配合 `[SugarColumn(IsJson = true)]` 映射
- **前置依赖**：Framework F-1.4（值对象映射支持）
- **验证标准**：值对象可正确序列化到 JSON 列

---

### 第 3 层：领域实体重构（Domain/Entities）

#### 任务 B-3.1：聚合根降级（9 个实体）

- **涉及文件**：每个实体的主文件 + Aggregates/ 分部类 + Partials/ 分部类
- **具体工作**：

  **降级为 `BasicAppFullAuditedEntity`（7 个）：**
  1. `SysFieldLevelSecurity` — 改基类，移除 Aggregate 分部类中的领域事件发布
  2. `SysConstraintRule` — 同上
  3. `SysNotification` — 同上（保留 Aggregate 中的业务方法，迁移到主文件或 Expand）
  4. `SysFile` — 同上
  5. `SysDict` — 同上
  6. `SysConfig` — 同上
  7. `SysUserSession` — 同上（保留 Aggregate 中的会话管理方法）

  **降级为 `BasicAppCreationEntity`（2 个）：**
  8. `SysEmail` — 只写不改的发送记录，移除 Aggregate 分部类
  9. `SysSms` — 同上

- **每个实体的操作步骤**：
  1. 修改主文件中的基类声明
  2. 将 Aggregate 分部类中的业务方法迁移到主文件或删除
  3. 删除空的 Aggregate 分部类文件
  4. 更新对应的仓储接口（从 `IAggregateRootRepository` 改为 `ISoftDeleteRepositoryBase` 或 `IRepositoryBase`）
  5. 更新对应的仓储实现
  6. 验证编译通过
- **前置依赖**：B-2.1
- **验证标准**：`dotnet build` 通过；降级实体不再有 Aggregate 分部类

#### 任务 B-3.2：YesOrNo 枚举替换

- **涉及文件**：40+ 个实体的 `Status` 字段 + `Domain/Enums/YesOrNo.cs`
- **具体工作**：
  1. 定义语义明确的替换枚举：

  ```
  EnableStatus        → SysUser, SysRole, SysMenu, SysDepartment, SysDict, SysDictItem,
                         SysConfig, SysConstraintRule, SysFieldLevelSecurity, SysOAuthApp,
                         SysOperation, SysResource, SysTask, SysTenantEdition
  ValidityStatus      → SysRolePermission, SysUserPermission, SysUserRole, SysUserDepartment,
                         SysRoleDataScope, SysUserDataScope, SysTenantEditionPermission,
                         SysTenantUser, SysPermissionCondition
  ReviewStatus        → SysReview（替代 YesOrNo Status）
  TokenStatus         → SysOAuthToken
  OperationLogStatus  → SysOperationLog
  ```

  2. 为每个新枚举添加 `[Description("中文描述")]` 特性
  3. 逐实体替换 `YesOrNo Status` 为对应枚举
  4. 同步更新 Aggregate 分部类中的 `Enable()`/`Disable()` 方法
  5. 同步更新 DTO、Mapper、前端类型定义
  6. 最后删除 `YesOrNo.cs`
- **前置依赖**：B-3.1
- **验证标准**：`rg "YesOrNo" backend/` 返回 0 结果

#### 任务 B-3.3：SysUser 导航属性精简

- **涉及文件**：`Domain/Entities/Partials/SysUser.pl.cs`
- **具体工作**：
  1. 保留核心导航属性（4 个）：
     - `Security` (OneToOne → SysUserSecurity)
     - `UserRoles` (OneToMany → SysUserRole)
     - `UserPermissions` (OneToMany → SysUserPermission)
     - `UserDepartments` (OneToMany → SysUserDepartment)
  2. 移除所有日志、通知、邮件、短信、文件、OAuth、统计等导航属性（20 个）：
     - `LoginLogs`, `UploadedFiles`, `OperationLogs`, `UserNotifications`, `SentNotifications`
     - `OAuthCodes`, `OAuthTokens`, `SentEmails`, `ReceivedEmails`, `SentSms`, `ReceivedSms`
     - `UserStatistics`, `SubmittedReviews`, `CurrentReviews`, `ReviewLogs`
     - `AuditLogs`, `AccessLogs`, `ApiLogs`, `UserSessions`
     - `Roles` (ManyToMany 冗余), `Departments` (ManyToMany 冗余)
  3. 这些关联数据改为通过各自领域的仓储按需查询
- **前置依赖**：B-3.1
- **验证标准**：`SysUser.pl.cs` 只有 4 个导航属性；编译通过

#### 任务 B-3.4：SysPermission 灵活化

- **涉及文件**：`Domain/Entities/SysPermission.cs` + Partials
- **具体工作**：
  1. 将 `ResourceId` 和 `OperationId` 改为可空 `long?`
  2. 新增 `PermissionType` 枚举（ResourceBased=0, Functional=1, DataScope=2）
  3. 添加 `PermissionType` 字段到 SysPermission
  4. 更新索引：添加 `IX_{table}_TeId_PeTy` 索引
  5. 更新仓储接口和实现
- **前置依赖**：B-3.2
- **验证标准**：编译通过；SysPermission 支持三种权限类型

#### 任务 B-3.5：SysFileStorage 精简

- **涉及文件**：`Domain/Entities/SysFileStorage.cs` + Partials
- **具体工作**：
  1. 移除运行时生成字段：`SignedUrl`、`SignedUrlExpiresAt`
  2. 移除可推导字段：`StorageDirectory`（由 `StoragePath` 推导）
  3. 移除配置关联字段：`Endpoint`、`CustomDomain`（从存储配置获取）
  4. 更新 DTO 和前端类型
- **前置依赖**：B-3.1
- **验证标准**：编译通过；SysFileStorage 字段精简

#### 任务 B-3.6：SysTenant 补充导航

- **涉及文件**：`Domain/Entities/Partials/SysTenant.pl.cs`
- **具体工作**：
  1. 添加 `TenantUsers` 导航属性指向 `SysTenantUser`
  2. 确认 `[Navigate]` 配置正确
- **前置依赖**：B-3.1
- **验证标准**：编译通过

#### 任务 B-3.7：日志实体公共字段提取

- **涉及文件**：所有日志实体（SysAccessLog, SysApiLog, SysAuditLog, SysExceptionLog, SysLoginLog, SysOperationLog）
- **具体工作**：
  1. 定义 `RequestContext` 值对象（Ip, Location, UserAgent, Browser, Os, TraceId）
  2. 将各日志实体中重复的客户端信息字段替换为 `[SugarColumn(IsJson = true)] RequestContext`
  3. 或保持展开字段但统一命名和类型
- **前置依赖**：B-2.2
- **验证标准**：日志实体客户端字段统一

---

### 第 4 层：领域层完善（Domain/）

#### 任务 B-4.1：仓储接口调整

- **涉及文件**：`Domain/Repositories/` 全部 27 个接口
- **具体工作**：
  1. 降级实体的仓储接口从 `IAggregateRootRepository` 改为对应级别：
     - `SysFieldLevelSecurity` → `ISoftDeleteRepositoryBase<SysFieldLevelSecurity, long>`
     - `SysConstraintRule` → `ISoftDeleteRepositoryBase`
     - `SysNotification` → `ISoftDeleteRepositoryBase`
     - `SysFile` → `ISoftDeleteRepositoryBase`
     - `SysDict` → `ISoftDeleteRepositoryBase`
     - `SysConfig` → `ISoftDeleteRepositoryBase`
     - `SysUserSession` → `ISoftDeleteRepositoryBase`
     - `SysEmail` → `IRepositoryBase<SysEmail, long>`
     - `SysSms` → `IRepositoryBase<SysSms, long>`
  2. 保留聚合根仓储接口不变
  3. 确认方法命名规范：`GetByXxxAsync`、`FindByXxxAsync`、`ExistsXxxAsync`
- **前置依赖**：B-3.1
- **验证标准**：编译通过；仓储接口与实体基类匹配

#### 任务 B-4.2：领域服务审查

- **涉及文件**：`Domain/DomainServices/` 全部 15 个服务
- **具体工作**：
  1. 审查每个领域服务是否真正包含跨聚合业务规则
  2. 如果只是简单的 CRUD 委托，考虑合并到应用服务
  3. 确认领域服务不直接依赖基础设施（仓储除外）
  4. 确认领域服务通过构造函数注入仓储接口
- **前置依赖**：B-4.1
- **验证标准**：领域服务职责清晰

#### 任务 B-4.3：领域事件审查

- **涉及文件**：`Domain/Events/` 全部 20 个事件
- **具体工作**：
  1. 确认每个领域事件有对应的事件处理器
  2. 移除降级实体不再需要的领域事件（如 SysDict/SysConfig 的变更事件可保留用于缓存失效）
  3. 确认事件命名规范：`{Entity}{Action}DomainEvent`
- **前置依赖**：B-4.1
- **验证标准**：每个领域事件有处理器；无孤儿事件

---

### 第 5 层：基础设施层（Infrastructure/）

#### 任务 B-5.1：仓储实现调整

- **涉及文件**：`Infrastructure/Repositories/` 全部 27 个实现
- **具体工作**：
  1. 降级实体的仓储实现从 `SqlSugarAggregateRepository` 改为对应级别：
     - FullAudited 实体 → `SqlSugarSoftDeleteRepository<TEntity, long>`
     - Creation 实体 → `SqlSugarRepositoryBase<TEntity, long>`
  2. 保留聚合根仓储实现不变
  3. 确认分表仓储（Split）继承 `SqlSugarSplitRepository<TEntity>`
- **前置依赖**：B-4.1
- **验证标准**：编译通过；仓储实现与接口匹配

#### 任务 B-5.2：认证授权基础设施审查

- **涉及文件**：`Infrastructure/Authentication/` + `Infrastructure/Authorization/`
- **具体工作**：
  1. 确认 `RbacUserStore` 正确实现用户身份验证
  2. 确认 `RbacExternalLoginStore` 正确处理第三方登录
  3. 确认授权评估器与新的权限模型（PermissionType 三类型）兼容
  4. 确认 ABAC 策略评估器可处理 JSON 策略表达式
- **前置依赖**：B-3.4
- **验证标准**：认证授权流程完整

#### 任务 B-5.3：多租户适配验证

- **涉及文件**：`Infrastructure/MultiTenancy/`
- **具体工作**：
  1. 确认租户解析中间件正确设置 `ICurrentTenant`
  2. 确认 `TenantId = 0` 平台语义在基础设施层正确处理
  3. 确认跨租户操作使用 `CreateNoTenantQueryable()` 并记录审计日志
- **前置依赖**：Framework F-1.3
- **验证标准**：多租户隔离正确

---

### 第 6 层：应用服务层（Application/）

#### 任务 B-6.1：AppService 基类调整

- **涉及文件**：`Application/AppServices/Implementations/` 全部 34 个服务
- **具体工作**：
  1. 降级实体的 AppService 确认使用正确的仓储类型
  2. 确认所有 AppService 标注 `[DynamicApi(Group = "BasicApp.Saas")]`
  3. 确认所有 AppService 默认 `[Authorize]`
  4. 确认方法命名符合 DynamicApi 前缀映射规则
  5. 扫描确认无 Controller 类
- **前置依赖**：B-5.1
- **验证标准**：`rg "class.*Controller" backend/` 返回 0 结果

#### 任务 B-6.2：DTO 与枚举同步

- **涉及文件**：`Application/Dtos/` 全部 35 个 DTO 文件
- **具体工作**：
  1. 将所有 DTO 中的 `YesOrNo Status` 替换为对应的语义枚举
  2. 移除 SysFileStorage DTO 中已删除的字段
  3. 添加 SysPermission DTO 中新增的 `PermissionType` 字段
  4. 确认敏感字段（Password、Token、Secret、ConnectionString）不出现在响应 DTO 中
- **前置依赖**：B-3.2, B-3.4, B-3.5
- **验证标准**：DTO 与实体字段一致；无敏感字段泄露

#### 任务 B-6.3：Mapper 更新

- **涉及文件**：`Application/Mappers/`
- **具体工作**：
  1. 更新所有 Mapster 映射配置以匹配新的枚举类型
  2. 添加新增字段的映射规则
  3. 移除已删除字段的映射规则
- **前置依赖**：B-6.2
- **验证标准**：编译通过；映射测试通过

#### 任务 B-6.4：缓存策略审查

- **涉及文件**：`Application/Caching/` + `Application/EventHandlers/`
- **具体工作**：
  1. 确认降级实体的缓存失效事件处理器仍然正确工作
  2. 确认权限/角色/菜单/部门/FLS 变更后缓存正确失效
  3. 确认缓存 key 命名与新的枚举类型兼容
- **前置依赖**：B-6.3
- **验证标准**：缓存失效链路完整

---

### 第 7 层：前端 API 层（frontend/src/api/）

#### 任务 B-7.1：类型定义更新

- **涉及文件**：`frontend/src/api/modules/` 全部 33 个模块
- **具体工作**：
  1. 将所有 `YesOrNo` 类型引用替换为对应的语义枚举类型
  2. 更新 SysPermission 类型添加 `permissionType` 字段
  3. 移除 SysFileStorage 类型中已删除的字段
  4. 确认所有类型与后端 DTO 一一对应
  5. 删除旧兼容字段，不做静默映射
- **前置依赖**：B-6.2
- **验证标准**：`pnpm type-check` 通过

#### 任务 B-7.2：API 函数更新

- **涉及文件**：`frontend/src/api/modules/` 中的 API 函数文件
- **具体工作**：
  1. 确认 API 路径与 DynamicApi 生成的路由一致
  2. 更新枚举映射（如 `STATUS_MAP`）使用新枚举值
  3. 确认 `useBaseApi()` CRUD 工厂正确使用
- **前置依赖**：B-7.1
- **验证标准**：`pnpm type-check` 通过

---

### 第 8 层：前端页面层（frontend/src/views/）

#### 任务 B-8.1：页面组件更新

- **涉及文件**：`frontend/src/views/system/` 全部 24 个子目录
- **具体工作**：
  1. 更新所有使用 `YesOrNo` 的状态显示/筛选组件
  2. 更新表格列定义中的枚举渲染
  3. 更新表单中的枚举选择器
  4. 确认 VxeTable 和 NForm 使用规范
  5. 移除 `query-builder/` 空目录或补充实现
- **前置依赖**：B-7.2
- **验证标准**：`pnpm build` 通过

#### 任务 B-8.2：路由与权限适配

- **涉及文件**：`frontend/src/router/routes/index.ts`
- **具体工作**：
  1. 确认静态路由与后端菜单结构一致
  2. 确认动态路由逻辑适配新权限模型
  3. 确认路由守卫与后端权限校验双重保障
- **前置依赖**：B-8.1
- **验证标准**：路由正确加载

---

## 四、实施优先级与时间线

| 阶段 | 任务范围 | 预估工时 | 前置条件 |
|------|----------|----------|----------|
| 阶段 1 | B-2.1 ~ B-2.2（内核优化） | 6h | Framework F-1.4 |
| 阶段 2 | B-3.1 ~ B-3.7（实体重构） | 24h | 阶段 1 |
| 阶段 3 | B-4.1 ~ B-4.3（领域层） | 12h | 阶段 2 |
| 阶段 4 | B-5.1 ~ B-5.3（基础设施） | 10h | 阶段 3 |
| 阶段 5 | B-6.1 ~ B-6.4（应用服务） | 16h | 阶段 4 |
| 阶段 6 | B-7.1 ~ B-7.2（前端 API） | 8h | 阶段 5 |
| 阶段 7 | B-8.1 ~ B-8.2（前端页面） | 12h | 阶段 6 |

**总预估工时：约 88h**

---

## 五、验证门禁

### 每个任务完成后

- [ ] `dotnet build` 目标项目无错误
- [ ] 代码扫描通过：
  - `rg "class.*Controller" backend/` — 0 结果
  - `rg "TenantId IS NULL" backend/` — 0 结果
  - `rg "PlatformTenantId = 1" backend/` — 0 结果
  - `rg "TenantId == null" backend/` — 0 结果

### 阶段 2 完成后（实体重构）

- [ ] `rg "YesOrNo" backend/` — 0 结果
- [ ] 所有降级实体无 Aggregate 分部类
- [ ] SysUser 导航属性 ≤ 4 个

### 阶段 5 完成后（应用服务）

- [ ] 所有 AppService 有 `[DynamicApi]` 标注
- [ ] 所有 AppService 有 `[Authorize]` 标注
- [ ] 敏感字段不出现在响应 DTO 中

### 阶段 7 完成后（前端页面）

- [ ] `pnpm type-check` 通过
- [ ] `pnpm build` 通过
- [ ] 前端类型与后端 DTO 一一对应

---

## 六、风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 聚合根降级导致领域事件丢失 | 缓存不失效 | 降级前审查每个 Aggregate 的事件发布，保留必要的事件处理器 |
| YesOrNo 替换遗漏 | 编译错误 | 使用 `rg "YesOrNo"` 全量扫描，逐文件替换 |
| 前端类型不同步 | 运行时错误 | 每个后端 DTO 变更后立即同步前端类型 |
| 仓储降级后方法缺失 | 编译错误 | 降级前检查仓储接口中的自定义方法，确保新基类支持 |
| 分表实体多租户过滤失效 | 数据泄露 | 依赖 Framework F-2.1 验证结果 |

---

## 七、修改检查清单

每次修改前确认：

- [ ] 实体基类选择是否正确（参考 skill 6.1）
- [ ] TenantId 约定是否遵守（0=平台，业务从 1 开始）
- [ ] 全局过滤器是否覆盖（软删除 + 租户）
- [ ] 敏感字段是否标记 `[JsonIgnore]`
- [ ] 是否有 Controller（禁止）
- [ ] 前端类型是否与后端 DTO 对齐
- [ ] 枚举是否语义明确（禁止 YesOrNo）
- [ ] 导航属性是否精简
- [ ] 新增权限码已在 PermissionSeed 登记
- [ ] 审计日志覆盖授权变更 + 敏感读写
