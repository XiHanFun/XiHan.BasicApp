# 菜单与路由

本页说明管理端的路由组成、菜单来源、组件解析与路由守卫，适用于前端开发者。

后端菜单驱动动态路由，路由包与 5.x 相同：`packages/router` 的 `guard.ts`、`dynamic.ts`、`routes/core.ts` 原样沿用，只是菜单与权限的数据来源换成 `auth.permissions`，组件按页面登记精确解析。

## 三类路由

| 类型 | 定义位置 | 例子 |
| --- | --- | --- |
| 核心路由 | `packages/router/routes/core.ts` | `/auth/login`、`/auth/code-login`、`/auth/email-login`、`/auth/oauth-callback`、`/403`、`/404`、`/500`、兜底 |
| 应用静态路由 | `src/router/routes.ts` | `RootLayout`（`/`，承载业务页）、`/profile`、`/control-center`、`/oauth/authorize` |
| 动态路由 | 菜单树（`auth.permissions`） | `/identity/users`、`/settings/configs` … |

地址缺省走 hash（`VITE_ROUTER_HISTORY=history` 切 HTML5 历史，静态托管要回退到 `index.html`）。

## 菜单来源

1. 后端各包在 `<包>Pages.cs` 登记页面（页面码、路由、文案键、图标、排序、所需权限码）；
2. 种子 `access.menus` 对齐成菜单，运营可在菜单管理里改展示、加目录与外链；
3. `auth.permissions` 返回按权限裁剪的菜单树、有效权限码、角色与页面码。

## 组件解析

页面节点的 `component` 是 `<包>/<路径>/index`，前端按 `src/modules/*/views//index.vue` 精确匹配：

```text
component: settings/configs/index  →  src/modules/settings/views/configs/index.vue
```

- 登记了组件路径但找不到文件：控制台报错并显示 404 页（侧栏仍显示该条目，报错用于定位配置错误）；
- 视图键全局唯一，撞键由 `pnpm modules:check` 报错；
- `_core/*` 由 `packages/views` 的显式表解析；
- 每个页面包一层「名字 = 路由名」的壳组件，供 KeepAlive 命中。

## 守卫

顺序与 5.x 相同：白名单 → 用户上下文 → 装载路由 → 权限校验 → 标签页。

| 情况 | 处理 |
| --- | --- |
| 未登录 | 带回跳地址送到登录页 |
| 已登录访问认证页 | 回首页 |
| 首次进入（含刷新页面、打开深链） | 调 `auth.permissions` 装载动态路由，按 path + query + hash 重进（深链的查询串与 OAuth 的 `?code=` 不丢） |
| 装载失败 | 进 `/500`；权限没写进 store，下一次导航会重试 |
| 会话锁定 | 直接放行挂壳，由锁屏遮罩盖住，解锁后重新走守卫 |
| 会话已失效 | 回登录页，带上原路径 |
| `meta.permissions` 不在有效权限里 | 进 `/403` |
| 存在待强制阅读的通知 | 由 `inbox.mandatory` 拦截 |
| 独立公共页（`meta.standalone`，如控制中心） | 不挂主布局、不进标签栏 |

刷新令牌后权限清空、下一次导航重取，角色指派的变化随之生效；切换租户、开始或结束模仿后整页重建（`relaunchApp`）。

## 首页

`VITE_HOME_PATH`（仓库缺省 `/workbench/dashboard`）：固定的首页标签与守卫兜底。关掉后端的工作台模块时留空，`/` 落到第一个有权限的菜单。

## 标签页与缓存

访问过的页面进标签栏（首页与固定标签不可关）。标签标题存国际化键，切换语言时跟着变。菜单的「缓存」与「固定标签」两个展示字段在菜单管理里可调，后端登记给缺省值。

## 下一步

- [权限与脱敏](./permission) · [开发流程](./development) · [后端权限管理](../backend/permission)
