# 配置参考

本页说明宿主与前端的全部配置项、缺省值与含义，适用于开发、部署与运维时查阅。键与缺省值以 `backend/src/XiHan.BasicApp.Host/appsettings.json` 为准。

## 文件与优先级

宿主是标准 ASP.NET Core 应用，配置按顺序叠加，后者覆盖前者：`appsettings.json` → `appsettings.{环境}.json` → 用户机密（只在 Development）→ 环境变量 → 命令行参数。环境由 `ASPNETCORE_ENVIRONMENT` 决定：本机开发是 `Development`（`launchSettings.json` 已设），部署时缺省是 `Production`。

环境变量用双下划线分节，如 `XiHan__Security__Jwt__Secret`；数组写下标，如 `BasicApp__Cors__AllowedOrigins__0`。

配置分两组：

- 框架节 `XiHan:*`：由 XiHan.Framework 的各个 Feature 读取。写了就校验，类型、范围或枚举值不合法时启动即失败。列表与字典（如 `Exclude`、`Routes`、`AllowedHosts`）没有内容时整项不写：空的 `[]` / `{}` 会被读成空字符串而启动失败，JSON Schema 会把它们标为错误。
- 应用节 `BasicApp:*`：由各能力包的 Options 直接绑定。除特别注明的几节外，写错同样启动即失败，不会退回缺省值。

