# 多租户与版本

XiHan.BasicApp 是一套 **企业级通用中后台内核**：一份代码、一套部署，同时承载多个互不可见的租户（企业客户），并以**版本（Edition）**作为订阅售卖与功能门控的单元。本页讲清楚隔离怎么落地、登录后落到哪里、超级管理员如何跨租户运维、版本白名单如何在运行时门控权限，以及请求如何解析到租户上下文。

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

- **框架层**：`ICurrentTenant.Id` 是 `long?`。`null` 与 `0` 同义，都表示**平台，也就是 0 号租户**。平台和任何一个租户一样只看、只写自己的数据：读只看 `TenantId=0` 的行，写只落 / 只改 `TenantId=0` 的行——不存在「没有租户上下文就看全部、写全部」的口径。
- **BasicApp 应用层落库约定**（见 `BasicAppEntity` 注释）：平台级/全局记录统一落 **`TenantId=0`**（"平台租户"占位），**不得用 NULL**；业务租户 Id 从 1 开始分配，0 号租户由平台保留。

二者衔接的规则：

- 平台判定统一以 `currentTenant.IsPlatformOperation()` 为准，其实现就是 `Id is null or 0`（见 `CurrentTenantPlatformExtensions`）。
- 查询"全局 + 私有"合并时用 `WHERE TenantId IN (0, {currentTenantId})`。授权快照构建即按此规则：绑定行按 `TenantId == 当前租户 || TenantId == 0` 生效，平台态（无上下文）则仅 `TenantId=0` 的全局绑定生效，防止多租户成员在平台态聚合出跨租户权限。
- 如需 `IsGlobal` 语义，实体在 Expand 里以只读派生属性 `IsGlobal => TenantId == 0` 暴露，**不落库**（避免与 `TenantId` 漂移）。
- 只有平台才允许维护 `TenantId=0` 的全局模板（菜单/权限/角色/版本等）；租户态（`Id>0`）对全局模板一律拒绝写入，避免某租户改动波及所有租户。平台同样不能直接改写租户的行。
- 平台与租户各自独有的运行数据（会话、令牌、授权码、导出任务、邮件短信、导入记录、任务调度与各类日志）实现 `IStrictMultiTenantEntity`：租户态只看本租户行，不读共享平台行。

> 服务层通过框架注入自动写入 `TenantId`，业务代码禁止直接操纵。跨租户只走显式通道：跨租户读取用仓储的 `...IgnoreTenantAsync`（内部 `CreateNoTenantQueryable()`）或查询上的 `.ClearTenantFilter()`；写某个租户的数据用 `ICurrentTenant.Change(tenantId)` 切入该租户，`using` 作用域结束自动恢复；账号域数据写在账号注册地——新增经 `IAccountScope.EnterAsync(userId)` 切入注册地，个人中心更新自己已有的行可用 `TenantWriteGuard.Suppress()`（只放宽更新与删除，不放宽新增）。后台逐租户维护用 `ITenantDataScopeRunner` 依次切入平台与每个数据可达的租户。

## 登录与落点：邮箱全局唯一，先登录后选租户

BasicApp 采用**先登录、后定上下文**：登录页不选择租户，统一在平台（`_currentTenant.Change(null)`）完成身份认证，成功后由服务端决定落点——登录后总是落在一个上下文里（平台，或一个租户）。

- **邮箱是全平台唯一的登录身份标识**。注册、开通租户管理员、找回密码都强制校验邮箱且全局唯一（`ExistsEmailGloballyAsync`）。平台账号也可用用户名登录。
- 支持密码登录、邮箱验证码登录、第三方（OAuth）登录，均在平台态定位用户后走统一落点逻辑（`IssueLoginTokenWithLandingAsync`）。

### 落点策略

平台是 0 号租户，只对**平台账号**（`SysUser.TenantId=0`）开放。落点由 `ResolveLoginLandingAsync` 决定：

| 账号 | 落点 |
| --- | --- |
| 平台账号 | 平台 |
| 租户账号 | 最近进入过的可进入租户 → 归属租户 → 第一个可进入的租户（`LoginLandingPolicy`） |
| 租户账号且一个可进入的租户都没有 | 拒绝登录，并说明是没有有效成员关系，还是所在租户均不可进入 |

