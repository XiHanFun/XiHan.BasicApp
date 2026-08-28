![logo](../assets/logo.png)

[English](README.md)

# XiHan.BasicApp 前端工程说明

本文件面向前端开发者与贡献者，记录**这一层怎么组织**：技术栈、monorepo 结构、架构硬约束与本地开发方式。

想先把整个系统跑起来，请看[仓库根 README](../README_cn.md)；逐主题的详细文档见[文档站](https://basicapp.docs.xihanfun.com)。

## 技术栈

Vue 3 + TypeScript + Vite。应用根的运行时依赖只有 vue、vue-router、pinia、vue-i18n、`@vueuse/core` 与 6 个 `@xihan-ui/*`，其余重库按归属下沉到各 workspace 包。

工具链：Vite、`@vitejs/plugin-vue(-jsx)`、vue-tsc、Turbo、Tailwind CSS v4（`@tailwindcss/vite`，CSS-first 无 JS config）、unplugin-auto-import、unplugin-vue-components、oxlint + ESLint。

## 环境要求

`packageManager` 为 `pnpm@11.7.0`，`engines` 要求 Node ≥ 24、pnpm ≥ 11。

> ⚠️ **安装前置**：`pnpm-workspace.yaml` 的 `overrides` 把 18 个 `@xihan-ui/*` 全部指向 `link:../../XiHan.UI/ui/packages/*`。**单独 clone 本仓、没有同级 `../../XiHan.UI` 检出时 `pnpm install` 会失败**。想回落到已发布版本，删掉该 overrides 块即可。

## Monorepo 结构

`pnpm-workspace.yaml` 声明 `packages/*`，共 19 个 private workspace 包（全部 `@xihan/<name>`、version `0.0.0`、别名 `~/`）：chat、components、composables、constants、design、diagram、hooks、iconify、layouts、locales、printing、request、router、stores、types、utils、views 等。

第三方重库下沉到归属包，应用根不直接依赖：axios → request，`@antv/x6` → diagram，tiptap / monaco / md-editor-v3 / jsoneditor → components，`@microsoft/signalr` / pinyin-pro → composables，vue-plugin-hiprint → printing，emoji-mart → chat，8 套 `@iconify-json` → iconify，dayjs / papaparse → utils。

所有第三方版本在 `pnpm-workspace.yaml` 的 `catalog:` 里声明一次（约 75 条），各包只写 `"catalog:"`。

路径别名两个：`@` → `./src`（应用层），`~` → `./packages`（基础设施层）。

### 架构硬约束

ESLint 具名规则 `xihan/packages-no-reverse-dep` 禁止 `packages/**` 反向引用 `@/*` 或 `../../src/**`——契约类型收在 `packages/types`，运行期依赖靠 app-context 注入。当前有 4 个文件挂着豁免待迁移。

## 目录结构

```text
frontend/
├── src/
│   ├── api/          # base·request·factory·helpers·types + 21 个域模块
│   ├── app/          # 应用装配
│   ├── components/   # 应用级组件
│   ├── constants/
│   ├── locales/      # zh-CN / en-US
│   ├── modules/      # 可插拔业务模块，见下
│   ├── router/
│   ├── styles/
│   ├── types/
│   └── views/        # 10 组 55 个页面
├── packages/         # 19 个 workspace 包（基础设施层）
└── public/
```

### 可插拔模块

`src/modules/<module>/` 下只允许 `views/`、`api/`、`locales/`、`setup.ts`、`README.md`。`main.ts` 用 `import.meta.glob('/src/modules/*/setup.ts', { eager: true })` 自动发现启动钩子——**删掉目录即卸载，不需要改任何注册代码**。

五个模块 ai / chat / codegen / printing / workflow 与后端可选模块一一对应，共 35 个页面。

### 路由模型

默认由后端菜单驱动：PascalCase 的 Component 路径映射到 kebab-case 文件，`coreComponentMap` 兜住 packages 里的 `_core` 页，另有 not-found 兜底。设 `VITE_AUTH_ROUTE_MODE=static` 可切到前端静态权限过滤模式。history 默认 hash，设 `VITE_ROUTER_HISTORY=history` 切 HTML5。

## 本地开发

```bash
pnpm install
pnpm dev          # 开发服务器，默认 9800 端口
pnpm build        # 生产构建（会先跑 type-check）
pnpm type-check
pnpm lint         # oxlint + eslint
pnpm test         # Vitest（jsdom）
pnpm validate     # i18n 与模块约定两个门禁
```

全部命令经 Turbo 委派到 `app:*`。

### 质量门禁

- `app:build` 依赖 `app:type-check`——类型错误直接阻断构建，容器镜像构建同样会失败
- `validate` 是两个零依赖 Node 门禁：`validate-i18n.mjs` 检查 zh-CN / en-US 键对称并扫 `t('...')` 找孤儿键（vue-i18n 运行期对缺失键静默返回原键，type-check 与 ESLint 都抓不到，所以孤儿键必须为 0）；`validate-modules.mjs` 检查模块目录约定、视图键不撞车、locales 必须成对
- `postinstall` 会安装 git hooks，pre-commit 走 lint-staged：ts/vue/js/mjs 过 oxlint + ESLint 修复，json/css/html/md/yaml 过 Prettier

## 样式与主题

`src/styles/index.css` 一处声明完整的 `@layer` 顺序：

```text
xihan.reset, xihan.tokens, xihan.motion, xihan.components,
theme, base, components, utilities, xihan.overrides
```

Tailwind 的 utilities 刻意排在 `xihan.components` 之后；组件的 `<style scoped>` 不属于任何层，因此永远优先。Tailwind 的 preflight 故意不导入，base reset 来自 XiHan.UI 的 `xihan.reset` 层。`packages/design/xihan-ui.css` 是把 `--xh-*` 改写到应用调色板的令牌桥，必须排在 XiHan.UI 皮肤之后。

## 组件与 API 自动导入

自定义 `XiHanUiResolver`：模板里任何 `Xh` 开头的标签自动从 `@xihan-ui/vue` 具名导入，解剖式多部件组件不用逐个手写 import，且仍可摇树。`unplugin-vue-components` 的 `dirs` 为空数组——应用自己的组件**不**自动注册。自动导入的 API 来自 vue、vue-router、pinia、`@vueuse/core`。

## 环境变量

三份提交进仓的 `.env`（`.env`、`.env.development`、`.env.production`）：

| 分组         | 变量                                                                                                                                                                                            |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 应用标识     | `VITE_APP_TITLE` / `SUBTITLE` / `DESCRIPTION` / `LOGO` / `NAMESPACE` / `VERSION`                                                                                                                |
| API 与路由   | `VITE_API_BASE_URL`（开发留空走同源代理避 CORS）、`VITE_API_PREFIX=/api`、`VITE_DEV_PROXY_TARGET=http://localhost:9708`、`VITE_PORT=9800`、`VITE_ROUTER_HISTORY`、`VITE_BASE`、`VITE_HOME_PATH` |
| API 安全管线 | 8 个 `VITE_API_SECURITY_*`，三份 .env 里 `ENABLED` 均为 false（默认关）                                                                                                                         |
| 打印         | `VITE_HIPRINT_HOST` / `VITE_HIPRINT_TOKEN`                                                                                                                                                      |

## 构建与部署

构建目标 es2022，手写 `manualChunks` 切出 vendor-ui、vendor-printing（hiprint 系动态加载，不进后台首屏）、vendor-monaco、vendor-icon-`<set>` 等十余组，产物命名 `assets/js/[name]-[hash].js`。

`Dockerfile` 两阶段（`node:24-alpine` → `nginx:1.29-alpine`），`ARG VITE_API_BASE_URL` 写进 `.env.production.local`（优先级高于 `.env.production`），留空即同源。`nginx.conf` 里 `upstream basicapp_backend = backend:9708`，SPA 走 `try_files`，`/assets/` 一年 immutable、`index.html` no-cache，反代 `/api/`（300s）、`/hubs/`（SignalR：带 Upgrade 头、关 `proxy_buffering`、3600s）与 `/uploads/`，`client_max_body_size 100m`。

## 卸载可选模块

删掉 `src/modules/<module>/`，以及对应的 `packages/{chat,printing,diagram}` 与 catalog 条目。

⚠️ **卸载必须伴随重建数据库**——已播种的菜单与权限码不会自动回收。
