来源：https://basicapp.docs.xihanfun.com/backend/introduction

# 架构总览

本页说明能力包的组成、包之间的分层与宿主的装配方式，适用于开始阅读或修改后端代码的开发者。`backend` 是一个 .NET 10 解决方案（`XiHan.BasicApp.slnx`），包含 `Contracts` 与 18 个能力包、宿主 `XiHan.BasicApp.Host`，以及架构测试与集成测试两个测试工程，全部建立在 XiHan.Framework 之上。

## 能力包

每个能力包有一个实现 `IFeature` 的类：手写清单（`Manifest`），在 `Configure` 里显式登记自己的一切。以 Settings 为例：

```csharp
public sealed class SettingsFeature : IFeature
{
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "settings",          // xihan.basicapp.settings
        Package = "XiHan.BasicApp.Settings",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(SettingsCapabilities.Settings, SettingsCapabilities.Version)],  // basicapp.settings@1
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(MultiTenancyCapabilities.Context, MultiTenancyCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
        ],
        Contributes = new Contributions { Permissions = SettingsPermissions.All.Select(static p => p.Code).ToList() },
        Entry = typeof(SettingsFeature).FullName,
    };

    public void Configure(FeatureContext context)
    {
        context.Services.AddMessageCatalog(SettingsMessages.Catalog);          // 10 种区域的文案
        context.Services.AddGeneratedRepositories();                           // 源生成的仓储登记
        context.Services.AddScoped<IConfigValueReader, ConfigValueReader>();   // 对其他包公开的 Contracts 接口
        // … 领域服务、Queries、种子
        context.Services.AddPermissionCatalog("settings", SettingsPermissions.All);
        SettingsFieldSecurity.Register(context.Services);                      // 字段安全绑定
        context.Services.AddPageCatalog("settings", SettingsPages.All);
        foreach (var permission in SettingsPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.AddGeneratedOperations();                                      // 源生成的操作登记
    }
}
```

没有约定扫描、`IScopedDependency` 或拦截器：一个包提供的仓储、服务、权限、页面、消息目录、中间件、作业与操作都登记在它的 `Feature` 类里。

## 分层

```text
Tier 0 基座       Contracts → Runtime
Tier 1 身份租户    Identity → Tenancy → Organization → Access
Tier 2 平台能力    Settings | Notification | Files | Auditing | Maintenance | OpenPlatform | Realtime
Tier 3 可选模块    Workbench | Workflow | Printing | CodeGeneration | Chat | AI
Host              XiHan.BasicApp.Host
```

| 规则 | 说明 |
| --- | --- |
| 只能向下依赖 | Tier 2 只引用 Tier 0 / 1；Tier 1 内部严格按箭头单向 |
| 同层不互相引用 | Tier 2、Tier 3 的包之间经 `Contracts` 的接口与消息协作 |
| 跨包协作经 `Contracts` | 如 `IConfigValueReader`、`INumberGenerator`、`ITenantQuota`、`IEditionGate`、`IFieldSecurity`、`IRecipientSource`、`IOrganizationDirectory`、`IMessageSender`、`IRealtimePush`、`ITemporaryFiles`、`IWorkbenchWidgetProvider`、`IWorkflowBusinessHandler`；消费方以 `IEnumerable<>` 注入，要处理没有实现的情形 |
| 能力依赖用能力 ID | 清单里写 `basicapp.identity@1` 这类能力，不写包名 |

层级由 `Architecture.Tests` 的 `TierDependencyTests` 守护，包内目录由 `PackageStructureTests` 守护。

## 宿主装配

`XiHan.BasicApp.Host` 的 `BasicAppHost.Create` 显式列出每一个 Feature（节选）：

