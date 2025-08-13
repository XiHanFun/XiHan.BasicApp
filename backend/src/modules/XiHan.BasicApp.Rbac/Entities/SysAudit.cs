#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAudit
// Guid:ec28152c-d6e9-4396-addb-b479254bad48
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 6:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar.Entities;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统审核实体
/// </summary>
[SugarTable("sys_audit", "系统审核表")]
[SugarIndex("IX_SysAudit_AuditStatus", "AuditStatus", OrderByType.Asc)]
[SugarIndex("IX_SysAudit_SubmitterId", "SubmitterId", OrderByType.Asc)]
[SugarIndex("IX_SysAudit_AuditorId", "AuditorId", OrderByType.Asc)]
[SugarIndex("IX_SysAudit_BusinessType", "BusinessType", OrderByType.Asc)]
[SugarIndex("IX_SysAudit_SubmitTime", "SubmitTime", OrderByType.Desc)]
[SugarIndex("IX_SysAudit_TenantId", "TenantId", OrderByType.Asc)]
public partial class SysAudit : SugarEntityWithAudit<long>
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "租户ID", IsNullable = true)]
    public virtual long? TenantId { get; set; }

    /// <summary>
    /// 审核标题
    /// </summary>
    [SugarColumn(ColumnDescription = "审核标题", Length = 200, IsNullable = false)]
    public virtual string Title { get; set; } = string.Empty;

    /// <summary>
    /// 审核内容
    /// </summary>
    [SugarColumn(ColumnDescription = "审核内容", ColumnDataType = "text", IsNullable = true)]
    public virtual string? Content { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    [SugarColumn(ColumnDescription = "业务类型", Length = 50, IsNullable = false)]
    public virtual string BusinessType { get; set; } = string.Empty;

    /// <summary>
    /// 业务ID
    /// </summary>
    [SugarColumn(ColumnDescription = "业务ID", IsNullable = false)]
    public virtual long BusinessId { get; set; }

    /// <summary>
    /// 业务数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "业务数据", ColumnDataType = "text", IsNullable = true)]
    public virtual string? BusinessData { get; set; }

    /// <summary>
    /// 提交用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "提交用户ID", IsNullable = false)]
    public virtual long SubmitterId { get; set; }

    /// <summary>
    /// 审核用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "审核用户ID", IsNullable = true)]
    public virtual long? AuditorId { get; set; }

    /// <summary>
    /// 审核状态
    /// </summary>
    [SugarColumn(ColumnDescription = "审核状态")]
    public virtual AuditStatus AuditStatus { get; set; } = AuditStatus.Pending;

    /// <summary>
    /// 审核结果
    /// </summary>
    [SugarColumn(ColumnDescription = "审核结果", IsNullable = true)]
    public virtual AuditResult? AuditResult { get; set; }

    /// <summary>
    /// 审核意见
    /// </summary>
    [SugarColumn(ColumnDescription = "审核意见", Length = 1000, IsNullable = true)]
    public virtual string? AuditOpinion { get; set; }

    /// <summary>
    /// 提交时间
    /// </summary>
    [SugarColumn(ColumnDescription = "提交时间")]
    public virtual DateTimeOffset SubmitTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 审核时间
    /// </summary>
    [SugarColumn(ColumnDescription = "审核时间", IsNullable = true)]
    public virtual DateTimeOffset? AuditTime { get; set; }

    /// <summary>
    /// 优先级（1-5，数字越小优先级越高）
    /// </summary>
    [SugarColumn(ColumnDescription = "优先级")]
    public virtual int Priority { get; set; } = 3;

    /// <summary>
    /// 是否需要多级审核
    /// </summary>
    [SugarColumn(ColumnDescription = "是否需要多级审核")]
    public virtual bool IsMultiLevel { get; set; } = false;

    /// <summary>
    /// 当前审核级别
    /// </summary>
    [SugarColumn(ColumnDescription = "当前审核级别")]
    public virtual int CurrentLevel { get; set; } = 1;

    /// <summary>
    /// 总审核级别
    /// </summary>
    [SugarColumn(ColumnDescription = "总审核级别")]
    public virtual int TotalLevel { get; set; } = 1;

    /// <summary>
    /// 截止时间
    /// </summary>
    [SugarColumn(ColumnDescription = "截止时间", IsNullable = true)]
    public virtual DateTimeOffset? Deadline { get; set; }

    /// <summary>
    /// 附件路径（多个用逗号分隔）
    /// </summary>
    [SugarColumn(ColumnDescription = "附件路径", Length = 2000, IsNullable = true)]
    public virtual string? Attachments { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
