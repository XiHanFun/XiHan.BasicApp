# 架构总览

`backend-next` 是一个 .NET 10 解决方案：14 个能力包工程 + 宿主 + 两个测试工程，全部建立在 XiHan.Framework 5 之上。本页讲清三件事：能力包是什么、包之间怎么分层、一个包怎样被宿主装配起来。

## 能力包

每个业务能力是一个实现 `IFeature` 的类，手写**清单**（`Manifest`）并在 `Configure` 里显式登记自己的一切：

```csharp
public sealed class SettingsFeature : IFeature
{
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "settings",          // xihan.basicapp.settings
        Package = "XiHan.BasicApp.Settings",
        Version = AppInfo.Version,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(SettingsCapabilities.Settings, SettingsCapabilities.Version)],  // basicapp.settings@1
        Requires =
        [
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
        context.Services.AddMessageCatalog(SettingsMessages.Catalog);   // 10 种区域的文案
        context.Services.AddGeneratedRepositories();                    // 源生成的仓储登记
        context.Services.AddScoped<IConfigValueReader, ConfigValueReader>();  // 对其他包公开的 Contracts 接口
        context.Services.AddPermissionCatalog("settings", SettingsPermissions.All);
        context.Services.AddPageCatalog("settings", SettingsPages.All);
        foreach (var permission in SettingsPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.AddGeneratedOperations();                               // 源生成的操作登记
    }
}
```

没有约定扫描、没有 `IScopedDependency`、没有拦截器：一个包贡献了什么，读它的 `Feature` 类就知道。

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
| 同层不互相引用 | Tier 2 的包之间经 `Contracts` 的接口与消息协作 |
| 跨层协作经 `Contracts` | 如 `IConfigValueReader`、`INumberGenerator`、`ITenantQuota`、`IFieldSecurity`、`IRecipientSource`、`IMessageSender`、`IRealtimePush`、`IWorkbenchWidgetProvider`、`CacheNamespace` |
| 能力依赖用能力 ID | 清单里写 `basicapp.identity@1` 这类能力，不写包名 |

层级由 `Architecture.Tests` 的 `TierDependencyTests` 守护。Tier 3 里只有 Workbench 已落地；Workflow、Printing、CodeGeneration、Chat 规划在 N3，AI 规划在 N4。

## 宿主装配

`XiHan.BasicApp.Host` 的 `BasicAppHost.Create` 显式列出每一个 Feature：

```csharp
builder.AddXiHan(xihan =>
{
    xihan
    .UseFullProfile()
    .UseFeature(new SqlSugarPersistenceFeature(SqlSugarRuntime.Configure) { DiffLog = DiffLogBuffer.IsLogged })
    .UseFeature<JwtSecurityFeature>()
    .UseFeature<DatabaseMultiTenancyFeature>()     // 租户目录 TenantRecord
    .UseFeature<DatabaseSecurityFeature>()         // 授予存储 PermissionGrantRecord
    .UseFeature<DatabaseAuditingFeature>()         // 操作审计 AuditRecord
    .UseFeature<DatabaseSchedulingFeature>()       // 作业与运行记录
    .UseFeature<HostedBlobUrlFeature>()            // 本地存储签名地址
    .UseFeature<Ip2RegionSecurityFeature>()        // IP 归属地
    .UseFeature<OutboxFeature>()                   // 领域事件与业务写入同事务派发
    .UseFeature<RuntimeFeature>()
    .UseFeature<IdentityFeature>().UseFeature<TenancyFeature>().UseFeature<OrganizationFeature>().UseFeature<AccessFeature>()
    .UseFeature<SettingsFeature>().UseFeature<FilesFeature>().UseFeature<NotificationFeature>()
    .UseFeature<AuditingFeature>().UseFeature<RealtimeFeature>().UseFeature<MaintenanceFeature>()
    .UseFeature<OpenPlatformFeature>();
    if (modules.Workbench)
    {
        xihan.UseFeature<WorkbenchFeature>();      // BasicApp:Modules:Workbench:Enabled
    }
});
```

