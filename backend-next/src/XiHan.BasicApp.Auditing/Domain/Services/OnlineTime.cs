// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Auditing.Domain.Services;

/// <summary>
/// 按会话算在线时长。
/// </summary>
/// <remarks>
/// 4.x 同口径：一个会话从签发算到登出或吊销，仍有效的算到最后活动（最近一次刷新与这个会话最后一次访问取较晚的），
/// 都不超过现在；多个会话重叠的时段只算一次（4.x 逐个会话相加，同时开几个端一天能超过 24 小时），再按时区切成逐日的秒数。
/// </remarks>
public static class OnlineTime
{
    /// <summary>
    /// 把会话切成逐日的在线秒数。
    /// </summary>
    /// <param name="sessions">会话。</param>
    /// <param name="lastAccess">各会话最后一次访问的时间。</param>
    /// <param name="since">从何时起算。</param>
    /// <param name="now">现在。</param>
    /// <param name="zone">按哪个时区分天。</param>
    /// <returns>每天的在线秒数（只含大于零的天）。</returns>
    public static IReadOnlyDictionary<DateOnly, long> ByDay(IEnumerable<SessionSpan> sessions, IReadOnlyDictionary<Guid, DateTimeOffset> lastAccess, DateTimeOffset since, DateTimeOffset now, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(lastAccess);
        ArgumentNullException.ThrowIfNull(zone);
        var intervals = new List<(DateTimeOffset Start, DateTimeOffset End)>();
        foreach (var session in sessions)
        {
            var active = lastAccess.TryGetValue(session.SessionId, out var accessed) && accessed > session.LastActiveAt ? accessed : session.LastActiveAt;
            var end = session.EndedAt ?? active;
            end = end < now ? end : now;
            var start = session.StartedAt > since ? session.StartedAt : since;
            if (start < end)
            {
                intervals.Add((start, end));
            }
        }

        var days = new Dictionary<DateOnly, long>();
        foreach (var (from, to) in Merge(intervals))
        {
            var start = from;
            var end = to;
            while (start < end)
            {
                var day = DayOf(start, zone);
                var next = StartOf(day.AddDays(1), zone);
                var stop = next < end ? next : end;
                var seconds = (long)(stop - start).TotalSeconds;
                if (seconds > 0)
                {
                    days[day] = days.GetValueOrDefault(day) + seconds;
                }

                start = stop;
            }
        }

        return days;
    }

    /// <summary>
    /// 合并重叠的时段。
    /// </summary>
    private static List<(DateTimeOffset Start, DateTimeOffset End)> Merge(List<(DateTimeOffset Start, DateTimeOffset End)> intervals)
    {
        var merged = new List<(DateTimeOffset Start, DateTimeOffset End)>();
        foreach (var interval in intervals.OrderBy(static i => i.Start))
        {
            if (merged.Count > 0 && interval.Start <= merged[^1].End)
            {
                var last = merged[^1];
                merged[^1] = (last.Start, interval.End > last.End ? interval.End : last.End);
                continue;
            }

            merged.Add(interval);
        }

        return merged;
    }

    /// <summary>
    /// 某个时刻在某时区是哪一天。
    /// </summary>
    /// <param name="instant">时刻。</param>
    /// <param name="zone">时区。</param>
    /// <returns>日期。</returns>
    public static DateOnly DayOf(DateTimeOffset instant, TimeZoneInfo zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);

    /// <summary>
    /// 某一天在某时区的零点。
    /// </summary>
    /// <param name="day">日期。</param>
    /// <param name="zone">时区。</param>
    /// <returns>零点（UTC）。</returns>
    public static DateTimeOffset StartOf(DateOnly day, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        var local = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime();
    }
}
