# 定时任务

本页说明作业的登记方式、内建作业、计划写法与管理操作，适用于后端开发者与运维人员。

6.0 不再有自建的 `SysTask` 调度表：作业的定义、触发器、运行记录、节点与租约全部由框架 `Scheduling.Database`（五张框架表）管理，多实例按框架的数据库调度协调。Maintenance 包只提供管理面。

## 两种作业

| 来源 | 登记方式 | 能否删除 |
| --- | --- | --- |
| 代码登记 | 包的 `Configure` 里 `context.Services.AddJob<TJob>(name, schedule)` | 不能；可暂停、改计划、恢复代码定义 |
| 运行时登记 | `jobs.create` 经 `IJobScheduler.ScheduleAsync` 按已登记的作业类型新建 | 能（`jobs.delete`，运行中不可删） |

运行时新建只能选已登记的作业类型（`jobs.types`），参数须是 JSON 对象；作业名有文法，重名给 `MAINTENANCE_CONFLICT_003`。

## 内建作业

| 作业 | 计划（UTC） | 作用 |
| --- | --- | --- |
| `auditing.retention` | `0 3 * * *`（`BasicApp:Auditing:RetentionCron`） | 按参数 `auditing.retention-days`（没建即 180 天）逐批删除平台与各租户的过期日志，再清操作审计表 |
| `openplatform.oauth-cleanup` | `30 3 * * *` | 授权码过期一天、令牌过期七天后物理删除 |
| `files.uploads.expire` | `15 * * * *` | 过期的分片上传会话：中止存储上的分片并清理记录 |
| `tenancy.tenants.expire` | `*/5 * * * *` | 过了到期时间、仍生效的租户标为到期并停用目录（到期时间一过登录即被拒，不等作业） |

作业的显示名与说明取包的文案 `jobs.<作业名>.title` / `.description`。

## 计划写法

- Cron：5 到 7 段（Quartz 写法的超集），如 `0 3 * * *`、`0 0/15 * * * ?`；
- 固定间隔：`every [d.]hh:mm:ss`，如 `every 00:05:00`。

`jobs.validate-schedule` 只校验不改作业，前端的 Cron 构建器与预览用它。

## 管理操作（平台侧）

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `jobs.list` / `get` / `types` / `validate-schedule` | `maintenance.jobs.read` | 列表含代码与运行时作业，可按类型筛选 |
| `jobs.create` / `jobs.delete` | `maintenance.jobs.create` / `delete` | 运行时作业；删除代码登记的逐项给 `MAINTENANCE_CONFLICT_004` |
| `jobs.pause` / `resume` | `maintenance.jobs.update` | 批量；暂停后不再按计划触发，立即执行仍可 |
| `jobs.trigger` | `maintenance.jobs.trigger` | 排进一个一次性的手动触发器，由任一节点执行，不等结果 |
| `jobs.reschedule` | `maintenance.jobs.reschedule` | 只换计划（作业只能有一个计划触发器） |
| `jobs.restore` | `maintenance.jobs.reschedule` | 丢弃运行时改过的计划，按代码定义重新登记，暂停状态保留 |
| `job-runs.list` | `maintenance.job-runs.read` | 运行历史，框架给出总数与筛选 |

## 尚未支持

- 重试策略与单次执行超时：框架 `JobDefinition` 目前没有这两项（5.x 有 `maxRetryCount`、`timeoutSeconds`），已提给框架，升级锁定提交后再接。
- 租户级作业：作业只在平台一侧管理；需要逐租户处理的作业在作业内部经 `TenantShardScope` 逐个绑定租户。

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

作业没有调用主体，写库时操作人字段为空；要逐租户处理的，在作业里自建作用域并经 `TenantShardScope` 绑定租户（参照 `FileUploadExpiryJob`）。

## 下一步

- [缓存与异步](./caching) · [日志审计](./logging) · [健康与可观测性](./health-observability)
