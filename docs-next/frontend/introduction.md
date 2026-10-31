# 框架简介

本页介绍 6.0 管理端的技术栈、目录结构、模块划分与启动流程，适用于前端开发者。

`frontend-next` 是 6.0 的管理端：Vue 3.5 + Vite 8 + TypeScript 6 + Pinia + vue-router + vue-i18n + Tailwind CSS 4 + XiHan.UI 5（`next` 线）。Node 24+、pnpm 11+，没有锁文件。

## 与 5.x 管理端的关系

壳层原样从 5.x 搬入，页面背后的数据换成 6.0：

| 部分 | 6.0 |
| --- | --- |
| 壳层（布局、主题、偏好中心、全局搜索、标签栏、收藏、分屏、通知中心、灵动岛、锁屏） | 从 5.x 搬入 `packages/layouts`、`packages/design`、`packages/components` 等，行为一致 |
| 路由与守卫 | `packages/router` 与 5.x 相同，菜单数据换成 `auth.permissions` |
| 列表页 | `SchemaPage` 从 5.x 原样搬入，资源适配器对接 6.0 的 `PageRequest` |
| API 层 | 5.x 的 `src/api` 整体消失：由后端 `openapi.json` 生成 `packages/contracts`，运行时只有一个通用客户端 `packages/request` |
| 页面组织 | 5.x 的 `src/views` 平铺；6.0 页面全部进 `src/modules/<包>/views`，与后端能力包一一对应 |
| 按钮权限 | 不再有按钮码：按钮直接用对应操作的权限码 |

## 目录

```text
frontend-next/
├─ contracts/            后端导出的 openapi.json、realtime.json（入库，CI 比对）
├─ scripts/              gen-contracts 与门禁脚本
├─ packages/  (~)        壳层与通用能力，禁止反向引用 @/
│  ├─ contracts/         生成物 + paging.ts
│  ├─ request/           运行时客户端
│  ├─ router/  stores/  layouts/  components/  composables/  hooks/
│  ├─ design/            令牌与全局样式
│  ├─ locales/           壳层文案 × 10
│  ├─ iconify/           离线图标与 IconPicker
│  └─ views/_core/       认证、控制中心、个人中心、OAuth 同意页、403 / 404 / 500
└─ src/  (@)             应用侧
   ├─ main.ts  App.vue  app/context.ts  router/  styles/
   └─ modules/<包>/      views/、locales/<区域>.ts × 10、setup.ts
```

| 别名 | 指向 |
| --- | --- |
| `@` | `frontend-next/src` |
| `~` | `frontend-next/packages`（如 `~/contracts`、`~/components`、`~/request`、`~/stores`） |

## 已有的模块

| 模块 | 页面 |
| --- | --- |
| `workbench` | 工作台 |
| `identity` | 用户、在线用户 |
| `organization` | 部门、岗位 |
| `tenancy` | 租户、套餐、我的订阅 |
| `access` | 角色、权限目录、委托、字段安全、访问策略、约束规则、菜单 |
| `settings` | 参数、字典、业务编号 |
| `files` | 文件库、存储配置、导出中心 |
| `notification` | 通知公告、发送记录、模板、邮件 / 短信 / 机器人 / Telegram 配置、我的消息 |
| `auditing` | 链路追踪与八个日志页 |
| `maintenance` | 作业、缓存、服务监控、版本、升级记录 |
| `openplatform` | OAuth 应用 |

`workflow`、`printing`、`codegen`、`chat`、`ai` 随后端 N3 / N4 再建。

## 应用启动

1. `main.ts` 创建应用、Pinia、i18n 与路由；
2. 各模块的 `setup.ts` 登记本模块的文案（`registerModuleLocales`）与壳层扩展；
3. `src/app/context.ts` 向壳层注入接口实现（`AppContextApis`：用户设置、字典选项、收件箱、导出、文件上传、个人中心等），壳层不直接依赖业务模块；
4. `configureRequest` 注入令牌、区域与错误钩子；
5. 守卫首次进入时调 `auth.permissions`，把菜单树装成动态路由。

## 命令

| 任务 | 命令 |
| --- | --- |
| 安装 | `pnpm install --no-frozen-lockfile` |
| 开发 | `pnpm dev`（`http://localhost:9100`，`/api`、`/hubs` 等代理到 9710） |
| 生成契约 | `pnpm gen:contracts` |
| 综合检查 | `pnpm check` |
| 单测 | `pnpm test` |
| 构建 | `pnpm build` |

## 下一步

- [开发流程](./development) · [契约与请求](./request) · [菜单与路由](./routing)
