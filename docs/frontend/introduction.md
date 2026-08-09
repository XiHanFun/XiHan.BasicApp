# 前端架构

前端不是「一个 Vue 工程」，而是**分层的单仓库多包结构**。本页讲分层与依赖方向、启动引导链路、请求链路的完整时序，以及第三方依赖的归属规则。具体怎么写页面见 [开发流程](./development)。

## 技术栈

| 领域 | 选型 |
| --- | --- |
| 框架 | Vue 3.5（`<script setup>`） |
| 语言 / 构建 | TypeScript · Vite |
| UI 库 | Naive UI |
| 状态 | Pinia（Setup Store + `$reset` 插件） |
| 样式 | Tailwind CSS 4（CSS-first `@theme`，**preflight 关闭**） |
| 路由 | vue-router（默认 hash 模式） |
| 国际化 | vue-i18n（`legacy: false`） |
| 实时 | `@microsoft/signalr` |
| 图标 | Iconify（**离线模式**） |
| 拖拽 | `@dnd-kit/vue` |

> 版本号以仓库 `package.json` 为准。

## 五层结构

**依赖方向单向向下，`packages/` 绝不反向依赖 `src/`。**

```text
┌──────────────────────────────────────────────────────────────────┐
│  ⑤ 业务视图层  src/views/**                                       │
│     按域分目录：approval / develop / file / identity / log /      │
│     message / oauth / openapi / setting / tenant / workbench /    │
│     workflow                                                      │
│     每个列表页 = 一份 PageSchema + 一个 <SchemaPage>              │
├──────────────────────────────────────────────────────────────────┤
│  ④ 应用装配层  src/main.ts · App.vue · app/context.ts · router/  │
│     引导顺序、Provider 装配、把 api 实例 / 视图 glob / 静态路由    │
│     注册进 app-context，安装路由守卫                              │
├──────────────────────────────────────────────────────────────────┤
│  ③ 接口契约层  src/api/**                                         │
│     base（动态 API 客户端）· factory（资源工厂）· helpers（分页）  │
│     modules/**（按域的 API 与 DTO 类型）                          │
├──────────────────────────────────────────────────────────────────┤
│  ② 可复用内核  packages/**                                        │
│     components(schema/chat/common) · composables · hooks ·        │
│     stores · layouts · router · locales · design · iconify ·      │
│     diagram · plugins · views · types · utils · constants         │
├──────────────────────────────────────────────────────────────────┤
│  ① 传输底座  packages/request                                     │
│     RequestClient：请求头注入 / 响应解包 / 401 刷新重放 /          │
│     423 锁屏 / 可选签名加密                                        │
└──────────────────────────────────────────────────────────────────┘
                              ↕ HTTP + SignalR
                        后端动态 API（无 Controller）
```

两个路径别名贯穿全项目：

| 别名 | 指向 | 层 |
| --- | --- | --- |
| `@` | `src/` | ③④⑤ 应用侧 |
| `~` | `packages/` | ①② 内核侧 |

### 为什么 `src/` 薄、`packages/` 厚

`packages/` 里的东西**与具体业务无关**：布局、Schema 表格引擎、请求客户端、主题、i18n、图标、状态管理骨架。`src/` 只做两件事：装配（把内核接起来）和业务视图。

这样分的好处是内核可以被独立演进甚至复用，而业务视图始终只依赖稳定的契约。代价是**新人容易找错文件**——记住：找「怎么渲染表格 / 怎么发请求 / 怎么切主题」去 `packages/`，找「某个页面长什么样」去 `src/views/`。

### 第三方依赖的归属规则

- **底层功能依赖归属对应的 `packages/` 子包**（自包含）。
- 根 `package.json` 只留应用层跨切面依赖（`vue` / `vue-router` / `pinia` / `@vueuse` 因 Vite AutoImport 全局导入不可下沉，加上 `naive-ui` / `vue-i18n`）。
- **`src/` 里禁止直接 import 已下沉的第三方**，一律经 `~/` 转出。
- 改完依赖归属须跑 `pnpm install` + `pnpm type-check`；死依赖当场删。

## 启动引导链路

`src/main.ts` 的顺序决定了「图标、语言、请求头、登出钩子、路由守卫」在挂载前如何就位：

```ts
await setupIconifyOffline()           // ① 预加载离线图标集（必须最先，否则首屏图标空白）
invalidateCacheIfBuildTimeChanged()   // ② 构建时间变则清本地缓存
// createApp → pinia → setupI18n      // ③ 应用实例 / 状态 / 国际化
bindRouter(router)                    // ④ 请求层拿到 router（401 跳登录用）
bindLogoutHook(() => { /* 重置 access / user store */ })
registerApplicationContext(router)    // ⑤ 注册视图 glob、api 实例、静态路由到 app-context
setupRouterGuard(router)              // ⑥ 安装 beforeEach 权限/路由守卫
app.use(router).mount('#app')
```

