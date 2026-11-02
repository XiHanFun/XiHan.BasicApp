// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Chat;

/// <summary>
/// 聊天模块的权限码。
/// </summary>
/// <remarks>
/// 照 4.x 的 4 个码（<c>chat:read / send / manage / audit</c>）按「资源段与操作同词」拆成 5 个，两侧都有：
/// 4.x 的 <c>chat:read</c> 对应会话读与消息读两个码（打开会话、退群、置顶、免打扰、已读这些个人操作也用会话读，照 4.x），
/// <c>chat:send</c> 对应消息发送（撤回、编辑、回应、置顶消息、转发、附件），<c>chat:manage</c> 对应会话管理（建群与群治理），<c>chat:audit</c> 对应聊天审计。
/// 发送与管理进操作日志（4.x 同），读与审计不进。
/// </remarks>
public static class ChatPermissions
{
    /// <summary>
    /// 查看与参与会话。
    /// </summary>
    public const string ChatConversationsRead = "chat.chat-conversations.read";

    /// <summary>
    /// 建群与群治理。
    /// </summary>
    public const string ChatConversationsManage = "chat.chat-conversations.manage";

    /// <summary>
    /// 查看消息。
    /// </summary>
    public const string ChatMessagesRead = "chat.chat-messages.read";

    /// <summary>
    /// 发送消息。
    /// </summary>
    public const string ChatMessagesSend = "chat.chat-messages.send";

    /// <summary>
    /// 聊天审计。
    /// </summary>
    public const string ChatAuditRead = "chat.chat-audit.read";

    /// <summary>
    /// 全部权限描述。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(ChatConversationsRead, "Use chat conversations", PermissionScope.Both, description: "List and open the caller's conversations, read members and read positions, leave, pin, mute and mark conversations as read."),
        new(ChatConversationsManage, "Manage chat groups", PermissionScope.Both, audited: true, description: "Create group chats and govern them as owner or administrator: members, info, announcement, owner, roles and silencing."),
        new(ChatMessagesRead, "Read chat messages", PermissionScope.Both, description: "Read the history, search and pinned messages of conversations the caller belongs to, and sign their attachments."),
        new(ChatMessagesSend, "Send chat messages", PermissionScope.Both, audited: true, description: "Send, recall, edit, react to, pin, forward messages and upload attachments in conversations the caller belongs to."),
        new(ChatAuditRead, "Audit chat messages", PermissionScope.Both, description: "Search every message on the caller's side, view message details and conversation members without being a member."),
    ];
}
