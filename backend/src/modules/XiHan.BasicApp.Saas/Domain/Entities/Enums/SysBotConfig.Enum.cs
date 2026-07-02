#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysBotConfig.Enum
// Guid:25c613c6-561e-44ed-94d2-c2c8d8c3f4c3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// Webhook 型机器人服务商类型
/// </summary>
public enum BotProviderType
{
    /// <summary>
    /// 钉钉
    /// </summary>
    [Description("钉钉")]
    DingTalk = 0,

    /// <summary>
    /// 飞书
    /// </summary>
    [Description("飞书")]
    Lark = 1,

    /// <summary>
    /// 企业微信
    /// </summary>
    [Description("企业微信")]
    WeCom = 2
}
