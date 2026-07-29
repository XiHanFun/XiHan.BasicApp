// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Numbering;
using Xunit.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 业务编号格式、周期、时区和容量边界测试。
/// </summary>
public sealed class NumberingFormatterTests
{
    private readonly NumberingFormatter _formatter = new();
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// 初始化格式器测试并接收逐项验证摘要输出器。
    /// </summary>
    /// <param name="output">xUnit 测试输出器。</param>
    public NumberingFormatterTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 非空格式段应按配置分隔符拼接，空白段不能产生多余分隔符。
    /// </summary>
    [Fact]
    public void Format_ShouldJoinOnlyNonEmptySegments()
    {
        Assert.Equal("ORD-20260727-0001", _formatter.Format(" ORD ", "-", "20260727", 4, 1));
        Assert.Equal("0001", _formatter.Format(null, "-", null, 4, 1));
        Assert.Equal("ORD/0001", _formatter.Format("ORD", "/", null, 4, 1));
    }

    /// <summary>
    /// 十八位流水必须使用整数计算并完整保留，不得发生浮点精度损失。
    /// </summary>
    [Fact]
    public void GetMaxValue_ShouldSupportEighteenDigitsExactly()
    {
        const long expected = 999_999_999_999_999_999L;

        Assert.Equal(expected, _formatter.GetMaxValue(18));
        Assert.Equal(expected.ToString(), _formatter.Format(null, string.Empty, null, 18, expected));
    }

