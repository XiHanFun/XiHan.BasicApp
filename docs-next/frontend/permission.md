# 权限与脱敏

**安全边界在后端**：前端只决定「显示什么」，让没有权限的入口不出现、受保护的字段如实显示打码值。隐藏按钮不能代替后端授权。

## 权限数据

登录后守卫首次进入时调 `auth.permissions`，一次拿到：

| 项 | 用途 |
| --- | --- |
| 有效权限码 | 按钮与页面门控（已按 Deny、有效期、作用侧、套餐收窄） |
| 角色 | 展示 |
| 菜单树 | 侧栏与动态路由 |
| 页面码 | 页面级门控 |

刷新令牌后权限清空，下一次导航重取；切换租户、模仿开始与结束后整页重建。

## 按钮即操作

6.0 没有第二套按钮码：一个写动作对应一个操作，门控直接用该操作的权限码。

```ts
const auth = useAuthStore()
auth.can('identity.users.create')       // 精确匹配，不认通配
```

```vue
<XhButton v-if="auth.can('settings.configs.create')">新建</XhButton>
```

SchemaPage 的 `actions[].permission`、`removePermission`、`statusPermission`、`exportPermission`、`importPermission` 直接填权限码。也可以用 `usePermission()` 的 `hasPermission(code | codes)`。

权限码都来自生成的 `~/contracts` `permissions.ts`：`pnpm permissions:check` 扫描 `permission:`、`can('…')`、`hasPermission('…')` 里的三段点分字面量，不存在即失败——后端改名或删掉一个权限码，前端引用处在 CI 里就会暴露。

### 组合判断

很多入口要同时看两个权限，例如用户页的「分配角色」：

```ts
{ key: 'assignRoles', permission: 'access.roles.assign', visible: row => isOwnSide(row) && auth.can('access.roles.read') }
```

另一侧的数据（平台看到的租户数据、租户看到的平台数据）通常只读，用 `visible` 表达。

## 权限定义表

`permissionDefinitions` 带每个权限码的显示名与说明（按后端消息目录本地化），授权界面（角色的权限分配抽屉、用户直授、套餐白名单、委托）直接用它生成，不再手写权限树。

## 字段安全与脱敏

后端在投影时已经按字段安全规则打码，前端拿到的就是打码后的值：

| 情况 | 前端表现 |
| --- | --- |
| 隐藏 | 字段为空，列显示占位 |
| 星号 / 部分脱敏 / 哈希 / 固定文本 | 原样显示后端给的值 |
| 只读 | 编辑表单置灰；交回原值、打码值或空值时后端保留原值 |
| 按受保护字段筛选或排序 | 后端拒绝（如 `IDENTITY_DENIED_011`），页面提示 |

前端**不复制**字段安全的判定逻辑，也不尝试「还原」打码值。规则管理在 `/access/field-security`，见[数据权限与字段安全](../backend/data-permission)。

## 数据范围

同样只在后端：列表、详情与批量操作按数据范围收窄，前端看到的就是收窄后的结果。

## 下一步

- [菜单与路由](./routing) · [后端权限管理](../backend/permission)
