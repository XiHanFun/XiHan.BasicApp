# 配置参考

`appsettings` 的全量配置节说明。所有键名与默认值对照仓库里的 `appsettings.Development.json` 与各 Options 类核实。

## 文件与优先级

```text
backend/src/main/XiHan.BasicApp.WebHost/
├── appsettings.json                 # 基础（Logging / AllowedHosts / CodeGeneration）
├── appsettings.Development.json     # 开发环境（完整示例，带逐项注释）
└── appsettings.Production.json      # 生产环境
```

优先级（后者覆盖前者）：`appsettings.json` → `appsettings.{Environment}.json` → 环境变量 → 命令行。

::: tip 环境变量写法
配置层级用**双下划线**表示：`XiHan:Authentication:Jwt:SecretKey` → `XiHan__Authentication__Jwt__SecretKey`。

**生产的密钥类配置一律走环境变量或密钥库，不要提交明文。** 生产 `appsettings` 通常被 gitignore，需要在服务器上单独维护——这也是几个开关（如 `EnableDiffLog`）最容易漏配的原因。
:::

## `Hosting`

| 键 | 说明 | 示例 |
| --- | --- | --- |
| `Urls` | 监听地址与端口，多个用分号分隔 | `http://127.0.0.1:9708` |

仓库 Development 与 Production 当前都监听 `9708`；Docker Compose 也在容器内使用 `9708`，宿主机端口可由 `BACKEND_PORT` 覆盖。开发环境改端口后要同步修改前端的 `VITE_DEV_PROXY_TARGET`。

## `XiHan:Observability`

链路追踪（OpenTelemetry）。

| 键 | 默认 | 说明 |
| --- | --- | --- |
| `Enabled` | `false` | **总开关**。开启后每请求产生 W3C Activity，TraceId 变 32-hex，日志/审计/事件总线统一同源；关则退回 Kestrel `TraceIdentifier` |
| `ServiceName` | — | 写入 OTel Resource 的 `service.name` |
| `SamplingRatio` | — | 采样率 0~1，开发可设 `1.0` 全采 |
| `ConsoleExporter` | `false` | 控制台打印 span（调试用） |
| `OtlpEndpoint` | `""` | OTLP 导出端点（如 `http://localhost:4317`）。为空则只在本地产生 TraceId，不外发到 Jaeger/Tempo |

## `XiHan:DistributedIds:SnowflakeId`

雪花 ID 生成器。

| 键 | 说明 |
| --- | --- |
| `WorkerId` | **同一集群内每个节点必须唯一**，否则生成重复 ID。多节点部署务必逐节点改 |
| `DataCenterId` | 多机房区分，单机房固定即可 |
| `BaseTime` | 起始纪元，**一经上线不可更改**（改动会导致 ID 回退甚至冲突） |
| `WorkerIdBitLength` | 机器码位长，与序列号位长之和不超过 22。6 位 → WorkerId 上限 0-63 |
| `SeqBitLength` | 序列号位长，决定同毫秒并发上限，**一经上线不可更改** |
| `SnowflakeIdType` | `SnowFlakeMethod`（漂移算法，抗时钟回拨、吞吐更高）/ `ClassicSnowFlakeMethod` |

::: danger 多节点必改 WorkerId
这是最容易忽略、后果最严重的一项：两个节点同 `WorkerId` 会生成重复主键，且不会立刻报错，等到唯一约束冲突时数据已经乱了。
:::

## `XiHan:Authentication`

### `PasswordHasher`（PBKDF2）

| 键 | 默认 | 说明 |
| --- | --- | --- |
| `Version` | `1` | 哈希方案版本，用于将来平滑升级算法（老密码按旧版本校验） |
| `Iterations` | `600000` | 迭代次数，OWASP 对 PBKDF2-SHA256 的推荐量级 |
| `SaltSize` / `HashSize` | `32` / `32` | 盐与输出长度（字节） |
| `HashAlgorithm` | `SHA256` | — |

### `Jwt`

| 键 | 默认 | 说明 |
| --- | --- | --- |
| `SecretKey` | — | **签名密钥，生产务必改为高强度随机值并保密**（走环境变量/密钥库） |
| `Issuer` / `Audience` | — | 签发者 / 受众 |
| `AccessTokenExpirationMinutes` | `60`（框架默认） | 访问令牌有效期 |
| `RefreshTokenExpirationDays` | `7` | 刷新令牌有效期 |
| `ClockSkewMinutes` | `5` | 允许的时钟偏差，容忍多节点时间误差 |

> 仓库的 Development 配置把 `AccessTokenExpirationMinutes` 设为 `120`，以实际配置为准。

### `OAuth`（第三方登录）

