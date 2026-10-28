// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 手动拨的时钟：在宿主装配前登记为 <see cref="TimeProvider"/>（框架用 TryAdd，先登记的生效）。
/// 令牌校验走真实时间，所以拨动时钟的用例只在进程内调用，不走 HTTP。
/// </summary>
/// <param name="start">起始时刻。</param>
public sealed class ManualClock(DateTimeOffset start) : TimeProvider
{
    private long _ticks = start.UtcTicks;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref _ticks), TimeSpan.Zero);

    /// <summary>
    /// 拨到指定时刻。
    /// </summary>
    public void Set(DateTimeOffset value) => Interlocked.Exchange(ref _ticks, value.UtcTicks);
}
