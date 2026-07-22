// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
