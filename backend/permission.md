来源：https://basicapp.docs.xihanfun.com/backend/permission

# 权限管理

本页说明权限码、角色、授权规则、套餐门控与菜单，适用于后端开发者与权限配置人员。授权建立在框架之上，由 Access 包逐层收窄：

```text
框架 PermissionChecker
  ├─ 授予：框架 Security.Database 的 PermissionGrantRecord（角色与用户的 Grant）
  ├─ 授予来源 IPermissionGrantSource：AccessGrantSource（上级角色、限时委托带来的授予）
  └─ 收窄 IPermissionFilter（命中授予之后逐个 AND）
       ├─ AccessPermissionFilter：Deny、有效期、此刻算数的角色、作用侧、模仿态的拒绝清单
       └─ EditionPermissionFilter：套餐白名单（只对租户主体）
Authorize 阶段之后
  └─ PermissionConditionMiddleware：挂在授予上的 ABAC 条件，按请求属性精确判定
```

数据范围与字段安全不在这条链上：数据范围由各包在查询处显式套用，字段安全由 Access 的 `FieldSecurityMiddleware`（Handle 阶段）与响应过滤器统一套用，见[数据权限与字段安全](./data-permission)。

## 权限码

文法 `<包>.<资源>.<动作>`，全部小写点分，资源段等于操作 ID 的资源段，例如 `identity.users.read`、`access.roles.grant`、`tenancy.tenants.transfer-owner`。

每个包在 `<包>Permissions.cs` 中用 `PermissionDescriptor` 声明权限码（码、显示名、作用侧、是否审计、说明），并在 `Configure` 时登记到框架。权限目录、菜单门控、套餐白名单与前端生成的 `packages/contracts/permissions.ts` 都来自这份登记，没有手写的权限或按钮码清单。页面上的一个写动作对应一个操作，前端直接用该操作的权限码控制按钮。

显示名与说明另写在所属包语言文件的 `permissions.<码>.name` / `.description`（权限码的 `-` 换 `_`，10 种区域齐全，`PackageStructureTests` 守着）：权限目录 `permissions.list`、约束规则的目标名与接口文档都按调用方的区域取，描述表里的英文只在回退链用尽时用。

| 属性 | 说明 |
| --- | --- |
| 作用侧 | `Platform` / `Tenant` / `Both`，平台主体与租户主体只能命中各自作用侧的权限（持 `*` 也一样）。任务调度、缓存管理、租户与套餐管理、菜单管理为 `Platform`，用户级数据范围与「我的订阅」为 `Tenant`，多数目录类为 `Both` |
| 审计 | `audited: true` 的操作写操作日志 |

全部权限码可在权限目录页（`/access/permissions`，操作 `permissions.list`）按包、作用侧与审计筛选查看；可选模块关掉后它的权限码不在目录里。

权限码没有通配匹配的语义：前端 `can('identity.users.create')` 精确匹配；后端只有通配授予 `*` 覆盖全部权限（内置角色 `admin` 持有），它同样会被 Deny、作用侧与套餐收窄。

