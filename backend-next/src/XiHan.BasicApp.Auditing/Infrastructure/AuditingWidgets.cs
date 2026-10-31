// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using SqlSugar;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Workbench;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Infrastructure;

/// <summary>
/// 日志包给工作台的小组件。
/// </summary>
/// <remarks>
/// 只看当前一侧的日志（与日志页同一口径），按查看者的本地日统计。
/// 按时间分布的统计先在库里按 UTC 小时分组（各数据库都支持的取年、月、日、时），再换到查看者的本地日与星期。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
/// <param name="clock">时钟。</param>
public sealed class AuditingWidgets(SqlSugarUnitOfWork unitOfWork, TimeProvider clock) : IWorkbenchWidgetProvider
{
    /// <summary>
    /// 今日登录（成功的登录次数）。
    /// </summary>
    public const string LoginsToday = "auditing.logins-today";

    /// <summary>
    /// 今日访问（请求次数）。
    /// </summary>
    public const string RequestsToday = "auditing.requests-today";

    /// <summary>
    /// 今日异常。
    /// </summary>
    public const string ExceptionsToday = "auditing.exceptions-today";

    /// <summary>
    /// 登录趋势（逐日成功与失败）。
    /// </summary>
    public const string LoginTrend = "auditing.login-trend";

    /// <summary>
    /// 登录失败原因（按错误码）。
    /// </summary>
    public const string LoginFailures = "auditing.login-failures";

    /// <summary>
    /// 访问概况（逐日请求、失败与平均耗时）。
    /// </summary>
    public const string RequestKpi = "auditing.request-kpi";

    /// <summary>
    /// 访问时段（星期与小时的热力）。
    /// </summary>
    public const string RequestHeatmap = "auditing.request-heatmap";

    /// <summary>
    /// 访问最多的操作（次数、失败与平均耗时）。
    /// </summary>
    public const string TopOperations = "auditing.top-operations";

    /// <summary>
    /// 异常类型（按异常类名）。
    /// </summary>
    public const string ExceptionTypes = "auditing.exception-types";

    /// <summary>
    /// 构成类只列前几项，其余并成一项。
    /// </summary>
    private const int SliceLimit = 6;

    /// <summary>
    /// 访问最多的操作列前几个。
    /// </summary>
    private const int OperationLimit = 10;

    /// <summary>
    /// 并在一起的其余项的键。
    /// </summary>
    private const string Others = "others";

    /// <inheritdoc />
    public IReadOnlyList<WorkbenchWidget> Widgets { get; } =
    [
        new(LoginsToday, WorkbenchWidgetKind.Metric, AuditingPermissions.LoginLogsRead),
        new(RequestsToday, WorkbenchWidgetKind.Metric, AuditingPermissions.AccessLogsRead),
        new(ExceptionsToday, WorkbenchWidgetKind.Metric, AuditingPermissions.ExceptionLogsRead),
        new(LoginTrend, WorkbenchWidgetKind.Chart, AuditingPermissions.LoginLogsRead),
        new(LoginFailures, WorkbenchWidgetKind.Chart, AuditingPermissions.LoginLogsRead),
        new(RequestKpi, WorkbenchWidgetKind.Chart, AuditingPermissions.AccessLogsRead),
        new(RequestHeatmap, WorkbenchWidgetKind.Chart, AuditingPermissions.AccessLogsRead),
        new(TopOperations, WorkbenchWidgetKind.Chart, AuditingPermissions.AccessLogsRead),
        new(ExceptionTypes, WorkbenchWidgetKind.Chart, AuditingPermissions.ExceptionLogsRead),
    ];

