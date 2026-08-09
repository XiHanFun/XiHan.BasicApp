# 审计日志

XiHan.BasicApp 把"谁在什么时候、从哪里、对什么做了什么、结果如何"拆成**六类互相独立的日志**，各有专属实体、专属写入通道、专属查询接口与前端页面。它们通过一个共同的 `TraceId` 串联同一次请求的完整生命周期，从而既能各自聚焦（安全审计 / 性能分析 / 数据溯源 / 稳定性排障各取所需），又能跨表拼出一条完整链路。

日志的**底座**是框架的结构化日志能力（Serilog 双路输出、多租户日志上下文），详见 [框架 · Logging 包](https://framework.docs.xihanfun.com/packages/logging)。本页讲的是 BasicApp 在这套底座之上、面向业务与合规的**审计日志纵切**：实体、脱敏、写入机制、查询监控。

## 六类日志一览

| 日志 | 实体 | 记录什么 | 写入触发 | 权限码（查看） |
| --- | --- | --- | --- | --- |
| 访问日志 | `SysAccessLog` | 每次 HTTP 资源访问的鉴权/结果决策（含未授权尝试） | 请求日志中间件 | `saas:access-log:read` |
| API 日志 | `SysOpenApiLog` | 开放接口层的完整请求/响应详情（体/头/耗时/签名） | 开放接口中间件 | `saas:api-log:read` |
| 操作日志 | `SysOperationLog` | 业务语义的操作轨迹（用户"做了什么"） | MVC 动作过滤器 | `saas:operation-log:read` |
| 异常日志 | `SysExceptionLog` | 运行时异常（堆栈/请求上下文/严重级别） | 异常中间件 + 异常过滤器 | `saas:exception-log:read` |
| 登录日志 | `SysLoginLog` | 每次登录/登出/认证审计尝试（成功与失败） | 认证领域事件 | `saas:login-log:read` |
| 实体变更 | `SysDiffLog` | 数据库实体的增删改快照（字段级差异） | SqlSugar ORM 拦截器 | `saas:diff-log:read` |

> 注意"API 日志"的真实实体名是 `SysOpenApiLog`（开放接口日志），不是 `SysApiLog`。前端菜单叫"开放接口日志"，路由 `/log/api`。

这六类的定位刻意划分清楚，避免重复与混淆：

- **访问日志 vs API 日志**：访问日志覆盖**所有** HTTP 请求，只记决策结果与轻量上下文（`ResourceType="HttpApi"`），不落完整请求体；API 日志只针对带**开放接口签名头**（AccessKey / Signature）的调用，落完整请求/响应体与签名校验结果，用于第三方计费与 APM。
- **操作日志 vs API 日志**：操作日志关注**业务语义**（"用户导出了报表"），API 日志关注 **HTTP 事实**（`POST /api/export`）。
- **操作日志 vs 实体变更**：操作日志覆盖宽泛的业务动作；实体变更只记**数据库行**的字段级前后快照，是合规级的数据溯源。
- **登录日志 vs 操作日志**：认证类动作（登录/登出/令牌刷新/改密/绑定 MFA）由登录日志专门承担，操作日志写入器会**主动跳过**这些动作（见下），不重复记账。

所有六张表都是**只追加、永不更新**的审计数据，并且都是**按月分表**（`Sys_Xxx_Log_{year}{month}{day}`，`SplitType.Month`）。这带来一条硬约束：**查询与清理必须带时间范围**，否则框架会扫描全部月表。业务上不支持删除；清理已自动化——动态任务 `LogRetentionCleanupTask`（覆盖本页六类 + 下文的权限变更日志共 7 类分表日志）按 `CreatedTime` 逐行删除过期数据（只删行、不 `DROP` 表），保留天数读全局配置 `saas:log:retention-days`（默认 180 天，非法/缺省时回退默认值），不依赖人工 `TRUNCATE`/`DROP`。

## 逐类详解

### 访问日志 `SysAccessLog`

鉴权链路的**审计终点**：每次资源访问的最终决策落一条。核心字段是 `AccessResult`（枚举 `AccessResult`）——按响应状态码映射：

| 状态码 | AccessResult | 含义 |
| --- | --- | --- |
| < 400 | `Success` | 成功 |
| 401 | `Unauthorized` | 未授权（未登录/令牌失效） |
| 403 | `Forbidden` | 权限不足 |
| 404 | `NotFound` | 资源不存在 |
| 其它 4xx | `Failed` | 失败 |
| ≥ 500 | `ServerError` | 服务器错误 |

`Forbidden`/`Unauthorized` 是"为什么我没权限"这类问题的**第一手排查数据**。访问日志刻意**不记完整请求体**（防敏感信息泄露），只在 `ExtendData` 里保留已脱敏的查询串。记录方 IP/位置/浏览器/OS/设备等客户端信息由框架的 `IClientInfoProvider` 统一填充。

### API 日志 `SysOpenApiLog`

开放接口层的逐请求明细，字段最全：`RequestParams`/`RequestBody`/`ResponseBody`（均限长 32000 字符）、`RequestSize`/`ResponseSize`、`ExecutionTime`、`IsSignatureValid`/`SignatureType`（签名算法枚举 `SignatureType`：`None`/`HmacSha256`/`HmacSha512`/`RsaSha256`/`RsaSha512`/`Sm2`/`Sm3`/`Ed25519`/`Md5`；写入器按算法名字符串识别前六种，`Sm3`/`Ed25519` 已定义但暂未接入映射，无法识别的算法名一律落 `None`）、`ClientId`/`AppId`（第三方接入方）。

请求头/响应头字段 `RequestHeaders`/`ResponseHeaders` 存在于实体，但被 `[JsonIgnore]`（`System.Text.Json` 与 `Newtonsoft.Json` 双打），**禁止 API 回显**——`Authorization`/`Cookie`/`Set-Cookie` 等敏感头即使入库也不外泄。

### 操作日志 `SysOperationLog`

面向业务行为，写入器（`SaasOperationLogWriter`）有两处关键决策：

1. **查询动作不落库**：先按动作名语义识别 `OperationType`，若判定为 `Query` 则直接返回，不记操作日志（避免海量读请求灌满表）。
2. **认证动作让位登录日志**：`Auth` 控制器下的 `Login`/`Logout`/`EmailLogin`/`RefreshToken`/`Register`/`SwitchTenant` 等动作跳过（这些由登录日志承担）。

`OperationType` 是共享枚举（`OperationType`，也用于实体变更），语义识别优先看动作名而非 HTTP 方法（避免 POST 一律记成"新增"）：

| 值 | 标签 | 值 | 标签 |
| --- | --- | --- | --- |
| `Login` | 登录 | `Export` | 导出 |
| `Logout` | 登出 | `Review` | 审核 |
| `Query` | 查询 | `Approve` | 审批 |
| `Create` | 新增 | `StartTask` | 发起任务 |
| `Update` | 修改 | `Execute` | 执行命令 |
| `Delete` | 删除 | `Restore` | 恢复（软删除还原） |
| `Import` | 导入 | `Other` | 其他 |

`Result` 字段（`OperationExecuteResult`：`Success`/`Failed`/`PartialSuccess`）记该操作最终结果，勿与实体的启用状态混淆。

### 异常日志 `SysExceptionLog`

集中记录运行时异常，是技术排障与稳定性改进的数据源。除堆栈（`ExceptionStackTrace`，限长 32000）与请求上下文外，写入器（`SaasExceptionLogWriter`）还做两件加工：

- **异常来源归类** `ExceptionSource`：按异常类型/堆栈关键字归入"业务异常 / 数据库异常 / Redis 异常 / 第三方接口异常 / 消息队列异常 / 未捕获异常"。
- **严重级别** `SeverityLevel`（1–5）：状态码 ≥ 500 记 5，否则记 3。

还落有 `ThreadId`/`ProcessId`/`ServerHostName`/`EnvironmentName`/`ApplicationVersion` 等运行环境快照，便于生产复现。字段 `IsHandled`/`HandledTime`/`HandledBy`/`HandledRemark` 预留了异常"处理闭环"的数据结构，但**当前尚未真正打通**：写入器（`SaasExceptionLogWriter`）在入库时就把 `IsHandled` 恒置为 `true`、`HandledTime` 恒为写入时刻，且查询应用服务（`ExceptionLogQueryService`）只读——`HandledBy`/`HandledRemark` 目前没有可调用的更新入口，前端也只能查看/按 `IsHandled` 过滤，不能标记处理。

### 登录日志 `SysLoginLog`

**所有**登录尝试（含失败）都必须记录，不得丢弃——这是账户安全审计与风控的基础。它不由中间件/过滤器写入，而是由**认证领域事件**驱动（详见[写入机制](#写入机制)）：

- `AuthLoginSucceededDomainEvent` / `AuthLoginFailedDomainEvent` / `AuthLogoutDomainEvent` / `AuthSecurityAuditDomainEvent`
- 事件处理器 `AuthLoginEventHandler` 把它们统一投影为 `LoginLogRecord` 写入。

`LoginResult` 枚举既覆盖登录结果（`Success`/`InvalidCredentials`/`AccountLocked`/`AccountDisabled`/`RequiresTwoFactor`/`TwoFactorFailed`/`Failed`），也覆盖**认证审计事件**（`Logout`/`TokenRefreshed`/`PasswordChanged`/`PasswordReset`/`MfaBound`/`MfaUnbound`/`TenantSwitched`/`SessionRevoked`）——即令牌刷新、改密、绑定/解绑 MFA、切换租户（含进入平台运维态）、会话撤销（本人撤销其他会话或被管理员踢下线）也统一落这张表。`IsRiskLogin` 字段已建索引、查询与前端均可按其过滤，但**当前写入链路（`SaasLoginLogWriter`）尚未接入风控判定逻辑，恒为 `false`**（预留位，异地/新设备/深夜登录等规则待接入）。已经落地、真实生效的是登录事件处理器（`AuthLoginEventHandler`）的**并发登录检测**：登录成功时查同用户是否存在其它活跃会话（`SysUserSession`），并按设备是否曾用过区分"新设备登录"/"其它设备登录"，据此推送安全通知；登出时也会向其它在线设备推送"账号在其它设备登出"提醒。

### 实体变更 `SysDiffLog`（数据溯源）

数据库实体级的合规审计：**谁在什么时候改了哪张表哪条记录的哪些字段**。核心是三个 JSON 快照字段：

- `BeforeData`：变更前完整快照
- `AfterData`：变更后完整快照
- `ChangedFields`：本次变更涉及的字段名数组

变更类型区分 **新增 / 修改 / 删除 / 恢复**，复用 `OperationType` 枚举（`Create`/`Update`/`Delete`/`Restore`）。关键判定：**软删除**是 `IsDeleted` 从 false→true 的 `Update`，框架识别后记为 `Delete`；软删除**还原**记为 `Restore`。写入器（`SaasEntityDiffLogWriter`）还按操作类型自动定级 `RiskLevel`（`AuditRiskLevel`：低/中/高/极高/严重）——删除、导出、执行命令定为极高（4），修改/恢复/审核/审批/导入定为高（3），新增/发起任务定为中（2），其余（含查询）定为低（1）。

## 自动脱敏

敏感数据在**落库前**、于请求捕获点由框架的 `LogSanitizer`（`XiHan.Framework.Auditing.LogSanitizer`）统一脱敏——写入器拿到的记录已经是脱敏后的副本，脱敏后的日志与原始请求缓冲区互不影响。脱敏覆盖三种形态：

1. **JSON 键值对**：`"password":"xxx"` → `"password":"***"`
2. **表单/查询串键值对**：`password=xxx` → `password=***`
3. **证件号**：18 位（含 `YYMMDD`）或 15 位身份证，保留首尾各 3 位、中间打码

内置敏感键（大小写不敏感匹配）为固定清单：

<code v-pre>password | passwd | pwd | secret | token | credential | authorization | otp | verifycode | verificationcode | twofactorcode | bankcard | cardno | cardnumber | accountno | idcard | identitycard | idnumber</code>

脱敏在这些点位应用：请求日志中间件对查询串与请求体脱敏；MVC 动作过滤器对动作参数与响应结果脱敏。此外，敏感 HTTP 头（`Authorization`/`Cookie`/`Set-Cookie`）在 `SysOpenApiLog` 上直接 `[JsonIgnore]` 不回显，形成第二道防线。落库时各写入器还统一按列长裁剪（`SaasLogMappingHelper.TrimOrNull`），大文本（体/堆栈/头）限长 32000 字符防止日志爆量。

> 脱敏是**只前向**的：原文一旦被掩码就不再持久化，无法反查。需要原文的场景应走带审计的专门通道，不能依赖日志。

## 写入机制

写入采用**框架定义接口、BasicApp 提供实现**的分层：框架的日志管道（中间件/过滤器/AOP）负责在正确的时机捕获记录，`XiHan.BasicApp.Saas` 通过实现框架的 Writer 接口把记录落到自己的 `Sys*Log` 表。默认框架把这些 Writer 注册为 `Null*Writer`（空实现），BasicApp 在 `AddSaasLogWriters()` 里用真实实现**覆盖**：

```csharp
services.AddScoped<IAccessLogWriter, SaasAccessLogWriter>();
services.AddScoped<IApiLogWriter, SaasApiLogWriter>();
services.AddScoped<IOperationLogWriter, SaasOperationLogWriter>();
services.AddScoped<IExceptionLogWriter, SaasExceptionLogWriter>();
services.AddScoped<ILoginLogWriter, SaasLoginLogWriter>();
services.AddScoped<IEntityDiffLogWriter, SaasEntityDiffLogWriter>();
services.AddScoped<IEntityAuditContextProvider, SaasEntityDiffContextProvider>();
```

### 捕获时机：中间件 / 过滤器 / 事件 / AOP

不同日志在请求生命周期的不同位置被捕获：

| 日志 | 捕获方 | 类型 |
| --- | --- | --- |
| 访问日志 | `XiHanRequestLoggingMiddleware` → `IAccessLogPipeline` | ASP.NET 中间件 |
| API 日志 | `XiHanApiLoggingMiddleware` → `IApiLogPipeline`（仅带签名头的请求） | ASP.NET 中间件 |
| 操作日志 | `XiHanActionLoggingFilter` → `IOperationLogPipeline`（跳过 GET/HEAD/OPTIONS） | MVC 动作过滤器 |
| 异常日志 | `XiHanExceptionLoggingMiddleware` + 异常过滤器 → `IExceptionLogPipeline` | 中间件 + 过滤器 |
| 登录日志 | 认证领域事件 → `AuthLoginEventHandler` → `ILoginLogPipeline` | 领域事件（本地事件总线） |
| 实体变更 | SqlSugar ORM 拦截器 → `IEntityDiffLogWriter` | 数据层 AOP |

### 同步 vs 异步

访问 / API / 操作 / 异常 / 登录五类走框架的 `Pipeline` 抽象，**默认同步**写入（在请求线程内直接 `await writer.WriteAsync`）；可通过配置节 `XiHan:Auditing:LogQueue`（`XiHanAuditingLogQueueOptions`）切换为**异步队列**模式：

- `EnableAccessLogQueue` / `EnableApiLogQueue` / `EnableOperationLogQueue` / `EnableExceptionLogQueue` / `EnableLoginLogQueue`（默认全 `false` = 同步）
- 开启后记录进 `System.Threading.Channels` 有界队列（`QueueCapacity` 默认 10000，`DropOnFull` 默认 `false` = 背压等待），由后台 `BackgroundService`（如 `AccessLogQueueWorker`）**批量**排空写库（`BatchSize` 默认 100 / `BatchDelayMilliseconds` 默认 200ms，先到先触发）
- `IgnoredPathPrefixes`（默认 `["/hubs"]`）：SignalR 心跳/协商等路径不记日志

> 这与全局的"后台异步用 `IRedisDelayQueue` + 后台服务"约定属于**不同机制**：日志队列走进程内 `Channel`（低延迟、高吞吐、可丢弃/背压），发件箱/导出等跨请求的可靠任务才走 Redis 延迟队列。二者不要混淆。

**实体变更**是唯一不走 `Pipeline` 队列的：SqlSugar AOP 拦截 `Insert`/`Update`/`Delete`，在执行前后查前后快照、算差异字段、构建 `EntityDiffLogRecord`，然后**即发即弃**调用 `IEntityDiffLogWriter`。审计上下文（TraceId/UserId/UserName/TenantId）由 `SaasEntityDiffContextProvider.CreateBaseRecord()` 从请求上下文补全。为避免**递归审计**，该 Provider 的 `ShouldAudit` 只审计命名空间含 `Domain.Entities` 的类型，且**显式排除全部日志表自身**（`SysAccessLog`/`SysOpenApiLog`/`SysDiffLog`/`SysExceptionLog`/`SysLoginLog`/`SysOperationLog` 等）。

## 查询与监控

六类日志各有一个查询应用服务（`*QueryService`），经 `[DynamicApi]` 直接暴露为 REST（无 Controller），统一放在 `Application/QueryServices/Logging/`。它们只读（查询/详情），遵循 BasicApp 全局约定：

- **分页走 POST**：`GetXxxLogPageAsync` 显式标 `[HttpPost]`，前端把整个查询对象（含 `conditions`/`filters`/`sorts`）作 body 下发。
- **权限门控**：每个方法用 `[PermissionAuthorize(SaasPermissionCodes.XxxLog.Read)]` 校验查看权限。
- **字段级安全（FLS）**：排序与过滤在下推前经 `IFieldSecurityService.GuardSortsAsync` / `GuardFiltersAsync` 门控，剔除**不可读或已脱敏**的字段——保证用户不能借排序/过滤旁路脱敏策略。前端选择的多字段排序优先，无有效排序时回退默认按时间倒序（访问日志按 `AccessTime`、操作日志按 `OperationTime`、实体变更按 `AuditTime` 等）。
- **分表查询**：查询链路都带 `.SplitTable()`，配合时间区间条件命中对应月表。

查询支持的过滤维度按日志类型定制，共性包括：时间区间（`Between`）、`UserId`/`UserName`、`TraceId`（跨日志串联同一请求）、关键字模糊、执行耗时区间；再叠加各自特有维度（访问结果、操作类型、异常严重级别、登录结果、实体类型/风险等级等）。

### 导出

六类日志都可导出：每类有一个导出 Provider（`AccessLogExportProvider` 等，`BusinessType` 形如 `log.access`），复用查询服务分页取数，导出权限用对应的 `saas:xxx-log:export` 权限码。导出与列表遵循**一致的脱敏**策略。

### 前端页面

菜单由后端 `PageRegistry` 单一注册（建菜单即绑权限码），本页六个页面挂在"审计中心"分组（路由 `/log`）下：

| 菜单 | 路由 | 组件 | 图标 |
| --- | --- | --- | --- |
| 访问日志 | `/log/access` | `log/access/index` | `lucide:globe` |
| 开放接口日志 | `/log/api` | `log/api/index` | `lucide:webhook` |
| 操作日志 | `/log/operation` | `log/operation/index` | `lucide:mouse-pointer-click` |
| 登录日志 | `/log/login` | `log/login/index` | `lucide:log-in` |
| 异常日志 | `/log/exception` | `log/exception/index` | `lucide:triangle-alert` |
| 数据变更日志 | `/log/diff` | `log/diff/index` | `lucide:file-diff` |

> 同一分组下还有"日志链路追踪"（`/log/trace`）与"权限变更日志"（`/log/permission-change`）两个页面，见下文[相关能力](#相关能力-权限变更日志与链路追踪查询)。

每页是一个查询表格（时间区间 + 多维过滤 + 多字段排序 + 导出按钮），点行进共享的详情抽屉 `_components/LogDetailDrawer.vue` 查看完整字段——实体变更页在详情里渲染 `BeforeData`/`AfterData`/`ChangedFields` 的前后对比。导出按钮受 `saas:xxx-log:export` 权限码控制（无权限不渲染）。

## 相关能力：权限变更日志与链路追踪查询

"审计中心"菜单分组下，除本页六类外还挂了两个紧密关联、但**不计入"六类"**的能力，二者共用同一套写入基础设施与保留策略（含前文的 `LogRetentionCleanupTask`）：

- **权限变更日志 `SysPermissionChangeLog`**（菜单"权限变更日志"，路由 `/log/permission-change`，权限码 `saas:permission-change-log:read`，仅查看、无导出）：结构化记录"谁在什么时候给谁授予/撤销了什么权限"——`OperatorUserId`/`TargetUserId`/`TargetRoleId`/`PermissionId` + `ChangeType`，按月分表。授权写路径通过 `IAuthorizationChangeNotifier` 发布 `AuthorizationChangedDomainEvent`，`PermissionChangeLogEventHandler` 订阅后落库；查询走只读的 `PermissionChangeLogQueryService`。与 `SysDiffLog` 的边界：`SysDiffLog` 记通用实体字段变更，本表专注权限授予/撤销这一业务语义。
- **链路追踪查询 `TraceQueryService`**（菜单"日志链路追踪"，路由 `/log/trace`，权限码 `saas:log-trace:read`）：按维度（用户名 / 会话标识 / `TraceId` / IP / 用户主键）+ 时间范围（≤ 31 天，防止扫描过多月表）跨**全部 7 类**分表日志（本页六类 + 权限变更日志）聚合成一条时间倒序时间线，单类型默认最多返回 200 条（上限 500，超出标记 `Truncated`）。它是本页反复强调的"用 `TraceId` 串联同一次请求全链路"能力的实际查询入口。

## 与框架的关系

- **结构化日志底座**：Serilog 双路输出、按天/大小滚动、多租户日志上下文（UserId/TenantId/TraceId），见 [框架 · Logging 包](https://framework.docs.xihanfun.com/packages/logging)。审计日志是在此之上的、面向合规的**结构化数据表**，而非文本日志文件。
- **日志管道与脱敏**：请求捕获点（中间件 `XiHanRequestLoggingMiddleware`/`XiHanApiLoggingMiddleware`/`XiHanExceptionLoggingMiddleware`、过滤器 `XiHanActionLoggingFilter`）由框架 `XiHan.Framework.Web.Api` 提供；`Pipeline` 抽象、`Writer` 接口、异步队列与 `LogSanitizer` 脱敏器统一在 `XiHan.Framework.Auditing` 包，BasicApp 只提供 Writer 落库实现。
- **实体变更拦截**：由框架 `XiHan.Framework.Data.Auditing`（`EntityChangeInterceptor`，基于 SqlSugar 命令级 AOP）与仓储基类 `.EnableDiffLogEvent()`（`XiHan.Framework.Data.SqlSugar.Auditing.SqlSugarDiffLogAop`）两条互补路径提供，BasicApp 提供 `IEntityDiffLogWriter` 与 `IEntityAuditContextProvider`。
- **动态 API / 分页 / FLS**：查询服务遵循 BasicApp 全局约定，分别见[动态 API](https://framework.docs.xihanfun.com/guide/dynamic-api) 与[权限模型](./permission)（字段级脱敏、数据范围）。
