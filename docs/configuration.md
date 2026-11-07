# 配置参考

本页列出 6.0 宿主与管理端的配置项、缺省值与含义，适用于开发、部署与运维时查阅。

## 文件与优先级

宿主是标准 ASP.NET Core 应用，配置按 .NET 的缺省顺序叠加：`appsettings.json` → `appsettings.{环境}.json` → 用户机密（Development）→ 环境变量 → 命令行。环境变量用双下划线分节，如 `XiHan__Security__Jwt__Secret`。

配置分两组：

- 框架节 `XiHan:*`：由 XiHan.Framework 5 的各个 Feature 读取（内核、绑定、持久化、安全、租户、本地化、审计、协议）。
- 应用节 `BasicApp:*`：由各能力包的 Options 读取，直接绑定，不进入清单的配置贡献。

宿主的 `appsettings.json` 写出了上面两组的全部配置项，每一项都带注释：写出来的值就是缺省值，注释掉的是可选项（去掉 `//`、填上真实值即生效）。只在开发环境生效的值写在 `appsettings.Development.json`。.NET 的 JSON 配置允许 `//` 注释。

::: danger 不提交密钥
`appsettings.json` 里的 JWT 密钥、签名地址密钥、数据库连接串都留空；`appsettings.Development.json` 里的值只用于本机开发，部署前必须由环境变量或密钥存储替换。下文示例一律用占位符。
:::

## `Kestrel`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Kestrel:Endpoints:Http:Url` | `http://localhost:9710` | HTTP API、OpenAPI、MCP、Hub |
| `Kestrel:Endpoints:Grpc:Url` | `http://127.0.0.1:9711` | gRPC，`Protocols: Http2`，只应监听内网 |

## `XiHan:Bindings:db`

平台库连接，启动硬要求：缺失即 `RUNTIME_VALIDATION_001` 中断启动，不回退到内存实现；`provider` 不写即 PostgreSQL。

`appsettings.json` 不写任何绑定：框架先读 `SERVICE_BINDING_ROOT` 目录、再读配置节，配置节里的同名绑定会整个盖掉目录里挂进来的（值为空也一样）。本机开发写在 `appsettings.Development.json`，部署用环境变量（`XiHan__Bindings__db__value`）或 Kubernetes Service Binding（`db` 目录下放 `type`、`uri`，可选 `provider`）。

```json
{
  "XiHan": {
    "Bindings": {
      "db": {
        "type": "connection:relational",
        "value": "Host=<主机>;Port=5432;Database=<库名>;Username=<用户>;Password=<密码>",
        "provider": "postgresql"
      }
    }
  }
}
```

独立库租户的连接串不写在配置里：由 `tenants.configure-database` 写进租户目录，以 `ISecretProtector` 密文存放。

## 分布式缓存与消息

不配即单机：缓存与消息走框架的进程内实现，零外部依赖。配了连接，宿主就装上对应的 Provider，多个实例共享缓存与消息；配了而连不上，启动即失败，不退回内存实现。

| 连接（任选一处） | 装上的 Provider | 健康项 |
| --- | --- | --- |
| 绑定 `XiHan:Bindings:cache`（类型 `cache:distributed`）或 `XiHan:Caching:Redis:Configuration` | `Caching.Redis`：`ISharedCache`、字节存储、分布式锁与幂等去重；`Caching.Hybrid`：`ICache` 成为本机 L1 + Redis L2，写入经 Redis 频道失效各实例的 L1 | `caching.redis.connection` |
| 绑定 `XiHan:Bindings:queue`（类型 `queue`）或 `XiHan:Messaging:RabbitMQ:Configuration` | `Messaging.RabbitMQ`：发件箱派发与 CloudEvents 发布模式经它，各实例按主题竞争消费 | `messaging.rabbitmq.connection` |

```json
{
  "XiHan": {
    "Bindings": {
      "cache": { "type": "cache:distributed", "value": "<主机>:6379,password=<密码>" },
      "queue": { "type": "queue", "value": "amqp://<用户>:<密码>@<主机>:5672" }
    }
  }
}
```

