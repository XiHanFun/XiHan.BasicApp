# 健康与可观测性

## 健康检查

宿主 `MapXiHanHealth()` 映射三个端点，直接对应框架的应用状态机：

| 端点 | 用途 | 内容 |
| --- | --- | --- |
| `/health/startup` | 启动探针 | 应用是否已启动完毕（缺省时限 `XiHan:Kernel:StartTimeoutSeconds` 60 秒） |
| `/health/ready` | 就绪探针 | 各 Feature 状态与 `IHealthContributor` 条目 |
| `/health/live` | 存活探针 | 进程存活 |

健康项由各 Provider 自带（框架 ADR-0030），BasicApp 不另写：数据库、租户目录、授予存储、审计存储、调度、对象存储的每个命名实例（`objectstorage.store.<名>`）、IP 归属地库等。条目带 `description`、`checkedAt`、`durationMs` 与 `data`。

健康端点不经过维护模式拦截：升级期间负载均衡仍能探测。

## 维护模式

另一个节点升级共享库时，`MaintenanceModeMiddleware` 对除健康检查与 `/.well-known/` 之外的请求返回 503（`RUNTIME_UNAVAILABLE_003`，带 `Retry-After`），普通端点与 Hub 也一样挡住。

## 服务监控

Maintenance 的 `server.get`（`maintenance.server.read`，平台侧）：取自框架的 `IHostMonitor`（`Observability.Abstractions`），包括 CPU、内存、磁盘、网络、操作系统、.NET 运行时与进程信息。读不到的字段为空（区分「没有」与「为零」）；CPU 使用率与网卡速率由后台采样，启动后第一个采样间隔内为空。前端页面 `/maintenance/server`。

## 链路与日志

| 能力 | 6.0 做法 |
| --- | --- |
| Trace | 客户端每个请求带 W3C `traceparent`；框架缺失或不合法时生成新的 trace-id |
| 关联 | 失败响应的 Problem Details 带 `traceId`；成功响应不另带 trace 头，以客户端自己生成的 `traceparent` 为准 |
| 业务日志 | 八类审计日志与按 trace-id 的链路时间线，见[日志审计](./logging) |
| 结构化日志 | ASP.NET Core `ILogger`，`appsettings.json` 的 `Logging` 节；Development 把 `XiHan` 调到 `Debug` |
| OpenTelemetry | Full Profile 带 `Observability.OpenTelemetry`，**没有配置导出端点时不导出** |

命令模式下日志只写 stderr，stdout 只承载命令输出，方便脚本处理。

## 弹性

框架内核的 Resilience 阶段按操作 ID 提供熔断、固定窗口限流与并发上限（`XiHan:Kernel:Resilience`，可按操作覆盖），拒绝以 `UNAVAILABLE` 种类返回。截止时间由请求头 `xihan-deadline` 携带，超时 504。

## 下一步

- [部署](../deployment) · [日志审计](./logging) · [表结构升级](./upgrade)
