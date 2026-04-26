#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysNotification.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 通知类型枚举
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 系统通知
    /// </summary>
    System = 0,

    /// <summary>
    /// 用户通知
    /// </summary>
    User = 1,

    /// <summary>
    /// 公告
    /// </summary>
    Announcement = 2,

    /// <summary>
    /// 警告
    /// </summary>
    Warning = 3,

    /// <summary>
    /// 错误
    /// </summary>
    Error = 4
}

