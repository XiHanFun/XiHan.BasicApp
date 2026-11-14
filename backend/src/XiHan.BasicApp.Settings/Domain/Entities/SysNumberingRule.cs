// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Settings.Domain.Entities;

/// <summary>
/// 业务编号规则（平台库实体）
/// </summary>
/// <remarks>
/// <see cref="OwnerTenantId"/> 分平台规则与租户规则；平台规则 <see cref="AllowTenantUse"/> 时租户也能用它发号（共享一条流水）
/// 规则行同时保存当前周期与最后发出的流水值；发号在独立事务里先锁住规则行再推进，发出的号永久消耗（可以有空洞，不会重用）
/// 首次发号后格式（前缀、分隔符、日期格式、流水位数、重置周期、时区）冻结
/// </remarks>
[SugarTable(TableName = "sys_numbering_rule", TableDescription = "业务编号规则（平台库实体）")]
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(Code), Name = "ux_sys_numbering_rule_owner_tenant_id_code")]
public sealed class SysNumberingRule : AppAuditedEntity, IOwnedByTenant
{
    /// <summary>
    /// 还没有周期基线时 <see cref="CurrentPeriodOrdinal"/> 的值
    /// </summary>
    /// <remarks>
    /// 任何真实序号都不小于 0，首次发号必能通过「只前进」的翻转判断
    /// </remarks>
    public const long NoPeriodBaseline = -1;

    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 规则编码：所属一侧内唯一，创建后不可改
    /// </summary>
    [SugarColumn(ColumnName = "code", Length = 512, IsNullable = false, ColumnDescription = "规则编码：所属一侧内唯一，创建后不可改")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 512, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 前缀（如 ORD）
    /// </summary>
    /// <remarks>
    /// 空即不输出
    /// </remarks>
    [SugarColumn(ColumnName = "prefix", Length = 512, IsNullable = true, ColumnDescription = "前缀（如 ORD）")]
    public string? Prefix { get; set; }

    /// <summary>
    /// 非空格式段之间的分隔符，可为空串
    /// </summary>
    [SugarColumn(ColumnName = "separator", Length = 512, IsNullable = false, ColumnDescription = "非空格式段之间的分隔符，可为空串")]
    public string Separator { get; set; } = "-";

    /// <summary>
    /// 日期段格式
    /// </summary>
    [SugarColumn(ColumnName = "date_format", IsNullable = false, ColumnDescription = "日期段格式")]
    public NumberingDateFormat DateFormat { get; set; } = NumberingDateFormat.YyyyMMdd;

    /// <summary>
    /// 流水段固定位数（1 到 18）
    /// </summary>
    [SugarColumn(ColumnName = "serial_length", IsNullable = false, ColumnDescription = "流水段固定位数（1 到 18）")]
    public int SerialLength { get; set; } = 4;

    /// <summary>
    /// 自动重置周期
    /// </summary>
    [SugarColumn(ColumnName = "reset_cycle", IsNullable = false, ColumnDescription = "自动重置周期")]
    public NumberingResetCycle ResetCycle { get; set; } = NumberingResetCycle.Daily;

    /// <summary>
    /// 计算日期与周期用的时区（IANA 标识，缺省 UTC）
    /// </summary>
    [SugarColumn(ColumnName = "time_zone_id", Length = 512, IsNullable = false, ColumnDescription = "计算日期与周期用的时区（IANA 标识，缺省 UTC）")]
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// 当前周期里最后发出的流水值
    /// </summary>
    /// <remarks>
    /// 0 表示本周期还没发号
    /// </remarks>
    [SugarColumn(ColumnName = "current_value", IsNullable = false, ColumnDescription = "当前周期里最后发出的流水值")]
    public long CurrentValue { get; set; }

    /// <summary>
    /// 当前周期键
    /// </summary>
    /// <remarks>
    /// 取值为 <c>never</c> / <c>yyyyMMdd</c> / <c>yyyyMM</c> / <c>yyyy</c>；还没有基线时为空串
    /// </remarks>
    [SugarColumn(ColumnName = "current_period", Length = 512, IsNullable = false, ColumnDescription = "当前周期键")]
    public string CurrentPeriod { get; set; } = string.Empty;

    /// <summary>
    /// 当前周期序号：单调递增，时钟落后的节点不能把规则拉回旧周期
    /// </summary>
    [SugarColumn(ColumnName = "current_period_ordinal", IsNullable = false, ColumnDescription = "当前周期序号：单调递增，时钟落后的节点不能把规则拉回旧周期")]
    public long CurrentPeriodOrdinal { get; set; } = NoPeriodBaseline;

    /// <summary>
    /// 是否发过号：发过即格式冻结、不能删除
    /// </summary>
    [SugarColumn(ColumnName = "has_allocated", IsNullable = false, ColumnDescription = "是否发过号：发过即格式冻结、不能删除")]
    public bool HasAllocated { get; set; }

    /// <summary>
    /// 平台规则是否供租户使用
    /// </summary>
    /// <remarks>
    /// 租户规则恒为 <c>false</c>
    /// </remarks>
    [SugarColumn(ColumnName = "allow_tenant_use", IsNullable = false, ColumnDescription = "平台规则是否供租户使用")]
    public bool AllowTenantUse { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public SettingStatus Status { get; set; } = SettingStatus.Active;

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true, ColumnDescription = "说明")]
    public string? Description { get; set; }
}

/// <summary>
/// 编号日期段的格式（按规则时区取当地日期）
/// </summary>
public enum NumberingDateFormat
{
    /// <summary>
    /// 不输出日期段
    /// </summary>
    None = 0,

    /// <summary>
    /// 四位年份，如 2026
    /// </summary>
    Yyyy = 1,

    /// <summary>
    /// 四位年份与月份，如 202610
    /// </summary>
    YyyyMM = 2,

    /// <summary>
    /// 四位年份、月份与日期，如 20261028
    /// </summary>
    YyyyMMdd = 3,

    /// <summary>
    /// 两位年份与月份，如 2610
    /// </summary>
    YyMM = 4,

    /// <summary>
    /// 两位年份、月份与日期，如 261028
    /// </summary>
    YyMMdd = 5,

    /// <summary>
    /// 月份与日期，如 1028
    /// </summary>
    MMdd = 6,
}

/// <summary>
/// 流水的自动重置周期（按规则时区）
/// </summary>
/// <remarks>
/// 新周期从 1 开始；日期段必须能区分周期边界，否则不同周期会出同号
/// </remarks>
public enum NumberingResetCycle
{
    /// <summary>
    /// 永不重置
    /// </summary>
    Never = 0,

    /// <summary>
    /// 每天
    /// </summary>
    Daily = 1,

    /// <summary>
    /// 每月
    /// </summary>
    Monthly = 2,

    /// <summary>
    /// 每年
    /// </summary>
    Yearly = 3,
}
