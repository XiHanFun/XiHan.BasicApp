// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 规则流水基线的只读快照，用于确定占用区间或定位推进被拒绝的原因。
/// </summary>
/// <param name="CurrentValue">当前周期内最后一个已分配的流水值。</param>
/// <param name="CurrentPeriod">当前周期键；尚未建立周期基线时为空字符串。</param>
/// <param name="CurrentPeriodOrdinal">
/// 当前周期序号；「永不重置」规则恒为 0，尚未建立周期基线时为 <see cref="SysNumberingRule.NoPeriodBaseline"/>。
/// </param>
/// <param name="Status">规则启停状态。</param>
/// <param name="SerialLength">流水位数，用于换算容量上限。</param>
public readonly record struct NumberingSequenceState(
    long CurrentValue,
    string CurrentPeriod,
    long CurrentPeriodOrdinal,
    EnableStatus Status,
    int SerialLength);