**可进入的租户**只有一个口径（`IAuthContextQueryService.GetAccessibleTenantsAsync`）：有效成员关系（已接受、有效、在生效期内）∩ 可进入的租户（`AvailableTenantSpecification`：正常、已完成初始化、未删除、未过期）。登录落点、切换租户、控制中心的可切换列表都用它。成员关系的最近进入时间（`LastActiveTime`）在登录与切换时回写，决定下次登录落点。

签发的令牌只在进入租户时带 `TenantId` claim，平台不带。

### 随时切换租户

登录后通过页头的上下文标识进控制中心，经 `SwitchTenantAsync`（`api/Auth`）切换（类似 GitHub 切换 Org）：

- 目标 `TenantId` 归一：`null` 或 `<=0` 表示平台。
- 进入租户：必须在"可进入的租户"里，**超管也不例外**——平台账号要进某个租户，同样须先成为它的成员（如支持人员入驻）。
- 进入平台：只有平台账号可以。
- **切换即换会话**：会话属于它所在的上下文（`SysUserSession.TenantId` 只插入不更新）。切换时吊销当前会话、在目标上下文新建一条续接会话（沿用设备与登录时间，不是新登录、不发登录通知），旧访问令牌与刷新令牌经会话闸门随即失效；会话标识因此全局唯一（`UX_{table}_UsSeId`）。
- 在目标上下文内**重建授权快照并签发新令牌**，权限随目标上下文实时重算。
- 结束模仿登录回到发起人的原会话，只在原会话上重签令牌（原会话本就在发起时的上下文里）。

前端切换后整页重建：清掉本地用户信息与标签页、断开 SignalR；路由守卫在整页加载时把用户信息与权限一起重取，平台 / 租户标识不会沿用切换前的旧值。可切换的租户列表由 `GetMyAvailableTenantsAsync` 提供，与切换同一口径；前端用 `TenantSwitcherDto` 渲染。

## 成员关系：`SysTenantUser`

"谁能进入哪个租户"由 `SysTenantUser`（`Sys_Tenant_User`）承载——它是 用户 × 租户 的多对多成员表，是跨租户协作与平台运维统一建模的入口。语义上区分：

- `SysUser.TenantId` = 用户主账号的归属租户（注册地）
- `SysTenantUser.TenantId` = 用户拥有成员身份的租户（含主租户 + 外部协作租户）

所有租户访问（含主属）都统一走本表，鉴权路径一致。成员关系是**严格租户隔离**实体：租户里只看得到本租户的成员行，跨租户读取（如登录时取用户的全部成员关系）走显式通道。关键字段：

- `MemberType`（`TenantMemberType`）：`Owner` / `Admin` / `Member` / `External` / `Guest` / `Consultant` / `PlatformAdmin`。
  - 注意：`MemberType` 表达"成员身份类型"而非"权限级别"，**服务层禁止用它直接鉴权**（如 `if MemberType==Admin`），必须走 RBAC 权限链；它只用于成员列表分类、邀请流程控制、`PlatformAdmin` 创建校验。
- `InviteStatus`（`TenantMemberInviteStatus`）：`Pending → Accepted / Rejected / Revoked / Expired`。仅 `Accepted` 且 `Status=Valid` 且当前时间在生效/失效区间内的成员关系才可用于鉴权。
- `EffectiveTime` / `ExpirationTime`：外部协作者/访客常用的时效控制。
- **成员关系上的授权由所在租户维护**：角色、直授权限、部门、数据范围（成员覆盖 `DataScopeOverride` 挂在成员关系上）、ABAC 条件、字段级安全、约束、委托、申请都作用于「本租户的成员」；支持成员（`PlatformAdmin`，平台人员入驻）也一样——平台看不到也写不了租户的授权数据，他们在租户里能做什么由这个租户决定。

## 账号域：注册地租户戳 + 严格隔离

账号是身份层数据，不是某个租户的业务数据。`SysUser`、`SysUserSecurity`、`SysUserSetting`、`SysUserNotificationPreference`、`SysUserApiCredential`、`SysExternalLogin`、`SysPasswordHistory` 实现 `IStrictMultiTenantEntity`，`TenantId` 固定为注册地（平台账号为 0）：

