// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Settings.Domain.Entities;

/// <summary>
/// 业务编号规则（平台库实体）。
/// </summary>
/// <remarks>
/// <see cref="OwnerTenantId"/> 分平台规则与租户规则；平台规则 <see cref="AllowTenantUse"/> 时租户也能用它发号（共享一条流水）。
/// 规则行同时保存当前周期与最后发出的流水值；发号在独立事务里先锁住规则行再推进，发出的号永久消耗（可以有空洞，不会重用）。
/// 首次发号后格式（前缀、分隔符、日期格式、流水位数、重置周期、时区）冻结。
/// </remarks>
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(Code))]
public sealed class SysNumberingRule : AppAuditedEntity, IOwnedByTenant
{
    /// <summary>
    /// 还没有周期基线时 <see cref="CurrentPeriodOrdinal"/> 的值。
    /// </summary>
    /// <remarks>
    /// 任何真实序号都不小于 0，首次发号必能通过「只前进」的翻转判断。
    /// </remarks>
    public const long NoPeriodBaseline = -1;

    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 规则编码：所属一侧内唯一，创建后不可改。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 前缀（如 ORD）。
    /// </summary>
    /// <remarks>
    /// 空即不输出。
    /// </remarks>
    public string? Prefix { get; set; }

    /// <summary>
    /// 非空格式段之间的分隔符，可为空串。
    /// </summary>
    public string Separator { get; set; } = "-";

    /// <summary>
    /// 日期段格式。
    /// </summary>
    public NumberingDateFormat DateFormat { get; set; } = NumberingDateFormat.YyyyMMdd;

    /// <summary>
    /// 流水段固定位数（1 到 18）。
    /// </summary>
    public int SerialLength { get; set; } = 4;

    /// <summary>
    /// 自动重置周期。
    /// </summary>
    public NumberingResetCycle ResetCycle { get; set; } = NumberingResetCycle.Daily;

    /// <summary>
    /// 计算日期与周期用的时区（IANA 标识，缺省 UTC）。
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// 当前周期里最后发出的流水值。
    /// </summary>
    /// <remarks>
    /// 0 表示本周期还没发号。
    /// </remarks>
    public long CurrentValue { get; set; }

    /// <summary>
    /// 当前周期键。
    /// </summary>
    /// <remarks>
    /// 取值为 <c>never</c> / <c>yyyyMMdd</c> / <c>yyyyMM</c> / <c>yyyy</c>；还没有基线时为空串。
    /// </remarks>
    public string CurrentPeriod { get; set; } = string.Empty;

    /// <summary>
    /// 当前周期序号：单调递增，时钟落后的节点不能把规则拉回旧周期。
    /// </summary>
    public long CurrentPeriodOrdinal { get; set; } = NoPeriodBaseline;

    /// <summary>
    /// 是否发过号：发过即格式冻结、不能删除。
    /// </summary>
    public bool HasAllocated { get; set; }

    /// <summary>
    /// 平台规则是否供租户使用。
    /// </summary>
    /// <remarks>
    /// 租户规则恒为 <c>false</c>。
    /// </remarks>
    public bool AllowTenantUse { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public SettingStatus Status { get; set; } = SettingStatus.Active;

    /// <summary>
    /// 排序号。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 说明。
    /// </summary>
    public string? Description { get; set; }
}
