#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysEmail.Enum
// Guid:db42c50f-0832-4eea-82e6-432c308a5c1f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 邮件状态枚举
/// </summary>
public enum EmailStatus
{
    /// <summary>
    /// 待发送
    /// </summary>
    [Description("待发送")]
    Pending = 0,

    /// <summary>
    /// 发送中
    /// </summary>
    [Description("发送中")]
    Sending = 1,

    /// <summary>
    /// 发送成功
    /// </summary>
    [Description("发送成功")]
    Success = 2,

    /// <summary>
    /// 发送失败
    /// </summary>
    [Description("发送失败")]
    Failed = 3,

    /// <summary>
    /// 已取消
    /// </summary>
    [Description("已取消")]
    Cancelled = 4
}

/// <summary>
/// 邮件类型枚举
/// </summary>
public enum EmailType
{
    /// <summary>
    /// 系统邮件
    /// </summary>
    [Description("系统邮件")]
    System = 0,

    /// <summary>
    /// 验证邮件
    /// </summary>
    [Description("验证邮件")]
    Verification = 1,

    /// <summary>
    /// 通知邮件
    /// </summary>
    [Description("通知邮件")]
    Notification = 2,

    /// <summary>
    /// 营销邮件
    /// </summary>
    [Description("营销邮件")]
    Marketing = 3,

    /// <summary>
    /// 自定义邮件
    /// </summary>
    [Description("自定义邮件")]
    Custom = 99
}
