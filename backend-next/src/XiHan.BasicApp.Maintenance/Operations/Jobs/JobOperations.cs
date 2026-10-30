// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedulers;
using XiHan.Framework.Scheduling.Abstractions.Schedules;

namespace XiHan.BasicApp.Maintenance.Operations.Jobs;

/// <summary>
/// 分页列出作业。
/// </summary>
/// <remarks>
/// <c>POST /api/jobs:list</c>。代码登记与运行时登记的作业都在（框架调度的全部作业），按作业名排序。
/// 关键字匹配作业名、显示名与说明；筛选支持 <c>paused</c>、<c>running</c>（<c>eq</c>）、<c>source</c>、<c>kind</c>（<c>eq</c> / <c>in</c>，<c>kind</c> 看任一触发器）；
/// 排序支持 <c>name</c>、<c>title</c>、<c>nextRunAt</c>、<c>lastRunAt</c>、<c>updatedAt</c>。作业不多，在内存里筛选与分页。只在平台一侧。
/// </remarks>
/// <param name="scheduler">框架调度。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.JobsRead)]
public sealed class ListJobs(IJobScheduler scheduler) : IOperation<PageRequest, PageResult<JobResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "jobs.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<JobResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var statuses = await scheduler.StatusesAsync(context.CancellationToken).ConfigureAwait(false);
        if (!statuses.TryGetValue(out var all))
        {
            return statuses.Error!;
        }

        IEnumerable<JobResponse> jobs = all.Select(s => JobViews.From(s, context)).ToList();
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            jobs = jobs.Where(j => j.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) || j.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) || (j.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        foreach (var filter in request.Filters)
        {
            var value = filter.Value ?? string.Empty;
            Func<JobResponse, bool>? predicate = (filter.Field, filter.Operator) switch
            {
                ("paused", "eq") when bool.TryParse(value, out var paused) => j => j.Paused == paused,
                ("running", "eq") when bool.TryParse(value, out var running) => j => j.RunningCount > 0 == running,
                ("source", "eq" or "in") when JobViews.EnumsOf<JobSource>(value) is { } sources => j => sources.Contains(j.Source),
                ("kind", "eq" or "in") when JobViews.EnumsOf<JobScheduleKind>(value) is { } kinds => j => j.Triggers.Any(t => kinds.Contains(t.Kind)),
                _ => null,
            };
            if (predicate is null)
            {
                return Error.Validation(MaintenanceErrors.QueryUnsupported, MaintenanceMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));
            }

            jobs = jobs.Where(predicate);
        }

        IOrderedEnumerable<JobResponse>? ordered = null;
        foreach (var sort in request.Sorts)
        {
            Func<JobResponse, object?>? key = sort.Field switch
            {
                "name" => static j => j.Name,
                "title" => static j => j.Title,
                "nextRunAt" => static j => j.NextRunAt,
                "lastRunAt" => static j => j.Last?.StartedAt,
                "updatedAt" => static j => j.UpdatedAt,
                _ => null,
            };
            if (key is null)
            {
                return Error.Validation(MaintenanceErrors.QueryUnsupported, MaintenanceMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            ordered = ordered is null
                ? sort.Descending ? jobs.OrderByDescending(key) : jobs.OrderBy(key)
                : sort.Descending ? ordered.ThenByDescending(key) : ordered.ThenBy(key);
        }

        var list = (ordered ?? jobs.OrderBy(static j => j.Name, StringComparer.Ordinal)).ToList();
        return new PageResult<JobResponse>([.. list.Skip(request.Page.Skip).Take(request.Page.Size)], list.Count, request.Page);
    }
}

/// <summary>
/// 取一个作业。
/// </summary>
/// <remarks>
/// <c>GET /api/jobs/{id}</c>，<c>id</c> 是作业名。只在平台一侧。
/// </remarks>
/// <param name="scheduler">框架调度。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.JobsRead)]
public sealed class GetJob(IJobScheduler scheduler) : IOperation<JobIdRequest, JobResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "jobs.get";

    /// <inheritdoc />
    public async ValueTask<Result<JobResponse>> HandleAsync(JobIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        if (!JobName.IsValid(request.Id))
        {
            return JobViews.NotFound(request.Id);
        }

        var status = await scheduler.StatusAsync(new JobName(request.Id), context.CancellationToken).ConfigureAwait(false);
        if (!status.TryGetValue(out var found))
        {
            return status.Error!;
        }

        return found.TryGetValue(out var job) ? JobViews.From(job, context) : JobViews.NotFound(request.Id);
    }
}

/// <summary>
/// 暂停作业。
/// </summary>
/// <remarks>
/// <c>POST /api/jobs:pause</c>。暂停的作业不再按计划触发（立即执行仍可以），正在运行的不受影响；已暂停的照样算成功。
/// 代码登记的作业重启之后仍是暂停的。只在平台一侧。
/// </remarks>
/// <param name="scheduler">框架调度。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.JobsUpdate)]
public sealed class PauseJobs(IJobScheduler scheduler) : IOperation<JobIdsRequest, JobBatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "jobs.pause";

    /// <inheritdoc />
    public ValueTask<Result<JobBatchResult>> HandleAsync(JobIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return JobBatches.RunAsync(scheduler, request, context, static (s, names, ct) => s.PauseAsync(names, ct));
    }
}

