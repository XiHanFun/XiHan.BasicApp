// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Chat;

/// <summary>
/// 聊天 Hub 的方法名
/// </summary>
/// <remarks>
/// 服务端推 10 个方法（助手的两个随 N4 发出），客户端可调 3 个；<c>contracts export</c> 一并写进 <c>realtime.json</c>
/// </remarks>
public static class ChatRealtimeMethods
{
    /// <summary>
    /// 收到消息（含自己发的，多端同步）
    /// </summary>
    public const string ReceiveChatMessage = "ReceiveChatMessage";

    /// <summary>
    /// 消息被撤回
    /// </summary>
    public const string ChatMessageRecalled = "ChatMessageRecalled";

    /// <summary>
    /// 会话变更
    /// </summary>
    /// <remarks>
    /// 成员、信息、群主、角色、禁言、置顶消息、个人设置与部门同步，种类见 <see cref="ChangeTypes"/>
    /// </remarks>
    public const string ChatConversationChanged = "ChatConversationChanged";

    /// <summary>
    /// 有人正在输入
    /// </summary>
    public const string ChatTyping = "ChatTyping";

    /// <summary>
    /// 消息被编辑
    /// </summary>
    public const string ChatMessageEdited = "ChatMessageEdited";

    /// <summary>
    /// 表情回应变了
    /// </summary>
    public const string ChatReactionChanged = "ChatReactionChanged";

    /// <summary>
    /// 成员的已读位变了
    /// </summary>
    public const string ChatReadPositionChanged = "ChatReadPositionChanged";

    /// <summary>
    /// 有人上线或离线了
    /// </summary>
    /// <remarks>
    /// 发给他的单聊对端，以及正开着他所在的群或部门群的连接
    /// </remarks>
    public const string ChatPresenceChanged = "ChatPresenceChanged";

    /// <summary>
    /// 助手回复的流式增量（N4）
    /// </summary>
    public const string ChatAssistantDelta = "ChatAssistantDelta";

    /// <summary>
    /// 助手回复结束（N4）
    /// </summary>
    public const string ChatAssistantCompleted = "ChatAssistantCompleted";

    /// <summary>
    /// 客户端可调：进入会话
    /// </summary>
    /// <remarks>
    /// 核对成员后加入会话组，只为正在输入的组播
    /// </remarks>
    public const string JoinConversation = "JoinConversation";

    /// <summary>
    /// 客户端可调：离开会话
    /// </summary>
    public const string LeaveConversation = "LeaveConversation";

    /// <summary>
    /// 客户端可调：正在输入
    /// </summary>
    /// <remarks>
    /// 核对成员后组播给会话里的其他连接，不落库
    /// </remarks>
    public const string Typing = "Typing";

    /// <summary>
    /// 服务端推送的全部方法名（按字母序）
    /// </summary>
    public static IReadOnlyList<string> All { get; } =
    [
        ChatAssistantCompleted, ChatAssistantDelta, ChatConversationChanged, ChatMessageEdited, ChatMessageRecalled,
        ChatPresenceChanged, ChatReactionChanged, ChatReadPositionChanged, ChatTyping, ReceiveChatMessage,
    ];

    /// <summary>
    /// 客户端可调的全部方法名（按字母序）
    /// </summary>
    public static IReadOnlyList<string> Invocations { get; } = [JoinConversation, LeaveConversation, Typing];

    /// <summary>
    /// 会话变更的种类
    /// </summary>
    public static class ChangeTypes
    {
        /// <summary>
        /// 建群
        /// </summary>
        public const string Created = "created";

        /// <summary>
        /// 加了成员
        /// </summary>
        public const string MemberAdded = "member-added";

        /// <summary>
        /// 移出了成员或有人退群
        /// </summary>
        public const string MemberRemoved = "member-removed";

        /// <summary>
        /// 群信息变了
        /// </summary>
        public const string InfoChanged = "info-changed";

        /// <summary>
        /// 转让了群主
        /// </summary>
        public const string OwnerTransferred = "owner-transferred";

        /// <summary>
        /// 禁言或解除
        /// </summary>
        public const string MemberSilenced = "member-silenced";

        /// <summary>
        /// 成员角色变了
        /// </summary>
        public const string MemberRoleChanged = "member-role-changed";

        /// <summary>
        /// 置顶消息变了
        /// </summary>
        public const string PinnedChanged = "pinned-changed";

        /// <summary>
        /// 个人的置顶、免打扰变了（只推本人）
        /// </summary>
        public const string MemberSettingChanged = "member-setting-changed";

        /// <summary>
        /// 随部门归属进了部门群
        /// </summary>
        public const string DepartmentJoined = "department-joined";

        /// <summary>
        /// 随部门归属移出了部门群
        /// </summary>
        public const string DepartmentKicked = "department-kicked";
    }
}
