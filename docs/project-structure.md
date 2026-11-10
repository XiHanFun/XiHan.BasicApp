# 目录结构与代码地图

本页说明仓库、后端与前端的目录结构，并列出常见改动对应的代码位置，适用于开始修改代码之前查阅。

## 仓库全貌

```text
XiHan.BasicApp/
├─ AGENTS.md  CLAUDE.md         # Agent 约定（CLAUDE.md 链接到 AGENTS.md）
├─ .agents/skills/              # 仓库技能（.claude/skills 链接到这里）
├─ backend/                     # 后端
├─ frontend/                    # 前端
├─ docs/                        # 本文档站
├─ plans/                       # 总体方案、对齐矩阵与方案脚本
├─ docker-compose.yml  .env.example   # 单实例容器编排，见部署
└─ .github/workflows/ci.yml     # CI：后端构建测试、契约漂移、前端门禁
```

## 后端

```text
backend/
├─ XiHan.BasicApp.slnx         # 用框架的 NuGet 包
├─ XiHanFun.Local.slnx          # 另含同级框架的源码工程，联调框架用
├─ Directory.Build.props        # net10.0、版本号、警告即错误、生成文档文件
├─ Directory.Packages.props     # 中央包版本
├─ Directory.Build.targets      # XiHanFrameworkReference → ProjectReference 或 PackageReference
├─ props/framework.props        # 框架引用方式：包版本与源码的锁定提交
├─ Dockerfile                   # 后端镜像（框架从 NuGet 还原）
├─ src/
│  ├─ XiHan.BasicApp.Contracts      Tier 0  实体基类、分页 / 批量、权限描述、页面登记、跨包接口、消息主题、种子号段
│  ├─ XiHan.BasicApp.Runtime        Tier 0  SqlSugar 回调、TenantScopedQuery、操作人、数据保护、表结构升级、种子执行
│  ├─ XiHan.BasicApp.Identity       Tier 1  账号、登录、会话、个人中心、模仿
│  ├─ XiHan.BasicApp.Tenancy        Tier 1  租户、套餐、成员、订阅
│  ├─ XiHan.BasicApp.Organization   Tier 1  部门、岗位、部门成员
│  ├─ XiHan.BasicApp.Access         Tier 1  角色、授权与收窄规则、菜单
│  ├─ XiHan.BasicApp.Settings       Tier 2  参数、字典、业务编号
│  ├─ XiHan.BasicApp.Files          Tier 2  存储、文件、导出导入
│  ├─ XiHan.BasicApp.Notification   Tier 2  通知、模板、邮件短信、机器人
│  ├─ XiHan.BasicApp.Auditing       Tier 2  日志查询与写入
│  ├─ XiHan.BasicApp.Maintenance    Tier 2  作业、缓存、服务监控、版本
│  ├─ XiHan.BasicApp.OpenPlatform   Tier 2  签名开放接口、OAuth / OIDC
│  ├─ XiHan.BasicApp.Realtime       Tier 2  SignalR Hub 与推送
│  ├─ XiHan.BasicApp.Workbench      Tier 3  工作台小组件
│  ├─ XiHan.BasicApp.Workflow       Tier 3  流程定义与实例、审批中心
│  ├─ XiHan.BasicApp.Printing       Tier 3  打印模板与数据源
│  ├─ XiHan.BasicApp.CodeGeneration Tier 3  代码生成与零代码运行时
│  ├─ XiHan.BasicApp.Chat           Tier 3  在线聊天与聊天审计
│  ├─ XiHan.BasicApp.AI             Tier 3  AI 提供商、提示词、知识库、助手
│  └─ XiHan.BasicApp.Host           装配、普通端点、命令、Schema/、IpDatabases/、appsettings
└─ test/
   ├─ XiHan.BasicApp.Architecture.Tests   层级、Expose、权限码、目录与文案、XML 文档、框架锁定
   └─ XiHan.BasicApp.Integration.Tests    TestServer + SQLite / PostgreSQL 的端到端用例、一致性套件、冒烟
```