```csharp
builder.AddXiHan(xihan =>
{
    xihan
    .UseFullProfile()
    .UseFeature(new SqlSugarPersistenceFeature(SqlSugarRuntime.ConfigureWith(builder.Configuration)) { DiffLog = DiffLogBuffer.IsLogged })
    .UseFeature<JwtSecurityFeature>()
    .UseFeature<Ip2RegionSecurityFeature>()        // IP 归属地
    .UseFeature<DatabaseMultiTenancyFeature>()     // 租户目录 tenant_record
    .UseFeature<DatabaseSecurityFeature>()         // 授予存储 permission_grant_record
    .UseFeature<DatabaseAuditingFeature>()         // 操作审计 audit_record
    .UseFeature<DatabaseSchedulingFeature>()       // 作业、触发器与运行记录
    .UseFeature<DatabaseWorkflowFeature>()         // 工作流引擎的表，Workflow 模块关掉也装
    .UseFeature<DatabaseLocalizationFeature>()     // 运营覆盖的译文 message_translation
    .UseFeature<HostedBlobUrlFeature>()            // 本地存储的签名地址
    .UseFeature<OutboxFeature>()                   // 领域事件与业务写入同事务派发
    .UseFeature<RuntimeFeature>()
    .UseFeature<IdentityFeature>().UseFeature<TenancyFeature>().UseFeature<OrganizationFeature>().UseFeature<AccessFeature>()
    .UseFeature<SettingsFeature>().UseFeature<FilesFeature>().UseFeature<NotificationFeature>().UseFeature<AuditingFeature>()
    .UseFeature(new RealtimeFeature(scaleOut: providers.Redis))
    .UseFeature<MaintenanceFeature>().UseFeature<OpenPlatformFeature>();
    if (providers.Redis)                           // 配了绑定 cache
    {
        xihan.UseFeature<RedisCachingFeature>().UseFeature<HybridCachingFeature>();
    }

    if (providers.RabbitMQ)                        // 配了绑定 queue
    {
        xihan.UseFeature<RabbitMQMessagingFeature>();
    }

    if (modules.Workbench)                         // BasicApp:Modules:Workbench:Enabled
    {
        xihan.UseFeature<WorkbenchFeature>();
    }

    // Printing、Chat、Workflow、CodeGeneration 同理；AI 另装 OpenAIFeature，配了 Qdrant 再装知识库
});
```

