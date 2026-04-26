#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysEmail.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 邮件类型枚举
/// </summary>
public enum EmailType
{
    /// <summary>
    /// 系统邮件
    /// </summary>
    System = 0,

    /// <summary>
    /// 验证邮件
    /// </summary>
    Verification = 1,

    /// <summary>
    /// 通知邮件
    /// </summary>
    Notification = 2,

    /// <summary>
    /// 营销邮件
    /// </summary>
    Marketing = 3,

    /// <summary>
    /// 自定义邮件
    /// </summary>
    Custom = 99
}

/// <summary>
/// 邮件状态枚举
/// </summary>
public enum EmailStatus
{
    /// <summary>
    /// 待发送
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 发送中
    /// </summary>
    Sending = 1,

    /// <summary>
    /// 发送成功
    /// </summary>
    Success = 2,

    /// <summary>
    /// 发送失败
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 4
}