`appsettings.json` 不写这两个绑定，没配即单机；部署时用环境变量 `XiHan__Bindings__cache__value`、`XiHan__Bindings__queue__value` 或 Kubernetes Service Binding（`SERVICE_BINDING_ROOT` 下的 `cache` / `queue` 目录）提供。绑定与 `Configuration` 键都配了时绑定优先（框架 `25614c9a` 起），绑定的值为空才看 `Configuration`。口令单独存放时，绑定可以另带 `username` / `password` 属性（Service Binding 目录里的同名文件），各自覆盖连接串里的用户名与密码（框架 `42d4c570` 起）。

### `XiHan:Caching`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Redis:KeyPrefix` | `xihan:` | 键前缀；同一个 Redis 给几套环境用时各取一个 |
| `Redis:DefaultTimeToLiveSeconds` | `300` | 没指定过期时的存活时间 |
| `Redis:PublishChanges` | `true` | 写入与删除后向频道 `<前缀>__changes` 发通知，各实例据此失效 L1 |
| `Redis:HealthProbeIntervalSeconds` | `15` | 健康项的重探间隔 |
| `Hybrid:LocalCapacity` / `Hybrid:LocalTimeToLiveSeconds` | `10000` / `30` | 本机 L1 的条数上限与存活上限；收不到变更通知时，别的实例的写入最迟这么久后可见 |

### `XiHan:Messaging`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `RabbitMQ:Exchange` | `xihan` | 发布方只声明这个持久 direct 交换机，路由键 `<QueuePrefix><主题>`；不能为空或以 `amq.` 开头，RabbitMQ 上已有同名但不是持久 direct 的交换机即启动失败 |
| `RabbitMQ:QueuePrefix` | `xihan.` | 订阅方声明持久队列 `<前缀><主题>` 并以同名路由键绑定到交换机；没人订阅的主题发布即丢弃（记 Debug 日志），不堆积 |
| `RabbitMQ:PrefetchCount` | `16` | 每个主题的预取条数 |
| `RabbitMQ:HealthProbeIntervalSeconds` | `15` | 健康项的重探间隔 |
| `InstanceId` | 每次启动随机 | 本实例在消息里的标识：1–64 个字母、数字或 `._-`，字母或数字开头，不合文法即启动失败；配了就必须每个实例各不相同（多副本可设成 Pod 名）。Realtime 的断开连接是广播订阅，RabbitMQ 上按它给每个实例建自己的队列；不配即可 |
| `MaxAttempts` | `3` | 处理失败的重试次数，用完进死信队列 |
| `Outbox:PollIntervalSeconds` / `Outbox:MaxAttempts` | `1` / `10` | 发件箱的轮询间隔与派发尝试上限 |