`UseFullProfile()` 带来 HTTP / gRPC / MCP / CloudEvents 协议适配器、文件系统对象存储、进程内的缓存、消息、调度、审计与工作流、AI 工具桥与 OpenTelemetry；再 `UseFeature` 一个显式 Provider 即顶替对应的缺省实现。Redis 与 RabbitMQ 由 `HostProviders` 按配置决定：配了连接才装，没配即进程内实现、单机零外部依赖（见[配置参考](../configuration#分布式缓存与消息)）；配了 Redis 时实时推送也跨实例（见[部署](../deployment#多实例部署)）。可选模块由 `HostModules` 读 `BasicApp:Modules:<名>:Enabled`：没写即开，写了必须是 `true` / `false`。

随后 `BasicAppHost.Map` 按固定顺序接入 ASP.NET Core 中间件（见[请求生命周期](./request-lifecycle)）并映射端点：

| 调用 | 端点 |
| --- | --- |
| `MapXiHanHealth()` | `/health/startup`、`/health/ready`、`/health/live` |
| `MapXiHanOperations()` | `/api/**`：全部 `Expose.Http` 的操作 |
| `MapXiHanOpenApi()` + Scalar | `/openapi.json`、`/openapi/<组>.json`、`/scalar`（`BasicApp:ApiDocs:Enabled`，缺省只在 Development） |
| `MapXiHanMcp()` | `/mcp` |
| `MapXiHanGrpc()` / `MapXiHanProto()` | gRPC 服务与 `/operations.proto` |
| `MapXiHanBlobs()` | `/blobs`：本地存储的签名地址 |
| `MapXiHanCloudEvents()` | `/events`：内部服务投递的 CloudEvents（`BasicApp:CloudEvents:Enabled`，缺省关，只收持有 `runtime.cloud-events.deliver` 的调用方） |
| `MapBasicAppEndpoints()` | 根路径自述 `/`、文件上传与内容、头像上传、实时 Hub（`/hubs/notification`、`/hubs/chat`）、聊天附件上传与代码生成产物包下载（模块开着时）、Telegram Webhook、OAuth 2.0 / OIDC 服务端、第三方登录的跳转与回调 |

## 协议暴露

同一个操作按 `Expose` 决定发布到哪里，每个操作都显式写（漏写即编译错误 `XHG004`）：

| 协议 | 谁用 | 发布哪些 |
| --- | --- | --- |
| HTTP | 前端、第三方 | 全部操作（`Expose.Http`） |
| gRPC | 内部服务 | `users.get`、`users.list` |
| MCP / AI 工具 | AI 客户端、智能体 | 只读的目录类：用户、部门（详情与树）、岗位、部门成员、权限目录、字典、字典项、知识检索 `knowledge-documents.search`、我的待办 `tasks.list`（共 15 个） |

管理类写操作一律只走 HTTP；超出 HTTP 的每一个都登记在 `OperationExposureTests` 里，且必须只读（动词 `get` / `list` 或声明 `ReadOnly = true`）。

## 包清单

| 包 | 提供能力 | 页面 | 后台作业 / 中间件 |
| --- | --- | --- | --- |
| Contracts | — | — | — |
| Runtime | `basicapp.runtime@1` | — | `AuditorStampMiddleware`、`CacheInvalidationMiddleware`（Transaction）、`MaintenanceModeMiddleware`（ASP.NET Core）、表结构升级、种子执行 |
| Identity | `basicapp.identity@1` | 用户、在线用户 | `SessionGateMiddleware`（Authenticate） |
| Tenancy | `basicapp.tenancy@1` | 租户、套餐、我的订阅 | `tenancy.tenants.expire` |
| Organization | `basicapp.organization@1` | 部门、岗位 | 领域事件 `basicapp.organization.membership-changed` |
| Access | `basicapp.access@1` | 角色、权限目录、授权申请、字段安全、访问策略、约束规则、菜单 | `AccessPolicyMiddleware`（Tenant）、`PermissionConditionMiddleware`（Authorize）、`FieldSecurityMiddleware`（Handle）、权限过滤器与授予来源 |
| Settings | `basicapp.settings@1` | 参数配置、字典、业务编号 | — |
| Files | `basicapp.files@1` | 文件管理、存储配置、导出中心 | `files.uploads.expire`、`files.temporary.purge`、导出执行循环 |
| Notification | `basicapp.notification@1` | 我的消息、通知公告、消息模板、邮件短信、邮件 / 短信 / 机器人配置、Telegram 机器人 | 发送循环、Telegram 接收、`PublishProgressMiddleware`（Transaction 开头） |
| Auditing | `basicapp.auditing@1` | 链路追踪与八类日志 | `AuditLogMiddleware`（Observe）、日志写入循环、`auditing.retention` |
| Realtime | `basicapp.realtime@1` | — | `/hubs/notification`、`RealtimeDispatchMiddleware`（Transaction 开头） |
| Maintenance | `basicapp.maintenance@1` | 任务调度、缓存管理、服务监控、版本管理、升级记录 | — |
| OpenPlatform | `basicapp.openplatform@1` | 应用管理（OAuth 应用） | `RequestSigningMiddleware`（Authenticate）、`openplatform.oauth-cleanup` |
| Workbench | `basicapp.workbench@1` | 工作台 | — |
| Printing | `basicapp.printing@1` | 打印模板 | — |
| Chat | `basicapp.chat@1` | 在线聊天、聊天审计 | `/hubs/chat`、`chat.messages.cleanup` |
| Workflow | `basicapp.workflow@1` | 审批中心、流程定义、流程实例 | 待办、转办与故障通知的消息处理器 |
| CodeGeneration | `basicapp.codegeneration@1` | 代码生成 | — |
| AI | `basicapp.ai@1` | AI 提供商、助手、提示词、知识库 | `AiProviderCatalogMiddleware`（Transaction 开头） |

## 下一步

- [开发流程](./development)：写一个操作的完整步骤
- [请求生命周期](./request-lifecycle)：管道各阶段与 BasicApp 的中间件
- [实体基类](./entity) · [数据模型](./data-model)
