#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright (c)2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysSms.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 短信状态枚举
/// </summary>
public enum SmsStatus
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
/// 短信类型枚举
/// </summary>
public enum SmsType
{
    /// <summary>
    /// 验证码
    /// </summary>
    [Description("验证码")]
    VerificationCode = 0,

    /// <summary>
    /// 通知短信
    /// </summary>
    [Description("通知短信")]
    Notification = 1,

    /// <summary>
    /// 营销短信
    /// </summary>
    [Description("营销短信")]
    Marketing = 2,

    /// <summary>
    /// 自定义短信
    /// </summary>
    [Description("自定义短信")]
    Custom = 99
}
