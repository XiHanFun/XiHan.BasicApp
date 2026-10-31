# 权限管理

本页说明 6.0 的权限码、角色、授权规则、套餐门控与菜单，适用于后端开发者与权限配置人员。6.0 的授权建立在框架之上，由 Access 包逐层收窄：

```text
框架 PermissionChecker
  ├─ 授予：框架 Security.Database 的 PermissionGrantRecord（角色与用户的 Grant）
  ├─ 授予来源 IPermissionGrantSource：AccessGrantSource（上级角色、限时委托带来的授予）
  └─ 收窄 IPermissionFilter（命中授予之后逐个 AND）
       ├─ AccessPermissionFilter：Deny、有效期、作用侧
       └─ EditionPermissionFilter：套餐白名单（只对租户主体）
Authorize 阶段之后
  └─ PermissionConditionMiddleware：挂在授权上的 ABAC 条件，按请求属性精确判定
```

数据范围与字段安全不在这条链上，由各包在查询与投影处显式套用，见[数据权限与字段安全](./data-permission)。

## 权限码

文法 `<包>.<资源>.<动作>`，全部小写点分，例如 `identity.users.read`、`access.roles.grant`、`tenancy.tenants.transfer-owner`。

每个包在 `<包>Permissions.cs` 中用 `PermissionDescriptor` 声明权限码（码、显示名、作用侧、是否审计、说明），并在 `Configure` 时登记到框架。权限目录、菜单门控、套餐白名单与前端的 `permissions.ts` 都来自这份登记。页面上的一个写动作对应一个操作，前端直接用该操作的权限码控制按钮，6.0 不再有单独的按钮码。

| 属性 | 说明 |
| --- | --- |
| 作用侧 | `Platform` / `Tenant` / `Both`，平台主体与租户主体只能命中各自作用侧的权限。任务调度、缓存管理、租户管理为 `Platform`，用户级数据范围为 `Tenant`，多数目录类为 `Both` |
| 审计 | `audited: true` 的操作写操作日志 |

撰写时共有 164 个权限码，可在权限目录页（`/access/permissions`，操作 `permissions.list`）按包、作用侧与审计筛选查看。

权限码没有通配匹配的语义：前端 `can('identity.users.create')` 精确匹配；后端只有初始超级管理员持有框架的全局授予 `*`（同样会被 Deny 与套餐收窄）。

## 角色

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `roles.create` / `update` / `delete` / `get` / `list` | `access.roles.create` / `update` / `delete` / `read` | 角色码 2–64 位小写，创建后不可改；`OwnerTenantId` 区分平台角色与租户角色 |
| `roles.grant` | `access.roles.grant` | 授予权限码（1–200 个），可带生效 / 到期与原因 |
| `roles.deny` | `access.roles.grant` | 拒绝：撤掉同码授予并写 Deny |
| `roles.revoke` | `access.roles.grant` | 解除授予或拒绝 |
| `roles.assign` / `unassign` / `update-member` | `access.roles.assign` | 指派成员（用户注册地须与角色同一侧），指派可带有效期 |
| `roles.members` / `roles.by-user` / `roles.hierarchy` | `access.roles.read` | 成员、某用户的角色、继承关系 |
| `roles.set-parents` | `access.roles.set-parents` | 设置直接上级（最多 50 个），只存直接边，图上推导，拒绝成环 |

内置平台角色 `admin`（种子 `access.platform`）持有全部权限，数据范围「全部」。

### 合并优先级

```text
用户 Deny  >  用户 Grant  >  角色 Deny（本角色链）  >  角色 Grant
```

- Grant 行写框架的 `PermissionGrantRecord`；Deny 以及带有效期或原因的授予另有伴随行 `SysPermissionRule`（每个「侧 × 主体 × 权限码」一行）。
- 有效期在判定时按当前时间比较，到点即失效，不必重建缓存；规则与指派按主体缓存 60 秒，写入方即时失效。
- 判定用的角色取「令牌里激活的角色」与「有效指派」的交集：解除与到期立即生效；新指派的角色等下一次刷新令牌进令牌。

### 继承与委托

角色继承时，上级角色的授予归子角色的持有者所有。这些授予经框架的 `IPermissionGrantSource` 交给检查器，判定仍由过滤器按完整的授予集合给出，Deny 同样优先于继承得到的授予。数据范围与字段安全不继承。

限时委托（`delegations.create` / `list` / `revoke` / `delete`，权限 `access.delegations.*`）由委托人把当前持有的一个权限码或一个角色委托给同侧的另一个用户，必须设置到期时间，被委托人在有效期内获得该授予。委托不包含数据范围与字段安全。

## 用户直授

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `user-grants.list` | `access.user-grants.read` | 某用户的直接授予与拒绝 |
| `user-grants.grant` / `deny` / `revoke` | `access.user-grants.grant` | 同一权限码只能在授予或拒绝一侧 |

## 套餐门控

租户主体的有效权限与所在租户套餐的白名单求交（Tenancy 经 `Contracts` 暴露套餐网关，Access 的 `EditionPermissionFilter` 消费）。作用于 `auth.permissions`、`permissions.list` 与判定本身。租户未绑定套餐即不受限；新租户缺省绑定 Free 套餐。

::: tip 种子只首写
套餐白名单在套餐创建时按当时的权限目录写入，之后新增的权限码不会自动进已有套餐。升级后要在套餐页给相应套餐放行新权限。
:::

## 菜单

菜单从各包的 `<包>Pages.cs` 页面登记汇总而来（种子 `access.menus` 每次对齐）：

- 页面节点的来源字段（页面码、路由、组件路径、所需权限）归登记，展示字段（标题、图标、排序、是否缓存、是否固定标签、上级目录）可以在菜单管理里定制，`menus.reset` 恢复登记值；
- 运营可以另建目录与外链节点；
- 没有按钮节点，也不给角色另行分配菜单：菜单可见性完全由页面的权限码决定。

`auth.permissions` 一次返回有效权限码、角色、按权限裁剪的菜单树与页面码，前端据此装载动态路由。

## 权限变更日志

角色授予与撤销、Deny、成员加入与移出（含有效期）、上级角色、委托、用户直授、数据范围、字段安全、删除角色、套餐白名单、租户换套餐都写权限变更日志（只在调用成功时），见[日志审计](./logging)。

## 下一步

- [数据权限与字段安全](./data-permission) · [访问策略与权限条件](./access-policy) · [约束规则与审批](./approval)