| 键 | 说明 |
| --- | --- |
| `Enabled` | 总开关 |
| `FrontendCallbackUrl` | 登录成功后跳回的前端回调页 |
| `Providers[]` | 各提供商：`Name`（**内部标识，勿改**）、`DisplayName`、`Enabled`、`ClientId`、`ClientSecret`、`Scopes[]` |

内建 github / gitee / google / qq，需到对应平台申请后替换 `ClientId` / `ClientSecret`。

## `XiHan:Data:SqlSugarCore`

### 连接

`ConnectionConfigs[]` 每项：

| 键 | 说明 |
| --- | --- |
| `ConfigId` | 连接唯一标识（多库/多租户路由用），字符串 |
| `ConnectionString` | 主库连接串 |
| `DbType` | `PostgreSQL` / `MySql` / `SqlServer` / `Oracle` / `Dm` / `Kdbndp` 等 |
| `IsAutoCloseConnection` | 是否自动关闭连接 |
| `SlaveConnectionConfigs[]` | 从库（读写分离）；空数组=单库 |

配了从库后 SELECT 自动走从库、写与事务走主库，业务无感知。

::: warning `HitRate` 配不上
`HitRate`（读权重）是 SqlSugar 的**字段**、绑不上 `appsettings`，写了也无效、恒为 0。框架会把权重为 0 的从库归一化为 `DefaultSlaveHitRate`（默认 10），所以不写也能等权分担读。

需要差异化权重、挂 `ConfigureExternalServices` 或自写探活，用代码钩子 `XiHanSqlSugarCoreOptions.ConfigureConnectionConfigs`。
:::

### 日志与诊断

| 键 | 默认 | 说明 |
| --- | --- | --- |
| `EnableSqlLog` | `false` | 打印所有 SQL（**生产建议关闭**，日志会爆量） |
| `EnableSqlErrorLog` | `true` | 记录 SQL 异常 |
| `EnableSlowSqlLog` | `true` | 记录慢 SQL |
| `SlowSqlThresholdMilliseconds` | — | 慢 SQL 阈值，纯观测用途、不影响语句执行 |
| `CommandTimeoutSeconds` | `300` | ADO 命令超时，0/负值不覆盖；**须明显大于慢 SQL 阈值** |

### 初始化

| 键 | 说明 |
| --- | --- |
| `EnableDbInitialization` | 启动时自动建库（库不存在则创建） |
| `EnableTableInitialization` | 启动时 CodeFirst 建表 |
| `EnableDataSeeding` | 启动时写入种子数据 |

::: danger 建表只建不改
`DbInitializer` **表存在就跳过、从不为已有表补列**。给既有实体加字段后，存量库必须通过前向 `UpdateScripts`（或部署流程中的等价迁移步骤）执行 `ALTER TABLE`。当前 BasicApp 不会自动触发升级引擎，见[升级与迁移](./backend/upgrade)。
:::

### `EnableDiffLog`

| 键 | 默认 | 说明 |
| --- | --- | --- |
| `EnableDiffLog` | **`false`** | 实体差异日志（`SysDiffLog`）**总开关** |

::: danger 数据变更日志页恒空的头号原因
默认是 `false`——不开则 Diff AOP 根本不挂载，收集到的差异被直接丢弃。生产 `appsettings` 常被 gitignore，最容易漏配的就是这一项。

代价：开启后 update/delete 会先查一次旧值算差异，**每个写操作多一次 SELECT**。且只覆盖走仓储的写，绕过仓储直接用 DbClient 的写（如 `UpdateColumns`）不产生差异日志。
:::

### 从库健康探针

| 键 | 默认 | 说明 |
| --- | --- | --- |
| `DefaultSlaveHitRate` | `10` | 从库权重归一化默认值 |
| `EnableSlaveHealthCheck` | `false` | 周期探活，不可用从库自动摘除读权重 |
| `SlaveHealthCheckIntervalSeconds` | `30` | 探测周期 |
| `SlaveFailureCooldownSeconds` | `120` | 故障冷却窗口，恢复后先冷却再回填权重避免抖动 |

## `XiHan:Caching:Redis`

| 键 | 默认 | 说明 |
| --- | --- | --- |
| `IsEnabled` | — | **关闭则退化为进程内内存缓存**（失去分布式缓存/锁/队列） |
| `Configuration` | — | 连接串 `host:port,user=,password=,defaultDatabase=` |
| `InstanceName` | `XiHan:` | 缓存 Key 统一前缀（隔离不同应用/环境） |
| `ConnectTimeout` / `SyncTimeout` / `AsyncTimeout` | `5000` | 各类超时（毫秒） |
| `AllowAdmin` | `false` | 允许管理类命令（FLUSHDB/CONFIG），**生产慎开** |
| `UseSsl` | `false` | — |
| `AbortOnConnectFail` | `false` | `false` = 后台持续重连，更适合生产 |

