#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationType
// Guid:2f7f766c-b021-48a9-adef-44b1a2ddd4a5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

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
