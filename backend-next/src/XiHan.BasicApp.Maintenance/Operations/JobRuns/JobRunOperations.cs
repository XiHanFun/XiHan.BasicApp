// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Maintenance.Operations.Jobs;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedulers;

namespace XiHan.BasicApp.Maintenance.Operations.JobRuns;

/// <summary>
/// 作业的一次运行。
/// </summary>
/// <param name="Id">运行 ID。</param>
/// <param name="Job">作业名。</param>
/// <param name="TriggerName">触发器名；立即执行的是一次性触发器名。</param>
/// <param name="Trigger">怎么触发的：计划、手动或恢复。</param>
/// <param name="ScheduledAt">计划的触发时间。</param>
/// <param name="StartedAt">开始时间。</param>
/// <param name="CompletedAt">结束时间；还在运行为 <see langword="null"/>。</param>
/// <param name="DurationMs">耗时（毫秒）；还在运行为 <see langword="null"/>。</param>
/// <param name="State">运行状态。</param>
/// <param name="ErrorCode">失败的错误码。</param>
/// <param name="ErrorMessage">失败的原因。</param>
/// <param name="NodeId">执行的节点。</param>
/// <param name="CancelRequested">是否已请求取消。</param>
public sealed record JobRunResponse(
    string Id,
    string Job,
    string? TriggerName,
    JobTrigger Trigger,
    DateTimeOffset ScheduledAt,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    long? DurationMs,
    JobRunState State,
    string? ErrorCode,
    string? ErrorMessage,
    string NodeId,
    bool CancelRequested)
{
    /// <summary>
    /// 从框架的运行记录投影。
    /// </summary>
    public static JobRunResponse From(JobRunRecord run)
    {
        ArgumentNullException.ThrowIfNull(run);
        return new JobRunResponse(
            run.Id,
            run.Job.Value,
            run.TriggerName,
            run.Trigger,
            run.ScheduledAt,
            run.StartedAt,
            run.CompletedAt,
            run.CompletedAt is { } completed ? (long)(completed - run.StartedAt).TotalMilliseconds : null,
            run.State,
            run.ErrorCode,
            run.ErrorMessage,
            run.NodeId,
            run.CancelRequested);
    }
}

/// <summary>
/// 分页列出作业的运行历史。
/// </summary>
/// <remarks>
/// <c>POST /api/job-runs:list</c>。按计划时间倒序。筛选支持 <c>job</c>（<c>eq</c>）、<c>state</c>、<c>trigger</c>（<c>eq</c> / <c>in</c>）、
/// <c>scheduledAt</c>（<c>ge</c> / <c>lt</c>，ISO 8601）、<c>failed</c>（<c>eq</c>，有无错误码）；关键字匹配触发器名、节点与错误。
/// 运行历史只保留框架配置的天数（缺省 7 天）；框架的查询不给总数，这里逐批取齐符合条件的再分页。只在平台一侧。
/// </remarks>
/// <param name="scheduler">框架调度。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.JobRunsRead)]
public sealed class ListJobRuns(IJobScheduler scheduler) : IOperation<PageRequest, PageResult<JobRunResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "job-runs.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<JobRunResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        JobName? job = null;
        List<JobRunState>? states = null;
        DateTimeOffset? from = null;
        DateTimeOffset? to = null;
        var extra = new List<Func<JobRunRecord, bool>>();
        foreach (var filter in request.Filters)
        {
            var value = filter.Value ?? string.Empty;
            var accepted = (filter.Field, filter.Operator) switch
            {
                ("job", "eq") when JobName.IsValid(value) => Set(() => job = new JobName(value)),
                ("state", "eq" or "in") when JobViews.EnumsOf<JobRunState>(value) is { } parsed => Set(() => states = parsed),
                ("trigger", "eq" or "in") when JobViews.EnumsOf<JobTrigger>(value) is { } triggers => Add(extra, r => triggers.Contains(r.Trigger)),
                ("scheduledAt", "ge") when TimeOf(value) is { } start => Set(() => from = start),
                ("scheduledAt", "lt") when TimeOf(value) is { } end => Set(() => to = end),
                ("failed", "eq") when bool.TryParse(value, out var failed) => Add(extra, r => (r.ErrorCode is not null) == failed),
                _ => false,
            };
            if (!accepted)
            {
                return Error.Validation(MaintenanceErrors.QueryUnsupported, MaintenanceMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));
            }
        }

        if (request.Sorts.FirstOrDefault(static s => s.Field != "scheduledAt") is { } unsupported)
        {
            return Error.Validation(MaintenanceErrors.QueryUnsupported, MaintenanceMessages.SortUnsupported, MessageArguments.Of(("field", unsupported.Field)));
        }

        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            extra.Add(r => (r.TriggerName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || r.NodeId.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || (r.ErrorCode?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (r.ErrorMessage?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // 框架按计划时间倒序返回；逐批取到底，总数与分页都在这里算
        var matched = new List<JobRunRecord>();
        for (var skip = 0; ; skip += JobRunQuery.MaxLimit)
        {
            var batch = await scheduler.RunsAsync(new JobRunQuery { Job = job, States = states, From = from, To = to, Skip = skip, Limit = JobRunQuery.MaxLimit }, context.CancellationToken).ConfigureAwait(false);
            if (!batch.TryGetValue(out var runs))
            {
                return batch.Error!;
            }

            matched.AddRange(runs.Where(r => extra.All(predicate => predicate(r))));
            if (runs.Count < JobRunQuery.MaxLimit)
            {
                break;
            }
        }

        if (request.Sorts.Count > 0 && !request.Sorts[0].Descending)
        {
            matched.Reverse();
        }

        return new PageResult<JobRunResponse>([.. matched.Skip(request.Page.Skip).Take(request.Page.Size).Select(JobRunResponse.From)], matched.Count, request.Page);
    }

    private static bool Set(Action apply)
    {
        apply();
        return true;
    }

    private static bool Add(List<Func<JobRunRecord, bool>> predicates, Func<JobRunRecord, bool> predicate)
    {
        predicates.Add(predicate);
        return true;
    }

    private static DateTimeOffset? TimeOf(string value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var time) ? time : null;
}