`appsettings.json` 写出了全部配置项：写出来的值就是缺省值（个别是 BasicApp 的推荐值，下文写明），注释掉的是可选项，去掉 `//`、填上真实值即生效。文件里不逐项注释，每一项的说明在下文与 [JSON Schema](#json-schema)（编辑器里悬停可见）。

| 文件 | 入库 | 用途 |
| --- | --- | --- |
| `appsettings.json` | 是 | 全部配置项与缺省值，所有环境共用 |
| `appsettings.Development.json` | 是 | 本机开发：开发库连接、开发用密钥，启动时建库、建表并执行种子 |
| `appsettings.Production.json` | 否（`.gitignore`） | 正式环境的真实值：数据库、密钥、对外地址等；也可以全部改用环境变量 |
| `ConfigurationSchema.json` | 是 | `XiHan` 与 `BasicApp` 两节的 JSON Schema，见 [JSON Schema](#json-schema) |

::: danger 不提交密钥
`appsettings.json` 里的 JWT 密钥、签名地址密钥都留空，也不写数据库连接；`appsettings.Development.json` 里的值只用于本机开发。部署时写进不入库的 `appsettings.Production.json`，或由环境变量、密钥存储提供，下文示例一律用占位符。
:::

## 配置节点总览

```text
appsettings.json
├─ Kestrel                  监听地址：HTTP 9710、gRPC 9711
├─ Logging                  日志级别
├─ AllowedHosts             允许的 Host 请求头
├─ XiHan                    框架配置
│  ├─ Kernel                特性的启停时限与失败策略；Resilience 限流、并发与熔断
│  ├─ Bindings              服务绑定：平台库 db、Redis cache、RabbitMQ queue
│  ├─ Persistence           SqlSugar（建表、超时、从库探测）与运行时数据源
│  ├─ Caching               进程内缓存、幂等去重；Redis 与两级缓存
│  ├─ Messaging             消息投递与事务发件箱；RabbitMQ
│  ├─ ObjectStorage         存储健康探测与签名下载地址
│  ├─ Security              JWT、授予缓存、IP 归属地、通行密钥、出站地址策略
│  ├─ MultiTenancy          租户解析与租户目录缓存
│  ├─ Localization          启用的区域与数据库译文
│  ├─ Auditing              框架的操作审计
│  ├─ Scheduling            后台作业调度
│  ├─ Workflow              工作流引擎
│  ├─ AI                    知识库、护栏、工具名与提示词模板
│  ├─ Observability         主机运行信息采样与 OpenTelemetry 导出
│  └─ Protocols             HTTP、gRPC、MCP 与 CloudEvents 协议适配
└─ BasicApp                 应用配置
   ├─ Database              平台库的从库与 SQL 诊断日志
   ├─ Schema                表结构升级
   ├─ Seed                  种子与演示数据
   ├─ Modules               可选模块开关
   ├─ ApiDocs               接口文档
   ├─ ForwardedHeaders      反向代理转发头
   ├─ Cors                  跨域
   ├─ CloudEvents           CloudEvents 入站端点
   ├─ DataProtection        数据保护的密钥目录
   ├─ Auth                  令牌、锁定、密码、验证码、双因素与自助注册；OAuth 第三方登录
   ├─ Access                权限条件与访问策略
   ├─ Auditing              应用日志与保留期
   ├─ Files                 上传、分片与签名下载
   ├─ Notification          通知发件箱与 Telegram
   ├─ Realtime              SignalR Hub 参数
   ├─ OpenPlatform          开放接口签名与 OAuth2 / OIDC 授权服务
   ├─ CodeGeneration        代码生成
   └─ AI                    知识库的向量库（Qdrant）
```

各节点的键、缺省值与说明见下文对应小节。

## JSON Schema

`backend/src/XiHan.BasicApp.Host/ConfigurationSchema.json` 描述 `XiHan` 与 `BasicApp` 两节的每个键：类型、枚举值、取值范围、缺省值与说明（框架的键取自 XiHan.Framework 的配置索引）；框架节的列表与字典要求非空，写成空的 `[]` / `{}` 即报错。`Kestrel`、`Logging` 等 ASP.NET Core 自带的键由编辑器内置的 appsettings Schema 描述。

- **Visual Studio**：宿主工程把它登记为 `JsonSchemaSegment`，打开任一 `appsettings*.json` 即有补全、悬停说明与类型校验，不用另外配置。
- **VS Code**：在 `.vscode/settings.json` 里映射（可以和 ASP.NET Core 的 Schema 一起用）：

```json
{
  "json.schemas": [
    {
      "fileMatch": ["backend/src/XiHan.BasicApp.Host/appsettings*.json"],
      "url": "./backend/src/XiHan.BasicApp.Host/ConfigurationSchema.json"
    },
    {
      "fileMatch": ["backend/src/XiHan.BasicApp.Host/appsettings*.json"],
      "url": "https://json.schemastore.org/appsettings.json"
    }
  ]
}
```

- **Rider**：在「JSON Schema Mappings」里把同样的文件指向 `ConfigurationSchema.json`。

加配置项时同步补 Schema：架构测试 `ConfigurationSchemaTests` 核对 `appsettings.json` 与 `appsettings.Development.json` 里 `XiHan`、`BasicApp` 下写出的每个键都在 Schema 里，漏了即失败。

## ASP.NET Core

### `Kestrel`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Kestrel:Endpoints:Http:Url` | `http://localhost:9710` | 操作接口、OpenAPI、MCP、SignalR Hub 与文件端点。容器或对外部署改成 `http://0.0.0.0:9710`，HTTPS 由反向代理终止 |
| `Kestrel:Endpoints:Grpc:Url` | `http://127.0.0.1:9711` | gRPC（`Protocols: Http2`），只供内部服务调用，只监听内网地址 |

### `Logging` 与 `AllowedHosts`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Logging:LogLevel:Default` | `Information` | 键是日志类别前缀（取最长匹配） |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Warning` | ASP.NET Core 的请求日志只记警告以上 |
| `Logging:LogLevel:XiHan` | 不写（Development 为 `Debug`） | 框架日志 |
| `AllowedHosts` | `*` | 允许的 `Host` 请求头，分号分隔 |

日志同时经 OpenTelemetry 导出，见 [`XiHan:Observability`](#xihan-observability)。

## `XiHan:Kernel`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `StartTimeoutSeconds` | `60` | 单个特性的启动超时；框架缺省 30，BasicApp 取 60，因为表结构升级与开发环境建表都在启动阶段 |
| `StopTimeoutSeconds` | `30` | 单个特性的停止超时 |
| `FailurePolicy` | `TerminateStartup` | 特性启动失败时终止启动；`IsolateFeature` 隔离该特性继续启动 |
| `UseBuiltInMiddleware` | `true` | 内核内置的观测、截止时间、韧性与输入校验中间件；不要关 |

### `XiHan:Kernel:Resilience`

按操作 ID 的固定窗口限流、并发上限与熔断。根上的键是全部操作的缺省策略，`Operations:<操作 ID>` 按操作覆盖（子键相同，不写的沿用缺省策略）。缺省不限流、不限并发、不熔断。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `PermitLimit` | 不写（不限流） | 固定窗口内允许的调用数 |
| `WindowSeconds` | `1` | 限流窗口 |
| `QueueLimit` | `0` | 窗口用尽时排队的调用数，0 为直接拒绝 |
| `ConcurrencyLimit` | 不写（不限） | 同时执行的调用数上限 |
| `ConcurrencyQueueLimit` | `0` | 并发用尽时排队的调用数 |
| `FailureThreshold` | 不写（不熔断） | 连续失败多少次后熔断 |
| `BreakSeconds` | `30` | 熔断后拒绝调用的时长，到期放一次试探 |

```json
{
  "XiHan": {
    "Kernel": {
      "Resilience": {
        "Operations": { "auth.login": { "PermitLimit": 20, "WindowSeconds": 1 } }
      }
    }
  }
}
```

操作 ID 用 `dotnet run --project backend/src/XiHan.BasicApp.Host -- operations` 列出。

## `XiHan:Bindings:db`

平台库连接，启动硬要求：缺失即 `RUNTIME_VALIDATION_001` 中断启动，不退回内存实现；`provider` 不写即 `postgresql`。

框架先读 `SERVICE_BINDING_ROOT` 目录（Kubernetes Service Binding），再读配置节；配置节里的同名绑定会整个盖掉目录里挂进来的（值为空也一样），所以 `appsettings.json` 不写任何绑定。本机开发写在 `appsettings.Development.json`，部署用环境变量 `XiHan__Bindings__db__value` 或 Service Binding（`db` 目录下放 `type`、`uri`，可选 `provider`）。绑定名与属性名一律小写。

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
| 绑定 `XiHan:Bindings:queue`（类型 `queue`）或 `XiHan:Messaging:RabbitMQ:Configuration` | `Messaging.RabbitMQ`：发件箱派发、CloudEvents 发布模式与广播订阅经它，各实例按主题竞争消费 | `messaging.rabbitmq.connection` |

```json
{
  "XiHan": {
    "Bindings": {
      "cache": { "type": "cache:distributed", "value": "<主机>:6379", "password": "<密码>" },
      "queue": { "type": "queue", "value": "amqp://<主机>:5672", "username": "<用户>", "password": "<密码>" }
    }
  }
}
```

部署时用环境变量 `XiHan__Bindings__cache__value`、`XiHan__Bindings__queue__value` 或 Service Binding（`SERVICE_BINDING_ROOT` 下的 `cache` / `queue` 目录）提供。绑定与 `Configuration` 键都配了时绑定优先，绑定的值为空才读 `Configuration`。口令可以写进连接串，也可以放在绑定的 `username` / `password` 属性里（Service Binding 目录里的同名文件），各自覆盖连接串里的用户名与密码。

### `XiHan:Caching`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `InMemory:MaxEntries` | `10000` | 进程内缓存每张表（字节存储、对象缓存各一张）的条目上限；没配 Redis 时生效 |
| `InMemory:DefaultTimeToLiveSeconds` | `300` | 进程内缓存未指定过期时的存活时间 |
| `Idempotency:TimeToLiveSeconds` | `86400` | 幂等去重：已完成结果的保留时间 |
| `Idempotency:InProgressTimeToLiveSeconds` | `60` | 处理中标记的保留时间，即处理器崩溃时的最长封锁 |
| `Redis:Configuration` | 不写 | StackExchange.Redis 连接串；绑定 `cache` 优先 |
| `Redis:KeyPrefix` | `xihan:` | 键前缀；同一个 Redis 给几套环境用时各取一个 |
| `Redis:PublishChanges` | `true` | 写入与删除后向频道 `<前缀>__changes` 发通知，各实例据此失效 L1 |
| `Redis:DefaultTimeToLiveSeconds` | `300` | 未指定过期时的存活时间 |
| `Redis:HealthProbeIntervalSeconds` | `15` | 健康项的重探间隔与单次探测超时 |
| `Hybrid:LocalCapacity` / `Hybrid:LocalTimeToLiveSeconds` | `10000` / `30` | 本机 L1 的条目上限与存活上限；收不到变更通知时，别的实例的写入最迟这么久后可见 |

`Redis:*` 与 `Hybrid:*` 只在配了 Redis 时生效。

### `XiHan:Messaging`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `MaxAttempts` | `3` | 消息代理把一条消息投递给处理器的最多次数（含首次），超过进入死信 |
| `DeadLetterSuffix` | `.dlq` | 死信主题后缀 |
| `InstanceId` | 每次启动随机 | 本实例在消息代理上的标识：1–64 个字母、数字或 `._-`，字母或数字开头。配了就必须每个实例各不相同（多副本可设成 Pod 名）；不配即可 |
| `InMemory:MaxQueuedMessages` | `10000` | 进程内消息每个主题（含死信）的条目上限；没配 RabbitMQ 时生效 |
| `Outbox:PollIntervalSeconds` | `1` | 发件箱轮询间隔 |
| `Outbox:BatchSize` | `100` | 每轮最多投递的条数（1–1000），也是每轮最多清理的已投递行数 |
| `Outbox:MaxAttempts` | `10` | 发件箱投递到消息代理的最多次数（含首次），用尽进死信；与上面的 `MaxAttempts` 是两层 |
| `Outbox:RetryDelaySeconds` / `Outbox:MaxRetryDelaySeconds` | `5` / `300` | 首次失败后的重试间隔（之后每次翻倍）与上限 |
| `Outbox:DispatchedRetentionSeconds` | `86400` | 投递成功的行保留多久，0 为成功即删除；死信行不自动删 |
| `Outbox:IncludeShards` | `false`（框架缺省 `true`） | 是否也逐个轮询租户独立库里的发件箱；SqlSugar 把发件箱行留在平台库，BasicApp 关掉以省去每个独立库一轮的多余查询 |
| `Outbox:ShardRefreshIntervalSeconds` | `60` | 独立库清单多久从租户目录重读一次，`IncludeShards` 打开时才用到 |
| `RabbitMQ:Configuration` | 不写 | AMQP URI（`amqp://` 或 `amqps://`）；绑定 `queue` 优先 |
| `RabbitMQ:Exchange` | `xihan` | 发布用的持久 direct 交换机；不能为空、不能以 `amq.` 开头，已有同名但不是持久 direct 的交换机即启动失败 |
| `RabbitMQ:QueuePrefix` | `xihan.` | 订阅方声明持久队列 `<前缀><主题>`；没人订阅的主题发布即丢弃，不堆积 |
| `RabbitMQ:PrefetchCount` | `16` | 每个消费者的预取数（1–65535） |
| `RabbitMQ:HealthProbeIntervalSeconds` | `15` | 健康项的重探间隔与单次探测超时 |

目前的消息处理器：Realtime 的断开连接（广播订阅，RabbitMQ 上按 `InstanceId` 给每个实例建自己的队列）、Chat 订阅 Organization 的部门归属变更（领域事件，见[实体基类](./backend/entity#领域事件)），以及 Workflow 订阅引擎的任务创建、转办与实例故障（发待办与故障通知）。邮件、短信与机器人消息的发送记录是应用表，不经消息代理。

## `XiHan:Persistence:SqlSugar`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `InitTables` | `false`（Development 为 `true`） | 启动时按实体建缺表、加缺列；只用于开发，正式环境由 `Schema/` 脚本管理 |
| `CommandTimeoutSeconds` | `300` | 语句超时，正整数 |
| `HealthProbeIntervalSeconds` | `15` | 健康项 `persistence.sqlsugar.database` 的重探间隔与单次探测超时 |
| `Connections:<名>` | 不写 | 命名连接，值是连接串；名字合 `^[a-z0-9][a-z0-9-]{0,63}$`，不能是 `default` 或 `shard`。目前只有 SQLite 上的工作流租约表需要 |
| `Replicas:DefaultHitRate` | `10` | 没写权重的从库的读权重 |
| `Replicas:HealthCheck` | `true` | 周期探测从库：连不上的自动摘除、恢复后加回；关掉后宕机的从库照常接读流量 |
| `Replicas:HealthCheckIntervalSeconds` | `30` | 探测间隔 |
| `Replicas:FailureCooldownSeconds` | `120` | 从库最后一次失败后，过了这么久并且探通才恢复读流量 |

从库本身在 [`BasicApp:Database`](#basicapp-database) 配，健康快照看 `persistence.sqlsugar.replicas`。

### `XiHan:Persistence:DataSources`

运行时数据源，即代码生成登记的外部数据库。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `MaxCount` | `64` | 登记数上限（1–1024）；满了再登记新名字即失败，不淘汰已有的 |
| `HealthProbeIntervalSeconds` | `15` | 健康项 `persistence.datasource.<名>` 的重探间隔，也是连接测试的单次上限 |

## `XiHan:ObjectStorage`

BasicApp 的存储（本地目录、S3 兼容）在后台「存储配置」页面维护，本地根目录见 [`BasicApp:Files`](#basicapp-files)。框架的 `XiHan:ObjectStorage:S3` 节不读取，不用写。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `StoreHealthProbeIntervalSeconds` | `15` | 各存储实例健康项 `objectstorage.store.<名>` 的探测间隔 |
| `SignedUrls:Secret` | 空 | 本地存储签名下载地址（宿主 `/blobs` 端点）的 HMAC-SHA256 密钥，UTF-8 至少 32 字节，多实例必须一致；缺失或太短启动即失败。S3 兼容存储由存储自己签名，不用它 |
| `SignedUrls:BaseUrl` | `/blobs` | 地址前缀：以 `/` 开头的路径，或不带查询串的绝对 http(s) 地址 |
| `FileSystem:RootPath` | 程序目录下的 `blobs` | 框架缺省的文件系统存储；业务不用它，只是启动时会建出这个目录 |
| `FileSystem:MaxTotalBytes` / `MaxBlobBytes` / `MultipartExpiryHours` / `MaxMultipartUploads` | 1 GiB / 256 MiB / `168` / `1000` | 同上，总字节（暂存的分片也计入）、单个对象字节、未完成分片的保留时长（过期即拒绝）与未完成分片上传数的上限 |
| `Stores:<名>` | 不写 | 命名存储实例（`filesystem` 或 `s3`）；业务不按名取用，不要用 `local` 与 `storage-*` 这两类名字 |

## `XiHan:Security`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `RequireAuthentication` | `false` | 为 `true` 时匿名调用一律拒绝；BasicApp 由各操作的权限与策略决定，保持 `false` |
| `RoleClaim` | `role` | 令牌里承载角色的声明 |
| `Jwt:Secret` | 空 | HS256 对称密钥，UTF-8 至少 32 字节，缺失或太短启动即失败；Identity 签发与框架校验共用，Hub 的认证也用它；多实例必须一致 |
| `Jwt:Issuer` / `Jwt:Audience` | `xihan.basicapp` | 写了就按它校验，整项不写才不校验；写成空串不等于不写 |
| `Jwt:ClockSkewSeconds` | `60` | 校验有效期时允许的时钟偏移 |
| `Jwt:NameClaim` | `sub` | 作为主体 ID 的声明 |
| `Database:CacheSize` | `10000` | 授予缓存条目上限（每个主体与租户组合一条），满时整体清空 |
| `Database:CacheTimeToLiveSeconds` | `30` | 授予缓存的存活时间，0 为不过期 |
| `Ip2Region:V4Database` / `V6Database` | `IpDatabases/ip2region_v4.xdb` / `IpDatabases/ip2region_v6.xdb` | IP 归属地离线库（随发布目录分发），相对路径按程序目录；文件缺失不影响启动，首次查询记一条警告，之后该类地址没有归属地；写成空白启动即失败 |

## `XiHan:Security:Outbound`

出站地址策略：向用户填的地址发请求时能连哪些 IP。缺省只能连公网地址，回环、私网、链路本地和云元数据地址都拒绝。检查放在建立连接时，按解析出的实际 IP 判，重定向也一样，DNS 重绑定绕不过去。

受这道策略约束的：工作流 `Http` 活动、群机器人 Webhook、SMTP 服务器、AI 提供商地址、第三方登录、S3 兼容存储、代码生成的外部数据源、CloudEvents 出站 Webhook、Telegram（配了代理时不受约束，代理地址只有平台管理员能配）。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `AllowLoopback` | `false`（Development 为 `true`） | 放行回环地址（`127.0.0.0/8`、`::1`）；生产环境不要打开 |
| `AllowPrivateNetwork` | `false` | 放行私网地址（`10/8`、`172.16/12`、`192.168/16`、`fc00::/7`），不含链路本地与云元数据地址 |
| `AllowedNetworks` | 不写 | 放行的网段（CIDR，主机位为 0），优先于全部拒绝规则 |
| `AllowedHosts` | 不写 | 放行的主机名（整名相等、不区分大小写、不带协议和端口） |

自建 MinIO、内网 Webhook、内网 SMTP 中继、内网业务库、流程调内网服务、本机模型端点都要在这里放行，否则连接被拒。

## `XiHan:Security:WebAuthn`

通行密钥（见[统一认证](./backend/authentication#通行密钥-auth-begin-passkey-login-→-auth-login-by-passkey)）。`RelyingPartyId` 不是空白才装框架的 WebAuthn，之后还要在系统参数 `identity.login-methods` 列出 `passkey`。通行密钥绑定在依赖方 ID 上，换了域名已登记的全部失效。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `RelyingPartyId` | 空（Development 为 `localhost`） | 依赖方 ID：前端页面所在的可注册域名，不带协议、端口与路径；留空即不启用。IP 地址用不了通行密钥 |
| `Origins` | 不写（Development 为 9100 与 9130 的 `http://localhost`） | 允许的来源 `scheme://host[:port]`，即前端页面的地址，启用时至少一个；主机须等于依赖方 ID 或是它的子域，https 之外只允许 localhost 的 http；写错启动即失败 |
| `RelyingPartyName` | `XiHan BasicApp` | 认证器上显示的名称；不写即同依赖方 ID |
| `UserVerification` | `Required` | 用户验证（指纹、面容或设备密码）：`Required`、`Preferred` 或 `Discouraged`；做了用户验证的通行密钥登录顶替双因素 |
| `ResidentKey` | `Required` | 可发现凭据：`Required` 时登录页不用填账号 |
| `TimeoutSeconds` | `300` | 一次登记或登录的超时，票据随它过期 |
| `ChallengeSize` | `32` | 挑战的字节数（16 至 64） |

## `XiHan:MultiTenancy`

租户只从令牌声明解析，不支持查询串与 `X-Tenant-Id` 头。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `RequireTenant` | `false` | 调用必须带租户；平台主体没有租户声明，必须为 `false` |
| `ValidateAgainstStore` | `true` | 解析出的租户必须在租户目录里存在且启用；关掉后只校验格式 |
| `TenantClaim` | `tenant` | 令牌里承载租户 ID 的声明 |
| `ShardCacheSize` | `10000` | 租户分库解析的缓存容量（租户数），满时整体清空 |
| `ShardCacheTimeToLiveSeconds` | `30` | 租户分库解析缓存条目的存活时间，别的实例改了独立库连接最迟这么久后本实例换用新库；0 为不过期 |
| `DefaultTenant` | 不写 | BasicApp 不用：配了会把平台调用落到这个租户上 |
| `Database:CacheSize` / `Database:CacheTimeToLiveSeconds` | `10000` / `30` | 租户目录缓存的容量与存活时间 |
| `Database:MaxListedTenants` | `10000` | 列出租户时返回的上限，超出不返回并记警告 |

## `XiHan:Localization`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Locales` | `en-US,zh-CN,zh-TW,ja-JP,ko-KR,de-DE,es-ES,fr-FR,pt-BR,ru-RU` | 启用的区域，逗号分隔、按优先级排列；是一个字符串，写成数组会被当成没写 |
| `DefaultLocale` | `en-US` | 协商不到时用的区域，必须在 `Locales` 里 |
| `Database:RefreshIntervalSeconds` | `300` | 数据库译文快照的刷新间隔，0 只在启动时加载 |
| `Database:MaxTranslations` | `100000` | 快照最多加载的译文条数，超出记警告 |

请求按 `Accept-Language` 在启用的区域里协商。文案有两个来源：

- 各包与框架嵌在程序集里的语言文件，即消息目录。同名目录合并为一份：区域与键取并集，同一键同一区域的模板不同即启动失败（`LOCALIZATION_CONFLICT_001`）。
- 平台库表 `message_translation`（键、区域、模板）是运营覆盖的译文：同键同区域时数据库优先，没有的键照旧走嵌入目录与回退链。快照在宿主进入服务状态后加载，按刷新间隔重载，多实例各自刷新；健康项 `localization.database.translations`，加载失败为 Degraded 并保留上一份快照。这张表没有后台维护页面。

## `XiHan:Auditing`

框架的操作审计，与 [`BasicApp:Auditing`](#basicapp-auditing) 的应用日志分开。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Selection` | `None` | 没有显式动作时记哪些操作（`Protected`、`All`、`None`）；BasicApp 由 Auditing 包按「是否标了审计」显式声明，保持 `None` |
| `IncludeSuccesses` | `true` | 成功的调用也记；`false` 只记失败 |
| `Exclude` | 不写 | 按操作排除：操作 ID，或以 `*` 结尾的前缀 |
| `Database:QueueCapacity` | `10000` | 等待落库的事件数上限，满了丢弃新事件并计数 |
| `Database:BatchSize` / `Database:FlushIntervalSeconds` | `100` / `1` | 每批落库的事件数与刷写间隔 |
| `Database:LogEvents` | `true` | 每条事件同时写进日志（Information 级） |

## `XiHan:Scheduling`

后台作业调度的数据库实现，多节点选主。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `PollIntervalSeconds` | `1` | 引擎轮询周期 |
| `MaxConcurrentRuns` | `32` | 本节点同时运行的作业上限 |
| `AcquireBatchSize` | `16` | 一个轮询周期最多抢占的触发器数 |
| `MisfireThresholdSeconds` | `60` | 到期时刻落后超过它才算 misfire |
| `LeaseResource` / `LeaseTimeToLiveSeconds` | `scheduling.jobs` / `30` | 领导租约的资源名与存活时间 |
| `NodeTimeoutSeconds` | `90` | 节点心跳超过它即视为失联 |
| `HousekeepingIntervalSeconds` | `60` | 领导者维护（失联恢复、历史清理）的周期 |
| `HistoryRetentionDays` | `7` | 运行历史保留天数（可带小数），0 为不删 |
| `NodeId` | 机器名:进程 ID:随机串 | 本节点 ID |
| `Database:HealthProbeIntervalSeconds` | `15` | 健康项 `scheduling.database.leader` 的重探间隔 |

## `XiHan:Workflow`

宿主恒装框架的数据库工作流（Workflow 模块关掉也装）：定义、实例、节点实例与书签按分区落表（租户四张随租户分库、平台四张在平台库），另有一张租约表做实例锁与轮询领导权。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `MaxNodeExecutionsPerBurst` | `1000` | 一个波次最多执行的节点数，超过即实例故障（防死循环） |
| `MaxSubWorkflowDepth` | `16` | 子流程最大深度 |
| `LockTimeoutSeconds` | `10` | 等实例锁的上限，超过即 `WORKFLOW_TIMEOUT_001` |
| `NotResumableBackoffSeconds` | `300` | 到期书签因实例不在运行中而不能恢复时，到期时间后推的秒数 |
| `ExpressionCacheSize` | `10000` | 表达式缓存上限 |
| `RetentionDays` | `0` | 已结束实例的保留天数（可带小数），0 为不自动清理 |
| `HousekeepingIntervalSeconds` | `3600` | 按保留期清理的周期 |
| `HttpTimeoutSeconds` | `30` | `Http` 活动的缺省超时 |
| `MaxHttpTimeoutSeconds` | `300` | 流程定义里 `Http` 活动 `TimeoutSeconds` 的上限 |
| `MaxForEachItems` | `1000` | `ForEach` 一次最多处理的项数（每项一个子实例），超过即节点故障 |
| `MaxVariablesBytes` | `8388608`（8 MiB） | 一个实例的变量序列化后的字节数上限 |
| `Database:PollIntervalSeconds` | `1` | 到期书签（延时、超时、重试）的轮询周期 |
| `Database:BatchSize` | `100` | 每次轮询最多恢复的书签数 |
| `Database:LockLeaseSeconds` | `60` | 实例锁与轮询领导权的租约时长，每三分之一续约一次 |
| `Database:HealthProbeIntervalSeconds` | `15` | 健康项 `workflow.database.poller` 的重探间隔 |
| `Database:HolderId` | 机器名:进程 ID:随机串 | 本节点写进租约行的标识 |
| `Database:LockConnection` | 不写（租约表在平台库） | 租约表所在的命名连接。**SQLite 必须配**：在 `XiHan:Persistence:SqlSugar:Connections:<名>` 声明另一个库文件，再把这里写成 `<名>`，否则业务事务与租约互相等到 `WORKFLOW_TIMEOUT_001`；PostgreSQL 不用配 |

## `XiHan:AI`

框架的工具目录常装，这一节总会被读取校验：写错时关了 AI 模块也启动失败。模型提供商在后台「AI 提供商」页面维护，框架的 `Providers`、`DefaultProvider`、`DefaultEmbeddingProvider` 在 BasicApp 里不生效，不要写。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Knowledge:EmbeddingProvider` | 不写（标为缺省的提供商） | 嵌入用的提供商（后台「AI 提供商」里的名称） |
| `Knowledge:PromptName` | 不写（内置模板） | 检索增强的提示词模板名，只能引用 `Prompts` 里的模板；模板只能用 `{context}`、`{question}`，找不到或用了别的占位符启动即失败 |
| `Knowledge:Dimensions` | `1536` | 嵌入维度，要与嵌入模型一致 |
| `Knowledge:ChunkSize` / `Knowledge:ChunkOverlap` | `1000` / `100` | 切块大小与相邻块重叠（字符） |
| `Knowledge:Top` | `5` | 缺省检索条数 |
| `Knowledge:Collection` | `knowledge` | 缺省集合名，合 `^[a-z0-9][a-z0-9._-]{0,63}$` |
| `Guardrails:Enabled` | `false` | 是否登记内置关键词护栏 |
| `Guardrails:UseBuiltInInjectionHeuristics` | `true` | 启用内置的中英文提示注入模式 |
| `Guardrails:BlockedKeywords` / `Guardrails:InjectionPatterns` | 不写 | 禁止的关键词（不区分大小写的子串）与提示注入正则 |
| `Tools:<操作 ID>` | 不写 | 操作暴露成 AI 工具时的名字覆盖（字母、数字、`_`、`-`，1–64 个字符） |
| `Prompts:<名>` | 不写 | 配置里的提示词模板（给 `Knowledge:PromptName` 用），值是模板文本或 `{ "Template": …, "Description": … }`；后台「提示词」页面的模板不在这里 |
| `VectorData:HealthProbeIntervalSeconds` | `15` | 健康项 `ai.vectordata.store` 的重探间隔；配了 [`BasicApp:AI:Qdrant`](#basicapp-ai-qdrant) 才有 |

## `XiHan:Observability`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Host:SampleIntervalSeconds` | `5` | 主机运行信息（CPU、内存、网卡）的采样间隔，最大 3600 |
| `OpenTelemetry:ServiceName` / `ServiceVersion` | 入口程序集的名称与版本 | 资源属性 `service.name` / `service.version` |
| `OpenTelemetry:Tracing` / `Metrics` / `Logging` | `true` | 是否导出追踪、指标与日志 |
| `OpenTelemetry:Console` | `false` | 同时写到控制台导出器（排查用） |
| `OpenTelemetry:SamplingRatio` | `1` | 根 Span 的采样比例（0–1），有上游决定的沿用上游 |
| `OpenTelemetry:Sources` / `Meters` | 不写 | 额外订阅的 Activity 源与 Meter，逗号分隔的字符串 |
| `OpenTelemetry:Otlp:Endpoint` | 不写 | OTLP 端点；配了端点（或标准环境变量 `OTEL_EXPORTER_OTLP_ENDPOINT`）即启用导出 |
| `OpenTelemetry:Otlp:Protocol` | `Grpc` | `Grpc` 或 `HttpProtobuf`，须与 `Endpoint` 同时配置 |

## `XiHan:Protocols`

同一个操作按 `Expose` 发布到 HTTP、gRPC、MCP 与 CloudEvents。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Http:RoutePrefix` | `/api` | 操作路由前缀；前端与开放接口都按它调用，不要改 |
| `Http:Routes:<操作 ID>` | 不写 | 覆盖推导出的路由（`METHOD /模板`）；前端按约定路由调用，改了要同步 |
| `Http:MaxRequestBodyBytes` | `4194304`（4 MiB） | 操作请求体的上限，超过为 `PROTOCOLS_VALIDATION_011`；只管操作端点，文件上传等普通端点照 Kestrel 的限制。开放接口同样受它限制，`BasicApp:OpenPlatform:MaxRequestBodyBytes` 调到 4 MiB 以上时这里要一起调 |
| `Grpc:Package` | `xihan.basicapp` | protobuf 包名，也是服务路径前缀（`/xihan.basicapp.Users/Get`） |
| `Grpc:Methods:<操作 ID>` | 不写 | 覆盖 gRPC 方法名（`Service/Method`） |
| `Mcp:ServerName` | `XiHan.BasicApp` | MCP `initialize` 答复里的服务端名 |
| `Mcp:ServerVersion` / `Mcp:Instructions` | 不写 | 服务端版本（缺省框架版本）与给客户端模型的使用说明 |
| `Mcp:Tools:<操作 ID>` | 不写 | 覆盖 MCP 工具名 |
| `Mcp:AllowedOrigins:<序号>` | 不写 | HTTP 入口允许的浏览器源（`scheme://host[:port]`）；请求带的 `Origin` 不在其中即 403 `PROTOCOLS_DENIED_001`，不带 `Origin` 的客户端不受影响，浏览器里的 MCP 客户端要把它的源配进来 |
| `Mcp:MaxRequestBodyBytes` | `4194304`（4 MiB） | HTTP 入口的请求体上限，超过即 413；另外只收 `application/json`，否则 415 |
| `CloudEvents:Inbound` | `Dispatch` | `Dispatch` 在请求内派发给主题的处理器、处理器的结果即响应；`Publish` 发布到消息代理后立即 202 |
| `CloudEvents:Outbound` | `Binary` | 出站 Webhook 的内容模式：`Binary` 或 `Structured` |
| `CloudEvents:PropagateTraceContext` | `false` | 出站 Webhook 是否在 HTTP 头里写追踪上下文 |
| `CloudEvents:Topics:<事件类型>` | 不写 | 入站事件类型到主题的映射；`AcceptUnmappedTypes` 为 `false` 时没映射的类型一律 400 `PROTOCOLS_VALIDATION_009` |
| `CloudEvents:AcceptUnmappedTypes` | `false` | 入站是否接受没映射的事件类型（以类型本身为主题）；缺省只收映射过的，发送方不能投进任意有订阅的主题 |
| `CloudEvents:MaxRequestBodyBytes` / `CloudEvents:MaxBatchSize` | `1048576`（1 MiB）/ `100` | 入站请求体的字节上限（`PROTOCOLS_VALIDATION_013`）与 batch 的事件数上限（`PROTOCOLS_VALIDATION_014`） |
| `CloudEvents:Webhooks:<主题>` | 不写 | 主题对应的出站 Webhook 地址 |

入站端点是否映射由 [`BasicApp:CloudEvents`](#basicapp-cloudevents) 决定。

## `BasicApp:Database`

平台库的从库（读写分离）与 SQL 诊断日志。主库连接在 [`XiHan:Bindings:db`](#xihan-bindings-db)。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Replicas` | 空（单库） | 从库列表，每项 `ConnectionString` 必填，`HitRate` 是读权重（正整数，不写取 `XiHan:Persistence:SqlSugar:Replicas:DefaultHitRate`） |
| `LogSql` | `false` | 记录每条 SQL（只记语句，不记参数值）；日志量大，只在排查时打开 |
| `LogSqlErrors` | `true` | 记录执行出错的 SQL |
| `LogSlowSql` | `true` | 记录慢 SQL |
| `SlowSqlThresholdMilliseconds` | `1000` | 慢 SQL 的阈值，正整数；只影响记录，不影响语句执行 |

配了从库之后，只读工作单元的读走从库，会写的工作单元走主库。从库只给平台库，独立库租户读写都走它自己的主库。要注意的见[部署](./deployment#读写分离)。

## `BasicApp:Schema`

表结构升级：按 `Schema/<版本>/*.sql` 与版本台账升级平台库与各独立库租户。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `AutoUpgrade` | `true` | 启动时自动升级，失败即中断启动；关掉时由运维先执行 `XiHan.BasicApp.Host schema upgrade` 再启动 |
| `CreateDatabase` | `false` | 升级前平台库不存在时先建库（经维护库 `postgres`，连接账号要有建库权限）；缺省关，库名写错时不悄悄建一个空库，开发环境打开 |
| `Directory` | 程序目录下的 `Schema` | 脚本目录；相对路径按程序目录（不是当前目录）解析 |
| `LockExpiryMinutes` | `30` | 升级锁的失效时间（1–1440）：持锁节点中途退出时，过了这么久别的节点才能接手 |
| `LockWaitMinutes` | `10` | 等别的节点升级完的最长时间（1–1440）；等不到即升级失败、中断启动 |

## `BasicApp:Seed`

种子在执行 `seed` 命令时写入；打开 `RunOnStartup` 时也随启动执行。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `RunOnStartup` | `false`（Development 为 `true`） | 应用启动后执行一遍全部种子（种子可重复执行），失败即中断启动，跑完记一条 `Startup seeding completed.` 日志；关着时由 `seed` 命令或部署流程执行 |
| `EnableDemoData` | `false`（Development 为 `true`） | 是否写演示数据：演示租户 `demo` 及其组织、角色与访问策略，平台演示字典，平台与 `demo` 的草稿通知，演示流程与打印模板 |
| `SuperAdminPassword` | 空（Development 为 `SuperAdmin@123`） | `superadmin` 的初始密码，只在账号不存在、首次写入时用；首次执行 `seed` 时没配即失败（`IDENTITY_VALIDATION_026`） |

## `BasicApp:Modules`

可选模块开关 `BasicApp:Modules:<名>:Enabled`：没写即开启，写了必须是 `true` / `false`。关掉的模块宿主不装配：它的页面与权限码不下发，前端模块随菜单不再出现，操作返回 404。

| 名 | 模块 | 关掉之后 |
| --- | --- | --- |
| `Workbench` | 工作台 | 登录后落到第一个有权限的页面，前端把 `VITE_HOME_PATH` 留空 |
| `Printing` | 打印模板 | 其他包登记的打印数据源没有消费方，见[打印模板](./backend/printing#开关) |
| `Chat` | 在线聊天与聊天审计 | 聊天 Hub 与附件上传端点不映射，Organization 的部门归属事件没有订阅方，见[在线聊天](./backend/chat#开关) |
| `Workflow` | 流程定义、流程实例与审批中心 | 引擎与它的表照旧（宿主恒装数据库工作流）；其他包登记的审批业务没有消费方，见[工作流](./backend/workflow#模块开关) |
| `CodeGeneration` | 代码生成（只在平台一侧） | 表照样建，已登记的外部数据源连接不再建立，产物包下载端点不映射，见[代码生成](./backend/code-generation#模块开关) |
| `AI` | AI 提供商、提示词、知识库与助手 | 表照样建；框架的工具目录与 MCP 工具照常，聊天照常、助手入口隐藏，见[AI 能力](./backend/ai#模块开关) |

## `BasicApp:ApiDocs`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Enabled` | 不写时 Development 为 `true`、其余为 `false` | 是否映射 `/openapi.json`、`/openapi/<组>.json` 与 `/scalar`（不鉴权）；生产环境确需开放时显式写 `true` |

## `BasicApp:ForwardedHeaders`

反向代理的转发头：宿主在管道最前面还原 `X-Forwarded-For` / `-Proto` / `-Host`，访问日志、会话与登录地点、登录节流与自动封禁、访问策略的地址与地区条件拿到的是客户端地址。只信本机回环与下面列出的代理。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `KnownProxies` | `[]` | 受信代理的单个地址，如 `10.0.0.5` |
| `KnownNetworks` | `[]` | 受信代理的网段（CIDR，主机位必须为 0，如 `10.0.0.0/8`） |
| `ForwardLimit` | `1` | 从右往左认几层转发（正整数） |

## `BasicApp:Cors`

前端与后端不同源部署时打开。`AllowedOrigins` 与 `AllowAnyOrigin` 都没写即不启用跨域，只能同源访问。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `AllowedOrigins` | `[]` | 允许的来源：协议 + 主机 + 可选端口，不带路径与结尾斜杠 |
| `AllowAnyOrigin` | `false` | 放开全部来源；不能与 `AllowedOrigins` 同时写，也不能与 `AllowCredentials: true` 同时用 |
| `AllowAnyMethod` / `AllowedMethods` | `true` / `[]` | 为 `false` 时必须写 `AllowedMethods` |
| `AllowAnyHeader` / `AllowedHeaders` | `true` / `[]` | 为 `false` 时必须写 `AllowedHeaders` |
| `AllowCredentials` | `true` | 是否允许携带凭据（Cookie、`Authorization` 头） |
| `ExposedHeaders` | `[]` | 暴露给前端脚本读取的响应头，如 `Content-Disposition` |
| `PreflightMaxAgeSeconds` | `0` | 预检结果的缓存秒数，0 为不设置 |

## `BasicApp:CloudEvents`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Enabled` | `false` | 是否映射 CloudEvents 入站端点 `POST /events`，供内部服务投递事件 |

端点只收持有权限码 `runtime.cloud-events.deliver`（「投递 CloudEvents 事件」，只在平台一侧）的调用方，认证用同一套 JWT。这个码只授给平台的服务账号或服务角色：在角色管理里给平台侧的服务角色授予它，再把服务账号指派进去；普通用户投递即 403 `SECURITY_DENIED_004`，持 `*` 的租户管理员也调不了。端点与 gRPC 一样只开放给内网，不要经反向代理暴露到公网。

入站只收在 `XiHan:Protocols:CloudEvents:Topics` 映射过的事件类型，没映射的是 400 `PROTOCOLS_VALIDATION_009`。登记了处理器的主题都是应用内部事件（断开连接、部门归属变更、工作流的任务与故障），映射到其余主题的事件在派发模式下是 404 `PROTOCOLS_NOTFOUND_003`。主题映射、入站方式与出站 Webhook 见 [`XiHan:Protocols`](#xihan-protocols) 的 `CloudEvents:*`。

## `BasicApp:DataProtection`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `KeysDirectory` | 不写（ASP.NET Core 的缺省位置） | 数据保护密钥环目录 |

`ISecretProtector`（加密参数、存储密钥、租户连接串、认证器密钥、通道密钥与机器人 Webhook 地址）用 ASP.NET Core Data Protection 加密。单机开发用缺省位置即可；多实例部署必须指向各实例都能读写的同一个目录，否则各实例互相解不开密文。

## `BasicApp:Auth`

身份认证：令牌、登录失败锁定、密码策略、图形验证码、登录节流、双因素、验证码与自助注册。数字须为正数（`PasswordHistoryDepth` 可为 0）。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `AccessTokenMinutes` | `120` | 访问令牌有效期（分钟） |
| `RefreshTokenDays` | `7` | 刷新令牌（会话）有效期（天） |
| `MaxFailedAccessAttempts` / `LockoutMinutes` | `5` / `15` | 连续登录失败多少次锁定账号、锁定多久 |
| `PasswordMinLength` | `8` | 密码最短长度 |
| `PasswordHistoryDepth` | `3` | 不得与最近多少个历史密码相同；0 不检查，最多 1000，配得更大启动即失败 |
| `SessionCacheSeconds` | `30` | 会话门禁的缓存秒数；吊销会话最迟在这么久后对其他实例生效 |
| `CaptchaEnabled` / `CaptchaSeconds` | `true` / `300` | 密码登录是否要求图形验证码、验证码有效期 |
| `LoginThrottleWindowSeconds` | `60` | 登录节流的固定窗口 |
| `LoginThrottleAccountLimit` / `LoginThrottleIpLimit` | `5` / `30` | 每个窗口同一账号在同一地址、同一地址跨账号累计的失败上限 |
| `TwoFactorTicketMinutes` | `10` | 两步验证票据有效期，从签发起算、不顺延 |
| `TwoFactorTicketMaxFailures` | `5` | 一张票据允许输错的次数，用完即作废、须重新登录 |
| `TotpIssuer` | `XiHan BasicApp` | 认证器 App 里显示的签发方 |
| `SelfRegistrationEnabled` | `false` | 开放自助注册（`auth.register`，注册的是平台账号） |
| `VerificationCodeMinutes` | `10` | 邮件 / 短信验证码有效期 |
| `VerificationCodeIntervalSeconds` | `60` | 同一用途、同一目标两次发码的最短间隔 |
| `VerificationCodeDailyPerTarget` / `VerificationCodeDailyPerAddress` | `10` / `50` | 一个邮箱或手机号、一个客户端地址每天（UTC）最多收发几次，不分用途 |
| `VerificationCodeMaxFailures` / `VerificationCodeLockoutMinutes` | `5` / `10` | 一个验证码允许输错的次数；用完后同一用途、同一主体不能再发码与校验的时长 |

登录页开放哪些方式不在这里，而是系统参数 `identity.login-methods`（JSON 数组：`password`、`email`、`phone`、`passkey`），见[系统设置](./backend/settings)。

### `BasicApp:Auth:OAuth`（第三方登录）

第三方登录不分租户、不存库；启用且填了 `ClientId` 的提供方才出现在登录页。第三方账号要先在个人中心绑定本地账号才能登录，不自动建号。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Enabled` | `true` | 总开关；没有可用的提供方时等于关 |
| `FrontendCallbackUrl` | `/#/auth/oauth-callback` | 回调后带着一次性票据跳去的前端页面 |
| `CallbackBaseUrl` | 不写（取请求的来源） | 回调地址的来源（协议 + 主机 + 端口）；经反向代理时要么写它，要么保证转发头配置正确 |
| `Providers` | `[]` | 提供方列表，按顺序显示，同名只取第一个 |

在第三方后台登记的回调地址是 `<CallbackBaseUrl>/api/oauth/<Name>/callback`。

```json
{
  "BasicApp": {
    "Auth": {
      "OAuth": {
        "CallbackBaseUrl": "https://<后端对外地址>",
        "Providers": [
          { "Name": "github", "ClientId": "<客户端 ID>", "ClientSecret": "<客户端密钥>" },
          { "Name": "wecom", "ClientId": "<corpid>", "ClientSecret": "<应用 secret>", "AgentId": "<AgentId>", "Mode": "QrCode", "LoadMemberProfile": true }
        ]
      }
    }
  }
}
```

提供方的字段：

| 字段 | 说明 |
| --- | --- |
| `Name` | 路由与登录页用的名称（小写） |
| `Provider` | 类型，不写即按 `Name` 判定：`github`、`gitee`、`google`、`qq`、`wechat`（别名 `weixin`）、`wecom`（别名 `workweixin`）、`lark`（别名 `feishu`）、`dingtalk` |
| `ClientId` / `ClientSecret` | 应用凭据（微信为 appid / secret，企业微信为 corpid / 应用的 corpsecret） |
| `Enabled` / `DisplayName` | 缺省 `true`；登录页上的显示名，缺省同 `Name` |
| `Mode` | `QrCode`（扫码，缺省）或 `Account`（账号授权：微信公众号网页授权、企业微信网页授权、飞书、钉钉账号登录） |
| `AgentId` / `CorpId` / `LoadMemberProfile` | 企业微信的应用 AgentId；钉钉限定组织；企业微信再取成员资料（姓名通常只在这里有） |
| `Scopes` / `AuthorizationEndpoint` | 额外的授权范围（微信、企业微信整体替换缺省，其余追加）；授权页地址覆盖 |

## `BasicApp:Access`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `PermissionConditionMode` | `Enforce` | 权限条件的生效模式：`Enforce`（判定）、`LogOnly`（只记日志不拦截）、`Disabled`（不判定） |
| `AccessPolicyEnabled` | `true` | 访问策略总开关；策略把自己锁在外面时由运维在这里关掉 |
| `ConditionTimeZone` | `Asia/Shanghai` | 条件里时刻、小时、星期与日期所按的时区（IANA 或 Windows 写法） |

## `BasicApp:Auditing`

BasicApp 自己的访问、异常、登录与变更日志，按月分表。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `QueueCapacity` | `10000` | 等待落库的日志条数上限；满了新到的丢弃并计数，不阻塞调用 |
| `BatchSize` / `FlushIntervalSeconds` | `200` / `1` | 每批落库的条数与刷写间隔（秒，可带小数） |
| `AccessLog:Enabled` | `true` | 是否记访问日志 |
| `AccessLog:Exclude` | `[]` | 不记访问日志的操作 ID，如 `auth.me`；异常、登录与变更日志不受影响 |
| `DefaultRetentionDays` | `180` | 系统参数 `auditing.retention-days` 的值为空时的保留天数 |
| `RetentionCron` | `0 3 * * *` | 保留清理作业的计划（cron，按 UTC） |

## `BasicApp:Files`

上传、分片与签名下载地址；存储本身在后台「存储配置」里维护。这一节写错不报错：不合法或不是正数的值按缺省值处理。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `LocalRootPath` | `App_Data/files` | 本地存储根目录，相对路径按内容根目录；各本地存储配置的桶是它下面的子目录 |
| `MaxUploadBytes` | `104857600`（100 MiB） | 单个文件的大小上限 |
| `UploadPartBytes` | `8388608`（8 MiB） | 分片上传每片的大小，不小于存储要求的最小分片 5 MiB |
| `UploadLifetime` | `1.00:00:00` | 分片上传从发起到完成的时限 |
| `LinkLifetime` / `MaxLinkLifetime` | `00:30:00` / `7.00:00:00` | 签名下载地址的缺省与最长有效期 |

## `BasicApp:Notification`

邮件、短信、机器人发件箱与 Telegram 交互；通道与机器人在后台页面配置。这一节写错不报错：不合法的值按缺省值处理。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Brand` | 不写（应用名） | 邮件与短信模板变量 `brand` 的缺省值 |
| `DispatchInterval` | `00:00:05` | 发送循环的轮询间隔；有新记录时提前唤醒 |
| `RetryDelay` | `00:00:30` | 失败后重试的基础间隔，第 n 次失败后等 n 倍 |
| `MaxAttempts` | `3` | 每条记录最多尝试的次数 |
| `StuckAfter` | `00:10:00` | 发送中超过这么久没有结果即视为进程中断，改回排队中 |
| `Telegram:MaxDispatchConcurrency` | `16` | 全部机器人共用的同时处理上限 |
| `Telegram:DedupLifetime` | `00:30:00` | 消息去重标记的保留时长 |
| `Telegram:PollingTimeoutSeconds` | `50` | 长轮询每次等待的秒数（不配 Webhook 时用长轮询） |
| `Telegram:Texts:*` | 见 `appsettings.json` | 回复文案；写成空串即不发该条回复，`{botUsername}`、`{chatId}`、`{userId}` 会被替换 |
| `Telegram:Retry:MaxRetries` | `3` | 出站调用 Bot API 失败后最多再试的次数 |
| `Telegram:Retry:BaseDelayMs` / `MaxDelayMs` | `500` / `10000` | 退避：第 n 次等 min(`MaxDelayMs`, `BaseDelayMs` × 2^(n-1)) |
| `Telegram:Retry:MaxRetryAfterSeconds` | `60` | 429 的 `retry_after` 最多等多少秒，超过即按失败处理 |
| `Telegram:Retry:NotifyAdminOnFinalFailure` | `true` | 最终失败时私发管理员告警 |

机器人、Webhook 与代理在系统参数 `notification.telegram` 里配。

## `BasicApp:Realtime:SignalR`

SignalR 的 Hub 参数，对全部 Hub 生效；缺省即 ASP.NET Core 的缺省值，不配即可。配置写错启动即失败。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `EnableDetailedErrors` | `false` | 把详细错误发给客户端，生产环境不要打开 |
| `KeepAliveInterval` | `00:00:15` | 服务端发保活消息的间隔 |
| `ClientTimeoutInterval` | `00:00:30` | 多久没收到客户端的消息算断开，一般设成客户端保活间隔的两倍 |
| `HandshakeTimeout` | `00:00:15` | 握手的超时 |
| `MaximumReceiveMessageSize` | `32768` | 单条入站消息的大小上限（字节） |
| `StreamBufferCapacity` | `10` | 流式调用的缓冲容量 |
| `MaximumParallelInvocationsPerClient` | `1` | 每个客户端的最大并行调用数 |

时长写 `hh:mm:ss`（可带天数 `d.hh:mm:ss`），必须大于零；只写 `15` 或 `00:15` 都算写错。数字必须是正整数。前端用 SignalR 客户端的缺省超时（30 秒没收到服务端消息即断开），把 `KeepAliveInterval` 调到 30 秒以上会让浏览器不断掉线重连。

## `BasicApp:OpenPlatform`

签名开放接口与 OAuth 2.0 / OIDC 授权服务。这一节写错不报错：数字超出范围时夹到范围内。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `TimestampToleranceSeconds` | `300` | 签名请求的时间戳与服务端时间的最大偏差（30–3600）；随机串保留它的两倍 |
| `MaxRequestBodyBytes` | `2097152`（2 MiB） | 开放接口请求体上限（1 KiB–64 MiB） |
| `Issuer` | 不写（请求来源 + `/api/oauth2`） | OIDC 签发者，不带结尾斜杠；经反向代理时要显式写 |
| `ConsentPageUrl` | `/#/oauth/authorize` | 授权同意页（前端路由），授权端点把查询串原样带过去 |
| `IdTokenLifetimeSeconds` | `600` | `id_token` 有效期（60–3600） |

## `BasicApp:CodeGeneration`

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `TablePrefixes` | `sys_` | 导入表时推断类名去掉的表名前缀，逗号分隔（是一个字符串，不是数组），取最长的匹配 |
| `EnableGenerateToProject` | `false`（Development 为 `true`） | 允许「生成到项目」（直接写进后端与前端源码目录）；打开了也只在 Development 生效 |
| `AllowSqliteDataSource` | `false`（Development 为 `true`） | 是否开放 SQLite 数据源；SQLite 的地址是服务器上的文件路径，开放等于能读进程够得着的任意数据库文件 |
| `BackendRootPath` | 不写（Development 为 `../..`） | 生成到项目时后端的根目录（含 `src/` 的 `backend`），相对宿主的内容根目录 |
| `FrontendRootPath` | 不写（Development 为 `../../../frontend`） | 生成到项目时前端的根目录（含 `package.json` 的 `frontend`），相对宿主的内容根目录 |

## `BasicApp:AI:Qdrant`

知识库的向量库。写了 `Host` 宿主才登记 Qdrant 连接器并装框架的知识库 Provider，健康检查随之多一项 `ai.vectordata.store`；没写即知识库不可用，见[AI 能力](./backend/ai#知识库)。地址、密钥写错启动不报错，连不上时健康项是 Degraded，摄入的文档以失败保存。

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Host` | 不写 | 主机名；空白即没配 |
| `Port` | `6334` | gRPC 端口（1–65535）；`6333` 是 REST 端口，连接器不用 |
| `Https` | `false` | 是否走 HTTPS，写了必须是 `true` / `false` |
| `ApiKey` | 不写 | API 密钥，不要提交到仓库 |

向量维度、检索条数与切块参数在 [`XiHan:AI`](#xihan-ai) 的 `Knowledge:*`。

## 前端 `.env`

Vite 按模式叠加读取，后者覆盖前者：

| 文件 | 入库 | 用途 |
| --- | --- | --- |
| `.env` | 是 | 各环境共用：首页与通知 Hub |
| `.env.development` | 是 | `pnpm dev`：开发服务器端口与代理目标 |
| `.env.production` | 是 | `pnpm build`：接口地址（缺省同源）、路由模式与部署子路径 |
| `.env.local` / `.env.<模式>.local` | 否 | 本机改动 |
| `.env.example` | 是 | 全部变量与说明 |

前后端不同源部署时，在 `.env.production` 写后端地址（`VITE_API_BASE_URL`），并在后端 [`BasicApp:Cors`](#basicapp-cors) 的 `AllowedOrigins` 列出前端的来源。

| 变量 | 缺省 | 说明 |
| --- | --- | --- |
| `VITE_PORT` | `9100` | 开发服务器端口 |
| `VITE_DEV_PROXY_TARGET` | `http://localhost:9710` | 开发代理目标，即后端的 `Kestrel:Endpoints:Http` |
| `VITE_API_BASE_URL` | 空（同源） | 构建期 API 源 |
| `VITE_HOME_PATH` | `/workbench/dashboard` | 首页（固定首页标签、守卫兜底）；关掉工作台模块时留空，登录后落到第一个有权限的页面 |
| `VITE_ROUTER_HISTORY` | `hash` | `history` 需要静态托管把未知路径回退到 `index.html` |
| `VITE_BASE` | `/` | 部署子路径 |
| `VITE_REALTIME_HUB` | `/hubs/notification` | 通知 Hub 路径；留空即不建实时连接（强制下线、站内通知、跨端设置同步都不再实时） |
| `VITE_APP_TITLE` / `VITE_APP_SUBTITLE` / `VITE_APP_DESCRIPTION` / `VITE_APP_LOGO` | 空 | 登录页品牌区；留空取应用名文案与 `/favicon.png` |

## 下一步

- [部署](./deployment) · [数据库配置](./backend/database) · [统一认证](./backend/authentication)
