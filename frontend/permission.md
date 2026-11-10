来源：https://basicapp.docs.xihanfun.com/frontend/permission

# 权限与脱敏

本页说明前端如何使用后端下发的权限与字段安全结果，适用于前端开发者。

安全边界在后端。前端只决定显示哪些内容：没有权限的入口不显示，受保护的字段按后端给出的值显示。隐藏按钮不能代替后端授权。

## 权限数据

登录后守卫首次进入时调 `auth.permissions`，一次取得：

| 项 | 用途 |
| --- | --- |
| `permissions` | 有效权限码（`*` 已展开为本侧目录的全部码），按钮与页面门控 |
| `roles` | 本侧生效的角色，展示用 |
| `menus` | 按权限裁剪的菜单树，生成侧栏与动态路由 |
| `pages` | 可访问的页面码 |
| `isPlatform` | 是否平台侧 |

结果存在 `~/stores` 的 auth store。刷新令牌后权限清空，下一次导航重取，角色指派的变化随之生效；切换租户、切换激活角色、开始或结束模仿后整页重建（`relaunchApp`）。

## 按钮即操作

一个写动作对应一个操作，门控直接用该操作的权限码，没有另一套按钮码：

```ts
const auth = useAuthStore()
auth.can('identity.users.create') // 精确匹配，不认通配
```

```vue
<XhButton v-if="auth.can('settings.configs.create')">新建</XhButton>
```

SchemaPage 的 `actions[].permission`、`removePermission`、`statusPermission`、`exportPermission`、`importPermission` 直接填权限码。也可以用 `~/hooks` 的 `usePermission()`：`hasPermission(code | codes)`（数组是「或」，单个空串表示不限制）。

权限码都来自生成的 `~/contracts`（`permissions.ts`）：`pnpm permissions:check` 扫描 `permission:`、`permissions:`、`can('…')`、`hasPermission('…')` 里的三段点分字面量，不在契约里即失败。后端改名或删除权限码时，前端引用处在 CI 中报错。

### 组合判断

有的入口要同时看两个权限，例如用户页的「分配角色」：

```ts
{ key: 'assignRoles', permission: 'access.roles.assign', visible: row => isOwnSide(asRow(row)) && auth.can('access.roles.read') }
```

另一侧的数据（平台看到的租户数据、租户看到的平台数据）通常只读，用 `visible` 表达。

## 权限名称

`permissionDefinitions` 是导出契约时生成的权限码表（码与英文的显示名、说明），只用来核对权限码，不用来显示。界面上的权限名与说明一律取权限目录 `permissions.list`：后端按请求的 `Accept-Language` 从各包语言文件取文案。

授权界面（角色的权限分配、用户直授、套餐白名单、委托与约束规则的候选）直接用目录生成条目，不手写权限树。只拿到权限码的地方（角色详情里的已授与继承权限、委托列表、授权条件）用 access 模块的 `usePermissionNames()` 换名字（同一界面语言只取一次目录）；不在本侧目录里的码，或没有 `access.permissions.read` 时，原样显示权限码。

## 字段安全与脱敏

后端在投影时已按字段安全规则处理，前端收到的就是处理后的值：

| 情况 | 前端表现 |
| --- | --- |
| 隐藏 | 字段为空，列显示占位；非文本字段只能隐藏 |
| 星号 / 部分脱敏 / 哈希 / 固定文本 | 原样显示后端给的值 |
| 按受保护字段筛选、排序或关键字搜索 | 后端拒绝（`ACCESS_DENIED_006`），页面提示 `detail` |
| 修改受保护字段 | 交回原值、脱敏值或空值视为没改，后端保留原值；改了只读字段即拒绝（`ACCESS_DENIED_007`），新建时填写只读字段同样拒绝 |

前端不复制字段安全的判定逻辑，也不尝试「还原」打码值。规则在 `/access/field-security` 管理，见[数据权限与字段安全](../backend/data-permission)。

## 数据范围

数据范围同样只在后端生效：列表、详情与批量操作按数据范围收窄，前端收到的是收窄后的结果。

## 下一步

- [菜单与路由](./routing) · [后端权限管理](../backend/permission)
