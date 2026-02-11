#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationStatus
// Guid:d8d5a771-59ea-4718-b1da-b3a4bdc1bcf2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 通知状态枚举
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// 未读
    /// </summary>
    Unread = 0,

    /// <summary>
    /// 已读
    /// </summary>
    Read = 1,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted = 2
}
