#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTask.pl
// Guid:1e28152c-d6e9-4396-addb-b479254bad60
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 6:51:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统任务实体扩展
/// </summary>
public partial class SysTask
{
    /// <summary>
    /// 租户信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(TenantId))]
    public virtual SysTenant? Tenant { get; set; }

    /// <summary>
    /// 创建用户信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(CreatedId))]
    public virtual SysUser? CreatedBy { get; set; }

    /// <summary>
    /// 任务执行日志列表
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(SysTaskLog.TaskId))]
    public virtual List<SysTaskLog>? TaskLogs { get; set; }
}
