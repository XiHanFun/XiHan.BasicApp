#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
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
/// 短信类型枚举
/// </summary>
public enum SmsType
{
    /// <summary>
    /// 验证码
    /// </summary>
    VerificationCode = 0,

    /// <summary>
    /// 通知短信
    /// </summary>
    Notification = 1,

    /// <summary>
    /// 营销短信
    /// </summary>
    Marketing = 2,

    /// <summary>
    /// 自定义短信
    /// </summary>
    Custom = 99
}

/// <summary>
/// 短信状态枚举
/// </summary>
public enum SmsStatus
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

