# 路由与菜单

前端路由**默认由后端菜单驱动**——后端 `PageRegistry` 是菜单、路由、组件路径、权限码、i18n 键的单一事实源，前端只负责把菜单数据转成 `RouteRecordRaw` 并落地视图文件。本页讲这条链路怎么走通，以及卡住时怎么查。

## 两种模式

| 模式 | 触发 | 行为 |
| --- | --- | --- |
| **动态模式（默认）** | — | 登录后拉 `/api/Auth/Permissions` 得到 `menus`，转成路由并 `addRoute` |
| 静态模式 | `VITE_AUTH_ROUTE_MODE === 'static'` | 用前端静态路由 + `filterRoutesByPermission()` 按角色/权限过滤 |

静态模式是兜底（后端不可用时的演示、纯前端调试），生产用动态模式。

## 动态路由的生成链路

```text
后端 PageRegistry（PageDescriptor + ButtonDescriptor）
   │  SaasMenuSeeder 播种（先查权限码，查不到就跳过并告警）
   ▼
SysMenu 表
   │  GET /api/Auth/Permissions → { roles, permissions, menus }
   ▼
前端 mapMenuToRoutes()（packages/router/dynamic.ts）
   │  MenuRoute[] → RouteRecordRaw[]
   │  组件解析：后端 Component 路径 → 前端文件路径
   ▼
router.addRoute('RootLayout', ...)
```

## 组件怎么解析

后端 `PageDescriptor.Component` 决定视图落点，前端有两条解析路径：

### 一、常规页面：`src/views` 的 glob 匹配

约定 **`Component` = `Path` 去掉前导斜杠 + `/index`**：

| 后端 `Path` | 后端 `Component` | 前端文件 |
| --- | --- | --- |
| `/identity/position` | `identity/position/index` | `src/views/identity/position/index.vue` |
| `/log/access` | `log/access/index` | `src/views/log/access/index.vue` |

`src/app/context.ts` 里用 `import.meta.glob('/src/views/**/*.vue')` 把所有视图收集起来，运行时按路径匹配。后端 `Component` 可能是 PascalCase，解析时会做大小写归一。

### 二、`_core` 页面：`coreComponentMap` 显式登记

个人中心、关于页等**不落在 `src/views`** 的页面（源码在 `packages/views/_core/`），`Component` 写成 `_core/xxx/index`，由 `packages/router/dynamic.ts` 的 `coreComponentMap` 解析：

```ts
const coreComponentMap: Record<string, () => Promise<unknown>> = {
  '_core/about/index': () => import('~/views/_core/about/index.vue'),
  '_core/profile/index': () => import('~/views/_core/profile/index.vue'),
}
```

::: danger 新增 `_core` 页面必须同时登记
只放视图文件而不在 `coreComponentMap`（以及 `componentAliasMap`）登记，动态路由匹配不到组件，会**回退到 not-found**。表现是「菜单有、点进去 404」。
:::

## 路由守卫

`setupRouterGuard(router)`（`packages/router/guard.ts`）在 `beforeEach` 里依次做：

```text
1. 无 token                    → 跳登录页
2. 用户上下文失效              → 重新拉取用户信息 + 权限
3. 路由未加载                  → 装载动态/静态路由 + 拉取用户偏好
4. meta.roles / meta.permissions 校验  → 无权跳 403
5. 维护标签栏（多标签页状态）
```

第 3 步是**只做一次**的：路由装载完成后打标记，后续导航不再重复请求。因此「登录后第一次跳转」比后续慢是正常的。

## 静态公共页

纯静态的公共页（登录页、错误页、`/control-center`、`/editor-demo` 等）**不登记在 `PageRegistry`**，由前端 `src/router/routes.ts` 持有（认证页与错误页定义在 `packages/router/routes/core.ts`，由 `routes.ts` 展开），经 `app/context.ts` 注册进 app-context。

判断标准：需要权限控制、需要出现在侧边菜单里的 → 后端登记；纯展示、所有人可见的 → 前端静态路由。

## 菜单文案

`PageDescriptor.I18nKey` 的命名规则是 **`menu.{Code 中的 . 与 - 替换为 _}`**：

| 页面码 | `I18nKey` | 前端语言包的键 |
| --- | --- | --- |
| `identity.position` | `menu.identity_position` | `identity_position` |
| `log.permission-change` | `menu.log_permission_change` | `log_permission_change` |

文案维护在 `packages/locales/langs/{zh-CN,en-US}/menu.ts`，键是去掉 `menu.` 前缀后的部分。缺文案时会回退显示键名——看到侧边栏出现 `menu.xxx` 字样就是漏了这一步。

## 多标签页

守卫维护标签栏状态。`PageDescriptor.IsAffix: true` 的页面是**固定标签**（如仪表盘），不可关闭。

## 排查

| 现象 | 查什么 |
| --- | --- |
| 菜单不显示 | 后端：权限码是否已落库（`SaasMenuSeeder` **查不到权限就跳过并告警**）；当前用户是否有该权限码 |
| 菜单有、点进去 404 | 视图文件路径是否与 `Component` 约定一致；`_core` 页面是否登记了 `coreComponentMap` |
| 侧边栏显示 `menu.xxx` | `menu.ts` 漏了对应文案 |
| 刷新后路由丢失 | 路由是 `addRoute` 动态加的，刷新会重新走守卫装载——若一直 404，看第 2/3 步是否因接口失败中断 |
| 无权页面还能进 | 检查 `PageDescriptor.PermissionCode` 是否为空（空 = 纯展示菜单，不做门控） |

## 相关页面

- [后端架构 · 菜单](../backend/introduction#菜单-后端单一事实源)：`PageRegistry` 的字段与种子机制
- [权限与脱敏](./permission)：三级权限过滤
- [Schema 驱动页面](./schema-page)：页面本身怎么写
- [二次开发 · 加一个前端页面](../backend/development#配方-c-加一个前端页面)：端到端清单
