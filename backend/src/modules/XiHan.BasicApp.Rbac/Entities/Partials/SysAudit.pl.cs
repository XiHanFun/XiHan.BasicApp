#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAudit.pl
// Guid:ed28152c-d6e9-4396-addb-b479254bad57
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 6:48:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统审核实体扩展
/// </summary>
public partial class SysAudit
{
    /// <summary>
    /// 租户信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(TenantId))]
    public virtual SysTenant? Tenant { get; set; }

    /// <summary>
    /// 提交用户信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(SubmitterId))]
    public virtual SysUser? Submitter { get; set; }

    /// <summary>
    /// 审核用户信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(AuditorId))]
    public virtual SysUser? Auditor { get; set; }

    /// <summary>
    /// 审核日志列表
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(SysAuditLog.AuditId))]
    public virtual List<SysAuditLog>? AuditLogs { get; set; }
}
