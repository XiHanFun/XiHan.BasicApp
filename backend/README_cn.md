![logo](../assets/logo.png)

[English](README.md)

# XiHan.BasicApp 后端工程说明

本文件面向后端开发者与贡献者，记录**这一层怎么组织**：分层结构、工程清单、接口暴露方式、依赖构成与本地开发方式。

想先把整个系统跑起来，请看[仓库根 README](../README_cn.md)；逐主题的详细文档见[文档站](https://basicapp.docs.xihanfun.com)。

## 架构概览

三层，依赖自下而上，不允许回指：

```text
src/main/       XiHan.BasicApp.WebHost          唯一可执行工程
      ↑
src/modules/    Saas · CodeGeneration · AI      六个业务模块
                Workflow · Printing · Chat      只依赖 Saas 或 Web.Core
      ↑
src/framework/  XiHan.BasicApp.Core             全仓唯一引用 XiHan.Framework 的两个工程
                XiHan.BasicApp.Web.Core
```

`Saas` 是基础模块，承载身份、权限、多租户、审计、平台能力；其余五个是可选模块，可整体卸载。

## 工程清单

9 个 src 工程 + 10 个 test 工程，共 19 个 csproj，全部登记在 `XiHan.BasicApp.slnx`（`.slnx` 格式，不是 `.sln`）。

| 工程 | 职责 |
| --- | --- |
| `XiHan.BasicApp.Core` | 应用基座：聚合框架的非 Web 模块，提供全应用共享的基础类型与约定 |
| `XiHan.BasicApp.Web.Core` | Web 侧基座（`Microsoft.NET.Sdk.Web`）：聚合六个框架 Web 模块，提供维护模式（状态位 + 503/`Retry-After` 中间件，放行 `/health` 与 `/.well-known/`） |
| `XiHan.BasicApp.Saas` | 基础业务模块：身份认证、RBAC + 数据范围 + 字段脱敏、多租户、审计日志六类、消息中心、导出中心、定时任务、开放平台 |
| `XiHan.BasicApp.CodeGeneration` | 代码生成：DbFirst 表结构导入、单表/树表/主从三种模式、Scriban 模板、衍生产物生成 |
| `XiHan.BasicApp.AI` | AI：提供商与密钥托管、提示词库、Qdrant 知识库 RAG、技能注册与 MCP 投影、可配置多助手 |
| `XiHan.BasicApp.Workflow` | 工作流：框架引擎 + SqlSugar 持久化（Replace 掉内存默认实现）、定义/实例/待办三个应用服务 |
| `XiHan.BasicApp.Printing` | 打印：hiprint 模板管理，模板编码租户内唯一且创建后不可改，代码注册式数据源注册表 |
| `XiHan.BasicApp.Chat` | 聊天：Single / Group / Department / Assistant 四类会话、实时推送、敏感词拦截、保留期清理 |
| `XiHan.BasicApp.WebHost` | 启动宿主：模块装配、中间件管道、健康检查、种子与升级入口 |

## 接口暴露

**零 Controller**。159 个类型标注 `[DynamicApi]`，由框架的动态 API 约定自动发现并注册路由：

| 模块 | 动态 API 类型数 |
| --- | --- |
| Saas | 121 |
| CodeGeneration | 12 |
| AI | 11 |
| Workflow | 7 |
| Chat | 4 |
| Printing | 4 |

有一条测试强制约束：每个动态 API 方法必须命中 `PermissionAuthorizeAttribute`、`AllowAnonymousAttribute` 或显式白名单三者之一，漏标鉴权会在测试期失败。

非 HTTP 或非动态 API 的端点另有几处：SignalR 通知 Hub 与聊天 Hub；OAuth 外部登录回调 `/api/OAuth/ExternalLogin`、`/api/OAuth/Callback`；OIDC 身份提供方 `/connect/authorize`、`/connect/token`、`/connect/revoke` 与 discovery、JWKS、userinfo（未启用时不注册）。

## 数据模型

89 张表（`SugarTable`）：Saas 71、CodeGeneration 5、AI 4、Chat 4、Workflow 4、Printing 1。

## 依赖构成

- **63 个 XiHan.Framework 包**（`4.0.0`）：Core 引 56 个，Web.Core 引 7 个
- **src 侧第三方 NuGet 只有 1 个**：`Microsoft.SemanticKernel.Connectors.Qdrant`（AI 知识库用）
- SqlSugar、Serilog、Redis 客户端、Scriban 均由框架传递引入；SignalR 与 MVC 来自 ASP.NET Core 共享框架
- test 侧第三方 5 个：xunit、xunit.runner.visualstudio、Microsoft.NET.Test.Sdk、Moq、coverlet.collector

### 框架引用方式

`props/framework.props` 决定引框架的**源码**还是 **NuGet 包**：`UseXiHanFrameworkSource` 为 true 的条件是**解决方案名以 `XiHanFun` 开头**且框架的 Core csproj 存在，否则走 NuGet。可用 `-p:UseXiHanFrameworkSource=true` / `false` 强制覆盖。单独构建某个 csproj 时解决方案名为空，因此走 NuGet——容器构建正是靠这一条。只有 `Core` 与 `Web.Core` 两个 csproj 导入了它。

## 本地开发

```bash
dotnet restore backend/XiHan.BasicApp.slnx
dotnet build backend/XiHan.BasicApp.slnx --configuration Release
dotnet test backend/XiHan.BasicApp.slnx --configuration Release
```

启动 `src/main/XiHan.BasicApp.WebHost`。Development 监听 `http://127.0.0.1:9708`（`launchUrl` 为 scalar 文档页），Production 监听 `:9709`，均可由配置 `Hosting:Urls` 覆盖。

`scripts/` 下只有两个脚本：`nuget/VersionUpgrade.ps1`（改 `props/version.props` 的版本号）与 `project/ClearProjecBinObj.ps1`（清 bin/obj）。

## 配置与外部依赖

| 文件 | 说明 |
| --- | --- |
| `appsettings.json` | 环境无关的少量默认值 |
| `appsettings.Development.json` | 全量带注释的参考配置，新增配置项以它为准 |
| `appsettings.Production.json` | **被 `.gitignore` 忽略，仓库里没有**，需自行从 Development 复制改写 |

配置节全貌：`XiHan:{Observability, DistributedIds, Authentication, Data, Upgrade, Caching, Web, Localization, VirtualFileSystem, ObjectStorage}`。

外部依赖：

- **数据库** —— 唯一硬依赖，默认 PostgreSQL；连接配置支持 SqlServer / MySql / PostgreSQL / SQLite / Oracle
- **Redis** —— 可选。关掉即退化为进程内内存缓存，健康检查仍返回 Healthy 并标注「未启用（进程内回退）」
- **Qdrant** —— 仅 AI 知识库需要。不装也能启动，但 `/health` 会变红、知识库功能不可用

`GET /health` 匿名可访问，返回 database / redis / qdrant 三项的总状态与各项名称，刻意不返回连接串与异常详情。

## 首次启动

`EnableDbInitialization` / `EnableTableInitialization` / `EnableDataSeeding` 三个开关默认 **true**：给一个能连上的空库，启动即自建库表并播种。种子分两类——系统基线始终执行，演示数据由 `Saas:Seed:EnableDemoData` 控制（显式设为 false 才跳过）。

默认超级管理员：账号 `superadmin`、邮箱 `superadmin@xihan.fun`、角色码 `super_admin`、内置默认密码 `SuperAdmin@123`。可用配置 `Saas:Seed:SuperAdminPassword`（环境变量 `Saas__Seed__SuperAdminPassword`）覆盖；沿用内置默认值时启动日志会打 warning。

## 升级脚本

仓库内约定 `UpdateScripts/<版本号>/<版本号>.sql`，当前有 6 个版本目录（3.10.0、3.10.1、3.12.1、3.13.0、4.0.1、4.0.2）。按约定只提供 PostgreSQL 方言，且脚本须可重复安全空转（`IF NOT EXISTS` 之类）。

脚本目前**不会在启动时自动执行**，需要自行在目标库上按版本顺序应用。

## 测试

10 个测试工程、2301 条 `[Fact]` / `[Theory]` 标注（Saas 652、CodeGeneration 510、AI 306、Workflow 216、Chat 206、Printing 203、Core 84、Web.Core 56、WebHost 55、Api 13）。全部是单元测试与反射约束测试，不连数据库。

## 容器

`Dockerfile` 两阶段构建（`sdk:10.0` → `aspnet:10.0`），只 COPY `props` 与 `src`、走 NuGet 模式、以非 root 用户运行、`EXPOSE 9708`、内置 `HEALTHCHECK` 探 `/health`。

## 卸载可选模块

五个可选模块（CodeGeneration / AI / Workflow / Printing / Chat）可整体移除：删掉对应工程与测试工程、从 `XiHan.BasicApp.slnx` 摘除，再清掉 WebHost 侧的模块依赖登记。Chat 与 AI 之间有单向依赖，卸载 Chat 需连带删除 AI 侧的三个助手桥接文件。

⚠️ **卸载必须伴随重建数据库**——已播种的菜单、权限码与表结构不会自动回收。
