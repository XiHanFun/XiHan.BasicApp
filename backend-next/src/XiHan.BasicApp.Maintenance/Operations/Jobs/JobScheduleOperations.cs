// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedulers;

namespace XiHan.BasicApp.Maintenance.Operations.Jobs;

/// <summary>
/// <c>jobs.reschedule</c> 的请求。
/// </summary>
/// <remarks>
/// 作业名由路由值 <c>{id}</c> 绑定。
/// </remarks>
/// <param name="Id">作业名。</param>
/// <param name="Expression">新计划：Cron 表达式（5 到 7 段），或计划的文本写法（<c>cron …</c>、<c>every …</c>、<c>once …</c>）。</param>
/// <param name="TimeZone">Cron 表达式的时区（如 <c>Asia/Shanghai</c>）；不填按 UTC。</param>
/// <param name="TimeoutSeconds">单次执行的时限（秒）；不填不限。</param>
/// <param name="MaxRetries">失败后最多重试几次；不填不重试。</param>
/// <param name="RetryDelaySeconds">第一次重试前等几秒（设了重试次数才用）。</param>
/// <param name="RetryBackoff">重试的等待方式：每次相同或指数退避。</param>
/// <param name="RetryMaxDelaySeconds">指数退避的等待上限（秒）；不填只受 30 天约束。</param>
public sealed record RescheduleJobRequest(
    [Required, StringLength(JobName.MaxLength, MinimumLength = 1)] string Id,
    [Required, StringLength(256, MinimumLength = 1)] string Expression,
    [StringLength(64)] string? TimeZone = null,
    [Range(1, JobViews.MaxSeconds)] long? TimeoutSeconds = null,
    [Range(1, JobRetryPolicy.MaxRetriesLimit)] int? MaxRetries = null,
    [Range(1, JobViews.MaxSeconds)] long RetryDelaySeconds = 60,
    JobRetryBackoff RetryBackoff = JobRetryBackoff.Fixed,
    [Range(1, JobViews.MaxSeconds)] long? RetryMaxDelaySeconds = null);

/// <summary>
/// 已登记的作业类型。
/// </summary>
/// <param name="Items">作业类型的登记键，按序数排序。</param>
public sealed record JobTypesResponse(IReadOnlyList<string> Items);

