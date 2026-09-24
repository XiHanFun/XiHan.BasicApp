// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Chat.Domain.Configurations;

/// <summary>
/// 在线聊天配置键常量
/// </summary>
public static class ChatConfigKeys
{
    /// <summary>
    /// 配置分组
    /// </summary>
    public const string Group = "chat";

    /// <summary>
    /// 聊天策略（JSON，<see cref="ChatPolicySettings"/>）：消息保留天数与敏感词
    /// </summary>
    public const string Policy = "chat.policy";
}
