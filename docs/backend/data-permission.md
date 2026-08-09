# 数据权限

权限码回答「**能不能做**这个操作」，数据权限回答「**能对哪些行做**」「**能看到哪些字段**」。两者是独立的维度——有权限码但查不到数据、能查到但某些列被打码，都是数据权限在起作用。

权限码本身见 [权限管理](./permission)。

## 两个维度

```text
一次请求要过的门：

  权限码        能不能调这个接口          → 不过：403
     ↓
  数据范围      能操作哪些行              → 不过：查不到 / 改不动（不是 403）
     ↓
  字段级安全    能看哪些列、能改哪些列    → 不过：值被置空或被打码
```

::: warning 「有权限但列表是空的」不是 bug
返回 `200` 且 `items` 为空、而不是 `403`——这是数据范围在起作用。排查方向是数据范围配置，不是权限码。
:::

## 数据范围（行级）

范围枚举是 `DataPermissionScope`：角色的档位写在 `SysRole.DataScope`；`SysUser.DataScopeOverride` 是用户级档位，取 `Custom` 时才能维护用户级部门明细。

| 范围 | 含义 | 依赖 |
| --- | --- | --- |
| `SelfOnly` | 仅本人数据 | — |
| `DepartmentOnly` | 本部门 | `SysUserDepartment` 的**全部有效部门归属** |
| `DepartmentAndChildren` | 本部门及下级 | 部门**闭包表** |
| `All` | 全部数据（仍受租户过滤器约束） | — |
| `Custom` | 指定部门集合 | `Sys_Role_Data_Scope` / `Sys_User_Data_Scope` |

解析入口是 `IUserDataScopeFilterService.ResolveAccessibleUsersAsync`，当前由用户列表查询（`UserQueryService`）调用；持有 `super_admin` 角色的用户不受数据范围限制。

### 合并规则

```text
角色的数据范围        →  多角色取并集；任一角色为 All 即放行全部
        +
用户级自定义部门      →  Sys_User_Data_Scope 的部门按 Custom 并入同一并集
        ↓
一个部门都没命中      →  退回仅本人
```

用户级自定义部门要求 `SysUser.DataScopeOverride = Custom` 才能维护，用于「这个人比较特殊」的场景，不要拿它当常规手段——角色才是可维护的授权单位。

### 依赖组织架构

`DepartmentOnly` 与 `DepartmentAndChildren` 完全依赖 [组织架构](./organization)：

- `DepartmentOnly` 取用户在当前租户下**全部状态有效的部门归属**（`SysUserDepartment`，不区分 `IsMain`）；
- `DepartmentAndChildren` 在此基础上靠**部门闭包表**一次展开整棵子树。

::: danger 闭包表失真 = 数据范围算错
闭包表是查询加速镜像，**所有写入必须由 `SysDepartment` 变更触发**。绕过服务层直改部门表会让闭包表失真，表现是某些下级部门的数据看不到、或看到了本不该看到的。

修复要重建该子树的闭包，不要手工补行。
:::

### 组织变更的连锁影响

| 变更 | 影响 |
| --- | --- |
| 移动部门 | 闭包表重建 → 该子树下所有人的 `DepartmentAndChildren` 范围变化 |
| 改用户部门归属 | 该用户的 `DepartmentOnly` / `DepartmentAndChildren` 范围变化 |
| 改角色数据范围 | 该角色下所有用户的可见范围变化 |

数据范围每次请求实时从库解析，不进授权快照；角色写入路径会失效授权快照（`InvalidateAuthorizationAsync`），部门写入路径失效组织缓存（`InvalidateOrganizationAsync`）。

## 字段级安全（列级）

`SysFieldLevelSecurity` 按「资源 × 字段 × 主体」定义规则：