/// <summary>
/// 改作业的计划、重试与执行时限。
/// </summary>
/// <remarks>
/// <c>POST /api/jobs/{id}:reschedule</c>（平台侧）。按作业现在的登记重新登记一次，换计划，重试与执行时限按请求整体替换（不填即不重试、不限时）：作业只能有一个计划触发器（立即执行与故障恢复留下的临时触发器不算、也不会被冲掉，多个即 <c>MAINTENANCE_CONFLICT_002</c>），
/// 类型须在作业类型登记里（否则 <c>MAINTENANCE_CONFLICT_001</c>），暂停状态保留。代码登记的作业改过即标为 <c>Overridden</c>，
/// 重启后代码定义没变就保留这次的修改，用 <c>jobs.restore</c> 改回代码定义。
/// </remarks>
/// <param name="scheduler">框架调度。</param>
/// <param name="types">作业类型登记。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.JobsReschedule)]
public sealed class RescheduleJob(IJobScheduler scheduler, JobTypeRegistry types) : IOperation<RescheduleJobRequest, JobResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "jobs.reschedule";

    /// <inheritdoc />
    public async ValueTask<Result<JobResponse>> HandleAsync(RescheduleJobRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        if (!JobViews.TryParse(request.Expression, request.TimeZone, out var schedule))
        {
            return Error.Validation(MaintenanceErrors.ScheduleInvalid, MaintenanceMessages.ScheduleInvalid, MessageArguments.Of(("expression", request.Expression)));
        }

        if (!JobName.IsValid(request.Id))
        {
            return JobViews.NotFound(request.Id);
        }

        var name = new JobName(request.Id);
        var found = await scheduler.StatusAsync(name, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var maybe))
        {
            return found.Error!;
        }

        if (!maybe.TryGetValue(out var status))
        {
            return JobViews.NotFound(request.Id);
        }

        // 立即执行与故障恢复留下的临时触发器不算（框架重新登记时保留它们，不会冲掉还没执行的立即执行）
        var active = status.Triggers.Where(static t => !IsTransient(t.Name)).ToList();
        if (active.Count > 1)
        {
            return Error.Conflict(MaintenanceErrors.JobTriggersMany, MaintenanceMessages.JobTriggersMany, MessageArguments.Of(("id", request.Id), ("count", active.Count)));
        }

        if (!types.Find(status.Job.JobTypeName).TryGetValue(out var jobType))
        {
            return Error.Conflict(MaintenanceErrors.JobTypeUnknown, MaintenanceMessages.JobTypeUnknown, MessageArguments.Of(("id", request.Id), ("type", status.Job.JobTypeName)));
        }

        var trigger = active.Count == 1 ? active[0].Name : JobTriggerDefinition.DefaultName;
        var definition = new JobDefinition(name, jobType)
        {
            Triggers = [new JobTriggerDefinition(trigger, schedule)],
            Data = status.Job.Data.DeepClone().AsObject(),
            AllowConcurrent = status.Job.AllowConcurrent,
            RunOnEveryInstance = status.Job.RunOnEveryInstance,
            RequestsRecovery = status.Job.RequestsRecovery,
            Retry = JobViews.RetryOf(request.MaxRetries, request.RetryDelaySeconds, request.RetryBackoff, request.RetryMaxDelaySeconds),
            Timeout = JobViews.TimeoutOf(request.TimeoutSeconds),
            Description = status.Job.Description,
        };
        var scheduled = (await scheduler.ScheduleAsync([definition], context.CancellationToken).ConfigureAwait(false))[0];
        if (scheduled.IsFailure)
        {
            return scheduled.Error!;
        }

        var updated = await scheduler.StatusAsync(name, context.CancellationToken).ConfigureAwait(false);
        return updated.TryGetValue(out var after) && after.TryGetValue(out var current) ? JobViews.From(current, context) : JobViews.NotFound(request.Id);
    }

    /// <summary>
    /// 是否框架排进的临时触发器（立即执行、故障恢复）。
    /// </summary>
    private static bool IsTransient(string trigger) =>
        trigger.StartsWith(JobSchedulerEngine.ManualTriggerPrefix, StringComparison.Ordinal) || trigger.StartsWith(JobSchedulerEngine.RecoveryTriggerPrefix, StringComparison.Ordinal);
}

/// <summary>
/// 把作业恢复成代码定义。
/// </summary>
/// <remarks>
/// <c>POST /api/jobs:restore</c>（平台侧）。只对代码登记过的作业有效：运行时改过的计划丢弃、按代码定义重新登记，暂停状态保留；
/// 没有代码定义的作业逐项报 <c>SCHEDULING_NOTFOUND_005</c>。
/// </remarks>
/// <param name="scheduler">框架调度。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.JobsReschedule)]
public sealed class RestoreJobs(IJobScheduler scheduler) : IOperation<JobIdsRequest, JobBatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "jobs.restore";

    /// <inheritdoc />
    public ValueTask<Result<JobBatchResult>> HandleAsync(JobIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return JobBatches.RunAsync(scheduler, request, context, static (s, names, ct) => s.RestoreAsync(names, ct));
    }
}

/// <summary>
/// 列出已登记的作业类型。
/// </summary>
/// <remarks>
/// <c>GET /api/jobs:types</c>（平台侧）。作业列表按类型筛选与新建运行时作业（<c>jobs.create</c>）时的候选，只读。
/// </remarks>
/// <param name="types">作业类型登记。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = MaintenancePermissions.JobsRead)]
public sealed class ListJobTypes(JobTypeRegistry types) : IOperation<JobTypesRequest, JobTypesResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "jobs.types";

    /// <inheritdoc />
    public ValueTask<Result<JobTypesResponse>> HandleAsync(JobTypesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return JobViews.RequirePlatform(context) is { } denied
            ? ValueTask.FromResult(Result.Failure<JobTypesResponse>(denied))
            : ValueTask.FromResult(Result.Success(new JobTypesResponse(types.Keys)));
    }
}

/// <summary>
/// <c>jobs.types</c> 的请求（没有参数）。
/// </summary>
public sealed record JobTypesRequest;
