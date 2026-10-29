// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity;
using XiHan.Framework.Auditing.Database.Trail;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.MyActivity;

/// <summary>
/// <c>my-activity.summary</c> 的请求。
/// </summary>
/// <param name="TimeZone">按哪个时区分天（IANA 名称，如 <c>Asia/Shanghai</c>；缺省 UTC）。</param>
public sealed record MyActivityRequest([StringLength(64)] string? TimeZone = null);

/// <summary>
/// 一段时间里的次数。
/// </summary>
/// <param name="Logins">登录（成功的）。</param>
/// <param name="Accesses">访问（接口调用）。</param>
/// <param name="Operations">操作（记审计的写操作）。</param>
public sealed record ActivityCountsResponse(int Logins, int Accesses, int Operations);

/// <summary>
/// 有活动的一天。
/// </summary>
/// <param name="Date">日期（按请求的时区）。</param>
/// <param name="Operations">操作次数。</param>
/// <param name="Accesses">访问次数。</param>
public sealed record ActivityDayResponse(DateOnly Date, int Operations, int Accesses);

/// <summary>
/// 本人的活跃度。
/// </summary>
/// <param name="Today">今天。</param>
/// <param name="ThisWeek">本周（周一起）。</param>
/// <param name="ThisMonth">本月。</param>
/// <param name="LastLoginAt">最后登录（近一年内）。</param>
/// <param name="LastAccessAt">最后访问（近一年内）。</param>
/// <param name="LastOperationAt">最后操作（近一年内）。</param>
/// <param name="Trend">近 371 天里有活动的日子（稀疏，按日期正序；4.x 同）。</param>
public sealed record MyActivityResponse(
    ActivityCountsResponse Today,
    ActivityCountsResponse ThisWeek,
    ActivityCountsResponse ThisMonth,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset? LastAccessAt,
    DateTimeOffset? LastOperationAt,
    IReadOnlyList<ActivityDayResponse> Trend);

/// <summary>
/// 本人的活跃度：<c>GET /api/my-activity:summary</c>（4.x <c>Profile/Activity</c>）。今天、本周、本月的登录 / 访问 / 操作次数，最近一次的时间，与近 371 天按天的操作与访问；
/// 都直接按原始日志算（4.x 的周期摘要来自定时任务写的统计快照）。只按本人、不分平台与租户一侧；已登录即可。
/// 与 4.x 不同：按请求的时区分天（4.x 按 UTC）；没有在线时长（6.0 不记）。
/// </summary>
/// <param name="unitOfWork">工作单元。</param>
/// <param name="clock">时钟。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class GetMyActivity(SqlSugarUnitOfWork unitOfWork, TimeProvider clock) : IOperation<MyActivityRequest, MyActivityResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-activity.summary";

    /// <summary>
    /// 趋势覆盖的天数（含今天，4.x 同）。
    /// </summary>
    public const int TrendDays = 371;

    /// <inheritdoc />
    public async ValueTask<Result<MyActivityResponse>> HandleAsync(MyActivityRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var principal = context.GetSecurity().Principal;
        if (!Guid.TryParse(principal.Id, out var userId))
        {
            return Error.NotFound(IdentityErrors.UserNotFound, IdentityMessages.UserNotFound, MessageArguments.Of(("id", principal.Id)));
        }

        var zone = TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(request.TimeZone) && !TimeZoneInfo.TryFindSystemTimeZoneById(request.TimeZone.Trim(), out zone))
        {
            return Error.Validation(AuditingErrors.TimeZoneInvalid, AuditingMessages.TimeZoneInvalid, MessageArguments.Of(("timeZone", request.TimeZone)));
        }

        var today = DayOf(clock.GetUtcNow(), zone);
        var since = StartOf(today.AddDays(-(TrendDays - 1)), zone);
        var sinceUtc = since.UtcDateTime;
        var until = clock.GetUtcNow().UtcDateTime.AddDays(1);
        var ct = context.CancellationToken;

        // 只取时间列（投影到实体，走实体映射的时间转换），条件在分表之前施加
        var logins = (await unitOfWork.ConnectionFor(typeof(SysLoginLog)).Queryable<SysLoginLog>()
                .Where(e => e.UserId == userId && e.Event == LoginEvent.Login && e.Succeeded && e.OccurredAt >= sinceUtc)
                .SplitTable(sinceUtc, until)
                .Select(e => new SysLoginLog { OccurredAt = e.OccurredAt })
                .ToListAsync(ct).ConfigureAwait(false))
            .Select(static e => Instant(e.OccurredAt)).ToList();
        var accesses = (await unitOfWork.ConnectionFor(typeof(SysAccessLog)).Queryable<SysAccessLog>()
                .Where(e => e.UserId == userId && e.OccurredAt >= sinceUtc)
                .SplitTable(sinceUtc, until)
                .Select(e => new SysAccessLog { OccurredAt = e.OccurredAt })
                .ToListAsync(ct).ConfigureAwait(false))
            .Select(static e => Instant(e.OccurredAt)).ToList();
        var subject = userId.ToString("D");
        var operations = (await unitOfWork.ConnectionFor(typeof(AuditRecord)).Queryable<AuditRecord>()
                .Where(r => r.PrincipalId == subject && r.Timestamp >= since)
                .Select(r => new AuditRecord { Timestamp = r.Timestamp })
                .ToListAsync(ct).ConfigureAwait(false))
            .Select(static r => r.Timestamp).ToList();

        var loginDays = logins.Select(t => DayOf(t, zone)).ToList();
        var accessDays = accesses.Select(t => DayOf(t, zone)).ToList();
        var operationDays = operations.Select(t => DayOf(t, zone)).ToList();
        ActivityCountsResponse CountFrom(DateOnly first)
            => new(loginDays.Count(d => d >= first), accessDays.Count(d => d >= first), operationDays.Count(d => d >= first));

        var accessByDay = accessDays.GroupBy(static d => d).ToDictionary(static g => g.Key, static g => g.Count());
        var operationByDay = operationDays.GroupBy(static d => d).ToDictionary(static g => g.Key, static g => g.Count());
        var trend = accessByDay.Keys.Union(operationByDay.Keys).Order()
            .Select(d => new ActivityDayResponse(d, operationByDay.GetValueOrDefault(d), accessByDay.GetValueOrDefault(d)))
            .ToList();
        return new MyActivityResponse(
            CountFrom(today),
            CountFrom(today.AddDays(-(((int)today.DayOfWeek + 6) % 7))),
            CountFrom(new DateOnly(today.Year, today.Month, 1)),
            logins.Count > 0 ? logins.Max() : null,
            accesses.Count > 0 ? accesses.Max() : null,
            operations.Count > 0 ? operations.Max() : null,
            trend);
    }

    private static DateTimeOffset Instant(DateTime utc) => new(DateTime.SpecifyKind(utc, DateTimeKind.Utc));

    private static DateOnly DayOf(DateTimeOffset instant, TimeZoneInfo zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);

    private static DateTimeOffset StartOf(DateOnly day, TimeZoneInfo zone)
    {
        var local = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime();
    }
}
