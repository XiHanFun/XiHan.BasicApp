// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Auditing;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 条件命中记录：访问策略的命中（拒绝、放行、只记录）与权限条件的拒绝；同一规则对同一来源一分钟内的命中合并成一行。
/// 写入方是 Access 的访问策略与权限条件（「N1 四包深化」，排在本批之后），本批只有查询面。
/// </summary>
[SugarTable("sys_condition_hit_{year}{month}{day}")]
[SplitTable(SplitType.Month)]
public sealed class SysConditionHit : AuditLogRecord
{
    /// <summary>
    /// 来源。
    /// </summary>
    public ConditionHitSource Source { get; set; }

    /// <summary>
    /// 访问策略；权限条件的命中为空。
    /// </summary>
    public Guid? PolicyId { get; set; }

    /// <summary>
    /// 策略编码；权限条件的命中记权限码。
    /// </summary>
    [StringLength(128)]
    public string PolicyCode { get; set; } = string.Empty;

    /// <summary>
    /// 策略名称（命中时的快照）。
    /// </summary>
    [StringLength(256)]
    public string? PolicyName { get; set; }

    /// <summary>
    /// 规则要求的动作。
    /// </summary>
    public ConditionHitAction Action { get; set; }

    /// <summary>
    /// 求值时机。
    /// </summary>
    public ConditionHitPhase Phase { get; set; }

    /// <summary>
    /// 请求是否因此被拦下（拒绝被同层放行豁免时为否）。
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// 操作 ID。
    /// </summary>
    [StringLength(128)]
    public string? OperationId { get; set; }

    /// <summary>
    /// 请求方法。
    /// </summary>
    [StringLength(16)]
    public string? Method { get; set; }

    /// <summary>
    /// 请求路径（不含查询串）。
    /// </summary>
    [StringLength(512)]
    public string? Path { get; set; }

    /// <summary>
    /// 端点要求的权限码，逗号分隔。
    /// </summary>
    [StringLength(1024)]
    public string? PermissionCodes { get; set; }

    /// <summary>
    /// 这一分钟里的命中次数。
    /// </summary>
    public int HitCount { get; set; } = 1;
}