    /// <summary>
    /// 批量格式重建允许 1000 个结果，超过上限必须在分配内存前失败。
    /// </summary>
    [Fact]
    public void FormatRange_ShouldEnforceMaximumBatchSize()
    {
        var numbers = _formatter.FormatRange("B", "-", null, 4, 1, 1000);

        Assert.Equal(1000, numbers.Count);
        Assert.Equal("B-0001", numbers[0]);
        Assert.Equal("B-1000", numbers[^1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _formatter.FormatRange("B", "-", null, 4, 1, 1001));
    }

    /// <summary>
    /// 自动重置只有在日期段能够区分重置边界时才安全。
    /// </summary>
    /// <param name="dateFormat">日期格式。</param>
    /// <param name="resetCycle">重置周期。</param>
    [Theory]
    [InlineData(NumberingDateFormat.YyyyMMdd, NumberingResetCycle.Daily)]
    [InlineData(NumberingDateFormat.YyMMdd, NumberingResetCycle.Daily)]
    [InlineData(NumberingDateFormat.YyyyMM, NumberingResetCycle.Monthly)]
    [InlineData(NumberingDateFormat.Yyyy, NumberingResetCycle.Yearly)]
    [InlineData(NumberingDateFormat.None, NumberingResetCycle.Never)]
    public void Validate_ShouldAcceptSafeDateAndResetCombinations(
        NumberingDateFormat dateFormat,
        NumberingResetCycle resetCycle)
    {
        _formatter.Validate(dateFormat, resetCycle, 4, "UTC");
    }

    /// <summary>
    /// 无法区分周期的日期格式必须被拒绝，避免重置后生成历史重复编号。
    /// </summary>
    /// <param name="dateFormat">日期格式。</param>
    /// <param name="resetCycle">重置周期。</param>
    [Theory]
    [InlineData(NumberingDateFormat.None, NumberingResetCycle.Daily)]
    [InlineData(NumberingDateFormat.Yyyy, NumberingResetCycle.Monthly)]
    [InlineData(NumberingDateFormat.MMdd, NumberingResetCycle.Yearly)]
    public void Validate_ShouldRejectUnsafeDateAndResetCombinations(
        NumberingDateFormat dateFormat,
        NumberingResetCycle resetCycle)
    {
        Assert.Throws<InvalidOperationException>(() => _formatter.Validate(dateFormat, resetCycle, 4, "UTC"));
    }

    /// <summary>
    /// 周期边界必须按规则时区计算，而不是直接使用服务器 UTC 日期。
    /// </summary>
    [Fact]
    public void ConvertToRuleTime_ShouldApplyRuleTimeZoneBeforePeriodCalculation()
    {
        var utc = new DateTimeOffset(2026, 1, 31, 16, 30, 0, TimeSpan.Zero);

        var local = _formatter.ConvertToRuleTime(utc, "Asia/Shanghai");
        var legacyWindowsLocal = _formatter.ConvertToRuleTime(utc, "China Standard Time");

        Assert.Equal(2, local.Month);
        Assert.Equal(local, legacyWindowsLocal);
        Assert.Equal("202602", _formatter.GetPeriodKey(local, NumberingResetCycle.Monthly));
        Assert.Equal("20260201", _formatter.GetDateText(local, NumberingDateFormat.YyyyMMdd));
    }

    /// <summary>
    /// 后端暴露的每一个时区选项都必须可解析，并在冬夏样例中得到与 TimeZoneInfo 相同的本地日期和编号。
    /// </summary>
    [Fact]
    public void GetSupportedTimeZones_ShouldValidateAndFormatEveryOptionCorrectly()
    {
        var options = _formatter.GetSupportedTimeZones();
        var utcSamples = new[]
        {
            new DateTimeOffset(2026, 1, 15, 23, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 15, 0, 30, 0, TimeSpan.Zero)
        };

        Assert.NotEmpty(options);
        Assert.Equal("UTC", options[0].Id, ignoreCase: true);
        Assert.Equal(options.Count, options.Select(option => option.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        foreach (var option in options)
        {
            // 下拉目录属于持久化规则契约，必须使用可跨 Windows/Linux 保存的 IANA 标识；
            // 同时保留映射得到的 Windows ID，用于逐项验证历史规则兼容路径，而不是只验证单个示例。
            Assert.True(
                TimeZoneInfo.TryConvertIanaIdToWindowsId(option.Id, out var windowsTimeZoneId)
                && !string.IsNullOrWhiteSpace(windowsTimeZoneId),
                $"时区选项“{option.Id}”不是可移植的 IANA 标识。");

            var systemTimeZone = ResolveSystemTimeZone(option.Id);
            var legacyWindowsTimeZone = ResolveLegacyWindowsTimeZone(windowsTimeZoneId!);
            Assert.Equal(systemTimeZone.DisplayName, option.DisplayName);
            Assert.Equal((int)systemTimeZone.BaseUtcOffset.TotalMinutes, option.BaseUtcOffsetMinutes);
            Assert.Equal(systemTimeZone.SupportsDaylightSavingTime, option.SupportsDaylightSavingTime);

            // 与创建、更新和预览使用完全相同的校验入口，确保下拉项能够提交到后端。
            _formatter.Validate(NumberingDateFormat.YyyyMMdd, NumberingResetCycle.Daily, 4, option.Id);
            _formatter.Validate(NumberingDateFormat.YyyyMMdd, NumberingResetCycle.Daily, 4, windowsTimeZoneId!);

            foreach (var utcSample in utcSamples)
            {
                var expectedLocalTime = TimeZoneInfo.ConvertTime(utcSample, systemTimeZone);
                var actualLocalTime = _formatter.ConvertToRuleTime(utcSample, option.Id);
                var expectedLegacyWindowsLocalTime = TimeZoneInfo.ConvertTime(utcSample, legacyWindowsTimeZone);
                var actualLegacyWindowsLocalTime = _formatter.ConvertToRuleTime(utcSample, windowsTimeZoneId!);
                var expectedDateText = expectedLocalTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

                Assert.Equal(expectedLocalTime, actualLocalTime);
                Assert.Equal(expectedLegacyWindowsLocalTime, actualLegacyWindowsLocalTime);
                Assert.Equal(expectedDateText, _formatter.GetDateText(actualLocalTime, NumberingDateFormat.YyyyMMdd));
                Assert.Equal(expectedDateText, _formatter.GetPeriodKey(actualLocalTime, NumberingResetCycle.Daily));
                Assert.Equal(
                    $"TZ-{expectedDateText}-0001",
                    _formatter.Format("TZ", "-", expectedDateText, 4, 1));
            }
        }

        _output.WriteLine(
            "已逐项验证 {0} 个后端时区选项，共完成 {1} 次 IANA/Windows 冬夏时刻换算与编号断言。",
            options.Count,
            options.Count * utcSamples.Length * 2);
    }

    /// <summary>
    /// 使用与部署平台相反的标识转换路径解析测试选项，独立验证目录中的 IANA ID 可在当前系统落地。
    /// </summary>
    private static TimeZoneInfo ResolveSystemTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException) when (
            OperatingSystem.IsWindows()
            && TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }
    }

    /// <summary>
    /// 独立解析历史 Windows 时区 ID：Windows 使用原生目录，Unix 显式转换为系统 IANA ID。
    /// </summary>
    private static TimeZoneInfo ResolveLegacyWindowsTimeZone(string windowsTimeZoneId)
    {
        if (OperatingSystem.IsWindows())
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZoneId);
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(windowsTimeZoneId, out var ianaId)
            && !string.IsNullOrWhiteSpace(ianaId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ianaId);
        }

        throw new InvalidOperationException($"Windows 时区“{windowsTimeZoneId}”无法映射到当前 Unix 运行环境。");
    }

    /// <summary>
    /// 流水值超出固定位数容量时必须拒绝格式化。
    /// </summary>
    [Fact]
    public void Format_ShouldRejectSerialOverflow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _formatter.Format("ORD", "-", null, 2, 100));
    }
}