6.0 目前的消息处理器是 Realtime 断开连接的广播订阅与 Chat 订阅的部门归属变更（Organization 的领域事件，见[实体基类](./backend/entity#领域事件)），RabbitMQ 承接它们、框架的发件箱与 CloudEvents；邮件、短信与机器人消息的发送记录是应用表，不经消息代理。

## `XiHan:Persistence:SqlSugar`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `InitTables` | `false`（Development 为 `true`） | 启动时按实体建缺表、加缺列；只用于开发，正式环境由 `Schema/` 脚本管理 |
| `CommandTimeoutSeconds` | `300` | 语句超时（秒），正整数；写 0 或负值启动失败（5.x 是当作不覆盖） |
| `Replicas:DefaultHitRate` | `10` | 从库没写权重时的读权重 |
| `Replicas:HealthCheck` | `true` | 从库健康探测：周期探活，连不上的自动摘除、恢复后加回（5.x 缺省关） |
| `Replicas:HealthCheckIntervalSeconds` | `30` | 探活间隔 |
| `Replicas:FailureCooldownSeconds` | `120` | 摘除后多久再试 |

从库本身在 [`BasicApp:Database`](#basicapp-database) 配。健康快照看 `persistence.sqlsugar.replicas`。

## `XiHan:Security`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `RoleClaim` | `role` | 令牌里的角色声明名 |
| `Jwt:Secret` | 空 | HS256 密钥，至少 32 字节；Identity 签发与框架校验共用，Hub 的 JwtBearer 也用它 |
| `Jwt:Issuer` / `Jwt:Audience` | `xihan.basicapp` | 签发者与受众 |
| `Jwt:ClockSkewSeconds` | `60` | 时钟容差 |

## `XiHan:Security:Outbound`

出站地址策略：向用户填的地址发请求时能连哪些 IP。缺省只能连公网地址，回环、私网、链路本地和云元数据地址都拒绝。检查放在建立连接时，按解析出的实际 IP 判，重定向也一样，所以 DNS 重绑定绕不过去。

受这道策略约束的：工作流 `Http` 活动、群机器人 Webhook、AI 提供商地址、第三方登录、S3 兼容存储、CloudEvents 投递、Telegram（配了代理时不受约束，代理地址只有平台管理员能配）。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `AllowLoopback` | `false` | 放行回环地址。`appsettings.Development.json` 打开 |
| `AllowPrivateNetwork` | `false` | 放行私网地址 |
| `AllowedNetworks` | 空 | 放行的网段（CIDR） |
| `AllowedHosts` | 空 | 放行的主机名 |

自建 MinIO、内网 Webhook、流程调内网服务、本机模型端点都要在这里放行，否则连接被拒。

## `XiHan:ObjectStorage:SignedUrls:Secret`

本地存储签名地址（宿主 `/blobs` 端点）的签名密钥。S3 兼容存储由存储自己签名，不用它。

## `XiHan:MultiTenancy`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `RequireTenant` | `false` | 平台主体没有租户声明，必须为 `false` |
| `TenantClaim` | `tenant` | 令牌里的租户声明名 |

租户只从令牌声明（及框架 Baggage）解析，不支持查询串与 `X-Tenant-Id` 头。

## `XiHan:Localization`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `DefaultLocale` | `en-US` | 缺省区域；请求按 `Accept-Language` 选区域，内置 `en-US`、`zh-CN`、`zh-TW`、`ja-JP`、`ko-KR`、`de-DE`、`es-ES`、`fr-FR`、`pt-BR`、`ru-RU` |
| `Database:RefreshIntervalSeconds` | `300` | 数据库译文快照的刷新间隔，`0` 只在启动时加载 |
| `Database:MaxTranslations` | `100000` | 快照最多加载的译文条数，超出记警告 |

文案有两个来源，宿主装的是框架 `Localization.Database`：

- 各包与框架的语言文件嵌在程序集里，是消息目录。同名目录合并为一份（框架 ADR-0070）：区域与键取并集，同一键同一区域的模板不同即启动失败（`LOCALIZATION_CONFLICT_001`）。
- 平台库表 `message_translation`（键、区域、模板）是运营覆盖的译文：每个回退区域先查数据库再查嵌入目录，所以同键同区域时数据库优先，没有的键照旧走嵌入目录与回退链。快照在宿主进入服务状态后加载、按刷新间隔重载，多实例各自刷新；健康项 `localization.database.translations`（加载失败为 Degraded，保留上一份快照）。维护译文的后台页面还没有。

## `XiHan:Workflow`

宿主恒装框架的数据库工作流（`DatabaseWorkflowFeature`，顶替 Full Profile 的内存实现）：定义、实例、节点实例与书签按分区落表（租户四张随租户分库、平台四张在平台库），另有一张租约表做实例锁与轮询领导权。全部键都有缺省值，写错值启动即失败。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `LockTimeoutSeconds` | `10` | 等实例锁的上限，超过即 `WORKFLOW_TIMEOUT_001` |
| `RetentionDays` | `0` | 已结束实例的保留天数，`0` 不自动清理 |
| `Database:PollIntervalSeconds` | `1` | 到期书签（延时、超时、重试）的轮询周期 |
| `Database:BatchSize` | `100` | 每个周期每个分区最多恢复的书签数 |
| `Database:LockLeaseSeconds` | `60` | 租约时长，每三分之一续约一次 |
| `Database:HolderId` | 机器名:进程 ID:随机 | 本节点在租约表里的标识 |
| `Database:LockConnection` | 空（租约表在平台库） | 租约表所在的命名连接。**SQLite 必须配**：在 `XiHan:Persistence:SqlSugar:Connections:<名>` 声明另一个库文件，再把这里写成 `<名>`，否则业务事务与租约互相等到 `WORKFLOW_TIMEOUT_001`；PostgreSQL 不用配 |

## `XiHan:Auditing`

| 键 | 值 | 说明 |
| --- | --- | --- |
| `Selection` | `None` | 框架审计中间件不自行挑选；由 Auditing 包的中间件按「是否标了审计」显式声明 |
| `IncludeSuccesses` | `true` | 成功的调用也记 |

## `XiHan:Protocols`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Http:RoutePrefix` | `/api` | 操作路由前缀 |
| `Grpc:Package` | `xihan.basicapp` | gRPC 服务名前缀（`/xihan.basicapp.Users/Get`） |
| `Mcp:ServerName` | `XiHan.BasicApp` | MCP 服务名 |
| `CloudEvents:Topics:<事件类型>` | — | 入站事件类型到主题的映射；没写时事件类型本身须是合法主题 |
| `CloudEvents:Inbound` | `Dispatch` | `Dispatch` 在请求内派发给主题的处理器、处理器的结果即响应；`Publish` 发布到消息代理后立即 202 |
| `CloudEvents:Webhooks:<主题>` / `CloudEvents:Outbound` | — / `Binary` | 出站 Webhook 地址与内容模式 |

## `XiHan:Kernel`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `StartTimeoutSeconds` / `StopTimeoutSeconds` | `60` / `30` | 应用启停时限 |
| `FailurePolicy` | `TerminateStartup` | Feature 启动失败即中断 |
| `Resilience` | — | 按操作 ID 的熔断、固定窗口限流与并发上限，可按操作覆盖（框架 ADR-0029） |

## `BasicApp:Database`

平台库的从库（读写分离）与 SQL 诊断日志，配置写错启动即失败。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Replicas` | 空 | 平台库的从库，每项 `ConnectionString` 与可选的 `HitRate`（读权重，正整数；不写取 `XiHan:Persistence:SqlSugar:Replicas:DefaultHitRate`）。没配即单库 |
| `LogSql` | `false` | 记录每条 SQL（日志量大，只在排查时打开） |
| `LogSqlErrors` | `true` | 记录 SQL 错误 |
| `LogSlowSql` | `true` | 记录慢 SQL |
| `SlowSqlThresholdMilliseconds` | `1000` | 慢 SQL 的阈值，只影响记录，不影响语句执行 |

配了从库之后，只读工作单元的读走从库，会写的工作单元走主库。从库只给平台库：独立库租户读写都走它自己的主库。日志只记语句，不记参数值。要注意的见[部署文档](./deployment#读写分离)。

## `BasicApp:ApiDocs`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Enabled` | 没写时 Development 为真、其余为假 | 是否映射 `/openapi.json`、`/openapi/<组>.json` 与 `/scalar` |

## `BasicApp:ForwardedHeaders`

反向代理的转发头：宿主在管道最前面还原 `X-Forwarded-For` / `-Proto` / `-Host`，访问日志、会话与登录地点、登录节流与自动封禁、访问策略的地址与地区条件拿到的是客户端地址。只信本机回环与下面列出的代理，配置写错启动即报错。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `KnownProxies` | 空 | 受信代理的单个地址 |
| `KnownNetworks` | 空 | 受信代理的网段（CIDR，主机位为 0，如 `10.0.0.0/8`） |
| `ForwardLimit` | `1` | 从右往左认几层转发 |

## `BasicApp:Cors`

前端与后端不同源部署时打开。不写来源即不启用跨域，只能同源访问；配置写错启动即报错。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `AllowedOrigins` | 空 | 允许的来源（协议 + 主机 + 端口，不带路径） |
| `AllowAnyOrigin` | `false` | 放开全部来源；不能与 `AllowedOrigins` 或 `AllowCredentials` 同时写 |
| `AllowAnyMethod` / `AllowedMethods` | `true` / 空 | 允许的方法 |
| `AllowAnyHeader` / `AllowedHeaders` | `true` / 空 | 允许的请求头 |
| `AllowCredentials` | `true` | 是否允许携带凭据 |
| `ExposedHeaders` | 空 | 暴露给前端脚本的响应头 |
| `PreflightMaxAgeSeconds` | `0` | 预检结果的缓存秒数，0 为不设置 |

## `BasicApp:CloudEvents`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Enabled` | `false` | 是否映射 CloudEvents 入站端点 `POST /events`（框架 `MapXiHanCloudEvents`），供内部服务投递事件 |

端点只收持有入站权限码 `runtime.cloud-events.deliver`（「投递 CloudEvents 事件」，平台侧）的调用方（同一套 JWT）：入站事件直接进各包的处理器，这个码只授给平台的服务账号或服务角色——在角色管理里给平台侧的服务角色授予它，再把服务账号指派进去；只登录的普通用户投递即 403 `SECURITY_DENIED_004`。端点与 gRPC 一样只开放给内网，不要经反向代理暴露到公网。登记了处理器的主题只有 Realtime 的断开连接与 Chat 订阅的部门归属变更，都是 BasicApp 内部的事件，其余主题在派发模式下都是 404 `PROTOCOLS_NOTFOUND_003`；主题映射、入站方式与出站 Webhook 见上面的 `XiHan:Protocols:CloudEvents`。

## `BasicApp:Schema`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `AutoUpgrade` | `true` | 启动时执行表结构升级，失败即中断启动 |
| `Directory` | 程序目录下的 `Schema` | 脚本目录 |
| `LockExpiryMinutes` / `LockWaitMinutes` | `30` / `10` | 升级租约锁的有效期与等待时长 |

## `BasicApp:Seed`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `EnableDemoData` | `false`（Development 为 `true`） | 是否写演示数据：演示租户 `demo` 及其组织、角色与访问策略，平台演示字典，平台与 `demo` 的草稿通知 |
| `SuperAdminPassword` | 空，`appsettings.Development.json` 写的是 `SuperAdmin@123` | `superadmin` 的初始密码；首次执行 `seed` 时没配即失败（`IDENTITY_VALIDATION_026`） |

## `BasicApp:Modules`

可选模块开关 `BasicApp:Modules:<名>:Enabled`：没写即开启，写了必须是 `true` / `false`。关掉的模块宿主不装配，它的页面与权限码不下发，前端模块随菜单不再出现。

| 名 | 模块 | 关掉之后 |
| --- | --- | --- |
| `Workbench` | 工作台 | 登录后落到第一个有权限的页面，前端把 `VITE_HOME_PATH` 留空 |
| `Printing` | 打印模板 | 其他包登记的打印数据源没有消费方，见[打印模板](./backend/printing#开关) |
| `Chat` | 在线聊天与聊天审计 | 聊天 Hub 与附件上传端点不映射，Organization 的部门归属事件没有订阅方，见[在线聊天](./backend/chat#开关) |
| `Workflow` | 流程定义、流程实例与审批中心 | 引擎与九张表照旧（宿主恒装数据库工作流）；其他包登记的审批业务没有消费方，见[工作流](./backend/workflow#模块开关) |
| `CodeGeneration` | 代码生成（只在平台一侧） | 五张表照样建；已登记的外部数据源连接不再建立，见[代码生成](./backend/code-generation#模块开关) |
| `AI` | AI 提供商、提示词、知识库与助手 | 表照样建；框架的工具目录与 MCP 工具照常，聊天照常、助手入口隐藏，见[AI 能力](./backend/ai#模块开关) |

## `BasicApp:AI:Qdrant`

知识库的向量库。写了 `Host` 宿主才登记 Qdrant 连接器（`CommunityToolkit.VectorData.Qdrant`）并装框架的知识库 Provider，健康检查随之多一项 `ai.vectordata.store`；没写即知识库不可用，见[AI 能力](./backend/ai#知识库)。端点地址、密钥写错启动不报错，连不上时健康项是 Degraded，摄入的文档以失败保存。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Host` | 无 | 主机名；空白即没配 |
| `Port` | `6334` | gRPC 端口，1～65535，写错启动报错（`6333` 是 REST 端口，连接器不用） |
| `Https` | `false` | 写了必须是 `true` / `false`，否则启动报错 |
| `ApiKey` | 无 | API 密钥，不要提交到仓库 |

向量维度、检索条数与切块参数在框架节 `XiHan:AI:Knowledge`（`Dimensions` 缺省 1536，要与嵌入模型一致）。

## `BasicApp:CodeGeneration`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `TablePrefixes` | `sys_` | 导入表时推断类名去掉的表名前缀，逗号分隔、取最长的匹配（5.x 是 `CodeGeneration:TablePrefixes`，缺省 `Sys_,Saas_`） |
| `EnableGenerateToProject` | `false` | 允许「生成到项目」；打开了也只在 Development 生效（宿主的 `appsettings.Development.json` 打开） |
| `AllowSqliteDataSource` | `false`，`appsettings.Development.json` 写的是 `true` | 是否开放 SQLite 数据源。SQLite 的「地址」是服务器上的文件路径，开放等于能读进程够得着的任意数据库文件 |
| `BackendRootPath` | 无 | 写进项目时后端的根目录（含 `src/` 的 `backend-next`），相对宿主的内容根目录；开发配置是 `../..` |
| `FrontendRootPath` | 无 | 写进项目时前端的根目录（含 `package.json` 的 `frontend-next`），相对宿主的内容根目录；开发配置是 `../../../frontend-next` |

## `BasicApp:Auth`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `AccessTokenMinutes` | `120` | 访问令牌有效期 |
| `RefreshTokenDays` | `7` | 刷新令牌（会话）有效期 |
| `MaxFailedAccessAttempts` / `LockoutMinutes` | `5` / `15` | 连续失败锁定 |
| `PasswordMinLength` / `PasswordHistoryDepth` | `8` / `3` | 密码长度与不可复用的历史深度；历史深度 0 不检查，最多 1000（框架一次查询的取出上限），配得更大启动即失败 |
| `SessionCacheSeconds` | `30` | 会话门禁的缓存秒数 |
| `CaptchaEnabled` / `CaptchaSeconds` | `true` / `300` | 图形验证码 |
| `LoginThrottleWindowSeconds` | `60` | 登录节流窗口 |
| `LoginThrottleAccountLimit` / `LoginThrottleIpLimit` | `5` / `30` | 同账号同地址、同地址跨账号的失败上限 |
| `TwoFactorTicketMinutes` / `TwoFactorTicketMaxFailures` | `10` / `5` | 双因素票据 |
| `TotpIssuer` | `XiHan BasicApp` | 认证器里显示的签发方 |
| `SelfRegistrationEnabled` | `false` | 开放自助注册（`auth.register`，注册平台账号） |
| `VerificationCodeMinutes` / `VerificationCodeIntervalSeconds` | `10` / `60` | 邮箱 / 短信验证码有效期与发送间隔 |
| `VerificationCodeDailyPerTarget` / `VerificationCodeDailyPerAddress` | `10` / `50` | 每天每个目标、每个地址的发送上限 |
| `VerificationCodeMaxFailures` / `VerificationCodeLockoutMinutes` | `5` / `10` | 验证码输错锁定 |

登录页开放哪些方式不在这里，而是系统参数 `identity.login-methods`（`password`、`email`、`phone`），见[系统设置](./backend/settings)。

### `BasicApp:Auth:OAuth`（第三方登录）

```json
{
  "BasicApp": {
    "Auth": {
      "OAuth": {
        "Enabled": true,
        "FrontendCallbackUrl": "/#/auth/oauth-callback",
        "CallbackBaseUrl": "https://<后端对外地址>",
        "Providers": [
          { "Name": "github", "ClientId": "<客户端 ID>", "ClientSecret": "<客户端密钥>" },
          { "Name": "wecom", "ClientId": "<corpId>", "ClientSecret": "<secret>", "AgentId": "<agentId>", "Mode": "QrCode" }
        ]
      }
    }
  }
}
```

`Name` / `Provider` 支持 `github`、`gitee`、`google`、`qq`、`wechat`、`wecom`、`lark`（飞书）、`dingtalk`；没填 `ClientId` 或 `Enabled: false` 的提供方不出现在登录页。`Mode` 为 `QrCode`（缺省）或 `Account`，另有 `DisplayName`、`CorpId`、`Scopes`、`LoadMemberProfile`。

## `BasicApp:Access`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `PermissionConditionMode` | `Enforce` | 权限条件：`Enforce` / `LogOnly` / `Disabled` |
| `AccessPolicyEnabled` | `true` | 访问策略总开关 |
| `ConditionTimeZone` | `Asia/Shanghai` | 条件里时段、日期的判定时区 |

## `BasicApp:Auditing`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `QueueCapacity` / `BatchSize` / `FlushIntervalSeconds` | `10000` / `200` / `1` | 日志写入队列 |
| `AccessLog:Enabled` / `AccessLog:Exclude` | `true` / `[]` | 访问日志开关与排除的操作 |
| `DefaultRetentionDays` | `180` | 系统参数 `auditing.retention-days` 没建时的保留天数 |
| `RetentionCron` | `0 3 * * *` | 保留清理作业的计划（UTC） |

## `BasicApp:Files`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `LocalRootPath` | `App_Data/files` | 本地存储根目录（相对内容根） |
| `MaxUploadBytes` | 100 MB | 单文件上传上限 |
| `UploadPartBytes` | 8 MB | 分片大小（不小于存储要求的最小分片） |
| `UploadLifetime` | `1.00:00:00` | 分片上传会话有效期 |
| `LinkLifetime` / `MaxLinkLifetime` | `00:30:00` / `7.00:00:00` | 签名地址缺省与最长有效期 |

## `BasicApp:Notification`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Brand` | 应用名 | 模板变量 `brand` |
| `DispatchInterval` / `RetryDelay` / `MaxAttempts` / `StuckAfter` | `00:00:05` / `00:00:30` / `3` / `00:10:00` | 邮件 / 短信 / 机器人发件箱的轮询、重试与卡死判定 |
| `Telegram:*` | — | Telegram 交互：回复文案（`Texts`）、出站重试（`Retry`）、并发、去重时长、长轮询超时；机器人本身在后台页面配置 |

## `BasicApp:Realtime:SignalR`

SignalR 的 Hub 参数，对全部 Hub 生效。键、语义与缺省照 5.x 的 `XiHan:Web:RealTime:SignalR`，缺省值也就是 ASP.NET Core 的缺省值，不配即可。配置写错启动即失败。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `EnableDetailedErrors` | `false` | 把详细错误发给客户端，生产环境不要打开 |
| `KeepAliveInterval` | `00:00:15` | 服务端发保活消息的间隔 |
| `ClientTimeoutInterval` | `00:00:30` | 多久没收到客户端的消息算断开，一般设成客户端保活间隔的两倍 |
| `HandshakeTimeout` | `00:00:15` | 握手的超时 |
| `MaximumReceiveMessageSize` | `32768` | 单条入站消息的大小上限（字节） |
| `StreamBufferCapacity` | `10` | 流式调用的缓冲容量 |
| `MaximumParallelInvocationsPerClient` | `1` | 每个客户端的最大并行调用数 |

时长写 `hh:mm:ss`（可带天数 `d.hh:mm:ss`），必须大于零；只写 `15` 或 `00:15` 都算写错。数字必须是正整数。前端用的是 SignalR 客户端的缺省超时（服务端 30 秒没消息即断开），把 `KeepAliveInterval` 调到 30 秒以上会让浏览器不断掉线重连。5.x 还有 `EnableConnectionMetrics`，没有任何消费方，没有搬。

## `BasicApp:OpenPlatform`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `TimestampToleranceSeconds` | `300` | 签名请求的时间戳容差 |
| `MaxRequestBodyBytes` | 2 MB | 开放接口请求体上限 |
| `Issuer` | 请求来源 + `/api/oauth2` | OIDC 签发者，不带结尾斜杠；反向代理后面要显式配置 |
| `ConsentPageUrl` | `/#/oauth/authorize` | 前端授权同意页 |
| `IdTokenLifetimeSeconds` | `600` | ID 令牌有效期 |

## `BasicApp:DataProtection:KeysDirectory`

`ISecretProtector`（加密参数、存储密钥、租户连接串、认证器密钥、通道密钥与机器人 Webhook 地址）使用 ASP.NET Core Data Protection。多实例部署时必须配置共享的密钥目录，否则各实例互相解不开密文。

## 前端 `.env`

| 变量 | 缺省 | 说明 |
| --- | --- | --- |
| `VITE_PORT` | `9100` | 开发服务器端口 |
| `VITE_DEV_PROXY_TARGET` | `http://localhost:9710` | 开发代理目标 |
| `VITE_API_BASE_URL` | 空（同源） | 构建期 API 源 |
| `VITE_HOME_PATH` | `/workbench/dashboard` | 首页；关掉工作台模块时留空 |
| `VITE_ROUTER_HISTORY` | `hash` | `history` 需要静态托管回退到 `index.html` |
| `VITE_BASE` | `/` | 部署子路径 |
| `VITE_REALTIME_HUB` | `/hubs/notification` | 留空即不建实时连接 |
| `VITE_APP_TITLE` 等 | 空 | 登录页品牌区 |

本机改动写 `.env.local`，不提交。

## 下一步

- [部署](./deployment) · [数据库配置](./backend/database) · [统一认证](./backend/authentication)
