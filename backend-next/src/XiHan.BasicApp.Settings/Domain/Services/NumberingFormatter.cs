// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Domain.Services;

/// <summary>
/// 编号的格式、周期与容量计算。
/// </summary>
/// <remarks>
/// 纯函数，与 4.x 的 <c>NumberingFormatter</c> 同规则：号码是「前缀、日期段、补零流水」里非空的段用分隔符连起来。
/// 日期与周期都按规则时区的当地时间；周期序号单调递增，供「只前进」的翻转判断。
/// </remarks>
public static class NumberingFormatter
{
    /// <summary>
    /// 一次发号的上限。
    /// </summary>
    public const int MaxBatch = 1000;

    /// <summary>
    /// 一次预览的上限。
    /// </summary>
    public const int MaxPreview = 50;

    /// <summary>
    /// 流水位数的上限：18 位的最大值仍在 <see cref="long"/> 之内。
    /// </summary>
    public const int MaxSerialLength = 18;

    /// <summary>
    /// 日期格式能否区分重置周期的边界：不能区分时不同周期都从 1 开始，会出同号。
    /// </summary>
    public static bool IsSafe(NumberingDateFormat dateFormat, NumberingResetCycle resetCycle) => resetCycle switch
    {
        NumberingResetCycle.Never => true,
        NumberingResetCycle.Daily => dateFormat is NumberingDateFormat.YyyyMMdd or NumberingDateFormat.YyMMdd,
        NumberingResetCycle.Monthly => dateFormat is NumberingDateFormat.YyyyMM or NumberingDateFormat.YyMM or NumberingDateFormat.YyyyMMdd or NumberingDateFormat.YyMMdd,
        NumberingResetCycle.Yearly => dateFormat is NumberingDateFormat.Yyyy or NumberingDateFormat.YyyyMM or NumberingDateFormat.YyyyMMdd or NumberingDateFormat.YyMM or NumberingDateFormat.YyMMdd,
        _ => false,
    };

    /// <summary>
    /// 校验格式组合。
    /// </summary>
    /// <remarks>
    /// 日期格式区分得了重置周期、时区解析得了（流水位数的范围由请求的 DataAnnotations 管）。
    /// </remarks>
    public static Result Validate(NumberingDateFormat dateFormat, NumberingResetCycle resetCycle, string timeZoneId)
    {
        if (!IsSafe(dateFormat, resetCycle))
        {
            return Error.Validation(SettingsErrors.NumberingFormatUnsafe, SettingsMessages.NumberingFormatUnsafe, MessageArguments.Of(("dateFormat", dateFormat.ToString()), ("resetCycle", resetCycle.ToString())));
        }

        return TryResolveTimeZone(timeZoneId, out _)
            ? Result.Success()
            : Error.Validation(SettingsErrors.TimeZoneInvalid, SettingsMessages.TimeZoneInvalid, MessageArguments.Of(("timeZone", timeZoneId)));
    }

    /// <summary>
    /// 固定位数能表示的最大流水值（逐位计算，避开浮点）。
    /// </summary>
    public static long MaxValue(int serialLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(serialLength, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(serialLength, MaxSerialLength);
        var maximum = 0L;
        for (var index = 0; index < serialLength; index++)
        {
            maximum = checked((maximum * 10) + 9);
        }

        return maximum;
    }

    /// <summary>
    /// 解析时区。
    /// </summary>
    /// <remarks>
    /// IANA 或 Windows 标识都认，另一种写法也试一次（库里的值可能来自另一种操作系统）。
    /// </remarks>
    public static bool TryResolveTimeZone(string? timeZoneId, [NotNullWhen(true)] out TimeZoneInfo? timeZone)
    {
        timeZone = null;
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        var id = timeZoneId.Trim();
        if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out timeZone))
        {
            return true;
        }

