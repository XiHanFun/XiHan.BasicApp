# 健康与可观测性

本页说明健康检查、维护模式、服务监控、链路关联与弹性限制，适用于运维人员与后端开发者。

## 健康检查

宿主 `MapXiHanHealth()` 映射三个探针端点，直接对应框架的应用状态机。三者返回同一份正文（各 Feature 的状态与各健康项），只是判定不同：通过 200，否则 503。探针不经过内核管道。

| 端点 | 用途 | 通过条件 |
| --- | --- | --- |
| `/health/startup` | 启动探针 | 所有 Feature 都已启动完毕 |
| `/health/ready` | 就绪探针 | 应用处于 Ready 或 Degraded |
| `/health/live` | 存活探针 | 应用没有 Failed |

单个 Feature 的启动超时是 `XiHan:Kernel:StartTimeoutSeconds`（BasicApp 取 60 秒，表结构升级与开发环境建表都在启动阶段完成），失败策略见 `XiHan:Kernel:FailurePolicy`。

健康项由各 Provider 自带（框架 ADR-0030），BasicApp 不另写。常见的有：

| 健康项 | 来源 |
| --- | --- |
| `persistence.sqlsugar.database` | 平台库；配了从库另有 `persistence.sqlsugar.replicas`（有从库不可读时 Degraded） |
| `scheduling.database.leader` | 调度的领导租约表，不可达时 Degraded |
| `workflow.database.poller` | 工作流到期书签的轮询 |
| `objectstorage.store.<名>` | Files 存储配置对应的每个命名实例 |
| `persistence.datasource.<名>` | 代码生成登记的外部数据源（`codegen-<ID>`），连不上 Degraded |
| `localization.database.translations` | 数据库译文：还没加载为 Started，加载失败为 Degraded 并保留上一份快照 |
| `ai.vectordata.store` | 配了 Qdrant 时的知识库向量库，连不上 Degraded |
| `caching.redis.connection` / `messaging.rabbitmq.connection` | 配了 Redis / RabbitMQ 时：启动时连不上即启动失败，之后按 `HealthProbeIntervalSeconds`（15 秒）重探，断开时 Degraded、应用照常服务 |
| `caching.inmemory.capacity` / `messaging.inmemory.capacity` | 没配 Redis / RabbitMQ 时的进程内实现的容量 |

以 `/health/ready` 实际返回的为准。条目带 `description`、`checkedAt`、`durationMs` 与 `data`；多数健康项的重探间隔在各自配置节的 `HealthProbeIntervalSeconds`，见[配置参考](../configuration)。

## 维护模式

另一个节点升级共享库时，`MaintenanceModeMiddleware`（宿主排在最前）对除健康检查与 `/.well-known/` 之外的请求返回 503（`RUNTIME_UNAVAILABLE_003`，带 `Retry-After`），普通端点与 Hub 也一样挡住。判定依据是平台库的版本台账：有节点持着没过期的升级锁即进入维护模式；只看 PostgreSQL 库，台账还没建或读不到时不进入。健康端点不受拦截，升级期间负载均衡仍能探测。升级本身见[表结构升级](./upgrade)。

## 服务监控

Maintenance 的 `server.get`（`GET /api/server`，`maintenance.server.read`，平台侧）：运行信息取自框架的 `IHostMonitor`（`Observability.Abstractions`，不启动外部命令），包括 .NET 运行时与进程、处理器、内存、磁盘、网卡、显卡、主板与 BIOS，另附各 Provider 最近一次的健康项。读不到的字段为空（区分「没有」与「为零」）；CPU 使用率与网卡速率由后台按 `XiHan:Observability:Host:SampleIntervalSeconds`（5 秒）采样，启动后第一个采样间隔内为空。前端页面 `/maintenance/server`。

`server.packages`（`GET /api/server:packages`，同一权限，平台侧）给关于页的后端依赖清单：已加载的 `XiHan*` 程序集直接引用的第三方程序集与版本，不含 `XiHan`、`Microsoft`、`System` 开头的，同名取最高版本。

## 链路与日志

| 能力 | 做法 |
| --- | --- |
| Trace | 前端每个请求带 W3C `traceparent`；缺失或不合法时框架并入 ASP.NET Core 当前的请求 Activity，也没有时生成新的 trace-id |
| 关联 | 失败响应的 Problem Details 带 `traceId`；成功响应不另带 trace 头，以客户端自己生成的 `traceparent` 为准 |
| 业务日志 | 审计日志与按 trace-id 的链路时间线，见[日志审计](./logging) |
| 结构化日志 | ASP.NET Core `ILogger`，`appsettings.json` 的 `Logging` 节；Development 把 `XiHan` 调到 `Debug` |
| OpenTelemetry | Full Profile 带 `Observability.OpenTelemetry`：追踪、指标与日志缺省都开（`XiHan:Observability:OpenTelemetry`），配了 OTLP 端点（`Otlp:Endpoint` 或环境变量 `OTEL_EXPORTER_OTLP_ENDPOINT`）才导出 |

命令模式下日志只写 stderr，stdout 只承载命令输出，方便脚本处理。

## 弹性

框架内核的 Resilience 阶段按操作 ID 提供固定窗口限流、并发上限与熔断（`XiHan:Kernel:Resilience`，根上的键是默认策略，`Operations:<操作 ID>` 按操作覆盖）。缺省不限流、不限并发、不熔断。拒绝以 503 返回：限流 `KERNEL_UNAVAILABLE_002`、并发超限 `KERNEL_UNAVAILABLE_003`、熔断 `KERNEL_UNAVAILABLE_004`。

截止时间由请求头 `xihan-deadline`（绝对时间）携带，gRPC 用 `grpc-timeout`；超时即 504（`KERNEL_TIMEOUT_001`）。

## 下一步

- [部署](../deployment) · [日志审计](./logging) · [表结构升级](./upgrade)
