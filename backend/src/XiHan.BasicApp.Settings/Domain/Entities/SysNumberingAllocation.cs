// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Settings.Domain.Entities;

/// <summary>
/// 发号的永久记录（平台库实体，只追加）
/// </summary>
/// <remarks>
/// 每次发号留一条永久记录、只追加：它是幂等的事实来源（同一侧对同一规则用同一个幂等键只发一次号，参数指纹相同即按记录重放、不同即冲突），也是发号审计
/// <see cref="OwnerTenantId"/> 是规则所属一侧，<see cref="RequestTenantId"/> 是发起调用的一侧，两者在租户共用平台规则时不同；幂等唯一索引带上发起侧，各租户的幂等键互不干扰
/// 不软删除、不修改，所以幂等用普通唯一索引而不是部分唯一索引
/// 不存号码列表，只存格式快照与流水区间：号码按快照重建，规则停用后重放照样拿到当时那批号
/// 与推进规则流水写在同一个独立事务里，记录在即号已发；重置流水时以本周期已发出的最大值兜底，重置后不会重发
/// 没有删除，发过号的规则因此也不能删
/// </remarks>
[SugarTable(TableName = "sys_numbering_allocation", TableDescription = "发号的永久记录（平台库实体，只追加）")]
[SugarIndex("ux_sys_numbering_allocation_idempotency", nameof(OwnerTenantId), OrderByType.Asc, nameof(RuleId), OrderByType.Asc, nameof(RequestTenantId), OrderByType.Asc, nameof(IdempotencyKey), OrderByType.Asc, true)]
[SugarIndex("ix_sys_numbering_allocation_rule_id_generated_at", nameof(RuleId), OrderByType.Asc, nameof(GeneratedAt), OrderByType.Asc)]
[SugarIndex("ix_sys_numbering_allocation_rule_id_period_key_end_value", nameof(RuleId), OrderByType.Asc, nameof(PeriodKey), OrderByType.Asc, nameof(EndValue), OrderByType.Asc)]
public sealed class SysNumberingAllocation : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 规则主键
    /// </summary>
    [SugarColumn(ColumnName = "rule_id", IsNullable = false, ColumnDescription = "规则主键")]
    public Guid RuleId { get; set; }

    /// <summary>
    /// 规则编码快照
    /// </summary>
    [SugarColumn(ColumnName = "rule_code", Length = 512, IsNullable = false, ColumnDescription = "规则编码快照")]
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>
    /// 发起调用的一侧：租户 ID，平台为 <c>platform</c>
    /// </summary>
    [SugarColumn(ColumnName = "request_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "发起调用的一侧：租户 ID，平台为 platform")]
    public string RequestTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 调用方给的幂等键：同一侧对同一规则用同一个键只发一次号
    /// </summary>
    [SugarColumn(ColumnName = "idempotency_key", Length = 512, IsNullable = false, ColumnDescription = "调用方给的幂等键：同一侧对同一规则用同一个键只发一次号")]
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>
    /// 请求参数的 SHA-256 指纹（十六进制）：同键异参即冲突
    /// </summary>
    [SugarColumn(ColumnName = "fingerprint", Length = 512, IsNullable = false, ColumnDescription = "请求参数的 SHA-256 指纹（十六进制）：同键异参即冲突")]
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>
    /// 本次发出的个数（1 到 1000）
    /// </summary>
    [SugarColumn(ColumnName = "count", IsNullable = false, ColumnDescription = "本次发出的个数（1 到 1000）")]
    public int Count { get; set; }

    /// <summary>
    /// 起始流水值（含）
    /// </summary>
    [SugarColumn(ColumnName = "start_value", IsNullable = false, ColumnDescription = "起始流水值（含）")]
    public long StartValue { get; set; }

    /// <summary>
    /// 结束流水值（含）
    /// </summary>
    [SugarColumn(ColumnName = "end_value", IsNullable = false, ColumnDescription = "结束流水值（含）")]
    public long EndValue { get; set; }

    /// <summary>
    /// 所属周期键
    /// </summary>
    [SugarColumn(ColumnName = "period_key", Length = 512, IsNullable = false, ColumnDescription = "所属周期键")]
    public string PeriodKey { get; set; } = string.Empty;

    /// <summary>
    /// 前缀快照
    /// </summary>
    [SugarColumn(ColumnName = "prefix_snapshot", Length = 512, IsNullable = true, ColumnDescription = "前缀快照")]
    public string? PrefixSnapshot { get; set; }

    /// <summary>
    /// 分隔符快照
    /// </summary>
    [SugarColumn(ColumnName = "separator_snapshot", Length = 512, IsNullable = false, ColumnDescription = "分隔符快照")]
    public string SeparatorSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 日期段文本快照
    /// </summary>
    [SugarColumn(ColumnName = "date_text_snapshot", Length = 512, IsNullable = true, ColumnDescription = "日期段文本快照")]
    public string? DateTextSnapshot { get; set; }

    /// <summary>
    /// 流水位数快照
    /// </summary>
    [SugarColumn(ColumnName = "serial_length_snapshot", IsNullable = false, ColumnDescription = "流水位数快照")]
    public int SerialLengthSnapshot { get; set; }

    /// <summary>
    /// 发号时刻（UTC），决定周期与日期段
    /// </summary>
    [SugarColumn(ColumnName = "generated_at", IsNullable = false, ColumnDescription = "发号时刻（UTC），决定周期与日期段")]
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>
    /// 业务类型（调用方自述）
    /// </summary>
    [SugarColumn(ColumnName = "business_type", Length = 512, IsNullable = true, ColumnDescription = "业务类型（调用方自述）")]
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务单据主键（调用方自述）
    /// </summary>
    [SugarColumn(ColumnName = "business_id", Length = 512, IsNullable = true, ColumnDescription = "业务单据主键（调用方自述）")]
    public string? BusinessId { get; set; }
}