::: warning 关掉 Redis 的连锁反应
`IsEnabled=false` 时分布式锁退化为进程内锁——**多实例部署会各跑各的**：定时任务重复执行、后台 Worker 不再单活、工作流定时器多实例并发。单机开发无所谓，生产必须开。
:::

## `XiHan:Web`

### `Core:ClientInfo`

| 键 | 说明 |
| --- | --- |
| `EnableIpRegion` | 是否启用 IP 归属地解析 |
| `Ip2RegionDbPath` | ip2region 离线库路径 |

### `Api:Auth`

| 键 | 说明 |
| --- | --- |
| `RequireAuthenticatedUser` | 全局要求已认证（匿名接口需 `[AllowAnonymous]` 显式放行） |
| `SignalRHubPathPrefix` | SignalR Hub 路由前缀，默认 `/hubs` |

### `Api:Cors`

| 键 | 说明 |
| --- | --- |
| `AllowedOrigins[]` | 允许的来源。**携带凭证时不能用 `*`，必须显式列出** |
| `AllowAnyOrigin` | 与 `AllowCredentials` **互斥** |
| `AllowAnyMethod` / `AllowAnyHeader` | — |
| `AllowCredentials` | 是否允许携带 Cookie/Authorization |
| `ExposedHeaders[]` | 额外暴露给前端 JS 读取的响应头 |
| `PreflightMaxAgeSeconds` | 预检结果缓存秒数 |

### `Api:OpenApiSecurity`