第 ④⑤ 步是**依赖倒置的关键**：`packages/request` 不能反向依赖 `src/router` 与业务 store，所以由 `src/main.ts` 在启动时把 router 和登出回调「注入」给它。同理 `app/context.ts` 把 api 实例、视图 glob、静态路由注册进 `~/stores/app-context`，底层包据此工作而不必知道 `src/` 的存在。

## 请求链路时序

```text
视图 (SchemaPage)
  └─ resource.page(params)                       ← 页面自己写的适配器
       └─ src/api/modules/**  positionApi.page()
            └─ createDynamicApiClient('PositionQuery').post('PositionPage', body)
                 │      URL = /{apiPrefix}/{控制器名}/{动作名}
                 └─ packages/request  RequestClient.post()
                      ├─ 请求拦截
                      │    Authorization: Bearer <token>
                      │    X-Timezone   （已选时区 / 浏览器 Intl）
                      │    X-Language   （当前 locale）
                      │    X-Request-Id （前端自生成，供本地请求日志面板）
                      │    FormData 时删除 Content-Type 让浏览器带 boundary
                      │    （可选）API 签名与请求体加密
                      ▼
                    后端动态 API
                      ▲
                      ├─ 响应拦截
                      │    （可选）解密 → 记本地请求日志（含 traceId）
                      │    423 → 触发锁屏遮罩钩子（必须先于 401 分支，且不刷新不登出）
                      │    401 → 刷新令牌并重放原请求
                      │           · 并发请求排队等同一次刷新
                      │           · 刷新请求自带 _isRefresh 标记，不会再次触发刷新
                      │           · 刷新失败 → 清 token + 调 logout 钩子 + 跳登录页
                      └─ 解包 ApiResponse 信封
                           isSuccess === true → 返回 data（可能是 null）
                           否则 reject(new Error(message))
                 ← 业务代码拿到的是**已解包的强类型 data**
```

两个要点：

1. **业务代码永远看不到 `ApiResponse` 信封**。解包统一在 `RequestClient.request` 完成，失败时把可展示的中文提示抛成 `Error`，业务侧 `catch` 直接用 `error.message`。
2. **判定成功用 `isSuccess`，不是「有没有 `data` 字段」**——服务端 `WhenWritingNull` 会把 `data: null` 整个省略。

## 环境变量

| 变量 | 开发默认 | 说明 |
| --- | --- | --- |
| `VITE_PORT` | `9800` | dev server 端口 |
| `VITE_API_BASE_URL` | 空 | 协议+主机。**开发态留空**走同源 Vite 代理，天然无 CORS |
| `VITE_API_PREFIX` | `/api` | 接口前缀 |
| `VITE_DEV_PROXY_TARGET` | `http://localhost:9708` | 开发态代理目标（后端地址） |
| `VITE_ROUTER_HISTORY` | `hash` | 路由模式 |
| `VITE_HOME_PATH` | `/workbench/dashboard` | 首页兜底路径（后端菜单未派生出首页时用） |
| `VITE_AUTH_ROUTE_MODE` | 动态 | 设为 `static` 则用前端静态路由 + 权限过滤 |
| `VITE_APP_TITLE` / `SUBTITLE` / `LOGO` | — | 品牌兜底（登录后由 `/Auth/UserInfo` 的 `appTitle` / `appLogo` 覆盖） |
| `VITE_API_SECURITY_*` | `ENABLED=false` | 前端侧接口签名/加密开关与密钥 |

::: warning 改了后端端口
要同步改 `frontend/.env.development` 的 `VITE_DEV_PROXY_TARGET`，否则 dev server 转发到旧端口。
:::

## 后端驱动的部分

前端有相当一部分「事实源在后端」，这是理解整体架构的关键：

| 东西 | 事实源 | 前端做什么 |
| --- | --- | --- |
| 菜单 / 路由 / 组件路径 | 后端 `PageRegistry` → `SysMenu` | 登录后拉 `/api/Auth/Permissions` 拿 `menus`，`mapMenuToRoutes` 转成路由并 `addRoute` |
| 权限码 | 后端 `SaasPermissionDefinitions` | 拿到码集合，按页面/字段/操作三级过滤 |
| 枚举标签 | 后端枚举元数据（`Enums.{culture}.json`） | 按 `X-Language` 拉取，切语言响应式重取 |
| 字段脱敏规则 | 后端 `SysFieldLevelSecurity` | **脱敏在服务端完成**，前端只用规则控制只读/标识 |
| 站点品牌 | 后端 `/Auth/UserInfo` 的 `appTitle` / `appLogo` | 登录后写入 app store，覆盖 `.env` 兜底 |

因此**新增页面的主要工作在后端**（`PageRegistry` + 权限），前端只补视图组件、API 客户端与 i18n 文案。

## 相关页面

- [开发流程](./development)：新增一个页面的五步与自检清单
- [Schema 驱动页面](../frontend/schema-page)：列表页开发手册
- [路由与菜单](../frontend/routing)：动态路由生成与守卫
- [权限与脱敏](../frontend/permission)：三级过滤与 FLS
- [布局与主题](./theme)：布局、Tailwind v4、主题、偏好同步
- [国际化](./i18n)：语言包与文案
- [接口对接指南](../api-guide)：响应信封与请求协议
