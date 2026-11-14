// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Auditing;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 条件命中记录
/// </summary>
/// <remarks>
/// 访问策略的命中（拒绝、放行、只记录）与权限条件的拒绝；同一规则对同一来源一分钟内的命中合并成一行
/// 写入方是 Access 的访问策略与权限条件（「N1 四包深化」，排在本批之后），本批只有查询面
/// </remarks>
[SugarTable(TableName = "sys_condition_hit_{year}{month}{day}", TableDescription = "条件命中记录")]
[SplitTable(SplitType.Month)]
[SugarIndex("ix_{split_table}_tenant_id_occurred_at", nameof(TenantId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_trace_id", nameof(TraceId), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_user_id_occurred_at", nameof(UserId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_policy_id", nameof(PolicyId), OrderByType.Asc)]
public sealed class SysConditionHit : AuditLogRecord
{
    /// <summary>
    /// 来源
    /// </summary>
    [SugarColumn(ColumnName = "source", IsNullable = false, ColumnDescription = "来源")]
    public ConditionHitSource Source { get; set; }

    /// <summary>
    /// 访问策略
    /// </summary>
    /// <remarks>
    /// 权限条件的命中为空
    /// </remarks>
    [SugarColumn(ColumnName = "policy_id", IsNullable = true, ColumnDescription = "访问策略")]
    public Guid? PolicyId { get; set; }

    /// <summary>
    /// 策略编码
    /// </summary>
    /// <remarks>
    /// 权限条件的命中记权限码
    /// </remarks>
    [SugarColumn(ColumnName = "policy_code", Length = 128, IsNullable = false, ColumnDescription = "策略编码")]
    public string PolicyCode { get; set; } = string.Empty;

    /// <summary>
    /// 策略名称（命中时的快照）
    /// </summary>
    [SugarColumn(ColumnName = "policy_name", Length = 256, IsNullable = true, ColumnDescription = "策略名称（命中时的快照）")]
    public string? PolicyName { get; set; }

    /// <summary>
    /// 规则要求的动作
    /// </summary>
    [SugarColumn(ColumnName = "action", IsNullable = false, ColumnDescription = "规则要求的动作")]
    public ConditionHitAction Action { get; set; }

    /// <summary>
    /// 求值时机
    /// </summary>
    [SugarColumn(ColumnName = "phase", IsNullable = false, ColumnDescription = "求值时机")]
    public ConditionHitPhase Phase { get; set; }

    /// <summary>
    /// 请求是否因此被拦下（拒绝被同层放行豁免时为否）
    /// </summary>
    [SugarColumn(ColumnName = "is_blocked", IsNullable = false, ColumnDescription = "请求是否因此被拦下（拒绝被同层放行豁免时为否）")]
    public bool IsBlocked { get; set; }

    /// <summary>
    /// 操作 ID
    /// </summary>
    [SugarColumn(ColumnName = "operation_id", Length = 128, IsNullable = true, ColumnDescription = "操作 ID")]
    public string? OperationId { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    [SugarColumn(ColumnName = "method", Length = 16, IsNullable = true, ColumnDescription = "请求方法")]
    public string? Method { get; set; }

    /// <summary>
    /// 请求路径（不含查询串）
    /// </summary>
    [SugarColumn(ColumnName = "path", Length = 512, IsNullable = true, ColumnDescription = "请求路径（不含查询串）")]
    public string? Path { get; set; }

    /// <summary>
    /// 端点要求的权限码，逗号分隔
    /// </summary>
    [SugarColumn(ColumnName = "permission_codes", Length = 1024, IsNullable = true, ColumnDescription = "端点要求的权限码，逗号分隔")]
    public string? PermissionCodes { get; set; }

    /// <summary>
    /// 这一分钟里的命中次数
    /// </summary>
    [SugarColumn(ColumnName = "hit_count", IsNullable = false, ColumnDescription = "这一分钟里的命中次数")]
    public int HitCount { get; set; } = 1;
}
