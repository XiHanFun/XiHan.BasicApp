#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright (c)2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageChannel
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 消息通道
/// </summary>
[Flags]
public enum MessageChannel
{
    /// <summary>
    /// 站内通知
    /// </summary>
    [Description("站内通知")]
    SiteNotification = 1,

    /// <summary>
    /// 邮件
    /// </summary>
    [Description("邮件")]
    Email = 2,

    /// <summary>
    /// 短信
    /// </summary>
    [Description("短信")]
    Sms = 4
}
