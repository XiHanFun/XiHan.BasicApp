// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Numbering;

/// <summary>
/// 编号格式、周期和容量计算器实现。
/// </summary>
/// <remarks>
/// 类型不持有共享可变状态，所有方法均为确定性计算，因此单例调用线程安全。
/// </remarks>
public sealed class NumberingFormatter : INumberingFormatter
{
    /// <summary>
    /// 当前运行环境支持且可跨平台保存的时区目录；延迟初始化避免未使用编号功能时扫描系统时区。
    /// </summary>
    /// <remarks><see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> 保证单例并发访问只执行一次目录构建。</remarks>
    private static readonly Lazy<IReadOnlyList<NumberingTimeZoneDescriptor>> SupportedTimeZones = new(
        CreateSupportedTimeZones,
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// 单次批量结果上限；与发号 API 上限保持一致，避免重建时产生过大内存分配。
    /// </summary>
    public const int MaximumBatchSize = 1000;

    /// <summary>
    /// 获取当前运行环境允许新规则保存的可移植时区目录。
    /// </summary>
    /// <returns>UTC 优先、其余按基础偏移和 IANA ID 稳定排序的只读目录。</returns>
    /// <remarks>目录通过线程安全的 <see cref="Lazy{T}"/> 仅构建一次，调用方不得假设不同操作系统返回完全相同的显示名称。</remarks>
    public IReadOnlyList<NumberingTimeZoneDescriptor> GetSupportedTimeZones()
    {
        return SupportedTimeZones.Value;
    }

    /// <summary>
    /// 校验日期格式、重置周期、流水位数和规则时区是否能够安全组合。
    /// </summary>
    /// <param name="dateFormat">输出编号使用的日期段格式。</param>
    /// <param name="resetCycle">流水归零周期。</param>
    /// <param name="serialLength">十进制流水位数，范围为 1 至 18。</param>
    /// <param name="timeZoneId">IANA 时区 ID 或可映射的历史 Windows 时区 ID。</param>
    /// <remarks>自动重置要求日期段能够区分周期边界，否则不同周期从 1 开始后可能产生相同编号。</remarks>
    /// <exception cref="ArgumentOutOfRangeException">日期格式、重置周期或流水位数无效。</exception>
    /// <exception cref="InvalidOperationException">时区无效，或日期格式无法安全区分重置周期。</exception>
    public void Validate(NumberingDateFormat dateFormat, NumberingResetCycle resetCycle, int serialLength, string timeZoneId)
    {
        if (!Enum.IsDefined(dateFormat))
        {
            throw new ArgumentOutOfRangeException(nameof(dateFormat), "日期格式无效。");
        }

        if (!Enum.IsDefined(resetCycle))
        {
            throw new ArgumentOutOfRangeException(nameof(resetCycle), "重置周期无效。");
        }

        _ = GetMaxValue(serialLength);
        _ = ResolveTimeZone(timeZoneId);

        // 自动重置必须让输出日期至少能区分重置边界，否则不同周期可能生成相同编号。
        var safeCombination = resetCycle switch
        {
            NumberingResetCycle.Never => true,
            NumberingResetCycle.Daily => dateFormat is NumberingDateFormat.YyyyMMdd or NumberingDateFormat.YyMMdd,
            NumberingResetCycle.Monthly => dateFormat is NumberingDateFormat.YyyyMM or NumberingDateFormat.YyMM
                or NumberingDateFormat.YyyyMMdd or NumberingDateFormat.YyMMdd,
            NumberingResetCycle.Yearly => dateFormat is NumberingDateFormat.Yyyy or NumberingDateFormat.YyyyMM
                or NumberingDateFormat.YyyyMMdd or NumberingDateFormat.YyMM or NumberingDateFormat.YyMMdd,
            _ => false
        };
        if (!safeCombination)
        {
            throw new InvalidOperationException("日期格式无法安全区分所选重置周期，可能产生重复编号。");
        }
    }

    /// <summary>
    /// 将指定时刻转换为规则时区中的本地时间。
    /// </summary>
    /// <param name="utcNow">待转换时刻；无论传入偏移为何，都会先归一为 UTC。</param>
    /// <param name="timeZoneId">IANA 时区 ID 或可映射的历史 Windows 时区 ID。</param>
    /// <returns>包含目标时区实际偏移和夏令时结果的本地时间。</returns>
    /// <exception cref="InvalidOperationException"><paramref name="timeZoneId"/> 为空或当前环境无法解析。</exception>
    public DateTimeOffset ConvertToRuleTime(DateTimeOffset utcNow, string timeZoneId)
    {
        return TimeZoneInfo.ConvertTime(utcNow.ToUniversalTime(), ResolveTimeZone(timeZoneId));
    }

    /// <summary>
    /// 根据同一个规则本地时刻计算流水周期键。
    /// </summary>
    /// <param name="localTime">已经转换到规则时区的本地时刻。</param>
    /// <param name="resetCycle">流水重置周期。</param>
    /// <returns>日、月、年周期键，或永不重置时固定的 <c>never</c>。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="resetCycle"/> 不是已定义枚举值。</exception>
    public string GetPeriodKey(DateTimeOffset localTime, NumberingResetCycle resetCycle)
    {
        return resetCycle switch
        {
            NumberingResetCycle.Never => "never",
            NumberingResetCycle.Daily => localTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            NumberingResetCycle.Monthly => localTime.ToString("yyyyMM", CultureInfo.InvariantCulture),
            NumberingResetCycle.Yearly => localTime.ToString("yyyy", CultureInfo.InvariantCulture),
            _ => throw new ArgumentOutOfRangeException(nameof(resetCycle), "重置周期无效。")
        };
    }

    /// <summary>
    /// 根据同一个规则本地时刻计算单调递增的周期序号。
    /// </summary>
    /// <param name="localTime">已经转换到规则时区的本地时刻。</param>
    /// <param name="resetCycle">流水重置周期。</param>
    /// <returns>与 <see cref="GetPeriodKey"/> 一一对应的数值序号；永不重置时固定为 0。</returns>
    /// <remarks>
    /// 序号在同一条规则内严格单调：数据库的周期翻转语句只接受更大的序号，
    /// 因此时钟落后的节点无法把规则拉回旧周期重发编号。
    /// 重置周期在首次发号后被冻结，同一条规则的序号量级不会改变；未发号规则改周期时由领域服务清空周期基线。
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="resetCycle"/> 不是已定义枚举值。</exception>
    public long GetPeriodOrdinal(DateTimeOffset localTime, NumberingResetCycle resetCycle)
    {
        return resetCycle switch
        {
            NumberingResetCycle.Never => 0L,
            NumberingResetCycle.Daily => (localTime.Year * 10000L) + (localTime.Month * 100L) + localTime.Day,
            NumberingResetCycle.Monthly => (localTime.Year * 100L) + localTime.Month,
            NumberingResetCycle.Yearly => localTime.Year,
            _ => throw new ArgumentOutOfRangeException(nameof(resetCycle), "重置周期无效。")
        };
    }

    /// <summary>
    /// 根据规则本地时刻生成编号日期段文本。
    /// </summary>
    /// <param name="localTime">已经转换到规则时区的本地时刻。</param>
    /// <param name="dateFormat">日期段格式。</param>
    /// <returns>使用固定文化格式化的日期文本；不输出日期时返回 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dateFormat"/> 不是已定义枚举值。</exception>
    public string? GetDateText(DateTimeOffset localTime, NumberingDateFormat dateFormat)
    {
        var format = dateFormat switch
        {
            NumberingDateFormat.None => null,
            NumberingDateFormat.Yyyy => "yyyy",
            NumberingDateFormat.YyyyMM => "yyyyMM",
            NumberingDateFormat.YyyyMMdd => "yyyyMMdd",
            NumberingDateFormat.YyMM => "yyMM",
            NumberingDateFormat.YyMMdd => "yyMMdd",
            NumberingDateFormat.MMdd => "MMdd",
            _ => throw new ArgumentOutOfRangeException(nameof(dateFormat), "日期格式无效。")
        };
        return format is null ? null : localTime.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 使用格式快照拼接一个完整编号。
    /// </summary>
    /// <param name="prefix">可选前缀；空白值不参与拼接。</param>
    /// <param name="separator">非空编号段之间的分隔符；<see langword="null"/> 按空字符串处理。</param>
    /// <param name="dateText">可选日期段；空白值不参与拼接。</param>
    /// <param name="serialLength">流水补零位数，范围为 1 至 18。</param>
    /// <param name="value">待格式化流水值。</param>
    /// <returns>由所有非空业务段和固定宽度流水段拼接的编号。</returns>
    /// <exception cref="ArgumentOutOfRangeException">流水位数无效，或 <paramref name="value"/> 超出可表示范围。</exception>
    public string Format(string? prefix, string separator, string? dateText, int serialLength, long value)
    {
        var maximum = GetMaxValue(serialLength);
        if (value < 1 || value > maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"流水值必须在 1 至 {maximum} 之间。");
        }

        var segments = new[]
        {
            NormalizeOptional(prefix),
            NormalizeOptional(dateText),
            value.ToString($"D{serialLength}", CultureInfo.InvariantCulture)
        }.Where(segment => !string.IsNullOrEmpty(segment));

        return string.Join(separator ?? string.Empty, segments);
    }

    /// <summary>
    /// 根据同一格式快照重建连续流水区间内的编号。
    /// </summary>
    /// <param name="prefix">永久分配记录中的前缀快照。</param>
    /// <param name="separator">永久分配记录中的分隔符快照。</param>
    /// <param name="dateText">永久分配记录中的日期段快照。</param>
    /// <param name="serialLength">永久分配记录中的流水位数快照。</param>
    /// <param name="startValue">区间起始流水值，包含边界。</param>
    /// <param name="endValue">区间结束流水值，包含边界。</param>
    /// <returns>按流水值升序排列的编号列表。</returns>
    /// <remarks>该方法用于幂等重放，最多重建 1000 个编号，避免从数据库读取后产生不可控的大内存分配。</remarks>
    /// <exception cref="ArgumentOutOfRangeException">区间无效、超过批量上限，或区间中的流水值超出格式容量。</exception>
    public IReadOnlyList<string> FormatRange(string? prefix, string separator, string? dateText, int serialLength, long startValue, long endValue)
    {
        if (startValue < 1 || endValue < startValue)
        {
            throw new ArgumentOutOfRangeException(nameof(startValue), "流水区间无效。");
        }

        var count = checked(endValue - startValue + 1);
        if (count > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(endValue), $"单次最多重建 {MaximumBatchSize} 个编号。");
        }

        var result = new List<string>((int)count);
        for (var value = startValue; value <= endValue; value++)
        {
            result.Add(Format(prefix, separator, dateText, serialLength, value));
        }

        return result;
    }