| 字段 | 说明 |
| --- | --- |
| `fieldName` | 字段名（对应实体 / DTO 的属性名） |
| `isReadable` | 能否读取。不可读 → **值被服务端置空或按脱敏策略改写**，属性本身仍在响应里 |
| `isEditable` | 能否编辑。不可编辑 → 后端写校验直接拒绝（`EnsureEditableAsync` / `EnsureUpdatableAsync` 抛异常），前端表单置只读 |
| `maskStrategy` / `maskPattern` | 脱敏策略与掩码格式（部分脱敏用 `keep:N,M`，固定替换 / 自定义时 `maskPattern` 即占位符文本） |

同一字段命中多条规则时按 deny-overrides 合并：任一条不可读即不可读、任一条不可编辑即不可编辑，脱敏取最严的一条。

### 脱敏在服务端完成

::: danger 后端返回的就是打码后的值
如 `138****8000`。**前端不做二次打码**——前端拉取规则只用于两件事：表单置只读、展示「不可见 / 已脱敏」标识。

这条设计的意义：绕过前端直接调接口也拿不到明文。前端打码是纸糊的。
:::

脱敏由 `IFieldSecurityService.ApplyAsync` 在查询服务返回前就地改写 DTO（当前接入的资源是 `SysUser`）。

### 过滤与排序也受门控

读侧会经 `IFieldSecurityService.GuardFiltersAsync` / `GuardSortsAsync` 处理：

```text
前端下发的 filters / sorts
        ↓
剔除「不可读或已脱敏」字段的条件
        ↓
剔完没有有效排序 → 回退该接口的默认排序
```

::: warning 「排序点了没反应」优先查字段权限
被 FLS 剔掉时后端**不会报错**，只是安静地不生效。同理「按某字段搜索没效果」也可能是这个原因，而不是前端 bug。
:::

### 页面怎么接

前端 `PageSchema` 声明 `resourceCode`，页面调用 `useFieldSecurity(resourceCode)` 拉取当前用户的有效规则，再按 `isEditable` / `isReadable` 决定表单只读与「不可见 / 已脱敏」标识。

::: warning `resourceCode` 缺省则不拉规则
`useFieldSecurity` 在 `resourceCode` 为空时直接跳过请求，页面拿不到任何规则。需要 FLS 的页面记得写上。
:::

## 写路径的租户边界

除了行级与列级，还有一条容易忽略的边界：**读共享 ≠ 写共享**。

```text
读：全局过滤器放行 TenantId IN (0, 当前租户)   ← 租户能读到平台全局数据
写：禁止改写/删除非本租户行（含 TenantId=0 的全局行）
```

实现方式：预读守卫校验取回行的 `TenantId`，条件写自动追加当前租户 `Where`。

维护全局 / 跨租户数据的**唯一合法入口是平台态**（无租户上下文，`ICurrentTenant.Change(null)`）。

## 排查

| 现象 | 查什么 |
| --- | --- |
| 有权限但列表为空 | 用户部门归属对不对 → 角色数据范围设成了什么 → 闭包表是否完整 |
| 某些行改不动 | 是否跨租户写（读得到不等于写得动） |
| 列变成 null / 空 | FLS `isReadable` 为假，服务端已把值置空 |
| 列显示成 `***` | FLS 脱敏策略生效，服务端已打码 |
| 排序 / 搜索不生效 | 该字段被 FLS 剔出条件 |
| 改了数据范围不生效 | 该列表是否接了数据范围过滤；用户级自定义部门需 `SysUser.DataScopeOverride = Custom` |
| 表单该只读却可编辑 | 页面 `PageSchema.resourceCode` 没声明，或页面没调用 `useFieldSecurity`（后端写校验仍会拒绝） |

## 相关页面

- [权限管理](./permission)：权限码、RBAC 继承、ABAC、判定链
- [组织架构](./organization)：部门树与闭包表
- [多租户 SaaS](./multi-tenancy)：租户隔离与写路径边界
- [前端手册 · 权限与脱敏](../frontend/permission)：前端侧怎么接