- **按主键跨租户读**：账号、安全记录、个人设置等按用户主键走 `...IgnoreTenantAsync` / `CreateNoTenantQueryable()` 读取，展示别人的名字、取当前用户自己的资料都不依赖当前上下文。
- **写在注册地**：外部成员在别的租户里改设置、建凭据、改密码，行照样落回注册地（`IAccountScope`、密码历史切入 `user.TenantId` 写入）。
- **看得见谁由上下文决定**（`IUserDirectory`）：平台里是平台账号；租户里是本租户已接受的成员，含注册在别处的外部成员与入驻的平台人员。用户列表、详情、选择项、聊天候选人、"全员"通知都走同一个目录，列表项带 `IsExternalMember`。
- **身份类操作只对本地账号**：资料、状态、锁定、密码、双因素、登录策略、下线全部会话、删除只对注册在当前上下文的账号开放；外部成员给出明确原因（"由其注册地租户维护"），在这里只能管理它在本租户的角色、权限、部门与数据范围。前端对外部成员隐藏账号级操作，编辑弹窗只提交角色与部门。
- **平台建的是平台账号**：平台里建账号不写成员关系、不占席位；租户里建账号占一个席位并成为本租户已接受的成员。
- **删除 / 停用是账号级操作**：账号是任何一个租户的有效所有者都不能直接删除或停用；删除时逐租户切入，作废它在每个租户的成员关系。

## 超级管理员：只在平台成立

超级管理员（平台上的 `super_admin` 角色）是平台运营方账号，超管身份只在平台（0 号租户）成立：

- **平台里通配**：授权快照带 `*`，外加平台生效的全部权限；平台不受套餐门控。
- **进了业务租户就只是那个租户的成员**：平台的超管绑定不带进任何租户，快照按目标租户里的角色、直授与委托重建，再经套餐门控；在租户里没有角色就没有权限。`ISuperAdminProtector.IsCurrentUserSuperAdmin` 同口径——业务租户里恒为 `false`，各处"超管豁免"只在平台生效。
- 替租户排查问题走**模仿登录**（以目标租户成员身份操作、全程留痕），不靠超管身份直接改租户数据。平台里发起时先选范围：平台账号，或某个租户（再从该租户的成员里选人）；租户里只能选本租户成员。成员检索同时匹配成员显示名与账号的用户名 / 昵称 / 姓名 / 邮箱。

## 权限作用侧：平台 / 租户 / 两侧

权限目录的每条权限都声明**作用侧**（`SysPermission.Side`，`PermissionSide` 标志枚举）：

| 作用侧 | 值 | 在哪生效 | 典型权限 |
| --- | --- | --- | --- |
| `Platform` | 1 | 只在平台 | 租户目录、套餐、权限/资源/菜单目录维护、缓存、服务器、任务调度、AI Provider、代码生成、全局编号/打印模板管理、跨租户模仿 |
| `Tenant` | 2 | 只在业务租户 | 租户成员维护、部门、岗位、成员数据范围、审批、工作流 |
| `Both` | 3 | 两侧都生效 | 用户、角色、授权、角色数据范围（平台只设全局角色模板的档位）、日志、配置、字典、消息等 |

- **声明在代码里**：Saas 权限由 `SaasPermissionDefinitions` 的分组声明（条目可单独覆盖），其它模块由各自的权限种子声明；种子每次启动把作用侧同步到库里。后台新建权限必须选作用侧，未声明（0）的权限在任何上下文都不生效。
- **生效 = 当前上下文的授权绑定 ∩ 作用侧 ∩ 套餐白名单（仅业务租户）**。作用侧不含当前上下文的权限码随授权快照下发（`ContextDeniedCodes`），鉴权、菜单与按钮先于通配判定拒绝它们——平台超管的 `*` 也放不出租户侧权限。
- **授权绑定只在所属上下文生效**：`SysUserRole` / `SysUserPermission` / `SysPermissionDelegation` / `SysUserDepartment` / `SysUserDataScope` 是严格租户隔离实体，平台的绑定不进租户，租户的绑定也不进平台。
- **授权界面按上下文列可授权限**：业务租户里只列、也只允许授出租户能生效的权限（租户侧与两侧），写路径同口径拒绝；平台列全部，授权穿梭框标出单侧权限。分配角色不按作用侧拦（角色上的平台侧权限在租户里本就不生效）。

## 全局模板：平台下发，租户只读或覆盖

`TenantId=0` 的模板行（全局角色、参数配置、字典等）读共享：租户看得见、改不了，改写与删除只在平台。按能否被租户覆盖分两种：

