// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 业务编号规则实体，负责保存格式配置和当前已分配的最后一个流水值。
/// </summary>
/// <remarks>
/// <para><c>TenantId == 0</c> 表示平台全局规则；业务租户规则保存在当前租户范围内。</para>
/// <para>
/// 流水推进不由应用层读改写完成，而是由仓储的条件更新语句在数据库内一次完成，并显式推进 <c>RowVersion</c>；
/// 管理端的实体式更新走行版本乐观锁，因此不会把发号期间推进过的流水整行写回旧值。
/// </para>
/// </remarks>
[SugarTable(TableName = "Sys_Numbering_Rule", TableDescription = "业务编号规则表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_RuCo", nameof(TenantId), OrderByType.Asc, nameof(RuleCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_St_So", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc, nameof(Sort), OrderByType.Asc)]
public partial class SysNumberingRule : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 表示规则尚未建立周期基线的周期序号哨兵值。
    /// </summary>
    /// <remarks>
    /// 任何真实周期序号都不小于 0（「永不重置」恒为 0），因此 -1 保证首次发号一定能通过「只接受更大序号」的翻转守卫。
    /// </remarks>
    public const long NoPeriodBaseline = -1L;

    /// <summary>
    /// 规则编码，在规则所属租户范围内唯一，创建后不可修改。
    /// </summary>
    [SugarColumn(ColumnName = "Rule_Code", ColumnDescription = "规则编码", Length = 100, IsNullable = false)]
    public virtual string RuleCode { get; set; } = string.Empty;

    /// <summary>
    /// 规则名称。
    /// </summary>
    [SugarColumn(ColumnName = "Rule_Name", ColumnDescription = "规则名称", Length = 100, IsNullable = false)]
    public virtual string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 可选前缀，例如 ORD。
    /// </summary>
    [SugarColumn(ColumnName = "Prefix", ColumnDescription = "编号前缀", Length = 50, IsNullable = true)]
    public virtual string? Prefix { get; set; }

    /// <summary>
    /// 非空格式段之间的分隔符。
    /// </summary>
    [SugarColumn(ColumnName = "Separator", ColumnDescription = "格式分隔符", Length = 10, IsNullable = false)]
    public virtual string Separator { get; set; } = "-";

    /// <summary>
    /// 日期段格式。
    /// </summary>
    [SugarColumn(ColumnName = "Date_Format", ColumnDescription = "日期格式")]
    public virtual NumberingDateFormat DateFormat { get; set; } = NumberingDateFormat.YyyyMMdd;

    /// <summary>
    /// 流水段固定位数，允许 1 至 18 位。
    /// </summary>
    [SugarColumn(ColumnName = "Serial_Length", ColumnDescription = "流水位数")]
    public virtual int SerialLength { get; set; } = 4;

    /// <summary>
    /// 自动重置周期。
    /// </summary>
    [SugarColumn(ColumnName = "Reset_Cycle", ColumnDescription = "重置周期")]
    public virtual NumberingResetCycle ResetCycle { get; set; } = NumberingResetCycle.Daily;

    /// <summary>
    /// 计算日期与周期所使用的时区标识，默认 UTC。
    /// </summary>
    [SugarColumn(ColumnName = "Time_Zone_Id", ColumnDescription = "规则时区", Length = 100, IsNullable = false)]
    public virtual string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// 当前周期内最后一个已经分配的流水值；尚未分配时为 0。
    /// </summary>
    [SugarColumn(ColumnName = "Current_Value", ColumnDescription = "当前流水值")]
    public virtual long CurrentValue { get; set; }

    /// <summary>
    /// 当前流水值所属周期键，例如 20260727、202607、2026 或 never；尚未发号时为空字符串。
    /// </summary>
    /// <remarks>刻意不可空：周期翻转语句以等值和不等值谓词判断归属，可空列会让 SQL 三值逻辑把谓词求值成 NULL 而恒不成立。</remarks>
    [SugarColumn(ColumnName = "Current_Period", ColumnDescription = "当前周期", Length = 32, IsNullable = false)]
    public virtual string CurrentPeriod { get; set; } = string.Empty;

    /// <summary>
    /// 与 <see cref="CurrentPeriod"/> 一一对应的单调周期序号；尚未建立周期基线时为 <see cref="NoPeriodBaseline"/>。
    /// </summary>
    /// <remarks>
    /// 数据库只允许把周期推进到严格更大的序号，用于抵御多节点时钟偏差：
    /// 时钟落后的节点无法把规则拉回上一周期并从头重发编号。
    /// 用数值而非周期键文本比较，是因为字符串大小比较依赖列排序规则且 ORM 无法稳定翻译。
    /// 未建立基线时取 -1 而不是 0，否则「永不重置」规则的周期序号恒为 0，首次发号将无法通过严格递增守卫。
    /// </remarks>
    [SugarColumn(ColumnName = "Current_Period_Ordinal", ColumnDescription = "当前周期序号")]
    public virtual long CurrentPeriodOrdinal { get; set; } = NoPeriodBaseline;

    /// <summary>
    /// 是否曾经成功发号；为 true 后格式字段永久冻结。
    /// </summary>
    [SugarColumn(ColumnName = "Has_Allocated", ColumnDescription = "是否已发号")]
    public virtual bool HasAllocated { get; set; }

    /// <summary>
    /// 全局规则是否允许业务租户调用；租户私有规则固定为 false。
    /// </summary>
    [SugarColumn(ColumnName = "Allow_Tenant_Use", ColumnDescription = "允许租户使用")]
    public virtual bool AllowTenantUse { get; set; }

    /// <summary>
    /// 启停状态。
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 显示排序。
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; }

    /// <summary>
    /// 规则备注。
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