    /// <inheritdoc />
    public async ValueTask<Result<WorkbenchWidgetData>> GetAsync(string key, WorkbenchWidgetQuery query, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);
        var now = clock.GetUtcNow();
        var cancellationToken = context.CancellationToken;
        return key switch
        {
            LoginsToday => await TodayAsync(Scope<SysLoginLog>(context).Where(e => e.Event == LoginEvent.Login && e.Succeeded), query, now, cancellationToken).ConfigureAwait(false),
            RequestsToday => await TodayAsync(Scope<SysAccessLog>(context), query, now, cancellationToken).ConfigureAwait(false),
            ExceptionsToday => await TodayAsync(Scope<SysExceptionLog>(context), query, now, cancellationToken).ConfigureAwait(false),
            LoginTrend => await LoginTrendAsync(context, query, now, cancellationToken).ConfigureAwait(false),
            LoginFailures => await LoginFailuresAsync(context, query, now, cancellationToken).ConfigureAwait(false),
            RequestKpi => await RequestKpiAsync(context, query, now, cancellationToken).ConfigureAwait(false),
            RequestHeatmap => await RequestHeatmapAsync(context, query, now, cancellationToken).ConfigureAwait(false),
            TopOperations => await TopOperationsAsync(context, query, now, cancellationToken).ConfigureAwait(false),
            ExceptionTypes => await ExceptionTypesAsync(context, query, now, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null),
        };
    }

    /// <summary>
    /// 今天的条数，带昨天的作对比。
    /// </summary>
    private static async Task<WorkbenchWidgetData> TodayAsync<TEntity>(ISugarQueryable<TEntity> scoped, WorkbenchWidgetQuery query, DateTimeOffset now, CancellationToken cancellationToken)
        where TEntity : AuditLogRecord, new()
    {
        var today = query.StartOfToday(now).UtcDateTime;
        var yesterday = today.AddDays(-1);
        var end = now.UtcDateTime;
        var current = await scoped.Clone().Where(e => e.OccurredAt >= today && e.OccurredAt < end).SplitTable(today, end).CountAsync(cancellationToken).ConfigureAwait(false);
        var previous = await scoped.Clone().Where(e => e.OccurredAt >= yesterday && e.OccurredAt < today).SplitTable(yesterday, today).CountAsync(cancellationToken).ConfigureAwait(false);
        return new WorkbenchWidgetData(Value: current, Previous: previous);
    }

    /// <summary>
    /// 逐日的成功与失败登录。
    /// </summary>
    private async Task<WorkbenchWidgetData> LoginTrendAsync(InvocationContext context, WorkbenchWidgetQuery query, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var (from, to) = Range(query, now);
        var rows = await Scope<SysLoginLog>(context)
            .Where(e => e.Event == LoginEvent.Login && e.OccurredAt >= from && e.OccurredAt < to)
            .SplitTable(from, to)
            .MergeTable()
            .GroupBy(e => new { Year = SqlFunc.DateValue(e.OccurredAt, DateType.Year), Month = SqlFunc.DateValue(e.OccurredAt, DateType.Month), Day = SqlFunc.DateValue(e.OccurredAt, DateType.Day), Hour = SqlFunc.DateValue(e.OccurredAt, DateType.Hour) })
            .Select(e => new HourBucket
            {
                Year = SqlFunc.DateValue(e.OccurredAt, DateType.Year),
                Month = SqlFunc.DateValue(e.OccurredAt, DateType.Month),
                Day = SqlFunc.DateValue(e.OccurredAt, DateType.Day),
                Hour = SqlFunc.DateValue(e.OccurredAt, DateType.Hour),
                Count = SqlFunc.AggregateCount(e.Id),
                Failed = SqlFunc.AggregateSum(SqlFunc.IIF(e.Succeeded, 0, 1)),
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var days = Days(query, now);
        var succeeded = new long[days.Count];
        var failed = new long[days.Count];
        foreach (var row in rows)
        {
            if (DayIndex(row, query, days) is { } index)
            {
                succeeded[index] += row.Count - row.Failed;
                failed[index] += row.Failed;
            }
        }

        return new WorkbenchWidgetData(Series: new WorkbenchSeries(days, [new("succeeded", succeeded), new("failed", failed)]));
    }

    /// <summary>
    /// 失败登录按错误码的构成。
    /// </summary>
    /// <remarks>
    /// 显示名取该错误码的一条失败原因（写日志时按当时的区域）。
    /// </remarks>
    private async Task<WorkbenchWidgetData> LoginFailuresAsync(InvocationContext context, WorkbenchWidgetQuery query, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var (from, to) = Range(query, now);
        var rows = await Scope<SysLoginLog>(context)
            .Where(e => e.Event == LoginEvent.Login && !e.Succeeded && e.OccurredAt >= from && e.OccurredAt < to)
            .SplitTable(from, to)
            .MergeTable()
            .GroupBy(e => e.ErrorCode)
            .Select(e => new KeyCount { Key = e.ErrorCode, Count = SqlFunc.AggregateCount(e.Id), Label = SqlFunc.AggregateMax(e.ErrorMessage) })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new WorkbenchWidgetData(Slices: Top(rows));
    }

    /// <summary>
    /// 逐日的请求、失败与平均耗时。
    /// </summary>
    private async Task<WorkbenchWidgetData> RequestKpiAsync(InvocationContext context, WorkbenchWidgetQuery query, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var days = Days(query, now);
        var total = new long[days.Count];
        var failed = new long[days.Count];
        var duration = new long[days.Count];
        foreach (var row in await RequestHoursAsync(context, query, now, cancellationToken).ConfigureAwait(false))
        {
            if (DayIndex(row, query, days) is { } index)
            {
                total[index] += row.Count;
                failed[index] += row.Failed;
                duration[index] += row.Duration;
            }
        }

        var average = total.Select((count, index) => count == 0 ? 0 : duration[index] / count).ToArray();
        return new WorkbenchWidgetData(Series: new WorkbenchSeries(days, [new("total", total), new("failed", failed), new("average_ms", average)]));
    }

    /// <summary>
    /// 请求按本地星期与小时的分布。
    /// </summary>
    /// <remarks>
    /// 类目是 0 到 23 时，序列是星期一（<c>1</c>）到星期日（<c>7</c>）。
    /// </remarks>
    private async Task<WorkbenchWidgetData> RequestHeatmapAsync(InvocationContext context, WorkbenchWidgetQuery query, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var cells = new long[7, 24];
        foreach (var row in await RequestHoursAsync(context, query, now, cancellationToken).ConfigureAwait(false))
        {
            var local = query.Local(row.Start);
            var weekday = ((int)local.DayOfWeek + 6) % 7;
            cells[weekday, local.Hour] += row.Count;
        }

        var hours = Enumerable.Range(0, 24).Select(static h => h.ToString(CultureInfo.InvariantCulture)).ToList();
        var lines = Enumerable.Range(0, 7)
            .Select(weekday => new WorkbenchLine((weekday + 1).ToString(CultureInfo.InvariantCulture), Enumerable.Range(0, 24).Select(hour => cells[weekday, hour]).ToList()))
            .ToList();
        return new WorkbenchWidgetData(Series: new WorkbenchSeries(hours, lines));
    }

    /// <summary>
    /// 访问最多的操作。
    /// </summary>
    /// <remarks>
    /// 类目是操作 ID（按次数从多到少），序列是次数、失败次数与平均耗时（毫秒）。
    /// </remarks>
    private async Task<WorkbenchWidgetData> TopOperationsAsync(InvocationContext context, WorkbenchWidgetQuery query, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var (from, to) = Range(query, now);
        var rows = await Scope<SysAccessLog>(context)
            .Where(e => e.OccurredAt >= from && e.OccurredAt < to)
            .SplitTable(from, to)
            .MergeTable()
            .GroupBy(e => e.OperationId)
            .Select(e => new OperationBucket
            {
                OperationId = e.OperationId,
                Count = SqlFunc.AggregateCount(e.Id),
                Failed = SqlFunc.AggregateSum(SqlFunc.IIF(e.Result == AccessResult.Success, 0, 1)),
                Duration = SqlFunc.AggregateSum(e.DurationMs),
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var top = rows
            .OrderByDescending(static r => r.Count)
            .ThenBy(static r => r.OperationId, StringComparer.Ordinal)
            .Take(OperationLimit)
            .ToList();
        return new WorkbenchWidgetData(Series: new WorkbenchSeries(
            top.Select(static r => r.OperationId).ToList(),
            [
                new("count", top.Select(static r => r.Count).ToList()),
                new("failed", top.Select(static r => r.Failed).ToList()),
                new("average_ms", top.Select(static r => r.Count == 0 ? 0 : r.Duration / r.Count).ToList()),
            ]));
    }

    /// <summary>
    /// 异常按类名的构成。
    /// </summary>
    private async Task<WorkbenchWidgetData> ExceptionTypesAsync(InvocationContext context, WorkbenchWidgetQuery query, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var (from, to) = Range(query, now);
        var rows = await Scope<SysExceptionLog>(context)
            .Where(e => e.OccurredAt >= from && e.OccurredAt < to)
            .SplitTable(from, to)
            .MergeTable()
            .GroupBy(e => e.ExceptionType)
            .Select(e => new KeyCount { Key = e.ExceptionType, Count = SqlFunc.AggregateCount(e.Id) })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new WorkbenchWidgetData(Slices: Top(rows));
    }

    /// <summary>
    /// 统计区间内请求按 UTC 小时的次数、失败与总耗时。
    /// </summary>
    private async Task<List<HourBucket>> RequestHoursAsync(InvocationContext context, WorkbenchWidgetQuery query, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var (from, to) = Range(query, now);
        return await Scope<SysAccessLog>(context)
            .Where(e => e.OccurredAt >= from && e.OccurredAt < to)
            .SplitTable(from, to)
            .MergeTable()
            .GroupBy(e => new { Year = SqlFunc.DateValue(e.OccurredAt, DateType.Year), Month = SqlFunc.DateValue(e.OccurredAt, DateType.Month), Day = SqlFunc.DateValue(e.OccurredAt, DateType.Day), Hour = SqlFunc.DateValue(e.OccurredAt, DateType.Hour) })
            .Select(e => new HourBucket
            {
                Year = SqlFunc.DateValue(e.OccurredAt, DateType.Year),
                Month = SqlFunc.DateValue(e.OccurredAt, DateType.Month),
                Day = SqlFunc.DateValue(e.OccurredAt, DateType.Day),
                Hour = SqlFunc.DateValue(e.OccurredAt, DateType.Hour),
                Count = SqlFunc.AggregateCount(e.Id),
                Failed = SqlFunc.AggregateSum(SqlFunc.IIF(e.Result == AccessResult.Success, 0, 1)),
                Duration = SqlFunc.AggregateSum(e.DurationMs),
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 当前一侧的日志：租户主体是本租户，平台主体是 <c>platform</c>。
    /// </summary>
    private ISugarQueryable<TEntity> Scope<TEntity>(InvocationContext context)
        where TEntity : AuditLogRecord, new()
    {
        var tenant = context.GetSecurity().Tenant?.Id ?? TenantIds.Platform;
        return unitOfWork.ConnectionFor(typeof(TEntity)).Queryable<TEntity>().Where(e => e.TenantId == tenant);
    }

    /// <summary>
    /// 统计区间（UTC）：区间第一天的本地零点到此刻。
    /// </summary>
    private static (DateTime From, DateTime To) Range(WorkbenchWidgetQuery query, DateTimeOffset now)
        => (query.StartOfRange(now).UtcDateTime, now.UtcDateTime);

    /// <summary>
    /// 统计区间里的本地日期（<c>yyyy-MM-dd</c>），从早到晚。
    /// </summary>
    private static List<string> Days(WorkbenchWidgetQuery query, DateTimeOffset now)
    {
        var first = query.StartOfRange(now);
        return Enumerable.Range(0, query.Days).Select(offset => first.AddDays(offset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToList();
    }

    /// <summary>
    /// 一个 UTC 小时落在第几个本地日。
    /// </summary>
    /// <remarks>
    /// 落在区间外为空。
    /// </remarks>
    private static int? DayIndex(HourBucket bucket, WorkbenchWidgetQuery query, List<string> days)
    {
        var index = days.IndexOf(query.Local(bucket.Start).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        return index < 0 ? null : index;
    }

    /// <summary>
    /// 构成的前几项，其余并成 <c>others</c>。
    /// </summary>
    private static List<WorkbenchSlice> Top(List<KeyCount> rows)
    {
        var ordered = rows.OrderByDescending(static r => r.Count).ThenBy(static r => r.Key, StringComparer.Ordinal).ToList();
        var slices = ordered.Take(SliceLimit).Select(static r => new WorkbenchSlice(string.IsNullOrEmpty(r.Key) ? Others : r.Key, r.Count, r.Label)).ToList();
        var rest = ordered.Skip(SliceLimit).Sum(static r => r.Count);
        if (rest > 0)
        {
            slices.Add(new WorkbenchSlice(Others, rest));
        }

        return slices;
    }

    /// <summary>
    /// 一个 UTC 小时的统计。
    /// </summary>
    private sealed class HourBucket
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }

        public int Hour { get; set; }

        public long Count { get; set; }

        public long Failed { get; set; }

        public long Duration { get; set; }

        public DateTime Start => new(Year, Month, Day, Hour, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// 一个键的条数。
    /// </summary>
    private sealed class KeyCount
    {
        public string? Key { get; set; }

        public long Count { get; set; }

        public string? Label { get; set; }
    }

    /// <summary>
    /// 一个操作的统计。
    /// </summary>
    private sealed class OperationBucket
    {
        public string OperationId { get; set; } = string.Empty;

        public long Count { get; set; }

        public long Failed { get; set; }

        public long Duration { get; set; }
    }
}
