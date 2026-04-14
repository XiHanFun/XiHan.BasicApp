#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserNotification
// Guid:c5a8e2d1-3b7f-49c6-a12d-8e6f9c4b5d03
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/11 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 用户通知接收状态实体，记录每个用户对每条通知的已读/确认状态
/// </summary>
[SugarTable("Sys_User_Notification", "用户通知接收状态表")]
[SugarIndex("UX_SysUserNotification_TeId_NoId_UsId", nameof(TenantId), OrderByType.Asc, nameof(NotificationId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, true)]
[SugarIndex("IX_SysUserNotification_TeId_UsId_NoSt", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(NotificationStatus), OrderByType.Asc)]
public partial class SysUserNotification : BasicAppCreationEntity
{
    /// <summary>
    /// 通知ID
    /// </summary>
    [SugarColumn(ColumnDescription = "通知ID")]
    public virtual long NotificationId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID")]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 通知状态（未读/已读/已删除）
    /// </summary>
    [SugarColumn(ColumnDescription = "通知状态")]
    public virtual NotificationStatus NotificationStatus { get; set; } = NotificationStatus.Unread;

    /// <summary>
    /// 阅读时间
    /// </summary>
    [SugarColumn(ColumnDescription = "阅读时间", IsNullable = true)]
    public virtual DateTimeOffset? ReadTime { get; set; }

    /// <summary>
    /// 确认时间
    /// </summary>
    [SugarColumn(ColumnDescription = "确认时间", IsNullable = true)]
    public virtual DateTimeOffset? ConfirmTime { get; set; }
}
