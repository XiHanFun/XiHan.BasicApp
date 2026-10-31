// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Domain.Services;
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
/// <param name="OnlineSeconds">在线时长（秒，按会话算）。</param>
public sealed record ActivityCountsResponse(int Logins, int Accesses, int Operations, long OnlineSeconds);

/// <summary>
/// 有活动的一天。
/// </summary>
/// <param name="Date">日期（按请求的时区）。</param>
/// <param name="Operations">操作次数。</param>
/// <param name="Accesses">访问次数。</param>
/// <param name="OnlineMinutes">在线分钟数。</param>
public sealed record ActivityDayResponse(DateOnly Date, int Operations, int Accesses, int OnlineMinutes);

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
/// 本人的活跃度。
/// </summary>
/// <remarks>
/// <c>GET /api/my-activity:summary</c>（4.x <c>Profile/Activity</c>）。今天、本周、本月的登录 / 访问 / 操作次数，最近一次的时间，与近 371 天按天的操作与访问；
/// 次数直接按原始日志算，在线时长按会话算（<see cref="OnlineTime"/>，4.x 同口径；4.x 的周期摘要来自定时任务写的统计快照）。只按本人、不分平台与租户一侧；已登录即可。
/// 与 4.x 不同：按请求的时区分天（4.x 按 UTC）。
/// </remarks>
/// <param name="unitOfWork">工作单元。</param>
/// <param name="clock">时钟。</param>
/// <param name="sessions">会话时段（Identity 提供，没装即没有在线时长）。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class GetMyActivity(SqlSugarUnitOfWork unitOfWork, TimeProvider clock, IEnumerable<ISessionHistory> sessions) : IOperation<MyActivityRequest, MyActivityResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-activity.summary";

    /// <summary>
    /// 趋势覆盖的天数（含今天）。
    /// </summary>
    /// <remarks>
    /// 4.x 同。
    /// </remarks>
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

        var today = OnlineTime.DayOf(clock.GetUtcNow(), zone);
        var since = OnlineTime.StartOf(today.AddDays(-(TrendDays - 1)), zone);
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
        var accessRows = await unitOfWork.ConnectionFor(typeof(SysAccessLog)).Queryable<SysAccessLog>()
                .Where(e => e.UserId == userId && e.OccurredAt >= sinceUtc)
                .SplitTable(sinceUtc, until)
                .Select(e => new SysAccessLog { OccurredAt = e.OccurredAt, SessionId = e.SessionId })
                .ToListAsync(ct).ConfigureAwait(false);
        var accesses = accessRows.Select(static e => Instant(e.OccurredAt)).ToList();
        var subject = userId.ToString("D");
        var operations = (await unitOfWork.ConnectionFor(typeof(AuditRecord)).Queryable<AuditRecord>()
                .Where(r => r.PrincipalId == subject && r.Timestamp >= since)
                .Select(r => new AuditRecord { Timestamp = r.Timestamp })
                .ToListAsync(ct).ConfigureAwait(false))
            .Select(static r => r.Timestamp).ToList();

        // 在线时长：会话的起止与本人在每个会话里最后一次访问，切成逐日秒数（4.x 同口径）
        var spans = sessions.FirstOrDefault() is { } history ? await history.ListAsync(userId, since, ct).ConfigureAwait(false) : [];
        var lastAccess = accessRows.Where(static e => e.SessionId.HasValue)
            .GroupBy(static e => e.SessionId!.Value)
            .ToDictionary(static g => g.Key, static g => g.Max(static e => Instant(e.OccurredAt)));
        var onlineByDay = OnlineTime.ByDay(spans, lastAccess, since, clock.GetUtcNow(), zone);

        var loginDays = logins.Select(t => OnlineTime.DayOf(t, zone)).ToList();
        var accessDays = accesses.Select(t => OnlineTime.DayOf(t, zone)).ToList();
        var operationDays = operations.Select(t => OnlineTime.DayOf(t, zone)).ToList();
        ActivityCountsResponse CountFrom(DateOnly first)
            => new(loginDays.Count(d => d >= first), accessDays.Count(d => d >= first), operationDays.Count(d => d >= first), onlineByDay.Where(p => p.Key >= first).Sum(static p => p.Value));

        // 趋势里在线不满一分钟的天不单列（4.x 按分钟取整）
        var accessByDay = accessDays.GroupBy(static d => d).ToDictionary(static g => g.Key, static g => g.Count());
        var operationByDay = operationDays.GroupBy(static d => d).ToDictionary(static g => g.Key, static g => g.Count());
        var trend = accessByDay.Keys.Union(operationByDay.Keys).Union(onlineByDay.Where(static p => p.Value >= 60).Select(static p => p.Key)).Order()
            .Select(d => new ActivityDayResponse(d, operationByDay.GetValueOrDefault(d), accessByDay.GetValueOrDefault(d), (int)(onlineByDay.GetValueOrDefault(d) / 60)))
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
}
