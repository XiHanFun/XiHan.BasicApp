#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright (c)2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserNotification.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 通知状态枚举
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// 未读
    /// </summary>
    [Description("未读")]
    Unread = 0,

    /// <summary>
    /// 已读
    /// </summary>
    [Description("已读")]
    Read = 1,

    /// <summary>
    /// 已删除
    /// </summary>
    [Description("已删除")]
    Deleted = 2
}
