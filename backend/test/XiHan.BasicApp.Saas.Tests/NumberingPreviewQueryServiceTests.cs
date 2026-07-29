// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Numbering;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 业务编号单个与批量格式预览查询测试。
/// </summary>
/// <remarks>
/// 使用严格仓储替身验证预览路径保持纯计算，不读取规则、不写入流水，也不创建永久分配记录。
/// </remarks>
public sealed class NumberingPreviewQueryServiceTests
{
    /// <summary>
    /// 批量预览上限 50 应生成连续编号，并返回精确的字符串流水区间。
    /// </summary>
    [Fact]
    public async Task PreviewBatch_ShouldReturnFiftyContinuousNumbersWithoutRepositoryAccess()
    {
        var service = CreateService();

        var result = await service.PreviewNumberingBatchAsync(new NumberingBatchPreviewDto
        {
            Prefix = "PRE",
            Separator = "-",
            DateFormat = NumberingDateFormat.None,
            SerialLength = 3,
            ResetCycle = NumberingResetCycle.Never,
            TimeZoneId = "UTC",
            SampleValue = "1",
            Count = NumberingBatchPreviewDto.MaximumCount
        });

        Assert.Equal("1", result.StartValue);
        Assert.Equal("50", result.EndValue);
        Assert.Equal(NumberingBatchPreviewDto.MaximumCount, result.Numbers.Count);
        Assert.Equal("PRE-001", result.Numbers[0]);
        Assert.Equal("PRE-050", result.Numbers[^1]);
        Assert.Equal("never", result.PeriodKey);
    }

    /// <summary>
    /// 超过 50 个的批量预览必须在格式器分配结果列表前返回友好业务错误。
    /// </summary>
    [Fact]
    public async Task PreviewBatch_ShouldRejectCountAboveFifty()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<UserFriendlyException>(() => service.PreviewNumberingBatchAsync(new NumberingBatchPreviewDto
        {
            DateFormat = NumberingDateFormat.None,
            SerialLength = 4,
            ResetCycle = NumberingResetCycle.Never,
            TimeZoneId = "UTC",
            SampleValue = "1",
            Count = NumberingBatchPreviewDto.MaximumCount + 1
        }));
    }

    /// <summary>
    /// 连续区间越过固定位数容量时必须整体失败，不能返回部分预览结果。
    /// </summary>
    [Fact]
    public async Task PreviewBatch_ShouldRejectRangeThatExceedsSerialCapacity()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<UserFriendlyException>(() => service.PreviewNumberingBatchAsync(new NumberingBatchPreviewDto
        {
            DateFormat = NumberingDateFormat.None,
            SerialLength = 2,
            ResetCycle = NumberingResetCycle.Never,
            TimeZoneId = "UTC",
            SampleValue = "99",
            Count = 2
        }));
    }

    /// <summary>
    /// 创建只包含纯格式器和严格基础设施替身的查询服务。
    /// </summary>
    /// <returns>可直接执行格式预览的查询服务。</returns>
    private static NumberingRuleQueryService CreateService()
    {
        return new NumberingRuleQueryService(
            new Mock<INumberingRuleRepository>(MockBehavior.Strict).Object,
            new Mock<INumberingAllocationRepository>(MockBehavior.Strict).Object,
            new NumberingFormatter(),
            new Mock<ICurrentTenant>(MockBehavior.Strict).Object,
            new Mock<IFieldSecurityService>(MockBehavior.Strict).Object,
            TimeProvider.System);
    }
}