| 类型 | 实体 | 租户能做什么 | 判重 |
| --- | --- | --- | --- |
| 可覆盖型 | 参数配置、消息模板、编号规则、打印模板 | 建同键的自己的行，读取时租户优先 | 只在本上下文内判重 |
| 不可覆盖型 | 全局角色、字典 | 引用或分配；要改就新建自己的角色 / 字典 | 字典编码在「全局 + 本租户」里唯一 |

- **全局角色**：可以分配给本租户成员、被本租户角色继承；但它的授权、授权条件、数据范围、继承关系、启停与删除只在平台维护，租户不能往上叠加自己的授权。
- **字典**：字典项与所属字典同在一个上下文，全局字典的项只在平台维护；租户需要自己的选项就新建字典。平台建全局字典时要看所有租户，某个租户已用了这个编码就不能再建（否则会在那个租户里重名）。
- **平台删除全局行看所有租户**：全局角色被租户成员持有或被租户角色继承、全局字典还有字典项时不能删除——这些行平台自己看不见，要跨租户检查（`ISaasRepository.AnyIgnoreTenantAsync`）。

## 租户版本（Edition）：权限白名单 + 运行时门控

版本是**订阅售卖单元 + 功能门控单元**。三张表分工明确：

| 实体 | 表 | 职责 |
| --- | --- | --- |
| `SysTenantEdition` | `Sys_Tenant_Edition` | 版本能做什么、卖多少（`EditionCode`/`Price`/`BillingPeriodMonths`/`UserLimit`/`StorageLimit`/`IsDefault`） |
| `SysTenantEditionPermission` | `Sys_Tenant_Edition_Permission` | 版本 → 权限**白名单**映射（`EditionId` × `PermissionId`，唯一） |
| `SysTenant.EditionId` | `Sys_Tenant` | 某租户订阅了哪个版本（`null` 时取 `IsDefault=true` 的默认版本） |

内置版本种子（`SaasTenantEditionSeeder`）：`free`（免费/默认，只读+基础成员）、`basic`（+组织/用户/角色管理）、`pro`（+高级权限/审计/安全）、`enterprise`（全部作用侧含租户的权限、不限配额）。手写清单里混进平台侧权限会被剔除并告警；已存在的平台侧绑定（含后台手工维护的套餐）一律置为无效。版本记录本身是平台级（`TenantId=0`），由平台运营维护。

关键约束（见 `SysTenantEdition` 与领域服务）：

- `EditionCode` 全局唯一，如 `free`/`basic`/`pro`/`enterprise`。
- 同一时刻仅一个 `IsDefault=true`；设默认时先清其它默认（`ClearDefaultEditionsAsync`），且默认版本必须处于启用状态、不能被直接取消或禁用。
- 版本白名单只能绑定**平台级全局权限**（`permission.IsGlobal`，即 `TenantId=0`），权限须启用，且作用侧必须含租户——平台侧权限进不了租户，绑到套餐上会被拒绝。

### 运行时门控：越白名单的权限被拒

门控在 `AuthorizationSnapshotQueryService` 构建授权快照时叠加（`ApplyEditionGatingAsync`）——用户在某租户能生效的权限，被该租户所属版本的白名单**收窄取交集**：越出白名单的权限即便被授予，也不进入生效权限集，等同被拒。

门控**失败即拒绝**：

- 只有平台（0 号租户）不门控；业务租户一律门控，超管身份在租户里不成立，没有例外。
- 租户未绑定版本、或版本白名单为空时，生效权限为空。
- 门控缓存不可用时直接查库，不因缓存故障放行。

性能上，门控白名单走**独立的版本门控缓存**（`SaasEditionGateCacheItem`，缓存名 `SaasCacheNames.EditionGate` = `basicapp:saas:tenancy:edition-gate`，10 分钟 TTL），在 per-user 授权快照缓存**之外**按当前租户上下文叠加，避免切换租户后缓存串味，鉴权热路径不必每请求查库。版本白名单/租户换版的写路径会调 `InvalidateEditionGateAsync` 失效缓存（事务提交后生效）。

### 开通一站式：建管理员 + 角色 + 授权

创建租户时若同时提供 `AdminUserName` + `AdminPassword`（此时 `AdminEmail` 必填且须为有效邮箱），`CreateTenantAsync` 会调 `ProvisionTenantAdminAsync` 一站式开通（`TenantProvisionDomainService`）。写入按数据归属分作用域：租户注册表与版本白名单是平台数据，在平台作用域读写；管理员账号、成员关系、Owner 角色与授权绑定是新租户的数据，切入该租户（`ICurrentTenant.Change(tenantId)`）写入，行的 `TenantId` 由作用域决定，不预置：