### 宿主

```text
XiHan.BasicApp.Host/
├─ Program.cs                   # contracts export / 命令模式 / 启动前升级表结构 / 运行
├─ BasicAppHost.cs              # 装配框架与能力包、映射端点
├─ HostModules.cs  HostProviders.cs   # 可选模块开关；按配置装 Redis、RabbitMQ
├─ Commands/                    # seed、schema upgrade
├─ Endpoints/                   # 文件、头像、聊天附件、代码生成产物包等普通端点
├─ Schema/6.0.0/0001-baseline.sql     # 表结构基线
├─ IpDatabases/                 # IP 归属地离线库
├─ appsettings.json  appsettings.Development.json
└─ xihan.app.json               # 框架打包规范的应用清单
```

### 一个能力包内部

以 Identity 为例，目录名全仓统一（`PackageStructureTests` 守护白名单）：

```text
XiHan.BasicApp.Identity/
├─ IdentityFeature.cs           # IFeature：清单（Id、Provides、Requires）与 Configure 登记
├─ IdentityCapabilities.cs      # 本包对外提供的能力 ID（basicapp.identity@1）
├─ IdentityPermissions.cs       # PermissionDescriptor 常量：码、显示名、作用侧、是否审计
├─ IdentityPages.cs             # 页面登记：页面码、路由、文案键、图标、排序、所需权限
├─ IdentityMessages.cs          # 消息键常量与消息目录
├─ IdentityErrors.cs            # 错误码常量 IDENTITY_<KIND>_<NNN>
├─ IdentityJsonContext.cs       # 本包请求 / 响应 DTO 的源生成序列化上下文
├─ IdentityOptions.cs           # BasicApp:Auth 配置
├─ IdentityFieldSecurity.cs     # 可配置字段安全的实体与字段
├─ README.md
├─ Localization/<区域>.json × 10
├─ Domain/                      # 实体与领域服务
├─ Operations/<资源>/<动词>.cs  # 一方法一操作，请求 / 响应 record 与操作同目录
├─ Queries/                     # 分页、投影、联表（以 TenantScopedQuery 起手）
├─ Middleware/                  # 本包贡献的管道中间件（SessionGateMiddleware）
├─ Infrastructure/              # Provider 实现、外部网关、小组件提供者
└─ Seeds/                       # 种子
```

其他包按需带 `Jobs/`（`IJob`）、`Handlers/`（消息处理器）；没有用到的目录不建。表结构脚本统一放在 `Host/Schema/<版本>/`，不在包里。

## 前端

```text
frontend/
├─ package.json  pnpm-workspace.yaml  vite.config.ts  vitest.config.ts  eslint.config.mjs  stylelint.config.mjs
├─ .env  .env.example           # VITE_PORT、VITE_DEV_PROXY_TARGET、VITE_HOME_PATH、VITE_REALTIME_HUB …
├─ Dockerfile  nginx.conf       # 前端镜像：Nginx 托管产物并同源反代后端
├─ contracts/                   # 后端导出的 openapi.json、realtime.json（入库，CI 比对）
├─ scripts/                     # gen-contracts 与 px / token / i18n / modules / tests / permissions 门禁
├─ packages/                    # 别名 ~，壳层与通用能力；禁止反向引用 @/
│  ├─ contracts/                # 生成物：types.ts、operations.ts、permissions.ts、enums.ts、realtime.ts；手写的 paging.ts
│  ├─ request/                  # 运行时客户端：拼路由、放头、Problem Details → ApiError、刷新与钩子
│  ├─ router/                   # 守卫、菜单 → 路由、核心路由
│  ├─ stores/                   # access、auth、app、标签栏等
│  ├─ layouts/                  # 前端壳层（侧栏、顶栏、标签、偏好、全局搜索、分屏）
│  ├─ components/               # common（XDataTable、EditModal、PageShell…）与 schema（SchemaPage）
│  ├─ designers/  printing/     # 流程设计器与打印设计器的接入
│  ├─ composables/  hooks/  utils/  constants/  types/  iconify/
│  ├─ design/                   # 令牌（variables.css）、XiHan.UI 令牌桥（xihan-ui.css）与全局样式
│  ├─ locales/                  # 壳层文案 × 10 区域
│  ├─ views/_core/              # 认证页、控制中心、个人中心、OAuth 同意页、关于页、403 / 404 / 500
│  └─ <包>/tests/               # 该包的单测（*.spec.ts），按被测文件的子路径摆放
└─ src/                         # 别名 @，应用侧
   ├─ main.ts  App.vue  app/context.ts  router/  styles/
   └─ modules/<包>/             # 与后端能力包一一对应
      ├─ views/<页面>/index.vue
      ├─ locales/<区域>.ts × 10
      ├─ tests/                 # 该模块的单测（*.spec.ts）
      └─ setup.ts
```

