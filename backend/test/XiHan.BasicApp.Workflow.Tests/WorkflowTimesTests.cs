// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Workflow.Application.Mappers;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 引擎时间换算：DTO 用带偏移的时间，未标注 Kind 的引擎时间按时钟口径还原。
/// API 对 DateTime 按 UTC 再换算到用户时区，本地时钟写下的时间直接交出去会多出一个时区偏移。
/// </summary>
public sealed class WorkflowTimesTests
{
    private static readonly DateTime Unspecified = new(2026, 10, 14, 13, 57, 38, DateTimeKind.Unspecified);

    /// <summary>
    /// 本地时钟（BasicApp 缺省）写下的时间按服务器本地时间还原。
    /// </summary>
    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public void ToOffset_LocalClock_ShouldTreatUnspecifiedAsServerLocal(DateTimeKind clockKind)
    {
        var offset = WorkflowTimes.ToOffset(Unspecified, clockKind);

        Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(DateTime.SpecifyKind(Unspecified, DateTimeKind.Local)), offset.Offset);
        Assert.Equal(Unspecified, offset.DateTime);
    }

    /// <summary>
    /// UTC 时钟写下的时间按 UTC 还原。
    /// </summary>
    [Fact]
    public void ToOffset_UtcClock_ShouldTreatUnspecifiedAsUtc()
    {
        var offset = WorkflowTimes.ToOffset(Unspecified, DateTimeKind.Utc);

        Assert.Equal(TimeSpan.Zero, offset.Offset);
        Assert.Equal(Unspecified, offset.DateTime);
    }

    /// <summary>
    /// 已标注 Kind 的时间以自身为准，不受时钟口径影响。
    /// </summary>
    [Fact]
    public void ToOffset_KindAlreadySet_ShouldKeepInstant()
    {
        var utc = DateTime.SpecifyKind(Unspecified, DateTimeKind.Utc);

        Assert.Equal(new DateTimeOffset(utc), WorkflowTimes.ToOffset(utc, DateTimeKind.Local));
    }

    /// <summary>
    /// 可空时间为空时返回空。
    /// </summary>
    [Fact]
    public void ToOffset_Null_ShouldReturnNull()
    {
        Assert.Null(WorkflowTimes.ToOffset((DateTime?)null, DateTimeKind.Local));
    }
}
