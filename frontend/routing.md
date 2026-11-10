来源：https://basicapp.docs.xihanfun.com/frontend/routing

# 菜单与路由

本页说明前端的路由组成、菜单来源、组件解析与路由守卫，适用于前端开发者。

后端菜单驱动动态路由：`packages/router` 提供守卫（`guard.ts`）、菜单 → 路由（`dynamic.ts`）、核心路由（`routes/core.ts`）与视图解析（`views.ts`），菜单与权限的数据来自 `auth.permissions`，组件按页面登记的组件路径精确解析。

## 三类路由

| 类型 | 定义位置 | 例子 |
| --- | --- | --- |
| 核心路由 | `packages/router/routes/core.ts` | `/auth/login`、`/auth/code-login`、`/auth/email-login`、`/auth/forget-password`、`/auth/register`、`/auth/oauth-callback`、`/403`、`/404`、`/500`、兜底 |
| 应用静态路由 | `src/router/routes.ts` | `RootLayout`（`/`，承载业务页）、`/profile`（个人中心，挂在主布局下、不进菜单）、`/control-center`、`/oauth/authorize` |
| 动态路由 | 菜单树（`auth.permissions`） | `/identity/users`、`/settings/configs` … |

地址缺省走 hash；`VITE_ROUTER_HISTORY=history` 切 HTML5 历史（静态托管要回退到 `index.html`），`VITE_BASE` 设部署子路径。

## 菜单来源

1. 后端各包在 `<包>Pages.cs` 登记页面：页面码、路由、文案键、图标、排序、所需权限码与所在目录；
2. 种子把页面对齐成菜单，运营可在菜单管理里改展示、启停、配徽标、加目录（可多级嵌套）与外链；
3. `auth.permissions` 返回按权限裁剪的菜单树（停用的节点连同子树不下发，也就没有路由），以及有效权限码、角色与页面码。

前端把菜单树投影成路由（`toMenuRoutes`）：

| 菜单字段 | 路由 |
| --- | --- |
| `route`、`code` | `path`、`name`（目录与外链没有路由，用 `/<code>` 占位，不生成路由记录） |
| `component` | 页面节点的视图 |
| `title` / `titleKey` | `meta.title`：运营改过或新建的节点给原文，其余给文案键 |
| `hidden`、`keepAlive`、`affix` | `meta.hidden`、`meta.keepAlive`、`meta.affixTab` |
| `permission` | `meta.permissions` |
| `badge`、`badgeType`、`badgeDot` | `meta.badge`、`meta.badgeType`、`meta.dot`，由 `MenuBadge` 渲染 |
| 外链的 `externalUrl` | `meta.link`，点击时新标签打开 |

目录节点自动重定向到第一个可导航的子页。侧栏会展开到当前页：按地址直达、刷新或从标签页切过来时，通往当前页的各级目录自动展开；手动收起的目录在当前页换掉之前不会被重新打开。

## 组件解析

页面节点的 `component` 是 `<包>/<路径>/index`，前端在 `src/modules/*/views/**/index.vue` 里精确匹配，不做多候选猜测：

```text
component: settings/configs/index  →  src/modules/settings/views/configs/index.vue
```

- 壳层自带、不归任何模块的页面在 `packages/router/views.ts` 的 `coreViews` 里逐条登记（关于页 `about/project/index` → `packages/views/_core/about/index.vue`）；
- 登记了组件路径但找不到文件：控制台报错并回落到 404 页（侧栏仍显示该条目，报错用于定位配置错误）；菜单缺路由名的节点同样报错并跳过；
- 视图路径要求 kebab-case 且全局唯一，由 `pnpm modules:check` 检查；
- 每个页面包一层「名字 = 路由名」的壳组件，KeepAlive 按路由名命中缓存，页面组件自己的名字不参与；
- 审批中心按审批业务登记的单据组件路径（如 `access/permission-request-approval/index`）用同一个 `resolveView` 加载。

## 守卫

| 情况 | 处理 |
| --- | --- |
| 未登录 | 认证页与 `/403`、`/404`、`/500` 放行；其余带回跳地址送到登录页 |
| 已登录访问第三方回调页 | 放行（个人中心发起的账号绑定要在这里确认） |
| 已登录访问其他认证页 | 回首页 |
| 首次进入（含刷新页面、打开深链） | 调 `auth.permissions` 装载动态路由，再从后端拉取偏好；按 path + query + hash 重进（深链的查询串与 OAuth 的 `?code=` 不丢） |
| 装载失败 | 进 `/500`；权限没写进 store，下一次导航会重试 |
| 会话锁定 | 直接放行挂壳，由锁屏遮罩盖住，解锁后重新走守卫 |
| 会话已失效 | 回登录页，带上原路径 |
| 匹配不到路由 | 进 `/404` |
| `meta.permissions` 不在有效权限里 | 进 `/403` |
| 独立公共页（`meta.standalone`，如控制中心、OAuth 同意页） | 不挂主布局、不进标签栏；错误页同样不进标签栏 |

刷新令牌后权限清空、下一次导航重取，角色指派的变化随之生效；切换租户、切换激活角色、开始或结束模仿后整页重建（`relaunchApp`）。

未读的强制阅读公告不在守卫里处理：主布局挂载时拉取，以不可关闭的弹窗逐条拦截，读完才能继续操作。

## 首页

登录后的落点与 `/` 的跳转目标取菜单树里第一个显示的页面（深度优先），它也是固定的首页标签。菜单里没有页面时用 `VITE_HOME_PATH` 兜底（仓库的 `.env` 设为 `/workbench/dashboard`）；关掉后端工作台模块时把它留空。

## 标签页与缓存

访问过的页面进标签栏（首页与固定标签不可关）。标签标题存文案键，切换语言时跟着变。偏好里可关掉访问历史（只留当前页）或限制标签数。菜单的「缓存」与「固定标签」两个展示字段在菜单管理里可调，后端登记给缺省值。

## 下一步

- [权限与脱敏](./permission) · [开发流程](./development) · [后端权限管理](../backend/permission)
