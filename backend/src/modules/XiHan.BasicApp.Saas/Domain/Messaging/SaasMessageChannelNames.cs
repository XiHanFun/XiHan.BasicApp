// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Messaging;

/// <summary>
/// SaaS 消息通道名称常量（单一事实源）
/// </summary>
/// <remarks>
/// 与框架 Messaging 信封（MessageEnvelope.Channel）及各 IMessageSender.CanHandle 的通道字符串对应：
/// 投递服务构建信封、发件箱入队/领取、发送器路由均以常量引用，禁止散落魔法字符串。
/// 新增通道必须在此登记，并同步 <c>MessageChannel</c> 枚举。
/// </remarks>
public static class SaasMessageChannelNames
{
    /// <summary>
    /// 邮件通道
    /// </summary>
    public const string Email = "email";

    /// <summary>
    /// 短信通道
    /// </summary>
    public const string Sms = "sms";

    /// <summary>
    /// 机器人通道（仅直发，不支持发件箱重放）
    /// </summary>
    public const string Bot = "bot";
}
