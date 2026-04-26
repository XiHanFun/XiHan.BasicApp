#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageChannel
// Guid:b2f41f3f-f22f-4ac7-a3f0-0301ecc53f3a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 13:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    SiteNotification = 1,

    /// <summary>
    /// 邮件
    /// </summary>
    Email = 2,

    /// <summary>
    /// 短信
    /// </summary>
    Sms = 4
}
