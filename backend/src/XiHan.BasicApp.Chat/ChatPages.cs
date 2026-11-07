// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Chat;

/// <summary>
/// 聊天模块的页面登记
/// </summary>
/// <remarks>
/// 在线聊天与聊天审计两页，两侧都有。页面码与路由按包
/// </remarks>
public static class ChatPages
{
    /// <summary>
    /// 在线聊天页
    /// </summary>
    public static PageDescriptor Conversations { get; } = new("chat.conversations", "/chat/conversations", "chat.conversations.title", "lucide:messages-square", 710, ChatPermissions.ChatConversationsRead, directory: MenuDirectories.MessageCenter);

    /// <summary>
    /// 聊天审计页
    /// </summary>
    public static PageDescriptor Audit { get; } = new("chat.audit", "/chat/audit", "chat.audit.title", "lucide:shield-check", 720, ChatPermissions.ChatAuditRead, directory: MenuDirectories.MessageCenter);

    /// <summary>
    /// 全部顶层页面
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Conversations, Audit];
}
