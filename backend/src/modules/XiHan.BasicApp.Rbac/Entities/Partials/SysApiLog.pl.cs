#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysApiLog.pl
// Guid:3e28152c-d6e9-4396-addb-b479254bad62
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 6:53:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// API日志实体扩展
/// </summary>
public partial class SysApiLog
{
    /// <summary>
    /// 租户信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(TenantId))]
    public virtual SysTenant? Tenant { get; set; }

    /// <summary>
    /// 用户信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(UserId))]
    public virtual SysUser? User { get; set; }
}
