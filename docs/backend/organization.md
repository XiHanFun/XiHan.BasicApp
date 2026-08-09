# 组织架构

组织架构回答的是「**这个人在公司里是谁**」：属于哪个部门、担任什么岗位、能看到哪个范围的数据。它是数据范围（`DepartmentOnly` / `DepartmentAndChildren`）的底座，也是消息定向的依据。

> 用户主体与登录见 [身份与认证](./authentication)；数据范围如何参与鉴权见 [权限模型](./permission)；跨租户的成员关系见 [多租户与版本](./multi-tenancy)。

## 三个主体

| 实体 | 表 | 定位 |
| --- | --- | --- |
| `SysDepartment` | `Sys_Department` | 组织架构树节点（**严格单父树**） |
| `SysDepartmentHierarchy` | `Sys_Department_Hierarchy` | 部门层级**闭包表**（查询加速镜像） |
| `SysPosition` | `Sys_Position` | **扁平**岗位字典（职务/职位），不成树 |
| `SysUserDepartment` | `Sys_User_Department` | 用户 ↔ 部门的多对多归属，承载岗位、工号、职级、入职日期 |

前端页面：`/identity/org`（组织机构）与 `/identity/position`（岗位管理）。

## 部门树与闭包表

### 为什么需要闭包表

数据范围 `DepartmentAndChildren`（本部门及下级）要回答「某部门的全部后代」。在邻接表（只有 `ParentId`）上这是递归查询，深树上很慢且难以下推到 SQL。

`SysDepartmentHierarchy` 把**所有「祖先-后代」对预计算出来**（含 `Depth=0` 的自环），一次 `WHERE AncestorId = ?` 就能 O(1) 展开整棵子树。表里还冗余了 `Path` / `PathName` 便于直接渲染面包屑。

### 闭包表是镜像，不是业务数据

**所有写入必须由 `SysDepartment` 的变更触发**，`DepartmentDomainService` 在部门增/删/移时清空当前租户的闭包记录，再按全量部门整体重算（不是按子树增量维护）：

```text
新增部门        → 清空闭包记录，按全量部门重建
修改部门父级    → 清空闭包记录，按全量部门重建（仅 ParentId 变化时触发）
删除部门        → 清空闭包记录，按全量部门重建
```

重建时逐个部门回溯祖先链，遇到自身重复（环路）或父级缺失会直接抛异常终止，不会写出半截闭包。仅改名称、联系方式、排序或启停状态不触发重建。

::: warning 绕过服务层直改 `Sys_Department` 会让闭包表失真
表现是数据范围算错——某些下级部门的数据看不到，或看到了本不该看到的。修复要走服务层重建闭包表，不要手工补行。
:::

## 岗位

岗位是**扁平字典**，不参与层级、不参与数据范围计算。它的作用是描述「这个人在这个部门里做什么」，被 `SysUserDepartment.PositionId` 引用。

岗位编码在租户内唯一（`Sys_Position` 的 `TenantId + PositionCode` 唯一索引）。

需要「岗位也有上下级」的场景，用角色层级（`SysRoleHierarchy`）表达，别把岗位改成树。

## 用户归属

`SysUserDepartment` 是多对多关联，一个用户可以属于多个部门：

| 字段 | 说明 |
| --- | --- |
| `UserId` / `DepartmentId` | 关联主体 |
| `IsMain` | **主部门标识**——组织架构展示取主部门（数据范围按全部有效归属计算），一个用户至多一个 |
| `PositionId` | 岗位（可空） |
| 工号 / 职级 / 入职日期 | 人事属性 |

::: tip 主部门唯一性靠服务层保证
表上没有「一个用户只能有一个主部门」的约束，是服务层在设置主部门时把其余归属的 `IsMain` 置 false；用户第一条部门归属会自动成为主部门，主归属被撤销或置无效时自动提升另一条有效归属。批量导入用户时要走同一条服务方法，别直接写表。
:::

## 与数据范围的关系

数据范围是**独立于权限码的另一个维度**：权限码回答「能不能做这个操作」，数据范围回答「能对哪些行做」。

| 数据范围（`DataPermissionScope`） | 含义 | 依赖 |
| --- | --- | --- |
| `SelfOnly` | 仅本人 | — |
| `DepartmentOnly` | 本部门 | `SysUserDepartment` 的**全部有效归属** |
| `DepartmentAndChildren` | 本部门及下级 | 全部有效归属 + **闭包表**展开后代 |
| `All` | 全部数据 | — |
| `Custom` | 指定部门集合 | `Sys_Role_Data_Scope` / `Sys_User_Data_Scope` |

`Custom` 行上的 `IncludeChildren` 决定该部门是否连同后代一起展开。完整判定见 [权限模型](./permission)。

::: warning 「有权限但查不到数据」多半是数据范围
返回 200 且列表为空、而不是 403——这是数据范围在起作用，不是 bug。排查顺序：用户的部门归属对不对 → 角色的数据范围设成了什么 → 闭包表是否完整。
:::

## 组织变更的连锁影响

改组织架构不是改一张表，下面这些会跟着变：

| 变更 | 连锁影响 |
| --- | --- |
| 移动部门 | 闭包表整表重建 → 该子树下所有人的 `DepartmentAndChildren` 范围变化 |
| 增删用户部门归属 | 该用户的 `DepartmentOnly` / `DepartmentAndChildren` 范围变化 |
| 删除部门 | 存在子部门、用户归属、角色/用户数据范围引用或字段级安全引用时直接拒绝，必须先解除；删除后闭包表整表重建 |
| 任一变更 | 需失效 `SaasDepartmentTreeCacheItem`（`InvalidateOrganizationAsync`），涉及授权的还要失效授权快照 |

::: danger 漏失效缓存 = 改完不生效
部门树走 Redis 缓存（`basicapp:saas:organization:dept-tree`）。写侧改完必须调 `ISaasCacheInvalidator.InvalidateOrganizationAsync()`，否则前端组织树、下拉选项都还是旧的。详见 [缓存与异步](./caching)。
:::

## 相关页面

- [身份与认证](./authentication)：用户主体、会话、登录
- [权限模型](./permission)：数据范围如何参与鉴权
- [多租户与版本](./multi-tenancy)：租户成员关系（与部门归属是两件事）
- [数据模型](./data-model#组织与租户)：相关表结构
