#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAuditLog.pl
// Guid:fd28152c-d6e9-4396-addb-b479254bad58
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 6:49:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统审核日志实体扩展
/// </summary>
public partial class SysAuditLog
{
    /// <summary>
    /// 审核信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(NavigateType.ManyToOne, nameof(AuditId))]
    public virtual SysAudit? Audit { get; set; }

    /// <summary>
    /// 审核用户信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(NavigateType.ManyToOne, nameof(AuditorId))]
    public virtual SysUser? Auditor { get; set; }
}
