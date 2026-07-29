// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Numbering;
using XiHan.BasicApp.Saas.Domain.Permissions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 周期序号单调性与编号权限边界测试。
/// </summary>
/// <remarks>
/// 发号的并发串行化由数据库行锁承担，不再有进程内键锁可测；
/// 并发不变量的验证见 <see cref="NumberGeneratorTests"/> 中基于模拟事务与行锁的用例。
/// </remarks>
public sealed class NumberingConcurrencyAndPermissionTests
{
    /// <summary>
    /// 周期序号必须随时间严格单调递增，否则数据库的周期翻转守卫无法阻止时钟落后的节点重发编号。
    /// </summary>
    /// <param name="resetCycle">重置周期。</param>
    [Theory]
    [InlineData(NumberingResetCycle.Daily)]
    [InlineData(NumberingResetCycle.Monthly)]
    [InlineData(NumberingResetCycle.Yearly)]
    public void PeriodOrdinal_ShouldIncreaseMonotonicallyOverTime(NumberingResetCycle resetCycle)
    {
        var formatter = new NumberingFormatter();
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // 逐日推进三年，覆盖跨月、跨年与闰年边界。
        var ordinals = Enumerable.Range(0, 3 * 366)
            .Select(offset => formatter.GetPeriodOrdinal(start.AddDays(offset), resetCycle))
            .ToArray();

        Assert.Equal(ordinals.OrderBy(value => value), ordinals);
    }

    /// <summary>
    /// 周期序号与周期键必须一一对应：同键必同序，同序必同键。
    /// </summary>
    /// <remarks>两者一旦不同源，推进语句会因周期键不匹配而永远命中 0 行。</remarks>
    /// <param name="resetCycle">重置周期。</param>
    [Theory]
    [InlineData(NumberingResetCycle.Never)]
    [InlineData(NumberingResetCycle.Daily)]
    [InlineData(NumberingResetCycle.Monthly)]
    [InlineData(NumberingResetCycle.Yearly)]
    public void PeriodOrdinal_ShouldMapOneToOneWithPeriodKey(NumberingResetCycle resetCycle)
    {
        var formatter = new NumberingFormatter();
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var pairs = Enumerable.Range(0, 800)
            .Select(offset => start.AddDays(offset))
            .Select(moment => (
                Key: formatter.GetPeriodKey(moment, resetCycle),
                Ordinal: formatter.GetPeriodOrdinal(moment, resetCycle)))
            .Distinct()
            .ToArray();

        Assert.Equal(pairs.Length, pairs.Select(pair => pair.Key).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(pairs.Length, pairs.Select(pair => pair.Ordinal).Distinct().Count());
    }

    /// <summary>
    /// 全局管理权限必须是平台专属，而租户仍可获得查看、生成和私有规则维护权限。
    /// </summary>
    [Fact]
    public void Permissions_ShouldKeepOnlyGlobalManagementPlatformExclusive()
    {
        Assert.Contains(SaasPermissionCodes.Numbering.GlobalManage, SaasPermissionCodes.All);
        Assert.Contains(SaasPermissionCodes.Numbering.GlobalManage, SaasPlatformPermissions.PlatformOnlyCodes);
        Assert.False(SaasPlatformPermissions.IsTenantGrantable(SaasPermissionCodes.Numbering.GlobalManage));

        Assert.True(SaasPlatformPermissions.IsTenantGrantable(SaasPermissionCodes.Numbering.Read));
        Assert.True(SaasPlatformPermissions.IsTenantGrantable(SaasPermissionCodes.Numbering.Create));
        Assert.True(SaasPlatformPermissions.IsTenantGrantable(SaasPermissionCodes.Numbering.Generate));
        Assert.True(SaasPlatformPermissions.IsTenantGrantable(SaasPermissionCodes.Numbering.AllocationRead));
    }
}
