// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证按会话算的在线时长。
/// </summary>
/// <remarks>
/// 4.x 同口径：登出或吊销的算到结束，仍有效的算到最后活动（刷新与最后一次访问取较晚的），不超过现在，重叠只算一次，按时区切天。
/// </remarks>
public sealed class OnlineTimeTests
{
    private static readonly TimeZoneInfo _shanghai = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");

    /// <summary>
    /// 上海时间的某一刻。
    /// </summary>
    private static DateTimeOffset At(int day, int hour, int minute = 0) => new(2026, 10, day, hour, minute, 0, TimeSpan.FromHours(8));

    private static readonly Dictionary<Guid, DateTimeOffset> _noAccess = [];

    [Fact]
    public void EndedSessions_CountUntilTheyEnd_AndSplitAtLocalMidnight()
    {
        var sessions = new[]
        {
            new SessionSpan(Guid.NewGuid(), At(20, 8), At(20, 8), At(20, 10, 30)),
            new SessionSpan(Guid.NewGuid(), At(20, 23), At(20, 23), At(21, 1)),
        };
        var days = OnlineTime.ByDay(sessions, _noAccess, At(1, 0), At(25, 12), _shanghai);
        Assert.Equal(9000 + 3600, days[new DateOnly(2026, 10, 20)]);
        Assert.Equal(3600, days[new DateOnly(2026, 10, 21)]);
    }

    [Fact]
    public void ActiveSessions_CountUntilTheLaterOfRefreshAndLastAccess_NotPastNow()
    {
        var accessed = Guid.NewGuid();
        var refreshed = Guid.NewGuid();
        var running = Guid.NewGuid();
        var sessions = new[]
        {
            new SessionSpan(accessed, At(22, 9), At(22, 9), null),
            new SessionSpan(refreshed, At(22, 14), At(22, 15), null),
            new SessionSpan(running, At(25, 11), At(25, 11), null),
        };
        var lastAccess = new Dictionary<Guid, DateTimeOffset> { [accessed] = At(22, 9, 40), [refreshed] = At(22, 14, 30), [running] = At(25, 13) };
        var days = OnlineTime.ByDay(sessions, lastAccess, At(1, 0), At(25, 12), _shanghai);
        Assert.Equal(2400 + 3600, days[new DateOnly(2026, 10, 22)]);
        Assert.Equal(3600, days[new DateOnly(2026, 10, 25)]);
    }

    [Fact]
    public void OverlappingSessions_CountOnce()
    {
        var sessions = new[]
        {
            new SessionSpan(Guid.NewGuid(), At(23, 9), At(23, 9), At(23, 11)),
            new SessionSpan(Guid.NewGuid(), At(23, 10), At(23, 10), At(23, 12)),
            new SessionSpan(Guid.NewGuid(), At(23, 10, 30), At(23, 10, 30), At(23, 10, 45)),
        };
        var days = OnlineTime.ByDay(sessions, _noAccess, At(1, 0), At(25, 12), _shanghai);
        Assert.Equal(3 * 3600, days[new DateOnly(2026, 10, 23)]);
    }

    [Fact]
    public void SessionsStartingBeforeTheWindow_AreClipped_AndEmptyOnesAreSkipped()
    {
        var sessions = new[]
        {
            new SessionSpan(Guid.NewGuid(), At(9, 22), At(9, 22), At(10, 2)),
            new SessionSpan(Guid.NewGuid(), At(12, 8), At(12, 8), null),
        };
        var days = OnlineTime.ByDay(sessions, _noAccess, At(10, 0), At(25, 12), _shanghai);
        Assert.Equal(7200, Assert.Single(days).Value);
        Assert.Equal(new DateOnly(2026, 10, 10), days.Keys.Single());
    }
}
