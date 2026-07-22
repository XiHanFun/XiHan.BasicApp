// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Hubs;

/// <summary>
/// 聊天实时推送方法名常量（服务端 → 客户端）
/// </summary>
/// <remarks>
/// 应用级聊天专用方法名；框架通用方法名见 <c>SignalRConstants.ClientMethods</c>。
/// 前端按同名字符串 <c>signalR.on(...)</c> 订阅，禁止内联字符串。
/// </remarks>
public static class ChatRealtimeMethods
{
    /// <summary>
    /// 收到聊天消息（负载：消息项 + 会话摘要）
    /// </summary>
    public const string ReceiveChatMessage = "ReceiveChatMessage";

    /// <summary>
    /// 消息被撤回（负载：conversationId、messageId）
    /// </summary>
    public const string ChatMessageRecalled = "ChatMessageRecalled";

    /// <summary>
    /// 会话变更（成员增删/入群/被移出，前端应刷新会话列表；负载：conversationId、changeType）
    /// </summary>
    public const string ChatConversationChanged = "ChatConversationChanged";

    /// <summary>
    /// 对端输入中提示（Hub 组内广播，不落库；负载：conversationId、userId、userName）
    /// </summary>
    public const string ChatTyping = "ChatTyping";

    /// <summary>
    /// 消息被编辑（负载：conversationId、messageId、content、editedTime）
    /// </summary>
    public const string ChatMessageEdited = "ChatMessageEdited";

    /// <summary>
    /// 表情回应变更（负载：conversationId、messageId、emoji、userId、userName、added）
    /// </summary>
    public const string ChatReactionChanged = "ChatReactionChanged";

    /// <summary>
    /// 成员已读位变更（负载：conversationId、userId、lastReadMessageId；群已读回执实时刷新）
    /// </summary>
    public const string ChatReadPositionChanged = "ChatReadPositionChanged";

    /// <summary>
    /// 会话的 SignalR 组名（打开会话页时 JoinConversation 加入，用于 typing 等轻量组播）
    /// </summary>
    public static string ConversationGroup(long conversationId)
    {
        return $"chat:conv:{conversationId}";
    }
}
