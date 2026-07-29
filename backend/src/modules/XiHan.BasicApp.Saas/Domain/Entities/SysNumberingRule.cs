// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 业务编号规则实体，负责保存格式配置和当前已分配的最后一个流水值。
/// </summary>
/// <remarks>
/// <para><c>TenantId == 0</c> 表示平台全局规则；业务租户规则保存在当前租户范围内。</para>
/// <para>流水更新依赖实体基类的 RowVersion 乐观锁。应用层的进程内锁仅用于降低单节点竞争，不能替代数据库并发校验。</para>
/// </remarks>
[SugarTable(TableName = "Sys_Numbering_Rule", TableDescription = "业务编号规则表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_RuCo", nameof(TenantId), OrderByType.Asc, nameof(RuleCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_St_So", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc, nameof(Sort), OrderByType.Asc)]
public sealed class SysNumberingRule : BasicAppFullAuditedEntity, IValidatableObject
{
    /// <summary>
    /// 规则编码，在规则所属租户范围内唯一，创建后不可修改。
    /// </summary>
    [SugarColumn(ColumnName = "Rule_Code", ColumnDescription = "规则编码", Length = 100, IsNullable = false)]
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>
    /// 规则名称。
    /// </summary>
    [SugarColumn(ColumnName = "Rule_Name", ColumnDescription = "规则名称", Length = 100, IsNullable = false)]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 可选前缀，例如 ORD。
    /// </summary>
    [SugarColumn(ColumnName = "Prefix", ColumnDescription = "编号前缀", Length = 50, IsNullable = true)]
    public string? Prefix { get; set; }

    /// <summary>
    /// 非空格式段之间的分隔符。
    /// </summary>
    [SugarColumn(ColumnName = "Separator", ColumnDescription = "格式分隔符", Length = 10, IsNullable = false)]
    public string Separator { get; set; } = "-";

    /// <summary>
    /// 日期段格式。
    /// </summary>
    [SugarColumn(ColumnName = "Date_Format", ColumnDescription = "日期格式")]
    public NumberingDateFormat DateFormat { get; set; } = NumberingDateFormat.YyyyMMdd;

    /// <summary>
    /// 流水段固定位数，允许 1 至 18 位。
    /// </summary>
    [SugarColumn(ColumnName = "Serial_Length", ColumnDescription = "流水位数")]
    public int SerialLength { get; set; } = 4;

    /// <summary>
    /// 自动重置周期。
    /// </summary>
    [SugarColumn(ColumnName = "Reset_Cycle", ColumnDescription = "重置周期")]
    public NumberingResetCycle ResetCycle { get; set; } = NumberingResetCycle.Daily;

    /// <summary>
    /// 计算日期与周期所使用的时区标识，默认 UTC。
    /// </summary>
    [SugarColumn(ColumnName = "Time_Zone_Id", ColumnDescription = "规则时区", Length = 100, IsNullable = false)]
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// 当前周期内最后一个已经分配的流水值；尚未分配时为 0。
    /// </summary>
    [SugarColumn(ColumnName = "Current_Value", ColumnDescription = "当前流水值")]
    public long CurrentValue { get; set; }

    /// <summary>
    /// 当前流水值所属周期键，例如 20260727、202607、2026 或 never。
    /// </summary>
    [SugarColumn(ColumnName = "Current_Period", ColumnDescription = "当前周期", Length = 32, IsNullable = true)]
    public string? CurrentPeriod { get; set; }

    /// <summary>
    /// 是否曾经成功发号；为 true 后格式字段永久冻结。
    /// </summary>
    [SugarColumn(ColumnName = "Has_Allocated", ColumnDescription = "是否已发号")]
    public bool HasAllocated { get; set; }

    /// <summary>
    /// 全局规则是否允许业务租户调用；租户私有规则固定为 false。
    /// </summary>
    [SugarColumn(ColumnName = "Allow_Tenant_Use", ColumnDescription = "允许租户使用")]
    public bool AllowTenantUse { get; set; }

    /// <summary>
    /// 启停状态。
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 显示排序。
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public int Sort { get; set; }

    /// <summary>
    /// 规则备注。
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public string? Remark { get; set; }

    /// <summary>
    /// 是否为平台全局规则；该派生属性不落库。
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool IsGlobal => TenantId == 0;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(RuleCode))
        {
            yield return new ValidationResult("规则编码不能为空。", [nameof(RuleCode)]);
        }

        if (string.IsNullOrWhiteSpace(RuleName))
        {
            yield return new ValidationResult("规则名称不能为空。", [nameof(RuleName)]);
        }

        if (SerialLength is < 1 or > 18)
        {
            yield return new ValidationResult("流水位数必须在 1 至 18 之间。", [nameof(SerialLength)]);
        }

        if (string.IsNullOrWhiteSpace(TimeZoneId))
        {
            yield return new ValidationResult("规则时区不能为空。", [nameof(TimeZoneId)]);
        }
    }
}
