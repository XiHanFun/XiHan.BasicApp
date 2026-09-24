# 权限模型

XiHan.BasicApp 的权限体系融合 **RBAC（基于角色）** 与 **ABAC（基于属性）**，再叠加**角色继承、数据范围、字段级脱敏、多租户版本门控**。这是系统最核心也最需要理解的部分——一次请求要穿过认证、租户解析、权限码、约束、数据范围、字段脱敏六道关卡，每一道都可能收窄或拒绝。

权限相关的领域实体几乎全部集中在 `modules/XiHan.BasicApp.Saas/Domain/Entities/`（`Sys*`），运行时判定由框架 [授权模块](https://framework.docs.xihanfun.com/packages/authorization) 承接。本页以真实源码为准，讲清"怎么用、为什么这么设计、怎么扩展"。

## 权限码：单一事实源

权限的基本单位是**权限码**，格式固定为三段：

```text
module : resource : action
模块     资源       动作
```

- 例：`saas:user:read`（读取用户）、`saas:role:export`（导出角色）、`saas:permission:create`（创建权限定义）
- 超级管理员在**平台**的授权快照里持有**字面通配 `*`**；作用侧不含当前上下文的权限码先于通配拒绝，进了业务租户超管身份不成立（见 [多租户](./multi-tenancy)）。匹配是"命中 `*` 或精确等于该权限码"（大小写不敏感），**不支持** `resource:*:*` 之类的段级通配——通配仅有 `*` 这一个特例，见下文判定链

> 冒号分三段是硬约定：`SaasPermissionDefinitions.ResolveGroupCode` 按 `saas:{resource}:{action}` 取中间段作为**分组码**（资源段），前端权限中心据此把权限归类展示。

所有 SaaS 权限码的**唯一手写源**是 `Domain/Permissions/SaasPermissionCodes.cs`（常量）与 `Domain/Permissions/SaasPermissionDefinitions.cs`（分组 + 元数据）：

- `SaasPermissionCodes` 按资源分嵌套静态类，每个动作一个 `const string`，如 `SaasPermissionCodes.User.Read = "saas:user:read"`。后端 `[PermissionAuthorize(SaasPermissionCodes.User.Read)]` 直接引用常量，杜绝魔法字符串。
- `SaasPermissionDefinitions.Groups` 是**手写单一事实源**：每个资源块一个 `SaasPermissionGroup`（组码 + 中文组名 + 组内 `SaasPermissionItem` 列表）。落库扁平表 `All`、组码→组名 `GroupNames` 全由 `Groups` 派生，每条权限的 `ModuleCode`（恒为 `saas`）、`Tags`、`Priority`（恒等于 `Sort`）自动生成，无需手写。
- 新增资源时只在 `Groups` 增一个分组节点、或在已有节点增一条权限项即可，种子据此落库。
- 每个分组必须声明**作用侧**（`PermissionSide`：平台 / 租户 / 两侧），条目可单独覆盖；作用侧决定权限在哪个上下文生效、能否进入套餐白名单，详见 [多租户 · 权限作用侧](./multi-tenancy#权限作用侧平台-租户-两侧)。

每条权限项携带一个关键标志 `IsRequireAudit`：为 `true` 时，该权限对应的操作应强制写差异日志（`SysDiffLog`），用于合规审计。写类动作（create/update/delete/grant/revoke）多为 `true`，读与导出多为 `false`。

### 三级门控

前端页面、字段、操作三级都按权限码过滤——没有对应权限码，元素**不渲染 / 不可操作**。菜单本身由后端 `PageRegistry` 单一事实源注册并绑定权限码（建菜单即绑权限），前端据此渲染可见菜单树；页面内的按钮/操作与列表字段再按权限码与字段级安全策略二次过滤。

## 权限定义实体：`SysPermission`

权限码只是字符串标签，落库的权限定义是 `SysPermission`。它遵循标准 RBAC "权限 = 资源 + 操作"的原子建模：

| 字段 | 说明 |
| --- | --- |
| `PermissionCode` | 权限码（`TenantId + PermissionCode` 租户内唯一），推荐三段式 |
| `ModuleCode` | 模块段（如 `saas`），支持三段式权限码 |
| `PermissionType` | `ResourceBased`（绑定 `ResourceId`+`OperationId`）/ `Functional`（仅凭码）/ `DataScope` |
| `ResourceId` / `OperationId` | 关联 `SysResource` / `SysOperation`（`ResourceBased` 时必填） |
| `IsRequireAudit` | 该权限操作是否强制写 `SysDiffLog` |
| `Side` | 作用侧（`Platform` / `Tenant` / `Both`，必填）：权限在平台还是业务租户里生效 |
| `Priority` | 数字越大越高；**仅**用于同级别 Grant/Deny 排序，不参与 Grant vs Deny 跨级覆盖（Deny 始终优先） |
| `Status` | `Enabled` / `Disabled` |

`TenantId = 0` 的权限是**平台级全局权限**（派生属性 `IsGlobal=true`），作为模板供订阅版本（`SysTenantEditionPermission`）引用；合并查询用 `WHERE TenantId IN (0, 当前租户)`。删除权限仅软删，且删除前须校验无角色/用户仍授权、无菜单引用、无版本引用。

## RBAC：角色驱动

用户通过**角色**获得权限码集合。角色是权限分配单元：

- `SysRole`：承载一组权限，通过 `SysUserRole` 赋给用户。`RoleType` 分 `System`（平台预置，必须 `TenantId=0`）/ `Business` / `Custom`（租户自建，不可全局化）。平台基线只预置 `super_admin`（`TenantId=0` 的 `System` 角色）；租户开通时自动创建 Owner 角色 `tenant_owner` 并按版本白名单批量授权。
- `SysRolePermission`：角色↔权限绑定，字段含 `PermissionAction`（`Grant`/`Deny`）、生效/失效时间（`EffectiveTime`/`ExpirationTime`）、`GrantReason`（关联审批单/工单，审计追溯）。
- `SysUserRole`：用户↔角色绑定（用户"持有"角色）。

### 分配角色：两个入口，一套规则

角色既可以在用户那边分（「角色直授」：一个用户、多个角色，`BatchUpdateUserRoles`），也可以在角色这边分（「角色成员」：一个角色、多个成员，`BatchUpdateRoleMembers`，候选人来自当前上下文的用户目录，租户里含外部成员）。两个入口、以及单条改状态 / 改有效期让一条绑定重新占名额的路径，走同一套规则：

- 对象必须是本租户可授权的成员（已接受、有效、在有效期内）；支持成员也由所在租户分配。
- 角色必须可分配：启用；系统角色只在平台分配；全局角色在租户里可以分配给本租户成员，但只能在平台编辑。
- 加入后不违反静态职责分离（按「现有有效角色 + 新角色」展开继承链评估）。
- 不超角色成员上限 `MaxMembers`（0 表示不限）：状态有效且未过期的绑定占名额，尚未生效的预约同样占，已过期的不占；同批移出的名额可以让出。
- 撤销只置失效不删行，同一 用户×角色 的历史行再授予时就地复用、从现在起生效。

### 角色层级继承（闭包表）

角色支持层级继承，用**闭包表** `SysRoleHierarchy` 存储所有继承关系（含直接与传递）：

- 每条记录是 `(AncestorId, DescendantId, Depth, Path)`；`Depth=0` 是自关联，`Depth=1` 直接继承，`Depth=n` 为 n 级间接继承，`Path` 形如 `1/3/5`。
- 核心不变式：若 `A→B` 且 `B→C` 存在，则 `A→C` 也必须存在。表**不设** `Status`/`IsDeleted`——单条停用会破坏传递闭包一致性，变更时整体重建受影响路径，硬删。
- 继承语义：后代自动获得祖先的所有 **Grant** 权限，可被后代自己的 `SysRolePermission.Deny` 覆盖；**`DataScope` 不继承**（每个角色独立定义数据范围）；SSD/DSD 约束检查须展开继承链。
- 写入时服务层做**环路检测**（禁止 `A→B→A`）：新增 `A→B` 前先查 B 的祖先集是否已含 A。

### 用户直授与合并优先级

除角色外，用户可被**直接授权** `SysUserPermission`（`Grant`/`Deny`），用于临时提权、特殊例外、紧急收回。多来源冲突按 **deny-overrides + 优先级** 合并，优先级从高到低：

| 级别 | 来源 | 语义 |
| --- | --- | --- |
| 1（最高） | 用户直授 `Deny` | 最终拒绝，任何角色 Grant 都不能覆盖 |
| 2 | 用户直授 `Grant` | 最终授予，即使所有角色 Deny |
| 3 | 角色级 `Deny` | 仅作用于当前角色的继承链，不影响其他独立角色 |
| 4 | 角色级 `Grant` | 当前角色的权限授予 |

角色级 Deny 的作用域是关键细节：若角色 B 继承 A、B 对权限 P 标 Deny，则**经 B** 拿不到 P；但用户若同时直接持有 A，仍能**经 A** 拿到 P。

## 数据范围（Data Scope）

同一个权限码，能看到的数据行随**数据范围**收窄。范围档位由 `SysRole.DataScope`（枚举 `DataPermissionScope`）承载：

| 枚举值 | 含义 |
| --- | --- |
| `SelfOnly` | 仅本人创建的数据 |
| `DepartmentOnly` | 用户当前租户上下文下全部有效部门归属，仅限这些部门 |
| `DepartmentAndChildren` | 上述部门 + 其所有下级部门 |
| `All` | 全部数据 |
| `Custom` | 由 `SysRoleDataScope` 明确枚举可访问的部门列表 |

> **重要**：该枚举**不承诺"数值越大范围越广"**（`Custom=99`）。多角色合并必须按显式语义处理：任一角色 `All` → 全部；`DepartmentOnly`/`DepartmentAndChildren` → 求部门归属并集；`Custom` → 与其它范围并集叠加；仅 `SelfOnly` 才只返回本人数据。禁止用数值大小做合并判断。

成员还可以有自己的覆盖：成员关系 `SysTenantUser.DataScopeOverride`（null 表示跟随角色）。覆盖挂在成员关系上，同一个人在不同租户各自设置；有覆盖时**取代**该成员在本租户的全部角色，不与角色合并。

自定义范围有两张对称的明细表，仅在档位为 `Custom` 时枚举可见部门：

- `SysRoleDataScope`：服务 `SysRole.DataScope=Custom`。字段 `RoleId`+`DepartmentId`，`IncludeChildren=true` 时服务层配合 `SysDepartmentHierarchy` 展开所有后代部门（新增子部门自动纳入）。
- `SysUserDataScope`：服务成员覆盖 `SysTenantUser.DataScopeOverride=Custom`，本租户、本成员的部门明细。

档位与部门明细**一次设置、一次落地**：角色走 `SetRoleDataScopeAsync`，成员走 `SetUserDataScopeAsync`，权限码只有查看（`read`）与设置（`update`）。只有自定义档位带部门且至少一个；切到其它档位时已有的部门明细全部撤销（只置失效，历史行再选中时就地复用）。全局角色是各租户共用的模板，只在平台设档位、不能自定义部门（部门是租户自己的数据），在租户里只读。

数据范围在**查询层**生效，是租户侧概念：平台没有部门与成员关系，不施加数据范围。租户里先看成员覆盖；没有覆盖才按启用角色——任一角色为 `All` 则不限，自定义档位的角色并入它的部门明细（非自定义档位角色残留的明细不生效）。解析出可见部门集（`IncludeChildren` 经 `SysDepartmentHierarchy` 展开后代）后，取这些部门下的成员并入"本人"，收敛为可见用户主键集合追加到查询条件，与权限码组合决定"能对哪些行做这个动作"。无任何有效范围时只返回本人数据。

## 字段级脱敏（FLS）

即使有权读取某条记录，**敏感字段**仍可能被脱敏或禁读。策略实体 `SysFieldLevelSecurity` 控制某个资源特定字段的可读/可编辑与脱敏形式：

| 字段 | 说明 |
| --- | --- |
| `TargetType` / `TargetId` | 策略绑定目标（`FieldSecurityTargetType`：`Role` / `User` / `Permission` / `Department`）；服务端解析只匹配 `Role`（当前用户的角色）与 `User`（用户自身） |
| `ResourceId` / `FieldName` | 受控资源与字段名（区分大小写，对应实体属性名） |
| `IsReadable` | 是否可读；`false` 时按 `MaskStrategy` 返回 |
| `IsEditable` | 是否可编辑；`false` 时前端只读、后端写操作拒绝 |
| `MaskStrategy` | 脱敏策略（见下表） |
| `MaskPattern` | 配合策略的规则串（JSON/表达式） |
| `Priority` | 数字越大越高（策略元数据，不能为负）；服务端合并按 deny-overrides 处理，不按优先级放行 |

脱敏策略 `FieldMaskStrategy`：`None`（原值）/ `Hidden`（隐藏或返回 null）/ `FullMask`（全星号）/ `PartialMask`（保留首尾，如 `138****1234`）/ `Hash`（不可逆）/ `Redact`（固定替换如 `[已脱敏]`）/ `Custom`。

**可见性三段语义**（RBAC/ABAC 判定之后生效）：

- `IsReadable=true` + `MaskStrategy=None` → 可读原文
- `IsReadable=true` + `MaskStrategy!=None` → 可读脱敏值（如客服看手机号 `138****1234`）
- `IsReadable=false` → 不可读，按策略返回脱敏/隐藏结果

**冲突合并**采用 deny-overrides：命中同一字段的多条规则里，任一 `IsReadable=false` → 最终不可读，任一 `IsEditable=false` → 最终不可编辑，脱敏取最严。

### 服务端落地：读脱敏 + 写校验 + 排序/过滤门控

FLS 由 `IFieldSecurityService` 在服务端强制落地，**不依赖前端**：

- `ResolveAsync(resourceCode)`：解析当前用户在某资源上的有效规则（合并 deny-overrides），得到 `EffectiveFieldRule` 字典。
- `ApplyAsync(resourceCode, item/items)`：对返回 DTO/集合**反射就地脱敏**。列表、详情、导出都调用它。
- `EnsureEditableAsync` / `EnsureUpdatableAsync`：写路径校验——命中不可编辑字段被实际修改则抛异常。
- `GuardSortsAsync` / `GuardFiltersAsync`：**推断攻击防护**——就地剔除当前用户"不可读或已脱敏"字段的排序键与过滤条件。否则用户可按受保护字段排序/过滤，从结果顺序反推被脱敏的真实值。字段名大小写不敏感匹配，无显式规则默认放行。

前端另有 `MyFieldSecurityAppService.GetMineAsync(resourceCode)` 下发"可读/可编辑/脱敏"信息，供表单据 `IsEditable` 置只读、展示脱敏标识——但**脱敏值本身已由服务端在响应里落地**，前端仅做体验优化。

**导出与在线同一口径**：后台导出走 `ExportExecutor`，它在后台线程按任务发起人重建 `CurrentTenant` + `CurrentPrincipal`，再调用既有 QueryService，使**数据范围与字段脱敏原样生效**，并显式 `IPermissionChecker` 补齐进程内不触发 `[PermissionAuthorize]` 的缺口。重建的主体与在线请求同一口径：发起时记下的会话声明（会话已登出或被下线，导出随之失败）、与签发令牌同一来源的角色（超管判定等依赖角色的规则一致）、模仿者声明（模仿态禁用的权限在导出里同样禁用）；租户停用、到期或未就绪时导出直接失败。因此导出与在线列表看到的数据、脱敏结果一致。

## ABAC：属性驱动的约束

在 RBAC 之上，ABAC 用**属性与规则**做更细的运行时约束。两套机制并存：

### 权限条件 `SysPermissionCondition`（轻量 ABAC）

为某条角色权限或用户直授权限附加属性条件，与授权绑定一一挂钩：

- 排他约束：`RolePermissionId` 与 `UserPermissionId` **恰好一个非空**（服务层 XOR 校验，建议库层补 CHECK 约束）。
- 组合逻辑：同一 `ConditionGroup` 内条件为 **AND**，不同组之间为 **OR**（单权限最多 5 组、每组最多 10 条）。
- 属性命名走 XACML 风格命名空间：`subject.*` / `resource.*` / `environment.*`。建模层面的常用属性如 `subject.department`、`resource.status`、`resource.owner`、`resource.amount`、`environment.hour`、`environment.ip`、`environment.location`。
- `Operator`（`ConditionOperator`）覆盖 `Equals`/`NotEquals`/比较/`Contains`/`In`/`Between`/`StartsWith`/`EndsWith`/`IsNull` 等，`IsNegated` 可整体取反；`ValueType` 固定值解析类型防漂移。

条件建模示例（存储层语义）：

| 场景 | 属性 | 操作符 | 值 |
| --- | --- | --- | --- |
| 仅工作时间 | `environment.hour` | `Between` | `9,18` |
| 仅内网 | `environment.ip` | `StartsWith` | `192.168.` |
| 只能操作草稿 | `resource.status` | `Equals` | `draft` |
| 限额操作 | `resource.amount` | `LessThan` | `10000` |

> **建模 vs. 运行时**：上述属性/操作符描述的是 `SysPermissionCondition` 的**存储表达能力**，存储的条件行本身不参与运行时判定。运行时由框架 `IAbacAttributeCollector` 收集 `subject./resource./environment.` 属性、`IAbacEvaluator` 评估，混合策略里 ABAC 段以策略码触发（如 `same_tenant`/`self_only` 及比较式）。框架默认评估器对 `subject.*`/`resource.*` 的同租户、仅本人、字段比较有内建支持；时间窗/IP 这类 `environment.*` 条件的真实取值需应用侧属性收集器提供，未接入收集器的属性不会自动生效——落地前请以仓库为准确认对应属性已被收集。

### 约束规则 `SysConstraintRule`（+ `SysConstraintRuleItem`）

面向角色/权限组合的 RBAC 约束（职责分离等），规则元数据在主表、涉及的角色/权限/用户 ID 拆到 `SysConstraintRuleItem` 独立成行（保引用完整性）：

- `ConstraintType`：`SSD`（静态职责分离，不能同时**持有**互斥角色）/ `DSD`（动态职责分离，同一**会话**不能同时激活互斥角色）/ `MutualExclusion` / `Cardinality`（基数）/ `Prerequisite`（先决条件）/ `Temporal`（时间）/ `Location`（位置）/ `Custom`。
- `ViolationAction`：违规处理 `Deny` / `Warning` / `Log` / `RequireApproval`。
- 非 ID 类参数（时间窗、最大数量等）以 JSON 存 `Parameters`；有效性由 `Status=Enabled` 且当前时间落在 `EffectiveTime~ExpirationTime` 共同决定。
- **约束检查必须展开角色继承链**：若 Item 指向角色 A，任何继承 A 的后代角色都视为等效目标（经 `SysRoleHierarchy` 展开）。`ConstraintGroup` 标识互斥集合（先决条件约束中 0=必备项、1=目标项）。
- 后端目前只提供约束规则与目标项的定义管理（增删改查），授权写路径不会自动拦截违规组合。

## 会话角色激活（动态职责分离）

`SysUserRole` 是"持有"，`SysSessionRole` 是"激活"：用户可能持有多个角色，但一次会话只激活其中一个子集，用于动态职责分离与最小权限。

- `SessionId`（关联 `SysUserSession` 主键）+ `RoleId` 在租户内唯一；`Status`（`SessionRoleStatus`）为 `Active`/`Inactive`/`Expired`，配合 `ActivatedTime`/`DeactivatedTime`/`ExpirationTime` 记录生命周期。
- **激活的模型约定**：该用户确实持有此角色（`SysUserRole` 存在且未过期），且激活须通过 DSD 约束检查（`SysConstraintRule` 中 `ConstraintType=DSD`）。
- 后端目前只提供会话激活角色的查询（权限码 `saas:session-role:read`），激活/停用的写入接口未开放。
- 典型用法：敏感操作前临时激活特权角色，完成后立即停用；凌晨扫描失效已过期的会话角色。

设计意图是让一次会话只以其激活的角色子集办事，从而即便持有互斥角色也不会在同一会话同时生效；DSD 约束在**激活时**校验拦截。持久的角色持有关系仍记在 `SysUserRole`，会话激活是叠加在其上的运行期约束。

## 申请、审批、委托与留痕

- **权限申请** `SysPermissionRequest`：用户申请权限/角色 → 关联 `SysReview` 审批流 → 审批通过后由服务层**自动**写入 `SysUserRole`/`SysUserPermission` 完成授权。状态 `PermissionRequestStatus`：`Pending`/`Approved`/`Rejected`/`Withdrawn`/`Expired`；可带期望生效/失效时间。
- **权限委托** `SysPermissionDelegation`：建模"委托人 → 被委托人 → 权限范围 → 时间窗口"（如经理出差把审批权委托给副手）。`PermissionId`/`RoleId` 均可空且可同时填写：非空的 `PermissionId` 直接并入被委托人权限，非空的 `RoleId` 展开为该角色当前有效的 Grant 权限，两者都为空则不产生委托权限；`ExpirationTime` **必填**（委托必须有截止）；生效需 `DelegationStatus=Active`（`DelegationStatus`：`Pending`/`Active`/`Expired`/`Revoked`）且当前时间在窗口内。撤销走 `DelegationStatus=Revoked`，记录本身仅软删。
- **变更留痕** `SysPermissionChangeLog`：结构化记录"谁在什么时候给谁授予/撤销了什么权限"，是 RBAC 合规审计的核心证据。**只追加，禁止更新和删除**，按月分表（`SplitTable`），携带 `OperatorUserId`/`TargetUserId`/`TargetRoleId`/`PermissionId`/`ChangeType`/`OperationIp`/`TraceId`。`ChangeType`（`PermissionChangeType`）区分角色/用户的授予、撤销、拒绝、分配/移除角色等。与 `SysDiffLog`（通用实体字段变更）职责分离——本表专注权限授予/撤销的业务语义。

## 版本（Edition）门控

多租户下不同订阅版本开放不同权限。`SysTenantEditionPermission` 是 Plan-gating 核心：定义每个版本（`Edition`）向租户开放的**权限白名单**。

- `EditionId` + `PermissionId` 唯一，`PermissionId` 指向 `TenantId=0` 的全局权限（`IsGlobal=true`），且作用侧必须含租户（平台侧权限进不了租户）。
- 租户可用权限集 = `SysTenant.EditionId` → 此表 → 可用 `PermissionId` 集；租户管理员分配角色/用户权限时须在此集合内选择。
- 版本升级（如 Basic → Pro）增量写入新增权限；开通版本时一站式创建 Owner 角色并按白名单批量授权。

版本门控有**运行时**与**持久回收**两层，互为兜底：

**运行时门控**：构建授权快照时，`ApplyEditionGatingAsync` 把用户有效权限集与当前版本白名单**求交**，超出白名单的权限即使 DB 里仍有绑定也不进入本次生效集。只有平台（0 号租户）跳过门控；业务租户里无版本绑定或白名单为空时生效权限为空（失败即拒绝），门控缓存不可用时直接查库。版本权限变更会失效版本门控缓存，下次判定即按新白名单收窄。

**降级自动回收越权授权**：租户版本变更时（`TenantDomainService` 检测到 `EditionId` 变化），调用 `TenantProvisionDomainService.ReconcileTenantAuthorizationWithEditionAsync`，按新版本白名单**回收超出范围的存量角色/用户直授权限行**。关键实现细节：

- 白名单为空时运行时已一律拒绝，存量绑定保留不回收，白名单恢复后随之恢复。
- 仅处理该租户**自有**绑定行（`TenantId=本租户`）；全局行（`TenantId=0`）属平台运维资产，不在回收范围。

## 判定链

一次访问要逐层穿过下面的关卡，层层收紧、默认从严。请求先经框架中间件管道 `TraceId → 请求文化 → 认证 → 租户解析 → 授权 → 端点`，授权在其中由 `[PermissionAuthorize(...)]` 触发：

```text
请求进来
  → 认证：你是谁（从主体声明解析 userId；解析不到直接拒绝）
  → 租户解析：落到哪个租户上下文（字段级隔离，全局数据 TenantId=0）
  → 授权(RBAC)：IPermissionChecker 实时校验权限码（查授权快照）
        · deny-overrides：用户 Deny > 用户 Grant > 角色 Deny > 角色 Grant
        · 角色权限经 SysRoleHierarchy 展开继承链（祖先 Grant，后代 Deny 覆盖）
        · 委托权限（SysPermissionDelegation）并入快照，再统一被用户 Deny 收窄
        · 只取当前上下文的绑定（平台的绑定不进租户，反之亦然）
        · 作用侧：不在当前上下文生效的权限码先于通配拒绝
        · 业务租户：版本白名单求交（ApplyEditionGatingAsync，失败即拒绝）
        · 平台的 super_admin 角色 → 平台生效的全部权限 + 字面 * 通配放行
  → 授权(ABAC)：混合策略里编码在策略名中的 ABAC 策略码
        · 收集 subject./resource./environment. 属性 → 评估器按策略码判定
          （同租户/仅本人/比较式内建；时间窗/IP 依赖应用侧属性收集器）
  → 数据范围：DataPermissionScope 解析可见部门集 → 展开为可见用户主键集合追加到查询条件
  → 字段脱敏：FLS 按 deny-overrides 就地脱敏（读/导出一致），并门控排序/过滤字段
```

判定采用框架的**混合策略**：`[PermissionAuthorize]` 把"权限码 + ABAC 策略码"编码进一个 ASP.NET Core 策略名，由 `HybridPermissionAuthorizationHandler` 依次执行"权限码实时校验 → ABAC 属性评估"。

> **实时校验，非信任令牌**：授权处理器**不信任 JWT 里冻结的权限声明**，每次都走 `IPermissionChecker` 查当前授权快照。因此权限授予/回收、用户禁用、会话注销**即时生效**，无需等令牌过期。凡是影响授权的写操作（权限增删改、角色/用户授权变更）都会精准失效授权快照缓存。

## 下一步

- [系统架构](./introduction)：权限判定在请求管道中的位置
- [多租户](./multi-tenancy)：租户隔离与版本门控全貌
- [审计日志](./logging)：`SysDiffLog` / `SysPermissionChangeLog` 等留痕
- [框架 · 授权模块](https://framework.docs.xihanfun.com/packages/authorization)：底层 RBAC / Policy / ABAC 与混合策略实现
- [框架 · 认证模块](https://framework.docs.xihanfun.com/packages/authentication)：JWT / OAuth2 / 2FA 身份声明
