# frontend-next

XiHan.BasicApp 6.0 管理端（方案第 6 节）。Vue 3.5 + Vite + TypeScript + Pinia + vue-router + vue-i18n（10 种区域）+ Tailwind 4（CSS-first）+ XiHan.UI。Node 24+、pnpm 11+，没有锁文件。

## 结构

```text
frontend-next/
├─ contracts/openapi.json      后端 `contracts export` 导出的接口文档（入库，CI 比对）
├─ scripts/                    gen-contracts、各门禁（px / token / i18n / modules / permissions）
├─ packages/                   别名 ~，底层基建，禁止反向引用 @/
│  ├─ contracts/               生成物：types.ts、operations.ts、permissions.ts、enums.ts
│  ├─ request/                 运行时客户端：拼路由、放头、Problem Details → ApiError、刷新 / 会话 / 锁屏 / 维护钩子
│  ├─ stores/                  app（区域）、auth（会话：登录、刷新、注销）
│  ├─ locales/ router/ design/
└─ src/                        别名 @，应用侧
   ├─ main.ts  App.vue  styles/index.css
   └─ modules/<包>/{views/**, locales/<区域>.ts, setup.ts}   与后端能力包一一对应
```

## 命令

| 任务 | 命令 |
| --- | --- |
| 安装 | `pnpm install --no-frozen-lockfile` |
| 开发 | `pnpm dev`（9100，`/api` 等代理到后端 9710） |
| 生成契约层 | `pnpm gen:contracts`（读 `contracts/openapi.json`） |
| 综合检查 | `pnpm check`（type-check → lint → px → token → i18n → modules → contracts → permissions） |
| 单测 / 构建 | `pnpm test`、`pnpm build` |

## 契约层

一份 `openapi.json`、一个生成器：后端在仓库根执行 `dotnet run --project backend-next/src/XiHan.BasicApp.Host -- contracts export --out "$PWD/frontend-next/contracts"` 导出（`dotnet run` 以工程目录为工作目录，`--out` 给绝对路径），`pnpm gen:contracts` 生成 `packages/contracts/*`，生成物入库；CI 先比对导出文件、再比对生成物（`pnpm contracts:check`），任一漂移即失败。改契约的顺序：后端操作 → 导出 → 生成 → 改页面 → 同一提交。

运行时只依赖 `code` 与 `errors`：`SECURITY_DENIED_001`（未认证）与 `SECURITY_DENIED_003`（访问令牌过期或签名不符）走刷新钩子——用刷新令牌换一次并重放，刷新也失败即回登录页；`IDENTITY_DENIED_001`（会话被吊销：注销、改密、锁定、重置密码）直接回登录页；`IDENTITY_DENIED_002` 走锁屏钩子，`RUNTIME_UNAVAILABLE_003`（别的节点在升级共享库）与 `KERNEL_UNAVAILABLE_001`（内核未就绪）走维护钩子；字段级 `errors` 交给表单按字段显示。登录与刷新以 `anonymous: true` 调用，不带旧令牌。

## 会话、权限与菜单

`/auth/login`：租户（平台账号留空）、用户名、密码 → `auth.login`；会话（访问令牌、刷新令牌、会话 ID、租户、用户资料）由 `~/stores` 的 `auth` 保存在 `localStorage`，路由守卫把未登录的访问带回跳地址送到登录页。顶栏显示当前用户与租户、修改密码入口（`/identity/profile/change-password`，`profile.change-password`）与退出（`auth.logout`）；管理员重置过密码的账号登录后顶栏提示先改密。初始账号由后端 `seed` 命令写入（`superadmin`，见 `backend-next/src/XiHan.BasicApp.Host/README.md`）。

路由与 4.x 相同：`packages/router` 的守卫（`guard.ts`）、菜单 → 路由（`dynamic.ts`）与核心路由（`routes/core.ts`：认证壳与 403 / 404 / 500），应用侧 `src/router` 用 RootLayout（管理端壳）承载业务页，地址缺省走 hash（`VITE_ROUTER_HISTORY=history` 切 HTML5 历史）。登录后守卫首次进入时调 `auth.permissions`（有效权限码、角色、按权限裁剪的菜单树、页面码），把菜单树装成动态路由（节点的 `component` 是 `<包>/<路径>/index`，按 `src/modules/*/views/**/index.vue` 精确解析，找不到落 404 兜底并报错；页面包一层「名字 = 路由名」的壳组件供 KeepAlive 命中），侧栏导航由同一棵树渲染，`/` 落到第一个菜单；`meta.permissions` 不在有效权限里进 `/403`；访问过的页面进标签栏（首页与固定标签不可关）。按钮即操作：`auth.can('identity.users.create')` 精确匹配操作的权限码控制可见性，安全边界仍在后端。刷新令牌后权限清空、下一次导航重取（角色指派的变化随之生效）。

## XiHan.UI 本地联调

目标是 XiHan.UI 的 `next` 线；发布前在 `pnpm-workspace.yaml` 里加 `overrides` 把 `@xihan-ui/*` 指到同级源码（`link:../../XiHan.UI/ui/packages/<目录>`），overrides 不提交。CI 与常规开发用 catalog 里的发布版，页面只用两者共有的部件。