/// <summary>
/// 恢复作业。
/// </summary>
/// <remarks>
/// <c>POST /api/jobs:resume</c>。恢复后按计划从下一次触发时间接着跑；没暂停的照样算成功。只在平台一侧。
/// </remarks>
/// <param name="scheduler">框架调度。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.JobsUpdate)]
public sealed class ResumeJobs(IJobScheduler scheduler) : IOperation<JobIdsRequest, JobBatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "jobs.resume";

    /// <inheritdoc />
    public ValueTask<Result<JobBatchResult>> HandleAsync(JobIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return JobBatches.RunAsync(scheduler, request, context, static (s, names, ct) => s.ResumeAsync(names, ct));
    }
}

/// <summary>
/// 立即执行一次作业。
/// </summary>
/// <remarks>
/// <c>POST /api/jobs/{id}:trigger</c>。排进一个一次性的手动触发器、由任一节点执行，不等结果，返回触发器名（运行历史里看结果）；暂停的作业也可以立即执行。只在平台一侧。
/// </remarks>
/// <param name="scheduler">框架调度。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.JobsTrigger)]
public sealed class TriggerJob(IJobScheduler scheduler) : IOperation<JobIdRequest, JobQueuedResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "jobs.trigger";

    /// <inheritdoc />
    public async ValueTask<Result<JobQueuedResponse>> HandleAsync(JobIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        if (!JobName.IsValid(request.Id))
        {
            return JobViews.NotFound(request.Id);
        }

        var name = new JobName(request.Id);
        var status = await scheduler.StatusAsync(name, context.CancellationToken).ConfigureAwait(false);
        if (!status.TryGetValue(out var found))
        {
            return status.Error!;
        }

        if (!found.HasValue)
        {
            return JobViews.NotFound(request.Id);
        }

        var queued = await scheduler.EnqueueAsync(name, null, context.CancellationToken).ConfigureAwait(false);
        return queued.TryGetValue(out var trigger) ? new JobQueuedResponse(trigger) : queued.Error!;
    }
}

/// <summary>
/// 校验计划并列出接下来的运行时间。
/// </summary>
/// <remarks>
/// <c>GET /api/jobs:validate-schedule</c>。接受 Cron 表达式（5 到 7 段，Quartz 写法的超集）或计划的文本写法；写错即 <see cref="MaintenanceErrors.ScheduleInvalid"/>。不改任何作业。只在平台一侧。
/// </remarks>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = MaintenancePermissions.JobsRead)]
public sealed class ValidateSchedule(TimeProvider clock) : IOperation<ValidateScheduleRequest, ScheduleCheckResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "jobs.validate-schedule";

    /// <inheritdoc />
    public ValueTask<Result<ScheduleCheckResponse>> HandleAsync(ValidateScheduleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return ValueTask.FromResult(Result.Failure<ScheduleCheckResponse>(denied));
        }

        JobSchedule? schedule;
        try
        {
            if (!JobSchedule.TryParse(JobViews.TextOf(request.Expression, request.TimeZone), out schedule))
            {
                schedule = null;
            }
        }
        catch (TimeZoneNotFoundException)
        {
            schedule = null;
        }

        if (schedule is null)
        {
            return ValueTask.FromResult(Result.Failure<ScheduleCheckResponse>(Error.Validation(MaintenanceErrors.ScheduleInvalid, MaintenanceMessages.ScheduleInvalid, MessageArguments.Of(("expression", request.Expression)))));
        }

        var runs = new List<DateTimeOffset>();
        var after = clock.GetUtcNow();
        while (runs.Count < request.Count && schedule.NextRun(after).TryGetValue(out var next))
        {
            runs.Add(next);
            after = next;
        }

        return ValueTask.FromResult(Result.Success(new ScheduleCheckResponse(schedule.ToString(), schedule.Kind, runs)));
    }
}

/// <summary>
/// 按作业名的批量操作。
/// </summary>
internal static class JobBatches
{
    /// <summary>
    /// 先认出存在的作业，再对它们执行一次批量调用。
    /// </summary>
    public static async ValueTask<Result<JobBatchResult>> RunAsync(IJobScheduler scheduler, JobIdsRequest request, InvocationContext context, Func<IJobScheduler, IReadOnlyList<JobName>, CancellationToken, ValueTask<IReadOnlyList<Result>>> apply)
    {
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var statuses = await scheduler.StatusesAsync(context.CancellationToken).ConfigureAwait(false);
        if (!statuses.TryGetValue(out var all))
        {
            return statuses.Error!;
        }

        var known = all.Select(static s => s.Name.Value).ToHashSet(StringComparer.Ordinal);
        var failed = new List<JobBatchFailure>();
        var names = new List<JobName>();
        foreach (var id in request.Ids.Distinct(StringComparer.Ordinal))
        {
            if (known.Contains(id))
            {
                names.Add(new JobName(id));
            }
            else
            {
                failed.Add(new JobBatchFailure(id, MaintenanceErrors.JobNotFound));
            }
        }

        var succeeded = new List<string>();
        if (names.Count > 0)
        {
            var results = await apply(scheduler, names, context.CancellationToken).ConfigureAwait(false);
            for (var i = 0; i < names.Count; i++)
            {
                if (results[i].IsSuccess)
                {
                    succeeded.Add(names[i].Value);
                }
                else
                {
                    failed.Add(new JobBatchFailure(names[i].Value, results[i].Error!.Code));
                }
            }
        }

        return new JobBatchResult(succeeded, failed);
    }
}
