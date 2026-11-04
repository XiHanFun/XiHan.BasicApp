// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Infrastructure;

/// <summary>
/// <see cref="IUserActivityReader"/> 的实现
/// </summary>
/// <remarks>
/// 次数按原始日志算，在线时长按会话算（<see cref="OnlineTime"/>，与 <c>my-activity.summary</c> 同口径）；日志与会话都只看调用所在的一侧
/// </remarks>
/// <param name="unitOfWork">工作单元</param>
/// <param name="clock">时钟</param>
/// <param name="sessions">会话时段（Identity 提供，没装即没有在线时长）</param>
public sealed class UserActivityReader(SqlSugarUnitOfWork unitOfWork, TimeProvider clock, IEnumerable<ISessionHistory> sessions) : IUserActivityReader
{
    /// <inheritdoc />
    public async Task<UserActivityCounts> TodayAsync(InvocationContext context, Guid userId, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(zone);
        var side = context.GetSecurity().Tenant?.Id ?? TenantIds.Platform;
        var now = clock.GetUtcNow();
        var since = OnlineTime.StartOf(OnlineTime.DayOf(now, zone), zone);
        var from = since.UtcDateTime;
        var until = now.UtcDateTime.AddDays(1);
        var ct = context.CancellationToken;
        var logins = await unitOfWork.ConnectionFor(typeof(SysLoginLog)).Queryable<SysLoginLog>()
            .Where(e => e.TenantId == side && e.UserId == userId && e.Event == LoginEvent.Login && e.Succeeded && e.OccurredAt >= from)
            .SplitTable(from, until)
            .CountAsync(ct).ConfigureAwait(false);

        // 只取要的列（投影到实体，走实体映射的时间转换）；一个人一天的访问不多
        var accesses = await unitOfWork.ConnectionFor(typeof(SysAccessLog)).Queryable<SysAccessLog>()
            .Where(e => e.TenantId == side && e.UserId == userId && e.OccurredAt >= from)
            .SplitTable(from, until)
            .Select(e => new SysAccessLog { OccurredAt = e.OccurredAt, SessionId = e.SessionId })
            .ToListAsync(ct).ConfigureAwait(false);
        var spans = sessions.FirstOrDefault() is { } history ? await history.ListInSideAsync(userId, side, since, ct).ConfigureAwait(false) : [];
        var lastAccess = accesses.Where(static e => e.SessionId.HasValue)
            .GroupBy(static e => e.SessionId!.Value)
            .ToDictionary(static g => g.Key, static g => g.Max(static e => new DateTimeOffset(DateTime.SpecifyKind(e.OccurredAt, DateTimeKind.Utc))));
        return new UserActivityCounts(logins, accesses.Count, OnlineTime.ByDay(spans, lastAccess, since, now, zone).Values.Sum());
    }
}
