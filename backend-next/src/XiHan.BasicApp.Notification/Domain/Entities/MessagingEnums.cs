// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 投递渠道。
/// </summary>
/// <remarks>
/// 按位组合存在通知上；站内信必有。
/// </remarks>
[Flags]
public enum MessageChannels
{
    /// <summary>
    /// 不投递。
    /// </summary>
    None = 0,

    /// <summary>
    /// 站内信（收件箱与实时推送）。
    /// </summary>
    InApp = 1,

    /// <summary>
    /// 邮件。
    /// </summary>
    Email = 2,

    /// <summary>
    /// 短信。
    /// </summary>
    Sms = 4,

    /// <summary>
    /// 机器人（钉钉、飞书、企业微信的群机器人）。
    /// </summary>
    /// <remarks>
    /// 一条通知发到本侧每个服务商的缺省机器人。
    /// </remarks>
    Bot = 8,
}

/// <summary>
/// 一种投递渠道（请求与响应里按名称列出，不用按位组合的数）。
/// </summary>
public enum DeliveryChannel
{
    /// <summary>
    /// 站内信。
    /// </summary>
    InApp = 1,

    /// <summary>
    /// 邮件。
    /// </summary>
    Email = 2,

    /// <summary>
    /// 短信。
    /// </summary>
    Sms = 4,

    /// <summary>
    /// 机器人。
    /// </summary>
    Bot = 8,
}

/// <summary>
/// 消息模板的渠道。
/// </summary>
/// <remarks>
/// 4.x 同四种，与 <see cref="MessageChannels"/> 同值。
/// </remarks>
public enum TemplateChannel
{
    /// <summary>
    /// 站内通知（主题即通知标题，正文是纯文本）。
    /// </summary>
    /// <remarks>
    /// 内置的发送都不用它（4.x 同）。
    /// </remarks>
    InApp = 1,

    /// <summary>
    /// 邮件（有主题，正文可以是 HTML）。
    /// </summary>
    Email = 2,

    /// <summary>
    /// 短信（纯文本）。
    /// </summary>
    /// <remarks>
    /// 云厂商发送要在短信配置里把模板编码映射到服务商的模板 ID。
    /// </remarks>
    Sms = 4,

    /// <summary>
    /// 机器人（纯文本，不用主题）。
    /// </summary>
    /// <remarks>
    /// 通知发到机器人时按 <c>notification-bot</c> 渲染。
    /// </remarks>
    Bot = 8,
}

/// <summary>
/// 群机器人的服务商。
/// </summary>
public enum BotProvider
{
    /// <summary>
    /// 钉钉自定义机器人（可选加签与安全关键词）。
    /// </summary>
    DingTalk = 1,

    /// <summary>
    /// 飞书自定义机器人（可选签名校验与关键词）。
    /// </summary>
    Lark = 2,

    /// <summary>
    /// 企业微信群机器人（地址里带 key，没有签名）。
    /// </summary>
    WeCom = 3,
}

/// <summary>
/// 机器人消息走的通道。
/// </summary>
/// <remarks>
/// 三家群机器人（与 <see cref="BotProvider"/> 同值）与 Telegram（平台托管）。
/// </remarks>
public enum BotChannel
{
    /// <summary>
    /// 钉钉群机器人。
    /// </summary>
    DingTalk = 1,

    /// <summary>
    /// 飞书群机器人。
    /// </summary>
    Lark = 2,

    /// <summary>
    /// 企业微信群机器人。
    /// </summary>
    WeCom = 3,

    /// <summary>
    /// Telegram 机器人（只在平台一侧广播）。
    /// </summary>
    Telegram = 4,
}

/// <summary>
/// 短信服务商。
/// </summary>
public enum SmsProvider
{
    /// <summary>
    /// 阿里云短信（Dysmsapi）。
    /// </summary>
    Aliyun = 1,

    /// <summary>
    /// 腾讯云短信（SMS 2021-01-11）。
    /// </summary>
    Tencent = 2,
}

/// <summary>
/// 邮件与短信发送记录的状态。
/// </summary>
/// <remarks>
/// 4.x 同。
/// </remarks>
public enum MessageSendStatus
{
    /// <summary>
    /// 排队中（含等待重试）。
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 发送中。
    /// </summary>
    Sending = 1,

    /// <summary>
    /// 已发送。
    /// </summary>
    Success = 2,

    /// <summary>
    /// 失败（重试用完）。
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 已取消。
    /// </summary>
    Cancelled = 4,
}