开放接口签名/防重放/加密，完整说明见 [接口对接指南](./api-guide#开放接口-签名调用-无-jwt)。

| 键 | 框架默认 | 说明 |
| --- | --- | --- |
| `IsEnabled` | `false` | 总开关 |
| **`ProtectedPathPrefixes`** | **`["/api"]`** | **必须覆盖**，否则开启后整站接口都要验签。BasicApp 配 `["/api/openapi"]` |
| `IgnoredPathPrefixes` | — | 豁免前缀（文档/健康检查等） |
| `AllowUnsignedRequests` | `false` | 灰度开关：允许未带安全头的请求放行 |
| `RequireContentSignature` | `true` | 强制校验内容签名 |
| `EnableReplayProtection` | `true` | 防重放（Nonce 去重） |
| `TimestampToleranceSeconds` / `NonceExpireSeconds` | `300` | 时间戳容差 / Nonce 存活期 |
| `MaxRequestBodySize` | 2 MiB | 最大请求体 |
| `EnableResponseEncryption` | `true` | 启用响应加密 |
| `DefaultSignatureAlgorithm` | `HMACSHA256` | 也支持 `HMACSHA512` / `RSASHA256` / `SM2` |
| `DefaultContentSignatureAlgorithm` | `SHA256` | 也支持 `SHA512` |
| `Clients[]` | — | 配置文件里的静态客户端（`AccessKey` / `SecretKey` / `EncryptKey` / `IpWhitelist` 等）。**密钥敏感，生产走环境变量** |

### `RealTime:SignalR`

| 键 | 说明 |
| --- | --- |
| `EnableDetailedErrors` | 详细错误（**生产设 `false`**） |
| `KeepAliveInterval` / `ClientTimeoutInterval` / `HandshakeTimeout` | 心跳与超时（`hh:mm:ss`） |
| `MaximumReceiveMessageSize` | 最大接收消息大小（字节） |
| `StreamBufferCapacity` | 流缓冲容量 |
| `MaximumParallelInvocationsPerClient` | 每客户端最大并行调用数 |
| `EnableConnectionMetrics` | 连接指标 |

### `Gateway` / `GrayRouting`

| 键 | 说明 |
| --- | --- |
| `Gateway.EnableGrayRouting` / `EnableRequestTracing` / `EnableRateLimiting` / `EnableCircuitBreaker` | 各能力开关 |
| `Gateway.RequestTimeoutSeconds` | 网关请求超时 |
| `Gateway.AllowedOrigins[]` / `GlobalHeaders` | 允许来源 / 统一注入的响应头 |
| `GrayRouting.Rules[]` | 灰度规则：`RuleType`（**1=按百分比 2=用户白名单 3=租户 4=请求头**）、`Priority`（**越大越优先**）、`TargetVersion`、`Configuration`（JSON 字符串参数）、`IsEnabled` |

## `XiHan:Upgrade`

| 键 | 说明 |
| --- | --- |
| `MinSupportVersion` / `AppVersion` | 最低来源版本 / 当前版本（留空则运行时探测程序集版本） |
| `MigrationsRootPath` | 迁移脚本根目录 |
| `LockResourceKey` / `LockExpirySeconds` | 分布式锁（防多节点并发升级） |
| `EnableAutoCheckOnStartup` | 启动后初始化/检查版本状态；当前 BasicApp 未调用升级执行入口，不会因此自动跑 SQL |
| `NodeName` / `PrimaryNodeName` | 当前节点 / **仅主节点执行迁移，其余等待** |
| `EnableMultiTenantIsolation` | 是否按租户逐库执行 |
| `ConnectionConfigId` | 升级使用的连接 |
| `EnableMaintenanceMode` | 升级期间进入维护模式 |
| `EnableFileUpdate` / `EnableRollingRestart` | 文件更新 / 滚动重启 |

版本状态与执行历史分别保存在 `SysVersion`、`SysMigrationHistory`，不使用 `version.txt`。引擎被显式调用后会按 `UpdateScripts/{version}.sql` 顺序处理平台库及配置为独立库的租户，锁租约避免多节点并发。当前 BasicApp 没有 `IUpgradeCoordinator` / `IUpgradeEngine` 调用入口，因此启动只初始化版本状态、不执行脚本；完整边界见[升级与迁移](./backend/upgrade)。当前仓库脚本使用 PostgreSQL SQL，切换数据库提供程序时需要维护对应方言的脚本。

## `XiHan:Localization`

| 键 | 说明 |
| --- | --- |
| `ResourcesPath` | 资源文件目录 |
| `DefaultResourceName` | 默认资源名 |
| `DefaultCulture` | 默认文化，如 `zh-CN` |
| `EnumResourceName` | **枚举文案资源名**（枚举标签的单一事实源，如 `Enums`） |
| `EnableDynamicJsonReload` | 资源 JSON 热重载 |

## `XiHan:ObjectStorage`

| 键 | 默认 | 说明 |
| --- | --- | --- |
| `Local.RootPath` | `wwwroot/uploads` | 文件落盘根目录；这是 BasicApp 的显式配置，不是 Framework 3.10.1 的 `LocalStorageOptions` 默认值 |
| `Local.UrlPrefix` | `/uploads` | 对外访问 URL 前缀（**根相对路径**，跨源时前端拼 API origin） |

对象存储的其余后端（S3/OSS/COS/MinIO）**配置落库**在 `SysStorageConfig`，不写 `appsettings`。见 [文件与存储](./backend/file#存储配置-file-storage)。

## `XiHan:VirtualFileSystem`

| 键 | 说明 |
| --- | --- |
| `IncludeCurrentDirectory` / `IncludeAppBaseDirectory` | 是否挂载当前工作目录 / 应用基目录 |

## `XiHan:Workflow`

工作流引擎，见 [框架 · Workflow](https://framework.docs.xihanfun.com/packages/workflow#配置)。要点：`MaxNodeExecutionsPerBurst`（默认 1000）、`MaxSubWorkflowDepth`（默认 16）、`Worker:IsTimerEnabled`（**关掉后延时/重试/超时书签不会被自动恢复**）。

## `Saas:Seed`

注意这一节**不在 `XiHan:` 命名空间下**。

| 键 | 说明 |
| --- | --- |
| `EnableDemoData` | 演示种子开关，**缺省或非法值都视为启用**，显式 `false` 才整体跳过 |
| `SuperAdminPassword` | 超管初始密码（环境变量 `Saas__Seed__SuperAdminPassword`）。**生产务必覆盖** |

## `CodeGeneration`

| 键 | 默认 | 说明 |
| --- | --- | --- |
| `EnableCustomPathDisk` | `false` | 是否允许生成到自定义磁盘路径 |
| `AllowedRootPaths[]` | `[]` | 允许写入的根路径白名单 |

::: warning 生产不要开 `EnableCustomPathDisk`
开启后代码生成器可以往服务器磁盘写文件，`AllowedRootPaths` 是唯一的边界。生产环境保持关闭，用 Zip 下载。
:::

## 数据库里的配置

有几类配置**刻意不放 `appsettings`**，而是落库以便按租户隔离与运行期热切换：

| 内容 | 表 | 页面 |
| --- | --- | --- |
| 业务参数与功能开关 | `SysConfig` | `/setting/config` |
| 存储后端 | `SysStorageConfig` | `/file/storage` |
| 邮件 / 短信网关 | `SysEmailConfig` / `SysSmsConfig` | `/setting/email-config`、`/setting/sms-config` |
| 机器人 | `SysBotConfig` / `SysTelegramBot` | `/setting/bot-config`、`/setting/telegram-bot` |
| AI Provider / 提示词 | `SysAiProvider` / `SysAiPrompt` | `/develop/ai-provider`、`/develop/ai-prompt` |

它们通过 `services.Replace(...)` 覆盖框架默认的配置源实现。判断标准见 [系统设置](./backend/settings#参数配置-setting-config)。

## 相关页面

- [开发环境](./dev-environment)：Docker 起依赖并对齐连接串
- [部署](./deployment)：生产部署与环境变量注入
- [常见问题](./faq)：配置类故障速查
- [系统设置](./backend/settings)：落库配置的管理页面