模块：`access`、`ai`、`auditing`、`chat`、`codegeneration`、`files`、`identity`、`maintenance`、`notification`、`openplatform`、`organization`、`printing`、`settings`、`tenancy`、`workbench`、`workflow`。

## 代码定位

### 后端

| 要做的事 | 位置 |
| --- | --- |
| 新增 / 修改一个接口 | `src/<包>/Operations/<资源>/<动词>.cs`，请求与响应 record 同目录 |
| 新增权限码 | `<包>Permissions.cs`（`PermissionDescriptor`），在 `All` 里登记 |
| 新增页面（菜单） | `<包>Pages.cs`，前端在 `src/modules/<包>/views/<路径>/index.vue` |
| 新增错误码 / 文案 | `<包>Errors.cs`、`<包>Messages.cs` 与 `Localization/*.json`（10 种区域键集一致） |
| 改表结构 | 实体 + `Host/Schema/<版本>/*.sql` |
| 新增配置项 | 包的 `<包>Options.cs`（`BasicApp:*`）与 `Host/appsettings.json` |
| 新增种子 | 包的 `Seeds/`，实现 `ISeeder`，号段见 `Contracts/Seeding/SeedOrders.cs` |
| 新增后台作业 | 包的 `Jobs/`，在 `Configure` 里登记 |
| 新增普通端点（上传、下载、重定向、Hub） | 包内的 `Map*` 扩展或 `Host/Endpoints/`，宿主在 `BasicAppHost.MapBasicAppEndpoints` 里调用 |

### 前端

| 要做的事 | 位置 |
| --- | --- |
| 调用后端操作 | `import { users } from '~/contracts'`，不手写 URL |
| 列表页 | `SchemaPage` + 资源适配器（`packages/contracts/paging.ts`） |
| 新增模块的文案 | `src/modules/<包>/locales/<区域>.ts`，10 种区域键集一致 |
| 壳层注入的接口实现 | `src/app/context.ts`（`AppContextApis`） |
| 主题与令牌 | `packages/design/` |

### 前后端同时改动的情形

| 变更 | 后端 | 前端 |
| --- | --- | --- |
| 改 DTO 字段 | 请求 / 响应 record | 重新导出 `openapi.json` → `pnpm gen:contracts` → 改页面 |
| 新增操作 | 操作类 + 权限码 + 文案 | 生成契约 → 页面按 `can('<权限码>')` 门控 |
| 新增页面 | `<包>Pages.cs`（组件路径 = 路由去前导斜杠 + `/index`） | 同路径的 `index.vue` 与菜单文案键 |

契约改动的完整顺序见[前端契约与请求](./frontend/request)。

## 相关页面

- [系统概述](./overview) · [后端架构总览](./backend/introduction) · [前端框架简介](./frontend/introduction)
