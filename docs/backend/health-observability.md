# 健康检查与可观测性

BasicApp 在 WebHost 层提供数据库、Redis 与 Qdrant 三项真实健康检查，并把 `/health` 匿名暴露；链路与指标则复用 [XiHan.Framework.Observability](https://framework.docs.xihanfun.com/guide/observability) 的 OpenTelemetry 装配。两者用途不同：健康检查回答“依赖现在能不能访问”，OpenTelemetry 回答“请求经过了什么、哪里慢”。

## `/health` 端点

`XiHanBasicAppWebHostModule.ConfigureServices` 注册：

```csharp
services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<RedisHealthCheck>("redis")
    .AddCheck<QdrantHealthCheck>("qdrant");
```

初始化时映射匿名 `GET /health`，使用最小响应写入器，只公开总状态、总耗时和检查项名称/状态：

```json
{
  "status": "Healthy",
  "totalDurationMs": 18.4,
  "checks": [
    { "name": "database", "status": "Healthy" },
    { "name": "redis", "status": "Healthy" },
    { "name": "qdrant", "status": "Healthy" }
  ]
}
```

异常对象、连接串和检查描述不会写入响应。端点标记了 `AllowAnonymous()`，并在 OpenAPI 安全配置的忽略路径中放行。

## 三项检查实际做什么

| 名称 | 探测行为 | 未配置时 | 失败结果 |
| --- | --- | --- | --- |
| `database` | 当前 SqlSugar 连接执行 `SELECT 1` | 没有独立“关闭”态 | `Unhealthy("数据库连接失败")` |
| `redis` | 解析 `IConnectionMultiplexer` 后执行 `PING` | Redis 未启用时返回 `Healthy("Redis 未启用（进程内回退）")` | `Unhealthy("Redis 连接失败")` |
| `qdrant` | `VectorStore.ListCollectionNamesAsync()` 做一次真实往返 | `VectorStore` 未注册即不健康 | 3 秒超时或连接失败均为 `Unhealthy` |

::: warning Redis 健康不代表具备分布式能力
`XiHan:Caching:Redis:IsEnabled=false` 时，Redis 检查仍返回 Healthy，因为应用允许回退到进程内缓存。但此时分布式锁、定时任务单活、工作流定时器和 Redis 队列都失去跨实例语义。生产多实例不能只看 `/health` 绿灯，还要检查 Redis 是否按预期启用。
:::

::: warning Qdrant 当前是强制健康项
WebHost 无条件注册 `QdrantHealthCheck`，检查又把缺少 `VectorStore` 或 Qdrant 不可达视为不健康。即使业务不使用知识库，`/health` 也可能因 Qdrant 返回 Unhealthy。若部署确实不启用 AI/RAG，应在应用代码中按配置条件注册该检查，而不是让编排系统长期忽略红灯。
:::

## 存活与就绪探针

当前只有一个 `/health`，三项检查不带 tag，因此它更接近**就绪探针**：任何外部依赖失败都可能使整体状态变为 Unhealthy。

Kubernetes 示例：

```yaml
readinessProbe:
  httpGet:
    path: /health
    port: 9708
  initialDelaySeconds: 10
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3
```

不要直接把同一端点当作严格的 livenessProbe。数据库、Redis 或 Qdrant 短暂故障时重启应用通常无助于恢复依赖，反而可能造成重启风暴。若需要存活/就绪分离，应给检查加 tag，再映射独立端点：

```text
/health/live   只判断进程可响应
/health/ready  检查数据库、Redis、Qdrant
```

这两个端点当前未在源码中提供，属于部署定制项。

## 维护模式与健康检查

升级维护模式显式放行 `/health`。因此维护期间：

- 业务请求返回 503 和 `Retry-After: 30`；
- `/health` 继续执行真实依赖探测；
- 编排系统不会仅因为应用主动维护而把进程误判为失活。

维护模式是进程内状态，多副本统一摘流仍需网关或发布系统配合。详见[升级与迁移](./upgrade)。

## OpenTelemetry

`XiHanBasicAppCoreModule` 已依赖 `XiHanObservabilityModule`，无需在 WebHost 重复声明。配置节是 `XiHan:Observability`：

```json
{
  "XiHan": {
    "Observability": {
      "Enabled": true,
      "ServiceName": "XiHan.BasicApp",
      "EnableTracing": true,
      "EnableMetrics": true,
      "SamplingRatio": 0.1,
      "ConsoleExporter": false,
      "OtlpEndpoint": "http://otel-collector:4317"
    }
  }
}
```

关键行为：

| 配置 | 行为 |
| --- | --- |
| `Enabled=false` | 不装配 OpenTelemetry SDK；框架默认值 |
| `EnableTracing=true` | 采集 ASP.NET Core、HttpClient 与框架 `ActivitySource` |
| `EnableMetrics=true` | 导出 `XiHan.Metrics`；默认值是 `false`，需显式打开 |
| `SamplingRatio` | 根链路采样比例，限制在 `0~1` |
| `ConsoleExporter=true` | 输出到控制台，仅适合开发调试 |
| `OtlpEndpoint` | OTLP Collector 地址；为空且没开控制台导出时没有数据出口 |

仓库的 Development 配置打开了 `Enabled`，但关闭控制台导出且 `OtlpEndpoint` 为空。这种配置仍会让 ASP.NET Core 创建 W3C Activity，使响应与日志使用 32 位 TraceId，但不会把链路发送到外部后端。

::: warning `EnableLogging` 当前未接线
Framework 3.10.1 的 `EnableLogging` 只有选项字段，没有 OpenTelemetry Logs 装配。应用日志仍走 Serilog；不要因为打开该字段就认为日志已发送到 OTLP。
:::

## TraceId 如何贯通

Web.Api 管线优先使用 `Activity.Current.TraceId`，没有 Activity 时回退入站 `X-Trace-Id`，最后回退 Kestrel `TraceIdentifier`。同一个值进入：

- `X-Trace-Id` 响应头；
- `ApiResponse.traceId`；
- 请求、API、操作、异常、登录与实体差异日志；
- 分布式事件的关联 ID。

线上排查时先从失败响应取 `traceId`，再到日志、审计页面和追踪后端检索。开启 OpenTelemetry 后该值与 Jaeger、Tempo 等后端中的 TraceId 可直接对应。

## 进程监控页面

`/setting/server` 由 `ServerAppService` 提供当前节点的 CPU、内存、磁盘、GPU、网络、主板和 .NET 运行时信息，权限为 `saas:server:read`。

它不是集群监控：负载均衡下每次请求可能落到不同实例。要观察集群趋势、分位耗时和跨服务链路，应把 OpenTelemetry 数据导出到 Collector 与监控后端。

## 生产检查清单

1. 直接访问 `/health`，确认三项检查名称与状态符合部署意图。
2. 不使用 Qdrant 时决定是否按配置条件移除该健康项。
3. 多实例部署确认 Redis 已启用，而不是仅依赖“进程内回退也 Healthy”的结果。
4. 区分存活与就绪语义，避免外部依赖故障触发应用重启风暴。
5. 配置 OTLP 出口；只有 `Enabled=true` 而无导出器不会产生可查询数据。
6. 设置合理采样率，开发可全采，生产按流量与成本调整。
7. 验证代理转发 `traceparent` / `X-Trace-Id`，并确保健康响应不包含异常与凭据。

## 相关页面

- [日志审计](./logging)：TraceId、审计表与脱敏
- [请求生命周期](./request-lifecycle)：TraceId 中间件与管线顺序
- [升级与迁移](./upgrade)：维护模式和 `/health` 放行
- [配置参考](../configuration#xihan-observability)：BasicApp 配置字段
- [Framework 可观测性](https://framework.docs.xihanfun.com/guide/observability)：完整 OTel、指标与诊断 API
