// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Chat;

/// <summary>
/// 聊天模块提供的能力。
/// </summary>
public static class ChatCapabilities
{
    /// <summary>
    /// 在线聊天能力：会话、消息、聊天 Hub 与聊天审计。
    /// </summary>
    public const string Chat = AppInfo.CapabilityPrefix + "chat";

    /// <summary>
    /// 能力版本。
    /// </summary>
    public const int Version = 1;
}