    /// <summary>
    /// 计算固定十进制位数能够表示的最大流水值。
    /// </summary>
    /// <param name="serialLength">流水位数，范围为 1 至 18。</param>
    /// <returns>由 <paramref name="serialLength"/> 个数字 9 组成的最大值。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="serialLength"/> 不在 1 至 18 之间。</exception>
    public long GetMaxValue(int serialLength)
    {
        if (serialLength is < 1 or > 18)
        {
            throw new ArgumentOutOfRangeException(nameof(serialLength), "流水位数必须在 1 至 18 之间。");
        }

        // 10^18 - 1 仍位于 Int64 正数范围内，逐位计算可避免浮点 Math.Pow 的精度风险。
        var maximum = 0L;
        for (var index = 0; index < serialLength; index++)
        {
            maximum = checked((maximum * 10) + 9);
        }

        return maximum;
    }

    /// <summary>
    /// 解析时区，并把平台差异导致的底层异常转换为稳定业务错误。
    /// </summary>
    /// <param name="timeZoneId">IANA 时区 ID 或历史 Windows 时区 ID。</param>
    /// <returns>当前操作系统可用于时间换算的时区对象。</returns>
    /// <exception cref="InvalidOperationException"><paramref name="timeZoneId"/> 为空或无法直接/转换解析。</exception>
    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new InvalidOperationException("规则时区不能为空。");
        }

        var normalizedId = timeZoneId.Trim();
        if (TryFindTimeZone(normalizedId, out var timeZone))
        {
            return timeZone;
        }

        // 数据库可能来自另一操作系统或旧版本；显式转换 Windows/IANA 标识，避免迁移后规则失效。
        var hasAlternateId = OperatingSystem.IsWindows()
            ? TimeZoneInfo.TryConvertIanaIdToWindowsId(normalizedId, out var alternateId)
            : TimeZoneInfo.TryConvertWindowsIdToIanaId(normalizedId, out alternateId);
        if (hasAlternateId && !string.IsNullOrWhiteSpace(alternateId)
            && TryFindTimeZone(alternateId, out timeZone))
        {
            return timeZone;
        }

        throw new InvalidOperationException($"规则时区“{timeZoneId}”无效。");
    }

    /// <summary>
    /// 从当前操作系统建立可解析的时区目录，并显式补入通用 UTC。
    /// </summary>
    /// <returns>只包含跨 Windows/Unix 可移植 IANA ID 的稳定排序目录。</returns>
    private static IReadOnlyList<NumberingTimeZoneDescriptor> CreateSupportedTimeZones()
    {
        var candidates = new List<(TimeZoneInfo TimeZone, string RuleTimeZoneId)>();
        foreach (var timeZone in TimeZoneInfo.GetSystemTimeZones().Append(TimeZoneInfo.Utc))
        {
            // 新规则必须可跨平台迁移：Windows 中少数已废弃且没有 IANA 映射的原生时区，
            // 以及 Unix tzdata 中尚未进入 .NET Windows 映射表的新时区，都不能作为新规则持久化值。
            // 这些标识仍可由当前平台直接解析历史数据，但不会继续作为可新选契约暴露给前端。
            if (TryGetPortableTimeZoneId(timeZone, out var ruleTimeZoneId))
            {
                candidates.Add((timeZone, ruleTimeZoneId));
            }
        }

        return candidates
            .GroupBy(item => item.RuleTimeZoneId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            // 默认值 UTC 固定在首位，其余选项按偏移和标识排列，保证前端下拉顺序稳定。
            .OrderBy(item => string.Equals(item.RuleTimeZoneId, TimeZoneInfo.Utc.Id, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(item => item.TimeZone.BaseUtcOffset)
            .ThenBy(item => item.RuleTimeZoneId, StringComparer.Ordinal)
            .Select(item => new NumberingTimeZoneDescriptor(
                item.RuleTimeZoneId,
                string.IsNullOrWhiteSpace(item.TimeZone.DisplayName) ? item.RuleTimeZoneId : item.TimeZone.DisplayName,
                checked((int)item.TimeZone.BaseUtcOffset.TotalMinutes),
                item.TimeZone.SupportsDaylightSavingTime))
            .ToArray();
    }

    /// <summary>
    /// 尝试把系统原生时区标识归一为可跨 Windows/Linux 保存的 IANA 标识。
    /// </summary>
    /// <param name="timeZone">当前操作系统提供的时区。</param>
    /// <param name="portableTimeZoneId">成功时返回 IANA 标识或通用 UTC。</param>
    /// <returns>能够形成可移植标识时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    private static bool TryGetPortableTimeZoneId(TimeZoneInfo timeZone, out string portableTimeZoneId)
    {
        if (string.Equals(timeZone.Id, TimeZoneInfo.Utc.Id, StringComparison.OrdinalIgnoreCase))
        {
            portableTimeZoneId = TimeZoneInfo.Utc.Id;
            return true;
        }

        if (!OperatingSystem.IsWindows())
        {
            // Unix 目录虽然原生使用 IANA，但 tzdata 可能比 Windows/CLDR 映射表更新；
            // 仅保留还能映射回 Windows 的交集，避免规则随数据库迁移后在 Windows 节点失效。
            portableTimeZoneId = timeZone.Id;
            return !string.IsNullOrWhiteSpace(portableTimeZoneId)
                && TimeZoneInfo.TryConvertIanaIdToWindowsId(portableTimeZoneId, out _);
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZone.Id, out var ianaId)
            && !string.IsNullOrWhiteSpace(ianaId))
        {
            portableTimeZoneId = ianaId;
            return true;
        }

        portableTimeZoneId = string.Empty;
        return false;
    }

    /// <summary>
    /// 尝试按当前系统原生规则解析时区，不向上泄漏平台相关异常。
    /// </summary>
    /// <param name="timeZoneId">当前系统原生或兼容的时区 ID。</param>
    /// <param name="timeZone">成功时返回解析后的时区对象。</param>
    /// <returns>解析成功返回 <see langword="true"/>；时区不存在或定义损坏时返回 <see langword="false"/>。</returns>
    private static bool TryFindTimeZone(string timeZoneId, out TimeZoneInfo timeZone)
    {
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            timeZone = null!;
            return false;
        }
    }

    /// <summary>
    /// 把空白可选段归一为空，避免输出无意义分隔符。
    /// </summary>
    /// <param name="value">待归一的编号段。</param>
    /// <returns>去除首尾空白的编号段；空值或纯空白返回 <see langword="null"/>。</returns>
    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
