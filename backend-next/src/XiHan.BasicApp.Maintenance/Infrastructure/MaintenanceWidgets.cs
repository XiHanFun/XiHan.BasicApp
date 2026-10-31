// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Contracts.Workbench;
using XiHan.BasicApp.Maintenance.Operations.Jobs;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedulers;

namespace XiHan.BasicApp.Maintenance.Infrastructure;

/// <summary>
/// 运维包给工作台的小组件。
/// </summary>
/// <remarks>
/// 作业只在平台一侧，租户主体取不到（权限码本就是平台侧的）。
/// </remarks>
/// <param name="scheduler">作业调度器。</param>
/// <param name="clock">时钟。</param>
public sealed class MaintenanceWidgets(IJobScheduler scheduler, TimeProvider clock) : IWorkbenchWidgetProvider
{
    /// <summary>
    /// 作业执行（逐日成功与失败的运行次数）。
    /// </summary>
    public const string JobRuns = "maintenance.job-runs";

    /// <summary>
    /// 最多翻几页运行历史（每页取框架允许的上限）。
    /// </summary>
    private const int MaxPages = 20;

    /// <inheritdoc />
    public IReadOnlyList<WorkbenchWidget> Widgets { get; } =
    [
        new(JobRuns, WorkbenchWidgetKind.Chart, MaintenancePermissions.JobRunsRead),
    ];

    /// <inheritdoc />
    /// <remarks>
    /// 按计划时刻归到查看者的本地日；失败含「丢失」（节点掉线没跑完），取消与运行中不计。
    /// </remarks>
    public async ValueTask<Result<WorkbenchWidgetData>> GetAsync(string key, WorkbenchWidgetQuery query, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);
        if (!string.Equals(key, JobRuns, StringComparison.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }

        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var now = clock.GetUtcNow();
        var first = query.StartOfRange(now);
        var days = Enumerable.Range(0, query.Days).Select(offset => first.AddDays(offset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToList();
        var succeeded = new long[days.Count];
        var failed = new long[days.Count];
        var runs = new JobRunQuery { From = first, To = now, States = [JobRunState.Succeeded, JobRunState.Failed, JobRunState.Lost], Limit = JobRunQuery.MaxLimit };
        for (var page = 0; page < MaxPages; page++)
        {
            var batch = await scheduler.RunsAsync(runs with { Skip = page * JobRunQuery.MaxLimit }, context.CancellationToken).ConfigureAwait(false);
            if (!batch.TryGetValue(out var records))
            {
                return batch.Error!;
            }

            foreach (var record in records)
            {
                var index = days.IndexOf(record.ScheduledAt.ToOffset(query.Offset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                if (index < 0)
                {
                    continue;
                }

                if (record.State == JobRunState.Succeeded)
                {
                    succeeded[index]++;
                }
                else
                {
                    failed[index]++;
                }
            }

            if (records.Count < JobRunQuery.MaxLimit)
            {
                break;
            }
        }

        return new WorkbenchWidgetData(Series: new WorkbenchSeries(days, [new("succeeded", succeeded), new("failed", failed)]));
    }
}
