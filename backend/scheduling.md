来源：https://basicapp.docs.xihanfun.com/backend/scheduling

# 定时任务

本页说明作业的登记方式、内建作业、计划写法与管理操作，适用于后端开发者与运维人员。

作业的定义、触发器、运行记录、节点与租约全部由框架 `Scheduling.Database` 管理（`job_entity`、`trigger_entity`、`job_run_entity`、`scheduler_node_entity`、`scheduler_lease_entity` 五张框架表），多实例按领导租约选主、各节点抢占触发器执行，参数在 `XiHan:Scheduling`。Maintenance 包只提供管理面。

## 两种作业

| 来源 | 登记方式 | 能否删除 |
| --- | --- | --- |
| 代码登记 | 包的 `Configure` 里 `context.Services.AddJob<TJob>(name, schedule)` | 不能；可暂停、改计划、恢复代码定义 |
| 运行时登记 | `jobs.create` 经框架 `IJobScheduler.ScheduleAsync` 按已登记的作业类型新建 | 能（`jobs.delete`） |

运行时新建只能选已登记的作业类型（`jobs.types`，否则 `MAINTENANCE_CONFLICT_001`），参数须是 JSON 对象（`MAINTENANCE_VALIDATION_005`）；作业名有文法（如 `reports.daily`，首段即分组，不合规 `MAINTENANCE_VALIDATION_004`），重名给 `MAINTENANCE_CONFLICT_003`。

## 内建作业

| 作业 | 计划（UTC） | 作用 |
| --- | --- | --- |
| `auditing.retention` | `0 3 * * *`（`BasicApp:Auditing:RetentionCron`） | 按参数 `auditing.retention-days`（为空即 180 天）逐批删除平台与各租户的过期日志，再清操作审计表 |
| `openplatform.oauth-cleanup` | `30 3 * * *` | 授权码过期一天、令牌过期七天后物理删除 |
| `files.uploads.expire` | `15 * * * *` | 过期的分片上传会话：中止存储上的分片并清理记录 |
| `files.temporary.purge` | `45 * * * *` | 过了到期时间的临时文件（代码生成的产物包、导出中心的结果）：平台与各租户逐批（每批 100 条、一侧一次至多 20 批）删记录，存储里不再被引用的对象一起删 |
| `tenancy.tenants.expire` | `*/5 * * * *` | 过了到期时间、仍生效的租户标为到期并停用目录（到期时间一过登录即被拒，不等作业） |
| `chat.messages.cleanup` | `0 4 * * *` | Chat 模块开着才有：按平台参数 `chat.policy` 的保留天数（缺省 365 天）删平台与各租户的过期消息与表情回应，不删会话、成员与附件文件 |

作业的显示名与说明取包的文案 `jobs.<作业名>.title` / `.description`（作业名里的 `-` 换 `_`）。

## 计划写法

- Cron：5 到 7 段（Quartz 写法的超集），如 `0 3 * * *`、`0 0/15 * * * ?`；可另给时区（如 `Asia/Shanghai`），不给按 UTC；
- 文本写法：`cron …`、固定间隔 `every …`（如 `every 00:05:00`）、一次性 `once …`。

`jobs.validate-schedule` 只校验并列出接下来的运行时间、不改作业，前端的 Cron 构建器与预览用它；写错即 `MAINTENANCE_VALIDATION_002`。

## 管理操作（平台侧）

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `jobs.list` / `get` / `types` / `validate-schedule` | `maintenance.jobs.read` | 列表含代码与运行时作业，可按类型筛选 |
| `jobs.create` / `jobs.delete` | `maintenance.jobs.create` / `delete` | 运行时作业；删除连同触发器与等待中的运行，删除代码登记的逐项给 `MAINTENANCE_CONFLICT_004` |
| `jobs.pause` / `resume` | `maintenance.jobs.update` | 批量；暂停后不再按计划触发，立即执行仍可 |
| `jobs.trigger` | `maintenance.jobs.trigger` | 排进一个一次性的手动触发器，由任一节点执行，不等结果 |
| `jobs.reschedule` | `maintenance.jobs.reschedule` | 换计划，重试与时限按请求整体替换；作业只能有一个计划触发器（多个即 `MAINTENANCE_CONFLICT_002`），暂停状态保留。代码登记的作业改过即标为 `Overridden`，重启后代码定义没变就保留这次修改 |
| `jobs.restore` | `maintenance.jobs.reschedule` | 丢弃运行时改过的计划，按代码定义重新登记，暂停状态保留；没有代码定义的逐项 `SCHEDULING_NOTFOUND_005` |
| `job-runs.list` | `maintenance.job-runs.read` | 运行历史，框架给出总数与筛选；保留期 `XiHan:Scheduling:HistoryRetentionDays` |

## 重试与时限

`jobs.create` 与 `jobs.reschedule` 都可设：`timeoutSeconds` 是单次执行的时限，超过即取消并记失败（`SCHEDULING_TIMEOUT_001`）；`maxRetries`、`retryDelaySeconds`、`retryBackoff`（每次相同或指数退避）与 `retryMaxDelaySeconds` 是失败后的重试。代码登记的作业写在框架 `JobDefinition` 的 `Timeout` 与 `Retry` 上。不设即不限时、不重试。`jobs.list` 带这两项，`job-runs.list` 的 `retryOf` 指向重试所属的首次运行。

## 租户级作业

作业只在平台一侧管理。需要逐租户处理的作业在作业内部逐侧遍历（Runtime 的 `TenantSides`，停用、到期的租户照样算）并绑定分片；有一处失败时作业记为失败（`RUNTIME_UNAVAILABLE_006`），其余各处照常处理完。

## 编写作业

```csharp
public sealed class MyCleanupJob(/* 依赖 */) : IJob
{
    public const string Name = "mypackage.cleanup";
    public static JobSchedule Schedule { get; } = JobSchedule.FromCron("0 4 * * *");

    public async ValueTask<Result> RunAsync(JobRun run, InvocationContext context, CancellationToken cancellationToken)
    {
        // … 失败返回 Error，运行历史里记为失败
        return Result.Success();
    }
}

// <包>Feature.Configure
context.Services.AddJob<MyCleanupJob>(MyCleanupJob.Name, MyCleanupJob.Schedule);
```

作业没有调用主体，写库时操作人字段为空；要逐租户处理的，在作业里自建作用域并经 `TenantShardScope.BindTenantAsync` 绑定租户（参照 `FileUploadExpiryJob`）。再给作业名补 10 种区域的 `jobs.<作业名>.title` / `.description`。

## 下一步

- [缓存与异步](./caching) · [日志审计](./logging) · [健康与可观测性](./health-observability)
