// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 业务编号永久分配记录，兼作幂等事实记录和发号审计记录。
/// </summary>
/// <remarks>
/// 记录不提供删除能力。结果通过格式快照和流水区间重建，避免批量发号时存储大段 JSON。
/// <c>TenantId</c> 表示实际规则所属范围，<see cref="RequestTenantId"/> 表示原始调用租户，两者在共享全局规则场景下不同。
/// </remarks>
[SugarTable(TableName = "Sys_Numbering_Allocation", TableDescription = "业务编号分配记录表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_Ru_ReTe_IdKe", nameof(TenantId), OrderByType.Asc, nameof(RuleId), OrderByType.Asc, nameof(RequestTenantId), OrderByType.Asc, nameof(IdempotencyKey), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_Ru_GeTi", nameof(RuleId), OrderByType.Asc, nameof(GeneratedAtUtc), OrderByType.Desc)]
[SugarIndex("IX_{table}_ReTe_GeTi", nameof(RequestTenantId), OrderByType.Asc, nameof(GeneratedAtUtc), OrderByType.Desc)]
public sealed class SysNumberingAllocation : BasicAppCreationEntity
{
    /// <summary>
    /// 实际使用的规则主键。
    /// </summary>
    [SugarColumn(ColumnName = "Rule_Id", ColumnDescription = "规则主键")]
    public long RuleId { get; set; }

    /// <summary>
    /// 发号时的规则编码快照。
    /// </summary>
    [SugarColumn(ColumnName = "Rule_Code", ColumnDescription = "规则编码", Length = 100, IsNullable = false)]
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>
    /// 原始请求租户；平台或单体上下文为 0。
    /// </summary>
    [SugarColumn(ColumnName = "Request_Tenant_Id", ColumnDescription = "请求租户")]
    public long RequestTenantId { get; set; }

    /// <summary>
    /// 调用方提供的幂等键。
    /// </summary>
    [SugarColumn(ColumnName = "Idempotency_Key", ColumnDescription = "幂等键", Length = 100, IsNullable = false)]
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>
    /// 规范化请求参数的 SHA-256 指纹，用于识别同键异参冲突。
    /// </summary>
    [SugarColumn(ColumnName = "Request_Fingerprint", ColumnDescription = "请求指纹", Length = 64, IsNullable = false)]
    public string RequestFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// 本次分配数量，范围为 1 至 1000。
    /// </summary>
    [SugarColumn(ColumnName = "Allocation_Count", ColumnDescription = "分配数量")]
    public int Count { get; set; }

    /// <summary>
    /// 本次分配的起始流水值（含）。
    /// </summary>
    [SugarColumn(ColumnName = "Start_Value", ColumnDescription = "起始流水值")]
    public long StartValue { get; set; }

    /// <summary>
    /// 本次分配的结束流水值（含）。
    /// </summary>
    [SugarColumn(ColumnName = "End_Value", ColumnDescription = "结束流水值")]
    public long EndValue { get; set; }

    /// <summary>
    /// 本次分配所属周期键。
    /// </summary>
    [SugarColumn(ColumnName = "Period_Key", ColumnDescription = "周期键", Length = 32, IsNullable = false)]
    public string PeriodKey { get; set; } = string.Empty;

    /// <summary>
    /// 前缀格式快照。
    /// </summary>
    [SugarColumn(ColumnName = "Prefix_Snapshot", ColumnDescription = "前缀快照", Length = 50, IsNullable = true)]
    public string? PrefixSnapshot { get; set; }

    /// <summary>
    /// 分隔符格式快照。
    /// </summary>
    [SugarColumn(ColumnName = "Separator_Snapshot", ColumnDescription = "分隔符快照", Length = 10, IsNullable = false)]
    public string SeparatorSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 已按规则时区格式化的日期文本快照；无日期段时为 null。
    /// </summary>
    [SugarColumn(ColumnName = "Date_Text_Snapshot", ColumnDescription = "日期文本快照", Length = 16, IsNullable = true)]
    public string? DateTextSnapshot { get; set; }

    /// <summary>
    /// 流水位数快照。
    /// </summary>
    [SugarColumn(ColumnName = "Serial_Length_Snapshot", ColumnDescription = "流水位数快照")]
    public int SerialLengthSnapshot { get; set; }

    /// <summary>
    /// UTC 发号时间。
    /// </summary>
    [SugarColumn(ColumnName = "Generated_At_Utc", ColumnDescription = "UTC发号时间")]
    public DateTimeOffset GeneratedAtUtc { get; set; }

    /// <summary>
    /// 可选业务类型，用于把分配记录关联到调用方业务。
    /// </summary>
    [SugarColumn(ColumnName = "Business_Type", ColumnDescription = "业务类型", Length = 100, IsNullable = true)]
    public string? BusinessType { get; set; }

    /// <summary>
    /// 可选业务标识。
    /// </summary>
    [SugarColumn(ColumnName = "Business_Id", ColumnDescription = "业务标识", Length = 100, IsNullable = true)]
    public string? BusinessId { get; set; }
}
