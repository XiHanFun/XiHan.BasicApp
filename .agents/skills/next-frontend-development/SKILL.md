---
name: xihan-basicapp-next-frontend-development
description: 在 next 分支实现、重构、审查或测试 XiHan.BasicApp 6.0 管理端（frontend-next）时使用。覆盖生成的契约层 ~/contracts、模块目录与页面登记、SchemaPage 与编辑弹窗、权限码门禁、10 区域文案、从 4.x 搬入的壳层、XiHan.UI next 线与统一视觉、门禁与浏览器验证；4.x 的 frontend/ 用 frontend-development，契约变化同时用 next-fullstack-contracts。
---

# XiHan.BasicApp 6.0 管理端开发

只用于 `next` 分支的 `frontend-next/`。4.x 的 `src/api/modules` 手写 facade、`createDynamicApiClient`、`*.types.ts` 在这里一律不存在。

开始前读 `references/frontend.md`；视觉与交互改动另读仓库根 `AGENTS.md` 的「前端架构与设计」与 `docs-next/frontend/theme.md`；动公共包（`packages/` 下的壳层、路由、组件、错误页）前先对照 4.x `frontend/packages` 的同名文件。

## 边界

- 前端不手写接口：操作客户端、类型、权限码与推送方法名都是后端 `contracts export` 之后 `pnpm gen:contracts` 生成的 `~/contracts`，生成物入库；契约不对就回后端改，不在前端补字段、双字段或 `as any`。
- `@` 指向 `src`（应用侧），`~` 指向 `packages`（底层基建，禁止反向引用 `@/`）。业务页面只在 `src/modules/<包短名>/views/**`，模块名与后端能力包一致，带 `setup.ts` 与 10 个 `locales/<区域>.ts`。
- 页面路径由后端页面登记决定：路由 `/settings/configs` → 组件 `settings/configs/index` → `src/modules/settings/views/configs/index.vue`；菜单与路由由 `auth.permissions` 下发，不另写静态业务路由。
- 列表页用 `SchemaPage`，新增 / 编辑用 `XEditModal` + `XhFormRoot`，每个字段都有可见标签；按钮 `permission` 直接填后端权限码，安全边界在后端。
- 壳层是从 4.x 原样搬入的：改壳层先看 4.x 同名文件与测试，6.0 还没有的后端能力在 `AppContextApis` 里显式 `notAvailable()`、对应件缺省关，不在壳里绕。
- 组件只用 XiHan.UI（`next` 线）与语义令牌；组件库缺陷回 XiHan.UI 修，BasicApp 不写兼容；本地联调用 `pnpm-workspace.yaml` 的临时 `overrides`，不提交。

## 验证

```bash
cd frontend-next
pnpm install --no-frozen-lockfile
pnpm check   # type-check → lint → px → token → i18n → modules → contracts → permissions
pnpm test
pnpm build
```

视觉与交互改动在真实浏览器里看亮 / 暗色、窄视口、键盘与焦点、减少动态效果，用正式账号登录（验证码请用户输入）。没有锁文件，不加 `--frozen-lockfile`。
