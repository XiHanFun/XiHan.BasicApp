# 框架简介

XiHan.BasicApp 后端是一套基于 [XiHan.Framework](https://framework.docs.xihanfun.com/) 的多租户中后台内核。本页讲**组织方式**：整体全景、项目怎么分层、模块怎么装配、每个模块内部怎么切 DDD、服务注册有哪些必须遵守的约定、种子数据怎么排序。

动手改代码前读这页，能避开一整类「静默失效」的坑。

## 全景

```text
┌──────────────────────────────────────────────────────────────────────────┐
│                        XiHan.BasicApp.WebHost                             │
│        启动入口 Program.cs + 聚合模块 XiHanBasicAppWebHostModule           │
│             [DependsOn] Saas / CodeGeneration / AI / Workflow             │
│        健康检查 / MCP Server / Telegram Webhook / /health 端点            │
├───────────────┬──────────────────┬───────────────┬───────────────────────┤
│ BasicApp.Saas │ BasicApp.        │ BasicApp.AI   │ BasicApp.Workflow     │
│ 身份/权限/租户 │  CodeGeneration  │ Provider 库化 │ 流程定义/实例/待办     │
│ 消息/文件/日志 │ 数据源/表结构/   │ 知识库 RAG /  │ SqlSugar 持久化存储    │
│ 任务/审批/聊天 │ 模板/全栈生成    │ 提示词库      │ 待办站内通知           │
├───────────────┴──────────────────┴───────────────┴───────────────────────┤
│                        XiHan.BasicApp.Web.Core                            │
│      Web 能力聚合：动态 API / Scalar 文档 / SignalR / 网关灰度             │
├──────────────────────────────────────────────────────────────────────────┤
│                          XiHan.BasicApp.Core                              │
│  基座抽象：实体/DTO 基类（多租户审计）、查询服务标记接口、聚合框架能力模块    │
├──────────────────────────────────────────────────────────────────────────┤
│                            XiHan.Framework.*                              │
│ 认证 / 授权 / 数据(SqlSugar) / 缓存 / 事件总线 / 多租户 / 工作流 / AI / Bot │
└──────────────────────────────────────────────────────────────────────────┘
```

## 四条贯穿全局的设计

理解这四条，大部分「为什么这么写」的疑问就解开了。

### 一、一切皆模块

能力以 `XiHanModule` + `[DependsOn]` 为装配单元，框架按依赖图拓扑排序、逐阶段初始化。加一块能力就是加一行 `[DependsOn]`，减一块就是删一行。

### 二、后端驱动前端

菜单、路由、组件路径、权限码、国际化键、枚举标签、字段脱敏规则，**事实源全部在后端**。前端只提供视图组件与文案。新增页面的主要工作因此落在后端 `PageRegistry` 与权限定义上。

### 三、没有 Controller

应用服务打 `[DynamicApi]` 即成 REST 接口，路由由方法名推导。两个必须记住的约定：**分页方法要显式标 `[HttpPost]`**、**路由段只由显式 `[FromRoute]` 参数产生**。

### 四、读缓存、写失效、耗时异步

热点读走 Redis 分布式缓存，写路径精准失效（且必须排队到事务提交之后）；耗时动作入队由后台服务消费，数据库表是事实源、队列只承载待办工作。

## 项目分层

后端按**框架层 → BasicApp 基座层 → 业务模块层 → 主机层**自底向上组织：

| 项目 | 层 | 职责 |
| --- | --- | --- |
| `XiHan.Framework.*` | 框架 | 认证/授权/数据/缓存/事件总线/多租户/工作流/AI/Bot 等通用能力（独立仓库） |
| `XiHan.BasicApp.Core` | 基座 | 聚合全部要用的框架能力模块；提供 BasicApp 实体/DTO 基类与查询服务标记接口 |
| `XiHan.BasicApp.Web.Core` | Web 基座 | 聚合 `Core` 与框架 Web 能力（`WebCore`/`WebApi`/`WebDocs`/`WebRealTime`/`WebGateway`/`WebMcp`），并接入维护模式 |
| `XiHan.BasicApp.Saas` | 业务模块 | 核心业务：身份/角色/权限/菜单/组织/租户/配置/字典/文件/消息/日志/任务/审批/OAuth/聊天 |
| `XiHan.BasicApp.CodeGeneration` | 业务模块 | 代码生成 |
| `XiHan.BasicApp.AI` | 业务模块 | AI Provider 库化管理 / 知识库 RAG / 提示词库 / AI 助手 |
| `XiHan.BasicApp.Workflow` | 业务模块 | 工作流应用层（存储持久化 + 定义/实例/待办 + 待办通知） |
| `XiHan.BasicApp.WebHost` | 主机 | 启动入口，聚合四个业务模块，注册数据库 / Redis / Qdrant 健康检查与 Telegram Webhook |

**分层规则**：只能依赖比自己低的层，绝不反向。三个卫星模块（CodeGeneration / AI / Workflow）都依赖 `Saas`，彼此不直接依赖。

## 模块装配

一切以**模块**（`XiHanModule` + `[DependsOn]`）为装配单元，框架按依赖图拓扑排序、逐阶段初始化。

### 启动流程

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(/* 读 Hosting:Urls */);

await builder.AddApplicationAsync<XiHanBasicAppWebHostModule>();  // ① 按依赖图完成服务注册
var app = builder.Build();
await app.InitializeApplicationAsync();                           // ② 按阶段执行各模块初始化
await app.RunAsync();
```

全程 `try/catch/finally` 包裹并接 Serilog。

### 根模块

`XiHanBasicAppWebHostModule` 是依赖图的根：

```csharp
[DependsOn(
    typeof(XiHanBasicAppSaasModule),
    typeof(XiHanBasicAppCodeGenerationModule),
    typeof(XiHanBasicAppAIModule),
    typeof(XiHanBasicAppWorkflowModule)
)]
```

四个业务模块之外的可观测性与 MCP 等框架能力经 `Saas → Web.Core → Core → XiHan.Framework.*` 一路传递，**无需在根模块重复声明**。根模块额外负责：

- **健康检查**：`AddCheck<DatabaseHealthCheck>("database")` + `AddCheck<RedisHealthCheck>("redis")` + `AddCheck<QdrantHealthCheck>("qdrant")`；`/health` 匿名暴露，只回总状态与检查项名（不外泄连接串/异常）。
- **Telegram Webhook**：在 `OnPreApplicationInitialization` 注册，位于鉴权中间件**之前**，自带 `secret_token` 强校验。
- **可观测性**：开启后经 `AddOpenTelemetry` 装配链路追踪、可选指标与日志导出（`OtlpEndpoint` / `SamplingRatio`）。
- **MCP Server**：启用且配了密钥时把 AI 技能暴露为 MCP tools，端点由 `McpApiKeyEndpointFilter` fail-closed 守门。

### 生命周期钩子

| 钩子 | BasicApp 里的典型用法 |
| --- | --- |
| `ConfigureServices` | 各模块调自己的 `AddXxx` 扩展方法 |
| `OnPreApplicationInitialization` | Telegram Webhook 中间件（要插在鉴权前） |
| `OnApplicationInitialization` | Saas 映射两个 SignalR Hub 与 OAuth 端点 |
| `OnPostApplicationInitialization` | Saas 扫描声明式任务、把库里活跃的 `SysTask` 同步进调度器（含崩溃残留 Running 复位） |

## 模块内部：DDD 三层

四个业务模块内部统一分 **Domain / Application / Infrastructure**：

| 层 | 目录（以 Saas 为例） | 放什么 |
| --- | --- | --- |
| **Domain** | `Entities` / `DomainServices` / `Enums` / `Events` / `Permissions` / `Repositories` / `Specifications` / `ValueObjects` | 实体与聚合根、领域服务（业务规则）、领域事件、**权限码定义**、仓储接口 |
| **Application** | `AppServices` / `QueryServices` / `Contracts` / `Dtos` / `Mappers` / `Caching` / `EventHandlers` / `Pages` / `Exporting` | 应用服务（写侧）、查询服务（读侧）、DTO 与映射、缓存条目与失效器、事件处理器、**菜单单一事实源 `PageRegistry`** |
| **Infrastructure** | `Repositories` / `Seeders` / `Auth` / `Messaging` / `Logging` / `Security` / `MultiTenancy` / `Tasks` / `OAuth` | 仓储实现、种子数据、密钥保护器、存储/消息/日志适配 |

### CQRS 落地

- **写侧** `*AppService`：`[DynamicApi]` + `[UnitOfWork]` + `[PermissionAuthorize]`，调领域服务改状态，改完调缓存失效。
- **读侧** `*QueryService`：直接投影，多数带分布式缓存，经 FLS 门控过滤/排序字段。

两者都用 `[DynamicApi]` 暴露，**没有 Controller**。

## 服务注册约定

每个模块的接线集中在自己的 `Extensions/ServiceCollectionExtensions.cs`，由 `Module.ConfigureServices` 调用。Saas 的十个扩展方法：

```csharp
services.AddSaasDataSeeders();          // 系统基线种子
services.AddSaasDemoDataSeeders();      // 演示种子（Saas:Seed:EnableDemoData 开关）
services.AddSaasDomainServices();       // 领域服务（必须手写）
services.AddSaasApplicationServices();  // 需要手写的应用侧服务
services.AddSaasEventHandlers();        // 本地事件处理器（必须登记）
services.AddSaasLogWriters();           // 各类日志写入器
services.AddSaasAuthStores();           // Replace 覆盖框架认证相关默认实现
services.AddSaasMessageSenders();       // 消息通道
services.AddSaasJobInfrastructure();    // 任务调度存储
services.AddSaasExportInfrastructure(); // 导出基础设施
```

### 三条必须遵守的规则

::: danger 1. 领域服务必须手写注册
领域服务接口**不带** `IScopedDependency` / `IDomainService` 之类的 DI 标记接口，框架的约定注册扫不到。必须在 `AddXxxDomainServices` 里显式登记：

```csharp
services.AddScoped<IPositionDomainService, PositionDomainService>();
```

漏了就是运行期 DI 解析异常——这是新增纵切片**最常见**的漏接线点。

对比：仓储（`SaasRepository` → `IScopedDependency`）与应用/查询服务（`IApplicationService` → 瞬时）由约定**自动注册**，不用手写。
:::

::: danger 2. 覆盖框架默认实现一律用 `Replace`
框架模块用 `TryAdd` **先于**业务模块注册了默认实现（`IPermissionChecker`、`ISessionStateGate`、`IUserStore`、`IJobStore`、`IAiProviderConfigStore`、工作流的三个 Store、各 Bot `*ConfigStore` 等）。你再 `TryAdd` 会被**静默忽略**：

```csharp
services.Replace(ServiceDescriptor.Scoped<ISessionStateGate, SaasSessionStateGate>());
```
:::

::: danger 3. 本地事件处理器必须显式登记
事件总线只自动发现「以接口为服务类型」的注册，裸 `AddTransient<具体处理器>()` **不会被订阅**。用 `AddSaasLocalEventHandler<T>()` 封装（内部 `AddTransient` + 把类型加进 `XiHanLocalEventBusOptions.Handlers`）。
:::

### BasicApp 覆盖了框架的哪些默认实现

| 接口 | BasicApp 实现 | 效果 |
| --- | --- | --- |
| `IPermissionChecker` | `SaasPermissionChecker` | 鉴权改读 Redis **授权快照**，授权变更免重登即生效 |
| `ISessionStateGate` | `SaasSessionStateGate` | 会话失效 → 401、锁屏 → **423** |
| `IAiProviderConfigStore` | `SaasAiProviderConfigStore` | AI Provider 配置从数据库读（而非 appsettings） |
| `IAiPromptStore` | `SaasAiPromptStore` | 提示词从 `SysAiPrompt` 读 |
| `IWorkflowDefinitionStore` / `IWorkflowInstanceStore` / `IWorkflowBookmarkStore` | `SqlSugar*Store` | 工作流从内存存储换成落库，获得崩溃恢复 |

## 菜单：后端单一事实源

菜单、路由、组件路径、权限码、国际化键**全部在后端登记**，前端只提供视图文件。

`Application/Pages/PageRegistry.cs` 是单一事实源，两个集合：

- `All`：`PageDescriptor`（目录 / 菜单 / 外链）——页面码、标题、i18n 键、菜单类型、路由路径、路由名、前端组件路径、父页面码、**权限码**、图标、排序、重定向、外链。
- `Buttons`：`ButtonDescriptor`（页面内操作按钮）——按钮码、名称、父页面码、权限码、排序。

关键约定：

| 约定 | 说明 |
| --- | --- |
| `Component` = `Path` 去前导斜杠 + `/index` | 与前端 `src/views` 一一对应；`_core` 页面例外，走前端 `coreComponentMap` |
| `I18nKey` = `menu.{Code 中 . 与 - 换成 _}` | 文案在前端 `packages/locales/langs/{lang}/menu.ts` |
| `PermissionCode` 直接引用 `SaasPermissionCodes.*` | **建菜单即绑权限**，不要事后回填 |
| 纯静态公共页不登记 | `/about` 等由前端 `router/routes.ts` 持有 |

`SaasMenuSeeder`（`Order=25`）从 `PageRegistry` 映射出菜单种子：先按权限码查 `SysPermission`，**查不到就跳过并告警（fail-closed）**。所以权限种子（`Order=20`）必须排在它前面，父目录必须排在子项前面。

**新增/修改菜单只改 `PageRegistry`**，种子随之生效。

## 种子数据

分两类：

- **系统基线**（`AddSaasDataSeeders` 等）：**始终播种**。身份、权限、租户版本、配置、字典、菜单、消息模板、OAuth 应用、通知、存储配置、任务——应用可运行的最小骨架。
- **演示数据**（`AddSaasDemoDataSeeders`）：由 `Saas:Seed:EnableDemoData` 控制，**缺省或非法值都视为启用**，显式 `false` 才整体跳过。含示例组织、演示账号、演示业务租户。

### `Order` 段：模块间互不交叠

| 模块 | `Order` 段 |
| --- | --- |
| Saas | 10–37（系统基线 10–29、演示 30–37） |
| CodeGeneration | 100–105 |
| AI | 200–217（Provider 200–204、RAG 205–208、提示词 209–212、助手 213–217） |
| Workflow | 300–304 |

链内顺序恒为「**操作 → 资源 → 权限 → 菜单 → 角色授权**」——权限由「资源 × 操作」派生，所以操作/资源种子必须排在权限种子之前。缺了 `SysOperationSeeder` 会让整条链**静默失效**。

新模块选一段未用的 `Order`（如 400–）。

## 框架引用：源码还是 NuGet

后端**只有一套 csproj**，框架引用方式由 `backend/props/framework.props` 的 `UseXiHanFrameworkSource` 决定，**看你打开/构建的是哪个解决方案**：

| 解决方案 | 框架引用 | 场景 |
| --- | --- | --- |
| `backend/XiHan.BasicApp.slnx` | `PackageReference` → NuGet | 常规开发、发布、外部克隆 |
| 仓库根 `XiHanFun.slnx` / `XiHanFun.Local.slnx` | `ProjectReference` → 同级框架源码 | 连框架源码一起调试 |

判定条件是 `$(SolutionName)` 以 `XiHanFun` 开头**且**框架源码在位；直接 `dotnet build` 单个 csproj 走 NuGet。强制指定：`dotnet build -p:UseXiHanFrameworkSource=true|false`。

切换点只在两个基座（`Core` / `Web.Core`），业务模块作者无需关心。

::: tip 为什么以解决方案为准而不是探测目录
源码模式下 VS 要求被 `ProjectReference` 的工程也是解决方案成员，否则设计时报 `NU1105`。而 `XiHan.BasicApp.slnx` 里没有、也不该有框架工程（它要能被单独克隆的人打开），所以它必须始终走 NuGet。
:::

## 前后端协作数据流

```text
Vue 页面（Schema 驱动列表页）
   │  分页走 POST，body = { conditions, page }；附 X-Language / X-Timezone 头
   ▼
动态 API（*AppService / *QueryService，[DynamicApi] 暴露，无 Controller）
   │  中间件管道：转发头 → TraceId → 请求文化 → 路由 → CORS → 认证
   │                → 租户解析 → 会话闸门(401/423) → 授权(授权快照)
   ▼
应用服务（写侧 [UnitOfWork]） / 查询服务（读侧投影 + 缓存 + FLS 门控）
   │  → 领域服务（业务规则） → 仓储（SqlSugar，自动挂租户/软删过滤） → 数据库
   ▼
UoW 收尾：本地事件(提交前) → 提交 → 分布式事件(提交后) → 精准失效缓存 → 队列入队
   ▲
   └─ 响应统一 ApiResponse 信封；本地化覆盖 Data；时间按 X-Timezone 换算
```

## 下一步

- [开发流程](./development)：新增功能的完整接线清单
- [请求生命周期](./request-lifecycle)：中间件管道逐段与收尾时序
- [实体基类](./entity) → [数据库配置](./database) → [数据模型](./data-model)
- [框架 · 模块系统](https://framework.docs.xihanfun.com/guide/modularity)：`[DependsOn]` 与拓扑排序机制
