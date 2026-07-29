// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Numbering;

/// <summary>
/// 编号格式、周期和容量计算器；纯函数式实现可安全注册为单例。
/// </summary>
/// <remarks>
/// 本接口不读取数据库也不推进流水。调用方应先用规则时区转换 UTC 时刻，再基于同一个本地时刻计算日期段和周期键，
/// 避免跨周期边界时得到不一致的编号快照。
/// </remarks>
public interface INumberingFormatter
{
    /// <summary>
    /// 获取后端运行环境实际支持的编号规则时区。
    /// </summary>
    /// <returns>按 UTC、基础偏移和标识稳定排序的只读时区列表。</returns>
    /// <remarks>返回列表仅包含可在 Windows 与 Unix 之间安全映射的新规则保存值，并由实现以线程安全方式缓存。</remarks>
    IReadOnlyList<NumberingTimeZoneDescriptor> GetSupportedTimeZones();

    /// <summary>
    /// 校验格式配置、日期格式与重置周期组合以及时区标识。
    /// </summary>
    /// <param name="dateFormat">日期格式。</param>
    /// <param name="resetCycle">重置周期。</param>
    /// <param name="serialLength">流水位数。</param>
    /// <param name="timeZoneId">IANA 时区标识或可由运行环境映射的历史 Windows 时区标识。</param>
    /// <remarks>自动重置要求日期段能够区分对应周期，否则不同周期会重新从 1 开始并生成相同文本。</remarks>
    /// <exception cref="ArgumentOutOfRangeException">流水位数或枚举值无效。</exception>
    /// <exception cref="InvalidOperationException">日期/周期组合或时区无效。</exception>
    void Validate(NumberingDateFormat dateFormat, NumberingResetCycle resetCycle, int serialLength, string timeZoneId);

    /// <summary>
    /// 取得指定 UTC 时刻在规则时区中的本地时间。
    /// </summary>
    /// <param name="utcNow">待转换时刻；实现会先归一为 UTC。</param>
    /// <param name="timeZoneId">IANA 时区标识或可由运行环境映射的历史 Windows 时区标识。</param>
    /// <returns>规则本地时间。</returns>
    /// <remarks>返回值保留目标时区在该时刻的实际偏移，包括夏令时变化。</remarks>
    /// <exception cref="InvalidOperationException">时区标识无效。</exception>
    DateTimeOffset ConvertToRuleTime(DateTimeOffset utcNow, string timeZoneId);

    /// <summary>
    /// 根据规则本地时间计算稳定的周期键。
    /// </summary>
    /// <param name="localTime">规则本地时间。</param>
    /// <param name="resetCycle">重置周期。</param>
    /// <returns>用于判断流水是否跨周期重置的稳定周期键；永不重置时固定为 <c>never</c>。</returns>
    /// <exception cref="ArgumentOutOfRangeException">重置周期无效。</exception>
    string GetPeriodKey(DateTimeOffset localTime, NumberingResetCycle resetCycle);

    /// <summary>
    /// 根据规则本地时间生成日期段文本。
    /// </summary>
    /// <param name="localTime">规则本地时间。</param>
    /// <param name="dateFormat">日期格式。</param>
    /// <returns>使用固定文化格式化的日期文本；不输出日期时返回 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期格式无效。</exception>
    string? GetDateText(DateTimeOffset localTime, NumberingDateFormat dateFormat);

    /// <summary>
    /// 使用格式快照拼接一个编号。
    /// </summary>
    /// <param name="prefix">可选前缀。</param>
    /// <param name="separator">非空编号段之间的分隔符；<see langword="null"/> 按空字符串处理。</param>
    /// <param name="dateText">可选日期文本。</param>
    /// <param name="serialLength">流水位数。</param>
    /// <param name="value">流水值。</param>
    /// <returns>由所有非空段和补零流水段拼接出的完整编号。</returns>
    /// <remarks>空白前缀和日期段不会产生多余分隔符；本方法只格式化，不检查数据库中是否已经分配该值。</remarks>
    /// <exception cref="ArgumentOutOfRangeException">位数或流水值无效。</exception>
    string Format(string? prefix, string separator, string? dateText, int serialLength, long value);

    /// <summary>
    /// 重建一个连续流水区间内的编号。
    /// </summary>
    /// <param name="prefix">可选前缀。</param>
    /// <param name="separator">非空编号段之间的分隔符；<see langword="null"/> 按空字符串处理。</param>
    /// <param name="dateText">可选日期文本。</param>
    /// <param name="serialLength">流水位数。</param>
    /// <param name="startValue">起始流水值（含）。</param>
    /// <param name="endValue">结束流水值（含）。</param>
    /// <returns>按流水值升序重建的编号列表。</returns>
    /// <remarks>主要用于根据永久格式快照重放幂等结果，最多返回 1000 项以限制内存占用。</remarks>
    /// <exception cref="ArgumentOutOfRangeException">区间无效或超过 1000 个结果。</exception>
    IReadOnlyList<string> FormatRange(string? prefix, string separator, string? dateText, int serialLength, long startValue, long endValue);

    /// <summary>
    /// 计算固定位数可容纳的最大流水值。
    /// </summary>
    /// <param name="serialLength">流水位数。</param>
    /// <returns>由指定十进制位数构成的最大流水值，例如 4 位返回 9999。</returns>
    /// <remarks>实现使用整数逐位计算，避免浮点幂运算对 18 位边界造成精度损失。</remarks>
    /// <exception cref="ArgumentOutOfRangeException">流水位数不在 1 至 18 之间。</exception>
    long GetMaxValue(int serialLength);
}
