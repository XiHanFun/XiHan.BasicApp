// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Chat;

/// <summary>
/// 聊天模块的权限码
/// </summary>
/// <remarks>
/// 资源段与操作同词，共 7 个码，两侧都有：会话读与消息读（打开会话、退群、置顶、免打扰、已读这些个人操作也用会话读），
/// 消息发送（撤回、编辑、回应、置顶消息、转发、附件），会话管理（建群与群治理），聊天审计，通话读（当前通话、会话里的通话、设置与穿透服务器）与通话（发起、接听、拒绝、加入、挂断）
/// 发送、管理与通话进操作日志，读与审计不进
/// </remarks>
public static class ChatPermissions
{
    /// <summary>
    /// 查看与参与会话
    /// </summary>
    public const string ChatConversationsRead = "chat.chat-conversations.read";

    /// <summary>
    /// 建群与群治理
    /// </summary>
    public const string ChatConversationsManage = "chat.chat-conversations.manage";

    /// <summary>
    /// 查看消息
    /// </summary>
    public const string ChatMessagesRead = "chat.chat-messages.read";

    /// <summary>
    /// 发送消息
    /// </summary>
    public const string ChatMessagesSend = "chat.chat-messages.send";

    /// <summary>
    /// 聊天审计
    /// </summary>
    public const string ChatAuditRead = "chat.chat-audit.read";

    /// <summary>
    /// 查看当前通话
    /// </summary>
    public const string ChatCallsRead = "chat.chat-calls.read";

    /// <summary>
    /// 语音与视频通话
    /// </summary>
    public const string ChatCallsCall = "chat.chat-calls.call";

    /// <summary>
    /// 全部权限描述
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(ChatConversationsRead, "Use chat conversations", PermissionScope.Both, description: "List and open the caller's conversations, read members and read positions, leave, pin, mute and mark conversations as read."),
        new(ChatConversationsManage, "Manage chat groups", PermissionScope.Both, audited: true, description: "Create group chats and govern them as owner or administrator: members, info, announcement, owner, roles and silencing."),
        new(ChatMessagesRead, "Read chat messages", PermissionScope.Both, description: "Read the history, search and pinned messages of conversations the caller belongs to, and sign their attachments."),
        new(ChatMessagesSend, "Send chat messages", PermissionScope.Both, audited: true, description: "Send, recall, edit, react to, pin, forward messages and upload attachments in conversations the caller belongs to."),
        new(ChatAuditRead, "Audit chat messages", PermissionScope.Both, description: "Search every message on the caller's side, view message details and conversation members without being a member."),
        new(ChatCallsRead, "Read chat calls", PermissionScope.Both, description: "See the call the caller is currently in, the ongoing call of a conversation the caller belongs to, the call settings and the ICE servers used to connect."),
        new(ChatCallsCall, "Make voice and video calls", PermissionScope.Both, audited: true, description: "Start, accept, reject, join and hang up voice and video calls in conversations the caller belongs to."),
    ];
}
