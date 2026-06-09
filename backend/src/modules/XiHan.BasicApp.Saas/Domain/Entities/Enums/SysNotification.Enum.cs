#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysNotification.Enum
// Guid:dd46a2aa-4292-4d09-b629-eead81506b16
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
    [Description("系统通知")]
    System = 0,

    /// <summary>
    /// 用户通知
    /// </summary>
    [Description("用户通知")]
    User = 1,

    /// <summary>
    /// 公告
    /// </summary>
    [Description("公告")]
    Announcement = 2,

    /// <summary>
    /// 警告
    /// </summary>
    [Description("警告")]
    Warning = 3,

    /// <summary>
    /// 错误
    /// </summary>
    [Description("错误")]
    Error = 4
}

/// <summary>
/// 通知目标类型枚举
/// 决定通知的接收方范围：全员/角色/部门/指定用户
/// </summary>
public enum NotificationTargetType
{
    /// <summary>
    /// 全员（所有用户）
    /// </summary>
    [Description("全员")]
    All = 0,

    /// <summary>
    /// 角色（按角色推送）
    /// </summary>
    [Description("角色")]
    Role = 1,

    /// <summary>
    /// 部门（按部门推送）
    /// </summary>
    [Description("部门")]
    Department = 2,

    /// <summary>
    /// 指定用户（按用户ID推送）
    /// </summary>
    [Description("用户")]
    User = 3
}
