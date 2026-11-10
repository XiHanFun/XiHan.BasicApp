来源：https://basicapp.docs.xihanfun.com/frontend/introduction

# 框架简介

本页说明前端的技术栈、目录结构、模块划分与启动流程，适用于前端开发者。

`frontend` 基于 Vue、Vite、TypeScript、Pinia、Vue Router、vue-i18n、Tailwind CSS 与 XiHan.UI。运行要求 Node.js 24+、pnpm 11+；仓库不提交锁文件，依赖版本统一写在 `pnpm-workspace.yaml` 的 `catalog`。

前端不手写接口：调用、类型、权限码与推送方法名都由后端导出的契约生成；菜单与路由由后端页面登记下发。

## 目录

```text
frontend/
├─ contracts/            后端导出的 openapi.json、realtime.json（入库，CI 比对）
├─ scripts/              gen-contracts 与门禁脚本（px / token / i18n / modules / permissions）
├─ packages/  (~)        壳层与通用能力，不得引用 @/
│  ├─ contracts/         生成的契约层 + 手写的分页适配 paging.ts
│  ├─ request/           运行时客户端
│  ├─ router/            守卫、菜单 → 路由、核心路由、视图解析
│  ├─ stores/            会话、权限、偏好、标签栏、壳层扩展等 Pinia store
│  ├─ layouts/           前端壳（侧栏、顶栏、标签栏、偏好中心……）
│  ├─ components/        公共组件与 SchemaPage
│  ├─ composables/  hooks/  utils/  constants/  types/
│  ├─ design/            应用令牌、组件库令牌桥与全局样式
│  ├─ locales/           壳层文案 × 10 与组件库语言包映射
│  ├─ iconify/           离线图标与 IconPicker
│  ├─ designers/  printing/   设计器接线与打印
│  └─ views/_core/       认证、控制中心、个人中心、OAuth 同意页、关于页、403 / 404 / 500
└─ src/  (@)             应用侧
   ├─ main.ts  App.vue  app/context.ts  router/  styles/
   └─ modules/<包>/      views/**、locales/<区域>.ts × 10、setup.ts
```

| 别名 | 指向 |
| --- | --- |
| `@` | `frontend/src` |
| `~` | `frontend/packages`（如 `~/contracts`、`~/components`、`~/request`、`~/stores`） |

模块之间不互相引用，共用的东西放 `packages/`。`packages/` 反向引用 `@/` 由 lint 拦截。

## 模块

`src/modules/<包>` 与后端能力包同名，页面只放在这里（`pnpm modules:check` 守护）：

| 模块 | 页面 |
| --- | --- |
| `workbench` | 工作台 |
| `identity` | 用户、在线用户 |
| `organization` | 部门、岗位 |
| `tenancy` | 租户、套餐、我的订阅 |
| `access` | 角色、权限目录、委托、字段安全、访问策略、约束规则、菜单；授权申请的审批单据 |
| `settings` | 参数、字典、业务编号 |
| `files` | 文件库、存储配置、导出中心 |
| `notification` | 通知公告、发送记录、模板、邮件 / 短信 / 机器人 / Telegram 配置、我的消息 |
| `auditing` | 链路追踪；操作、登录、访问、异常、数据变更、权限变更、条件命中、开放接口八个日志页 |
| `maintenance` | 作业、缓存、服务监控、版本、升级记录 |
| `openplatform` | OAuth 应用 |
| `printing` | 打印模板（设计器、样例数据、预览与直打） |
| `chat` | 在线聊天（全屏页与顶栏浮动面板）、聊天审计 |
| `workflow` | 流程定义、流程实例、审批中心 |
| `codegeneration` | 代码生成（数据源、表配置、模板、生成历史） |
| `ai` | AI 提供商、提示词、知识库、助手 |

`workbench`、`workflow`、`printing`、`codegeneration`、`chat`、`ai` 是可选模块：后端 `BasicApp:Modules:<名>:Enabled` 关掉后，页面与权限码都不下发，前端对应入口随之消失。关于页 `about/project/index` 由壳层提供（`packages/views/_core/about`）。

## 应用启动

1. `main.ts` 预载离线图标集，创建应用、Pinia 与 i18n；
2. 依次执行各模块 `setup.ts` 的默认导出：登记本模块文案（`registerModuleLocales`），可选模块另登记壳层扩展（`registerShellExtension`）、看板小组件（`registerWorkbenchWidget`）或公共包接线（如打印的 `configurePrinting`），只登记、不发请求；
3. `configureRequest` 注入接口前缀、令牌、区域与错误钩子；
4. 装好当前区域的文案后，`registerApplicationContext` 向壳层注入接口实现（`AppContextApis`：登录配置、个人中心、用户设置、收件箱、导入导出、文件、切换租户、模仿等），壳层不直接依赖业务模块；壳层用不到的接口显式抛错，不回空值；
5. 挂上路由守卫后挂载；守卫首次进入时调 `auth.permissions`，把菜单树装成动态路由。

## 命令

| 任务 | 命令 |
| --- | --- |
| 安装 | `pnpm install --no-frozen-lockfile` |
| 开发 | `pnpm dev`（`http://localhost:9100`；`/api`、`/blobs`、`/openapi.json`、`/mcp`、`/health` 与 WebSocket `/hubs` 代理到 `VITE_DEV_PROXY_TARGET`，缺省 `http://localhost:9710`） |
| 生成契约 | `pnpm gen:contracts` |
| 综合检查 | `pnpm check` |
| 单测 | `pnpm test` |
| 构建 | `pnpm build` |

环境变量见 `frontend/.env.example`；本机改动写 `.env.local`，不提交。

## 下一步

- [开发流程](./development) · [契约与请求](./request) · [菜单与路由](./routing)
