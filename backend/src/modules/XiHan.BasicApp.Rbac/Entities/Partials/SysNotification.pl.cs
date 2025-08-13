#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysNotification.pl
// Guid:ad28152c-d6e9-4396-addb-b479254bad44
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 5:59:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统通知实体扩展
/// </summary>
public partial class SysNotification
{
    /// <summary>
    /// 租户信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(TenantId))]
    public virtual SysTenant? Tenant { get; set; }

    /// <summary>
    /// 接收用户信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(UserId))]
    public virtual SysUser? User { get; set; }

    /// <summary>
    /// 发送用户信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(SenderId))]
    public virtual SysUser? Sender { get; set; }
}
