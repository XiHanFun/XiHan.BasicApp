// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.ValueObjects;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 生效周期值对象边界测试：权限/数据范围/委托等全部授权链路的有效期判定基座。
/// </summary>
public sealed class EffectivePeriodTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// 无生效/失效时间的永久周期在任意时刻生效。
    /// </summary>
    [Fact]
    public void IsActive_AlwaysPeriod_ShouldBeActive()
    {
        Assert.True(EffectivePeriod.Always.IsActive(Now));
        Assert.True(EffectivePeriod.Always.IsActive(DateTimeOffset.MinValue));
        Assert.True(EffectivePeriod.Always.IsActive(DateTimeOffset.MaxValue));
    }

    /// <summary>
    /// 生效时间边界包含性：生效时间等于当前时刻即生效。
    /// </summary>
    [Fact]
    public void IsActive_EffectiveTimeEqualToNow_ShouldBeActive()
    {
        var period = new EffectivePeriod(Now, null);

        Assert.True(period.IsActive(Now));
    }

    /// <summary>
    /// 未到生效时间不得生效。
    /// </summary>
    [Fact]
    public void IsActive_BeforeEffectiveTime_ShouldBeInactive()
    {
        var period = new EffectivePeriod(Now.AddSeconds(1), null);

        Assert.False(period.IsActive(Now));
    }

    /// <summary>
    /// 失效时间边界排他性：失效时间等于当前时刻即失效。
    /// </summary>
    [Fact]
    public void IsActive_ExpirationTimeEqualToNow_ShouldBeInactive()
    {
        var period = new EffectivePeriod(null, Now);

        Assert.False(period.IsActive(Now));
    }

    /// <summary>
    /// 有效期内应生效。
    /// </summary>
    [Fact]
    public void IsActive_WithinPeriod_ShouldBeActive()
    {
        var period = new EffectivePeriod(Now.AddMinutes(-1), Now.AddMinutes(1));

        Assert.True(period.IsActive(Now));
    }

    /// <summary>
    /// 有效期外应失效。
    /// </summary>
    [Fact]
    public void IsActive_AfterExpirationTime_ShouldBeInactive()
    {
        var period = new EffectivePeriod(Now.AddMinutes(-10), Now.AddMinutes(-1));

        Assert.False(period.IsActive(Now));
    }

    /// <summary>
    /// 合法周期（无边界或生效早于失效）校验不抛异常。
    /// </summary>
    [Fact]
    public void EnsureValidRange_ValidPeriods_ShouldNotThrow()
    {
        new EffectivePeriod(null, null).EnsureValidRange();
        new EffectivePeriod(Now, null).EnsureValidRange();
        new EffectivePeriod(null, Now).EnsureValidRange();
        new EffectivePeriod(Now.AddMinutes(-1), Now).EnsureValidRange();
    }

    /// <summary>
    /// 失效时间早于生效时间必须抛异常。
    /// </summary>
    [Fact]
    public void EnsureValidRange_ExpirationEarlierThanEffective_ShouldThrow()
    {
        var period = new EffectivePeriod(Now, Now.AddMinutes(-1));

        Assert.Throws<InvalidOperationException>(period.EnsureValidRange);
    }

    /// <summary>
    /// 失效时间等于生效时间必须抛异常（空区间无意义）。
    /// </summary>
    [Fact]
    public void EnsureValidRange_ExpirationEqualToEffective_ShouldThrow()
    {
        var period = new EffectivePeriod(Now, Now);

        Assert.Throws<InvalidOperationException>(period.EnsureValidRange);
    }
}
