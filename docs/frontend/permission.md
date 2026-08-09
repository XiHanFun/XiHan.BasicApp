# 权限与脱敏（前端）

前端在**页面 / 字段 / 操作**三个层级按权限码过滤，并在服务端脱敏的基础上做只读与标识提示。本页讲前端这一侧怎么写；判定规则本身（权限码、数据范围、ABAC、约束规则）见 [权限模型](../backend/permission)。

## 前提：前端过滤是体验，不是安全边界

::: danger 服务端才是安全边界
前端隐藏按钮只是**不让用户看到无效入口**。真正的门控在服务端：`[PermissionAuthorize]` 特性 + 授权快照判定。

绕过前端直接调接口一定会被后端拒绝——所以**不要因为前端已经隐藏了按钮就省掉后端的权限标注**。
:::

## 权限码

以 `:` 分段，多数是 `module:resource:action`（如 `saas:user:read`、`saas:tenant-edition-permission:grant`），也有两段的 `module:action`（如 `workflow:execute`）。超管用**字面通配 `*`**（不是段级 `*:*:*`）。

**真源在后端** `SaasPermissionDefinitions`（Saas 模块）与各模块自己的权限码类。前端只是消费者——写权限码字符串时去后端对一下，别凭记忆写。

用户的权限码集合来自登录后的 `GET /api/Auth/Permissions`。

::: tip 权限码不在 JWT 里
Access Token 里没有具体权限码（超管例外，只放一个 `*` 作快路径）。这带来一个好性质：**管理员改了授权，用户不用重新登录就生效**——前端下次拉取权限或后端下次判定时就是新的。
:::

## 三级过滤

### 一、页面级

由路由守卫按 `meta.roles` / `meta.permissions` 校验，无权跳 403。数据来自后端 `PageDescriptor.PermissionCode`。

`PermissionCode` 为空的页面是**纯展示菜单**，不做门控。

### 二、字段级

Schema 页里字段声明 `permission`，**无权时该列与该搜索项整个不渲染**（`selectors.ts` 的 `isFieldPermitted`）：

```ts
{ key: 'phone', title: t('identity.org.col_phone'), dataType: 'string',
  permission: 'saas:department:read' }
```

注意这是「**不渲染**」而不是「渲染成空」——用户看不到这一列存在。

### 三、操作级

- Schema 页的 `actions[].permission`：无权时按钮不出现。
- `PageSchema` 上的 `exportPermission` / `importPermission` / `removePermission`（批量删除）/ `statusPermission`（批量启停）：门控对应的内置按钮。其中只有 `exportPermission` 是**严格门控**（未声明就不显示导出），另外三个未声明时不做限制。

::: warning `exportPermission` 未声明 = 不显示导出
这是刻意的默认值选择：宁可漏显示，不可错显示。要导出按钮就必须声明权限码。
:::

### 页面里手写门控

没有包裹式权限组件——页面按钮/区块级门控统一是「`usePermission()` 派生 computed + 模板 `v-if`」这一种写法：

```ts
const { hasPermission } = usePermission()
const canGrant = computed(() => hasPermission('saas:tenant-edition-permission:grant'))
```

```vue
<NButton v-if="canGrant">分配权限</NButton>
```

`usePermission()`（`~/hooks/usePermission.ts`）提供：

| 方法 | 说明 |
| --- | --- |
| `hasPermission(code \| code[])` | 是否有权限码（传数组时任一命中即通过） |
| `hasRole(role \| role[])` | 是否有角色（传数组时任一命中即通过） |
| `hasAnyPermission(codes[])` | 任一命中即通过 |

::: tip 统一用这一种写法
不要另造包裹组件或指令。全仓统一「computed + `v-if`」的好处是：审计权限点时一个 grep 就能找全，也不会出现「指令生效时机与渲染时机不一致」的坑。
:::

## 字段级脱敏（FLS）

### 脱敏在服务端完成

**后端返回的敏感字段已经是打码后的值**（如 `138****8000`），前端**不再二次打码**。

前端侧的 `useFieldSecurity(resourceCode)`（`~/components/schema/useFieldSecurity.ts`）由页面自行调用：传入后端资源码，再调 `resolve()` 拉取当前用户在该资源上的有效字段规则（`GET /MyFieldSecurity/Mine?resourceCode=`）。它返回 `ruleFor` / `isReadable` / `isEditable` / `resolve`，用途只有两个：

1. 表单按 `isEditable(fieldKey)` 置**只读**；
2. 展示「不可见 / 已脱敏」的**标识**。

规则形如：

```ts
{ fieldName, isReadable, isEditable, maskStrategy, maskPattern }
```

端点未就绪或无规则时**默认放行**（`isReadable` / `isEditable` 缺省 `true`）。

::: warning `SchemaPage` 不会自动接线 FLS
`useFieldSecurity` 是独立 hook，`SchemaPage` 不会自动调用它——`PageSchema.resourceCode` 只用于导入留痕。要按 FLS 置只读，得在页面里自己 `useFieldSecurity(...)` + `resolve()` 并把 `isEditable` 接到表单上。
:::

### FLS 也门控过滤与排序

服务端读侧会经 `GuardFiltersAsync` / `GuardSortsAsync` 剔除**不可读或已脱敏**字段的过滤与排序条件，剔完没有有效排序时回退默认排序。

所以「排序点了没反应」「按某字段搜索没效果」优先怀疑字段权限，而不是前端 bug。

## 多租户

数据按 `TenantId` 隔离，全局数据约定 `TenantId=0`。**前端不需要手动处理租户过滤**——它由后端在请求管道里完成，前端只按拿到的权限码集合渲染。

前端要处理的只有两件事：

| 事情 | 说明 |
| --- | --- |
| 租户切换 | 调 `POST /api/Auth/SwitchTenant`（`tenantId` 传 `null` 回平台态），**换发令牌但不产生新登录/新设备** |
| 平台态判断 | `UserInfo.isPlatform`（当前是否平台运维态）、`canAccessPlatform`（能否进入） |

切换后要重新拉取权限与菜单——租户不同，可用的功能也不同（受[租户版本门控](../backend/multi-tenancy)影响）。

## 排查

| 现象 | 原因 |
| --- | --- |
| 按钮不显示 | 权限码写错（去后端 `SaasPermissionDefinitions` 对）；或用户确实没这个权限 |
| 导出按钮不显示 | `PageSchema.exportPermission` 没声明 |
| 列不显示 | 字段 `permission` 无权；或 `visible: false` |
| 表单该只读却可编辑 | 页面没调 `useFieldSecurity(...)` + `resolve()`，或没把 `isEditable` 接到表单字段上 |
| 前端能点、后端 403 | 正常——前端过滤是体验层，以后端为准。检查该接口的 `[PermissionAuthorize]` 是否与前端用的码一致 |
| 改了授权前端还是旧的 | 重新拉 `/api/Auth/Permissions`；后端侧检查是否调了 `InvalidateAuthorizationAsync` |

## 相关页面

- [权限模型](../backend/permission)：权限码、RBAC 继承、数据范围、ABAC、约束规则
- [Schema 驱动页面](./schema-page)：字段与操作的权限声明位置
- [路由与菜单](./routing)：页面级门控
- [多租户与版本](../backend/multi-tenancy)：版本门控与租户切换
