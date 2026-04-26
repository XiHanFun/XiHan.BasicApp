#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserNotification.Expand
// Guid:d6b9e3f2-4c8a-5ad7-b23e-9f7a0d5c6e14
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/14 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 用户通知接收状态实体扩展
/// </summary>
public partial class SysUserNotification
{
    /// <summary>
    /// 关联通知
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(NotificationId))]
    public virtual SysNotification? Notification { get; set; }

    /// <summary>
    /// 关联用户
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(UserId))]
    public virtual SysUser? User { get; set; }
}