## 角色

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `roles.create` / `update` / `delete` / `get` / `list` | `access.roles.create` / `update` / `delete` / `read` | 角色码 2–64 位，小写字母开头、只含小写字母数字与 `-`，创建后不可改；`OwnerTenantId` 区分平台角色与租户角色 |
| `roles.grant` | `access.roles.grant` | 授予权限码（1–200 个），可带生效 / 到期与原因 |
| `roles.deny` | `access.roles.grant` | 拒绝：撤掉同码授予并写 Deny |
| `roles.revoke` | `access.roles.grant` | 解除授予或拒绝 |
| `roles.assign` / `unassign` / `update-member` | `access.roles.assign` | 指派成员（用户须与角色同一侧：注册在这一侧，或本租户的[外部成员](./multi-tenancy#外部成员)与[运维支持成员](./multi-tenancy#运维支持成员)），指派可带有效期 |
| `roles.members` / `roles.by-user` / `roles.hierarchy` | `access.roles.read` | 成员、某用户的角色、继承关系 |
| `roles.set-parents` | `access.roles.set-parents` | 设置直接上级（最多 50 个），只存直接边，图上推导，拒绝成环 |

内置角色 `admin` 持有通配授予 `*`、数据范围「全部」：平台一侧由种子 `access.platform` 建并指派 `superadmin`，每个租户在 `tenants.initialize-admin` 时建一份并指派租户管理员。

### 合并优先级

```text
用户 Deny  >  用户 Grant ≈ 委托的权限码  >  角色链 Deny（只拿走本链）  >  角色链 Grant
```

- Grant 行写框架的 `PermissionGrantRecord`；Deny 以及带有效期或原因的授予另有伴随行 `SysPermissionRule`（每个「侧 × 主体 × 权限码」一行）。
- 授予按侧存放：平台侧的授予（租户列为空）只对平台调用生效，租户侧的只对该租户生效，两边互不渗透（框架 ADR-0197）；平台账号切换进租户后只按该租户的角色与授予判定，超级管理员的 `*` 也不带进租户。
- 有效期在判定时按当前时间比较，到点即失效，不必重建缓存；规则、指派、继承图与委托在共享缓存里存 60 秒，写入方在工作单元结束后即时失效。
- 判定用的角色取「令牌里激活的角色」与「有效指派」的交集：解除与到期立即生效；新指派的角色等下一次刷新令牌进令牌。
- 授予存储读不到时把失败原样返回（框架给 503 `SECURITY_UNAVAILABLE_001`），不当作没有权限。

### 会话角色

签发令牌时（登录、续签、切换租户）由 Access 的 `ISessionRoleActivator` 决定这次会话激活哪些角色：按角色排序号、再按编码逐个加入，会违反拦截类动态职责分离规则的不激活，激活集记在 `SysSessionRole`，有角色被停掉时记一条登录日志（`RoleActivation`）。用户可经 `auth.session-roles` 查看可激活的角色、`auth.activate-roles` 换激活集；`session-roles.list / get`（`access.session-roles.read`）给管理员查各会话的激活情况。规则写法见[约束规则与审批](./approval)。

### 继承与委托

角色继承时，上级角色的授予归子角色的持有者所有。这些授予经框架的 `IPermissionGrantSource` 交给检查器，判定仍由过滤器按完整的授予集合给出，链上的 Deny 同样优先于继承得到的授予。数据范围与字段安全不继承。

限时委托（`delegations.create` / `list` / `revoke` / `delete`，权限 `access.delegations.*`）由委托人把当前持有的一个权限码或一个角色委托给同侧的另一个用户，必须设置到期时间，被委托人在有效期内、且委托人此刻仍持有时获得该授予；委托不再顺着委托人的委托传下去，也不包含数据范围与字段安全。代他人委托时，操作者自己也要持有所委托的权限或角色（或持 `*`），否则是 `ACCESS_DENIED_012`。

## 用户直授

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `user-grants.list` | `access.user-grants.read` | 某用户的直接授予与拒绝 |
| `user-grants.grant` / `deny` / `revoke` | `access.user-grants.grant` | 同一权限码只能在授予或拒绝一侧，可带有效期 |

## 套餐门控

租户主体的有效权限与所在租户套餐的白名单求交（Tenancy 经 `Contracts` 的 `IEditionGate` 暴露套餐网关，Access 的 `EditionPermissionFilter` 消费）。作用于 `auth.permissions`、`permissions.list` 与判定本身，不在白名单里即 `SECURITY_DENIED_004`。平台主体不门控；租户未绑定套餐即不受限；新租户缺省绑定 Free 套餐（见[多租户 SaaS](./multi-tenancy#套餐与配额)）。

::: tip 种子只首写
套餐白名单在套餐创建时按当时的权限目录写入，之后新增的权限码不会自动进已有套餐。升级后要在套餐页给相应套餐放行新权限。
:::

## 菜单

菜单从各包的 `<包>Pages.cs` 页面登记汇总而来（种子 `access.menus` 每次对齐）：

- 页面节点的来源字段（页面码、路由、组件路径、所需权限）归登记，展示字段（标题、图标、排序、是否缓存、是否固定标签、上级目录、启停、徽标）可以在菜单管理里定制，改过即标为已定制、种子不再覆盖，`menus.reset` 恢复登记值（启用、没有徽标）；登记里已不存在的页面节点随种子删除；
- 运营可以另建目录与外链节点（`menus.create`），都可以放在顶层或某个目录下：目录可以一层层套，只是不能挂到自己或自己的下级目录下，上级也只能是目录（`ACCESS_VALIDATION_006`）；内置的目录与帮助中心的外链（官方文档、GitHub、Gitee）种类相同，种子只首写一次，之后改、停用、删掉都不再覆盖或补回；
- 没有按钮节点，也不给角色另行分配菜单：菜单可见性完全由页面的权限码决定。

菜单管理的权限码 `access.menus.read / create / update / delete` 只在平台一侧，`menus.set-status` 与 `menus.reset` 用 `update`。

页面登记用 `directory` 指定缺省所属目录（编码在 `Contracts` 的 `MenuDirectories`）：没被定制的页面跟着目录走，目录不在即留在顶层，`menus.reset` 回到这个目录。缺省的树是：

- 工作台：单页，在顶层；
- AI 中心：AI 提供商、AI 助手、AI 提示词、AI 知识库；
- 流程中心：审批中心、流程定义、流程实例；
- 集成中心：邮件配置、短信配置，推送服务（机器人配置、Telegram 机器人），开放接口（应用管理、开放接口日志）；
- 消息中心：我的消息、通知公告、消息模板、邮件短信、在线聊天、聊天审计；
- 租户管理：租户、套餐、我的订阅；
- 系统管理：系统权限（部门、岗位、用户、角色、权限目录、授权申请、访问策略、约束规则、字段安全、菜单）、系统设置（参数配置、字典、业务编号）、系统服务（任务调度、缓存管理、版本管理、升级记录）、系统监控（服务监控、在线用户）、系统日志（链路追踪与七类日志）、文件服务（文件管理、存储配置、导出中心）、在线开发（代码生成、打印模板）；
- 帮助中心：关于项目与三个外链，排在最后。

目录只管展示，页面跨包归到一个目录没有问题；裁剪后空了的目录不下发，所以租户侧看不到只装平台页面的「系统服务」。

「在导航里显示」与「启停」是两回事：隐藏的页面仍有路由、可直接访问；停用的节点（`menus.set-status`）连同子树不下发，页面没有路由。徽标由文字（`badge`，最多 50 个字符）、语气（`badgeType`，取组件库的 `neutral`、`brand`、`info`、`success`、`warning`、`danger`）与圆点（`badgeDot`，优先于文字）组成，壳层显示在菜单项右侧。

`auth.permissions` 一次返回有效权限码、角色、按权限裁剪的菜单树（不含停用的节点）与页面码，前端据此装载动态路由。

## 权限变更日志

角色授予与撤销、Deny、成员加入与移出（含有效期）、上级角色、委托、用户直授、数据范围、字段安全、权限条件、约束违例、删除角色、套餐白名单、租户换套餐与所有权转移都写权限变更日志（只在调用成功时），见[日志审计](./logging)。

## 下一步

- [数据权限与字段安全](./data-permission) · [访问策略与权限条件](./access-policy) · [约束规则与审批](./approval)