        var converted = OperatingSystem.IsWindows()
            ? TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var alternate)
            : TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out alternate);
        return converted && !string.IsNullOrWhiteSpace(alternate) && TimeZoneInfo.TryFindSystemTimeZoneById(alternate, out timeZone);
    }

    /// <summary>
    /// 把时刻换成规则时区的当地时间。
    /// </summary>
    public static DateTimeOffset ToRuleTime(DateTimeOffset instant, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        return TimeZoneInfo.ConvertTime(instant.ToUniversalTime(), timeZone);
    }

    /// <summary>
    /// 周期键：<c>never</c> / <c>yyyyMMdd</c> / <c>yyyyMM</c> / <c>yyyy</c>。
    /// </summary>
    public static string PeriodKey(DateTimeOffset local, NumberingResetCycle resetCycle) => resetCycle switch
    {
        NumberingResetCycle.Never => "never",
        NumberingResetCycle.Daily => local.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
        NumberingResetCycle.Monthly => local.ToString("yyyyMM", CultureInfo.InvariantCulture),
        NumberingResetCycle.Yearly => local.ToString("yyyy", CultureInfo.InvariantCulture),
        _ => throw new ArgumentOutOfRangeException(nameof(resetCycle)),
    };

    /// <summary>
    /// 周期序号：永不重置为 0，其余是年月日拼成的数，同一规则内单调递增。
    /// </summary>
    public static long PeriodOrdinal(DateTimeOffset local, NumberingResetCycle resetCycle) => resetCycle switch
    {
        NumberingResetCycle.Never => 0L,
        NumberingResetCycle.Daily => (local.Year * 10000L) + (local.Month * 100L) + local.Day,
        NumberingResetCycle.Monthly => (local.Year * 100L) + local.Month,
        NumberingResetCycle.Yearly => local.Year,
        _ => throw new ArgumentOutOfRangeException(nameof(resetCycle)),
    };

    /// <summary>
    /// 日期段文本。
    /// </summary>
    /// <returns>日期段；不输出日期时为 <c>null</c>。</returns>
    public static string? DateText(DateTimeOffset local, NumberingDateFormat dateFormat)
    {
        var pattern = dateFormat switch
        {
            NumberingDateFormat.None => null,
            NumberingDateFormat.Yyyy => "yyyy",
            NumberingDateFormat.YyyyMM => "yyyyMM",
            NumberingDateFormat.YyyyMMdd => "yyyyMMdd",
            NumberingDateFormat.YyMM => "yyMM",
            NumberingDateFormat.YyMMdd => "yyMMdd",
            NumberingDateFormat.MMdd => "MMdd",
            _ => throw new ArgumentOutOfRangeException(nameof(dateFormat)),
        };
        return pattern is null ? null : local.ToString(pattern, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 拼一个号码。
    /// </summary>
    /// <remarks>
    /// 流水值必须在 1 到位数上限之间。
    /// </remarks>
    public static string Format(string? prefix, string separator, string? dateText, int serialLength, long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxValue(serialLength));
        string?[] segments = [prefix, dateText, value.ToString("D" + serialLength.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)];
        return string.Join(separator, segments.Where(static s => !string.IsNullOrEmpty(s)));
    }

    /// <summary>
    /// 按同一份格式拼连续区间里的号码（最多 <see cref="MaxBatch"/> 个）。
    /// </summary>
    public static IReadOnlyList<string> FormatRange(string? prefix, string separator, string? dateText, int serialLength, long startValue, long endValue)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(startValue, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(endValue, startValue);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endValue - startValue + 1, MaxBatch);
        var numbers = new List<string>((int)(endValue - startValue + 1));
        for (var value = startValue; value <= endValue; value++)
        {
            numbers.Add(Format(prefix, separator, dateText, serialLength, value));
        }

        return numbers;
    }

    /// <summary>
    /// 规则格式的示意（列表展示用），如 <c>ORD-yyyyMMdd-0000</c>。
    /// </summary>
    public static string Template(SysNumberingRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var date = rule.DateFormat switch
        {
            NumberingDateFormat.None => null,
            NumberingDateFormat.Yyyy => "yyyy",
            NumberingDateFormat.YyyyMM => "yyyyMM",
            NumberingDateFormat.YyyyMMdd => "yyyyMMdd",
            NumberingDateFormat.YyMM => "yyMM",
            NumberingDateFormat.YyMMdd => "yyMMdd",
            NumberingDateFormat.MMdd => "MMdd",
            _ => null,
        };
        string?[] segments = [rule.Prefix, date, new string('0', rule.SerialLength)];
        return string.Join(rule.Separator, segments.Where(static s => !string.IsNullOrEmpty(s)));
    }
}