`UseFullProfile()` 带来 HTTP / gRPC / MCP 协议、文件系统对象存储、内存缓存与消息、OpenTelemetry；再 `UseFeature` 一个显式 Provider 即顶替对应的缺省实现。宿主当前**没有**装 Redis、RabbitMQ、`Localization.Database` 等 Provider，缓存与消息是进程内实现（见[部署](../deployment#多实例的限制)）。

随后映射端点：

| 调用 | 端点 |
| --- | --- |
| `MapXiHanHealth()` | `/health/startup`、`/health/ready`、`/health/live` |
| `MapXiHanOperations()` | `/api/**`（全部 `Expose.Http` 的操作） |
| `MapXiHanOpenApi()` + Scalar | `/openapi.json`、`/openapi/<组>.json`、`/scalar`（`BasicApp:ApiDocs:Enabled`） |
| `MapXiHanMcp()` | `/mcp` |
| `MapXiHanGrpc()` / `MapXiHanProto()` | gRPC 服务与 `/operations.proto` |
| `MapXiHanBlobs()` | `/blobs`：本地存储的签名地址 |
| `MapBasicAppEndpoints()` | 文件上传与内容、头像、`/hubs/notification`、Telegram Webhook、OAuth 2.0 / OIDC 服务端、第三方登录回调 |

## 一份契约，三种协议

同一个操作按 `Expose` 决定发布到哪里：

| 协议 | 谁用 | 发布哪些 |
| --- | --- | --- |
| HTTP | 管理端、第三方 | 全部操作（`Expose.Http`） |
| gRPC | 内部服务 | 显式 `Expose.Grpc` 的：目前 `users.get`、`users.list` |
| MCP / AI 工具 | AI 客户端 | 显式 `Expose.Mcp \| Expose.Ai` 的只读目录类：用户、部门、岗位、部门成员、权限目录、字典与字典项（共 12 个） |

管理类写操作一律只走 HTTP；超出 HTTP 的每一个都登记在 `OperationExposureTests` 里，且必须只读。

## 包清单

| 包 | 提供能力 | 页面 | 后台作业 / 中间件 |
| --- | --- | --- | --- |
| Contracts | — | — | — |
| Runtime | `basicapp.runtime@1` | — | `AuditorStampMiddleware`（Transaction）、表结构升级、种子执行、维护模式 |
| Identity | `basicapp.identity@1` | 用户、在线用户 | `SessionGateMiddleware`（Authenticate） |
| Tenancy | `basicapp.tenancy@1` | 租户、套餐、我的订阅 | — |
| Organization | `basicapp.organization@1` | 部门、岗位 | — |
| Access | `basicapp.access@1` | 角色、权限目录、委托、字段安全、访问策略、约束规则、菜单 | `AccessPolicyMiddleware`（Tenant）、`PermissionConditionMiddleware`（Authorize）、权限过滤器与授予来源 |
| Settings | `basicapp.settings@1` | 参数、字典、业务编号 | — |
| Files | `basicapp.files@1` | 文件库、存储配置、导出中心 | `files.uploads.expire` |
| Notification | `basicapp.notification@1` | 通知公告、发送记录、模板、邮件 / 短信 / 机器人 / Telegram 配置、收件箱 | 发件箱发送循环、Telegram 接收 |
| Auditing | `basicapp.auditing@1` | 链路追踪与八类日志 | `AuditLogMiddleware`（Observe）、`auditing.retention` |
| Maintenance | `basicapp.maintenance@1` | 作业、缓存、服务监控、版本、升级记录 | — |
| OpenPlatform | `basicapp.openplatform@1` | OAuth 应用 | `RequestSigningMiddleware`（Authenticate）、`openplatform.oauth-cleanup` |
| Realtime | `basicapp.realtime@1` | — | `RealtimeDispatchMiddleware`（Transaction 开头） |
| Workbench | `basicapp.workbench@1` | 工作台 | — |

## 下一步

- [开发流程](./development)：写一个操作的完整步骤
- [请求生命周期](./request-lifecycle)：管道各阶段与 BasicApp 的中间件
- [实体基类](./entity) · [数据模型](./data-model)
