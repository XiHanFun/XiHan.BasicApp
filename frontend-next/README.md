# frontend-next

XiHan.BasicApp 6.0 管理端（方案第 6 节）。Vue 3.5 + Vite + TypeScript + Pinia + vue-router + vue-i18n（10 种区域）+ Tailwind 4（CSS-first）+ XiHan.UI。Node 24+、pnpm 11+，没有锁文件。

## 结构

```text
frontend-next/
├─ contracts/openapi.json      后端 `contracts export` 导出的接口文档（入库，CI 比对）
├─ scripts/                    gen-contracts、各门禁（px / token / i18n / modules / permissions）、dev-token
├─ packages/                   别名 ~，底层基建，禁止反向引用 @/
│  ├─ contracts/               生成物：types.ts、operations.ts、permissions.ts、enums.ts
│  ├─ request/                 运行时客户端：拼路由、放头、Problem Details → ApiError、刷新 / 锁屏 / 维护钩子
│  ├─ locales/ stores/ router/ design/
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
| 开发令牌 | `pnpm dev:token -- --sub alice --tenant acme --role admin` |

## 契约层

一份 `openapi.json`、一个生成器：后端 `dotnet run --project backend-next/src/XiHan.BasicApp.Host -- contracts export --out frontend-next/contracts` 导出，`pnpm gen:contracts` 生成 `packages/contracts/*`，生成物入库；CI 先比对导出文件、再比对生成物（`pnpm contracts:check`），任一漂移即失败。改契约的顺序：后端操作 → 导出 → 生成 → 改页面 → 同一提交。

运行时只依赖 `code` 与 `errors`：`SECURITY_DENIED_001` 走刷新钩子（N0 没有刷新令牌，直接清令牌），`IDENTITY_DENIED_002` 走锁屏钩子，`KERNEL_UNAVAILABLE_001` 走维护钩子；字段级 `errors` 交给表单按字段显示。

## N0 的开发会话

N1 的登录页落地前，顶栏有一个令牌输入：`pnpm dev:token` 按后端 Development 密钥签一枚 JWT（`sub` / `tenant` / `role`），粘进去即可调用示例操作；租户与授权行的准备见 `backend-next/src/XiHan.BasicApp.Host/README.md`。

## XiHan.UI 本地联调

目标是 XiHan.UI 的 `next` 线；发布前在 `pnpm-workspace.yaml` 里加 `overrides` 把 `@xihan-ui/*` 指到同级源码（`link:../../XiHan.UI/ui/packages/<目录>`），overrides 不提交。CI 与常规开发用 catalog 里的发布版，页面只用两者共有的部件。
