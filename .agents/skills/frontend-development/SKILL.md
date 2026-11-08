---
name: xihan-basicapp-frontend-development
description: 实现、重构、审查或测试 XiHan.BasicApp 前端（frontend/）时使用。覆盖生成的契约层 ~/contracts、模块目录与页面登记、SchemaPage 与编辑弹窗、权限码门禁、10 区域文案、壳层与公共包、XiHan.UI 与统一视觉、门禁与浏览器验证；契约变化同时用 fullstack-contracts。
---

# XiHan.BasicApp 前端开发

用于 `frontend/`。开始前读 `references/frontend.md`；视觉与交互改动读其中「视觉」一节与 `docs/frontend/theme.md`。

## 边界

- 前端不手写接口：操作客户端、类型、权限码与推送方法名都是后端 `contracts export` 之后 `pnpm gen:contracts` 生成的 `~/contracts`，生成物入库；契约不对就回后端改，不在前端补字段、双字段或 `as any`。
- `@` 指向 `src`（应用侧），`~` 指向 `packages`（底层基建，禁止反向引用 `@/`）。业务页面只在 `src/modules/<包短名>/views/**`，模块名与后端能力包一致，带 `setup.ts` 与 10 个 `locales/<区域>.ts`。
- 页面路径由后端页面登记决定：路由 `/settings/configs` → 组件 `settings/configs/index` → `src/modules/settings/views/configs/index.vue`；菜单与路由由 `auth.permissions` 下发，不另写静态业务路由。
- 跨页面状态用 Pinia；筛选、弹窗、选择、分页这类页面局部状态留在页面或局部 composable。
- 列表页用 `SchemaPage`，新增 / 编辑用 `XEditModal` + `XhFormRoot`，每个字段都有可见标签；按钮 `permission` 直接填后端权限码，安全边界在后端。
- 壳层（布局、路由守卫、偏好设置、错误页）在 `packages/`：后端还没有的能力在 `AppContextApis` 里显式 `notAvailable()`、对应件缺省关，不在壳里绕。
- 组件只用 XiHan.UI 与语义令牌，不引入其他组件库、不复制组件库运行时；组件库缺陷回 XiHan.UI 修，BasicApp 不写兼容；本地联调用 `pnpm-workspace.yaml` 的临时 `overrides`，不提交。

## 验证

```bash
cd frontend
pnpm install --no-frozen-lockfile
pnpm check   # type-check → lint → px → token → i18n → modules → tests → headers → contracts → permissions
pnpm test
pnpm build
```

视觉与交互改动在真实浏览器里看亮 / 暗色、窄视口、键盘与焦点、减少动态效果，用正式账号登录（验证码请用户输入）。没有锁文件，不加 `--frozen-lockfile`。