1. **确保版本**：租户未指定则取默认版本并回写 `SysTenant.EditionId`；
2. **建管理员**：创建 `SysUser`（校验邮箱全局唯一）+ `SysUserSecurity`（密码哈希）+ `SysTenantUser`（`MemberType=Owner`、`InviteStatus=Accepted`）；
3. **建 Owner 角色并按白名单授权**：创建角色 `tenant_owner`（数据范围 `All`），把该版本白名单里的有效权限批量写成 `SysRolePermission`（`Grant`）；
4. **绑定**：把管理员挂到 Owner 角色（`SysUserRole`）。

于是新租户开通即"能登录、有 Owner、拥有版本范围内的全部权限"，无需人工逐项授权。

#### 库隔离租户的开通

`Database` 隔离的租户创建出来时 `ConfigStatus` 是 `Pending`，独立库要等 `InitializeDatabase` 才建（建库是 DDL，不能包在事务型工作单元里，所以是独立一步）。库隔离租户的账号与授权数据该落在平台库还是租户库（实体归置）尚未完成，开通时显式拒绝，不再像过去那样在平台作用域预置租户戳、把它的数据写进平台库。

`InitializeDatabase` 建的是这个租户**一整套**布局：主库，加上它按约定自带的模块库。主连接下配了 `ModuleDataSourceConfigs` 的模块（如 `Erp`），租户也会有一个对应的库，库名由租户主库名派生成 `{租户库名}_{模块名}`——租户库叫 `qqq`，就还会建一个 `qqq_Erp`。主连接那条模块连接串留空（该模块不分库）时租户同样不分，模块表落它自己的主库。

这条约定由框架实现（`XiHan:Data:SqlSugarCore:EnableTenantModuleDatabaseConvention`，默认开），应用侧不写代码、不加配置。含义是：**租户声明了库隔离，它的数据就都在它自己的库里**，不会有一部分悄悄落回公共模块库。要把某个租户的模块库指到别的机器上，在 `ISqlSugarTenantConnectionProvider` 里显式给出 `ModuleDataSourceConfigs` 即可，显式的优先。

账号定位是跨租户的身份层操作：登录在平台作用域按全局唯一邮箱显式跨租户定位账号（`GetByEmailGloballyAsync`），账号的安全信息在账号归属租户内读写（见上文「登录与落点」）；`ExistsEmailGloballyAsync` / `ExistsUserNameInTenantAsync` 自身切到平台作用域、显式跨租户执行，租户范围由入参显式落进 `WHERE`，不依赖当前上下文。

### 降级自动回收越权授权

当版本白名单**收窄**（撤销/停用某条版本权限映射），系统会**自动回收该版本下各租户存量的越权授权**（`ReconcileEditionTenantsAuthorizationAsync` → `ReconcileTenantAuthorizationWithEditionAsync`）：

- 触发点：`RevokeTenantEditionPermissionAsync`（撤销）、`UpdateTenantEditionPermissionStatusAsync`（映射被置为非 `Valid`）。恢复为有效则无需回收。
- 回收动作：把该租户下**超出白名单**的 `SysRolePermission` / `SysUserPermission` 存量行置为 `Invalid` 并记 `ExpirationTime` 与回收原因（"套餐变更回收：超出当前版本权限白名单"）。
- 边界：只回收该租户**自有**绑定行（`TenantId=本租户`）；全局行（`TenantId=0`）属平台运维资产，不在回收范围。白名单为空时运行时已一律拒绝，存量绑定保留不回收，白名单恢复后随之恢复。

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

- 授权快照只取当前上下文的绑定行（严格租户隔离），按作用侧裁掉不在当前上下文生效的权限，业务租户里再与版本白名单取交集。
- 只有平台不受版本门控；超管的 `*` 只在平台成立。
- 版本白名单只挂平台级全局权限，且作用侧必须含租户。

## 下一步

- [权限模型](./permission)：RBAC + ABAC、权限码、数据范围、字段脱敏
- [系统架构](./introduction)：租户解析在请求管道中的位置
- [XiHan.Framework.MultiTenancy](https://framework.docs.xihanfun.com/packages/multitenancy)：框架层租户上下文、解析链与存储
