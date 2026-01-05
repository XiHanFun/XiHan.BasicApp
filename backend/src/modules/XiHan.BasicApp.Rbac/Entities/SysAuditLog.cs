#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAuditLog
// Guid:fc28152c-d6e9-4396-addb-b479254bad49
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 6:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统审核日志实体
/// </summary>
[SugarTable("Sys_Audit_Log_{year}{month}{day}", "系统审核日志表")]
[SplitTable(SplitType.Month)]
[SugarIndex("IX_SysAuditLog_AuditId", nameof(AuditId), OrderByType.Asc)]
[SugarIndex("IX_SysAuditLog_AuditorId", nameof(AuditorId), OrderByType.Asc)]
[SugarIndex("IX_SysAuditLog_AuditResult", nameof(AuditResult), OrderByType.Asc)]
[SugarIndex("IX_SysAuditLog_AuditTime", nameof(AuditTime), OrderByType.Desc)]
public partial class SysAuditLog : RbacFullAuditedEntity<long>
{
    /// <summary>
    /// 审核ID
    /// </summary>
    [SugarColumn(ColumnDescription = "审核ID", IsNullable = false)]
    public virtual long AuditId { get; set; }

    /// <summary>
    /// 审核用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "审核用户ID", IsNullable = false)]
    public virtual long AuditorId { get; set; }

    /// <summary>
    /// 审核级别
    /// </summary>
    [SugarColumn(ColumnDescription = "审核级别")]
    public virtual int AuditLevel { get; set; } = 1;

    /// <summary>
    /// 审核结果
    /// </summary>
    [SugarColumn(ColumnDescription = "审核结果")]
    public virtual AuditResult AuditResult { get; set; } = AuditResult.Pass;

    /// <summary>
    /// 审核意见
    /// </summary>
    [SugarColumn(ColumnDescription = "审核意见", Length = 1000, IsNullable = true)]
    public virtual string? AuditOpinion { get; set; }

    /// <summary>
    /// 审核时间
    /// </summary>
    [SugarColumn(ColumnDescription = "审核时间")]
    public virtual DateTimeOffset AuditTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 审核前状态
    /// </summary>
    [SugarColumn(ColumnDescription = "审核前状态")]
    public virtual AuditStatus BeforeStatus { get; set; } = AuditStatus.Pending;

    /// <summary>
    /// 审核后状态
    /// </summary>
    [SugarColumn(ColumnDescription = "审核后状态")]
    public virtual AuditStatus AfterStatus { get; set; } = AuditStatus.Approved;

    /// <summary>
    /// 审核IP
    /// </summary>
    [SugarColumn(ColumnDescription = "审核IP", Length = 50, IsNullable = true)]
    public virtual string? AuditIp { get; set; }

    /// <summary>
    /// 审核位置
    /// </summary>
    [SugarColumn(ColumnDescription = "审核位置", Length = 200, IsNullable = true)]
    public virtual string? AuditLocation { get; set; }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    [SugarColumn(ColumnDescription = "浏览器类型", Length = 100, IsNullable = true)]
    public virtual string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    [SugarColumn(ColumnDescription = "操作系统", Length = 100, IsNullable = true)]
    public virtual string? Os { get; set; }

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

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false, ColumnDescription = "创建时间")]
    [SplitField]
    public override DateTimeOffset CreatedTime { get; set; }
}
