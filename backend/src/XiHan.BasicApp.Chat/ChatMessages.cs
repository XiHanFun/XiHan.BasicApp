// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Chat;

/// <summary>
/// 聊天模块的消息键
/// </summary>
/// <remarks>
/// 各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>（资源段与动词的 <c>-</c> 换成 <c>_</c>）
/// 系统提示与会话预览按发起调用的区域写进库里
/// </remarks>
public static class ChatMessages
{
    /// <summary>
    /// 消息目录名
    /// </summary>
    public const string CatalogName = "basicapp.chat";

    /// <summary>
    /// 操作 <c>chat-conversations.mine</c> 的说明
    /// </summary>
    public const string MineChatConversationDescription = "chat_conversations.mine.description";

    /// <summary>
    /// 操作 <c>chat-conversations.members</c> 的说明
    /// </summary>
    public const string MembersChatConversationDescription = "chat_conversations.members.description";

    /// <summary>
    /// 操作 <c>chat-conversations.read-positions</c> 的说明
    /// </summary>
    public const string ReadPositionsChatConversationDescription = "chat_conversations.read_positions.description";

    /// <summary>
    /// 操作 <c>chat-conversations.department-tree</c> 的说明
    /// </summary>
    public const string DepartmentTreeChatConversationDescription = "chat_conversations.department_tree.description";

    /// <summary>
    /// 操作 <c>chat-conversations.user-options</c> 的说明
    /// </summary>
    public const string UserOptionsChatConversationDescription = "chat_conversations.user_options.description";

    /// <summary>
    /// 操作 <c>chat-conversations.user-profile</c> 的说明
    /// </summary>
    public const string UserProfileChatConversationDescription = "chat_conversations.user_profile.description";

    /// <summary>
    /// 操作 <c>chat-messages.search-all</c> 的说明
    /// </summary>
    public const string SearchAllChatMessageDescription = "chat_messages.search_all.description";

    /// <summary>
    /// 操作 <c>chat-messages.media</c> 的说明
    /// </summary>
    public const string MediaChatMessageDescription = "chat_messages.media.description";

    /// <summary>
    /// 操作 <c>chat-conversations.open-single</c> 的说明
    /// </summary>
    public const string OpenSingleChatConversationDescription = "chat_conversations.open_single.description";

    /// <summary>
    /// 操作 <c>chat-conversations.create-group</c> 的说明
    /// </summary>
    public const string CreateGroupChatConversationDescription = "chat_conversations.create_group.description";

    /// <summary>
    /// 操作 <c>chat-conversations.open-department</c> 的说明
    /// </summary>
    public const string OpenDepartmentChatConversationDescription = "chat_conversations.open_department.description";

    /// <summary>
    /// 操作 <c>chat-conversations.add-members</c> 的说明
    /// </summary>
    public const string AddMembersChatConversationDescription = "chat_conversations.add_members.description";

    /// <summary>
    /// 操作 <c>chat-conversations.remove-members</c> 的说明
    /// </summary>
    public const string RemoveMembersChatConversationDescription = "chat_conversations.remove_members.description";

    /// <summary>
    /// 操作 <c>chat-conversations.leave</c> 的说明
    /// </summary>
    public const string LeaveChatConversationDescription = "chat_conversations.leave.description";

    /// <summary>
    /// 操作 <c>chat-conversations.update-info</c> 的说明
    /// </summary>
    public const string UpdateInfoChatConversationDescription = "chat_conversations.update_info.description";

    /// <summary>
    /// 操作 <c>chat-conversations.transfer-owner</c> 的说明
    /// </summary>
    public const string TransferOwnerChatConversationDescription = "chat_conversations.transfer_owner.description";

    /// <summary>
    /// 操作 <c>chat-conversations.dissolve</c> 的说明
    /// </summary>
    public const string DissolveChatConversationDescription = "chat_conversations.dissolve.description";

    /// <summary>
    /// 操作 <c>chat-conversations.silence-members</c> 的说明
    /// </summary>
    public const string SilenceMembersChatConversationDescription = "chat_conversations.silence_members.description";

    /// <summary>
    /// 操作 <c>chat-conversations.set-member-role</c> 的说明
    /// </summary>
    public const string SetMemberRoleChatConversationDescription = "chat_conversations.set_member_role.description";

    /// <summary>
    /// 操作 <c>chat-conversations.pin</c> 的说明
    /// </summary>
    public const string PinChatConversationDescription = "chat_conversations.pin.description";

    /// <summary>
    /// 操作 <c>chat-conversations.mute</c> 的说明
    /// </summary>
    public const string MuteChatConversationDescription = "chat_conversations.mute.description";

    /// <summary>
    /// 操作 <c>chat-conversations.mark-read</c> 的说明
    /// </summary>
    public const string MarkReadChatConversationDescription = "chat_conversations.mark_read.description";

    /// <summary>
    /// 操作 <c>chat-messages.history</c> 的说明
    /// </summary>
    public const string HistoryChatMessageDescription = "chat_messages.history.description";

    /// <summary>
    /// 操作 <c>chat-messages.search</c> 的说明
    /// </summary>
    public const string SearchChatMessageDescription = "chat_messages.search.description";

    /// <summary>
    /// 操作 <c>chat-messages.pinned</c> 的说明
    /// </summary>
    public const string PinnedChatMessageDescription = "chat_messages.pinned.description";

    /// <summary>
    /// 操作 <c>chat-messages.send</c> 的说明
    /// </summary>
    public const string SendChatMessageDescription = "chat_messages.send.description";

    /// <summary>
    /// 操作 <c>chat-messages.recall</c> 的说明
    /// </summary>
    public const string RecallChatMessageDescription = "chat_messages.recall.description";

    /// <summary>
    /// 操作 <c>chat-messages.edit</c> 的说明
    /// </summary>
    public const string EditChatMessageDescription = "chat_messages.edit.description";

    /// <summary>
    /// 操作 <c>chat-messages.react</c> 的说明
    /// </summary>
    public const string ReactChatMessageDescription = "chat_messages.react.description";

    /// <summary>
    /// 操作 <c>chat-messages.pin</c> 的说明
    /// </summary>
    public const string PinChatMessageDescription = "chat_messages.pin.description";

    /// <summary>
    /// 操作 <c>chat-messages.unpin</c> 的说明
    /// </summary>
    public const string UnpinChatMessageDescription = "chat_messages.unpin.description";

    /// <summary>
    /// 操作 <c>chat-messages.forward</c> 的说明
    /// </summary>
    public const string ForwardChatMessageDescription = "chat_messages.forward.description";

    /// <summary>
    /// 操作 <c>chat-messages.fast-upload</c> 的说明
    /// </summary>
    public const string FastUploadChatMessageDescription = "chat_messages.fast_upload.description";

    /// <summary>
    /// 操作 <c>chat-messages.presign</c> 的说明
    /// </summary>
    public const string PresignChatMessageDescription = "chat_messages.presign.description";

    /// <summary>
    /// 操作 <c>chat-audit.list</c> 的说明
    /// </summary>
    public const string ListChatAuditDescription = "chat_audit.list.description";

    /// <summary>
    /// 操作 <c>chat-audit.get</c> 的说明
    /// </summary>
    public const string GetChatAuditDescription = "chat_audit.get.description";

    /// <summary>
    /// 操作 <c>chat-audit.members</c> 的说明
    /// </summary>
    public const string MembersChatAuditDescription = "chat_audit.members.description";

    /// <summary>
    /// 会话不存在
    /// </summary>
    public const string ConversationNotFound = "chat.conversation.not_found";

    /// <summary>
    /// 消息不存在
    /// </summary>
    public const string MessageNotFound = "chat.message.not_found";

    /// <summary>
    /// 用户不存在
    /// </summary>
    public const string UserNotFound = "chat.user.not_found";

    /// <summary>
    /// 部门不存在
    /// </summary>
    public const string DepartmentNotFound = "chat.department.not_found";

    /// <summary>
    /// 附件不存在或不在这个会话里
    /// </summary>
    public const string AttachmentNotFound = "chat.attachment.not_found";

    /// <summary>
    /// 只有会话成员可以这样做
    /// </summary>
    public const string NotMember = "chat.member.required";

    /// <summary>
    /// 只有群主或管理员可以这样做
    /// </summary>
    public const string ManagerRequired = "chat.member.manager_required";

    /// <summary>
    /// 只有群主可以这样做
    /// </summary>
    public const string OwnerRequired = "chat.member.owner_required";

    /// <summary>
    /// 被禁言的成员不能发送或编辑消息
    /// </summary>
    public const string Silenced = "chat.member.silenced";

    /// <summary>
    /// 只能撤回或编辑自己发的消息
    /// </summary>
    public const string NotSender = "chat.message.not_sender";

    /// <summary>
    /// 只有本部门的成员可以进入部门群
    /// </summary>
    public const string NotInDepartment = "chat.department.not_member";

    /// <summary>
    /// 不能禁言群主或管理员
    /// </summary>
    public const string SilenceProtected = "chat.member.silence_protected";

    /// <summary>
    /// 不能修改群主的角色
    /// </summary>
    public const string OwnerRoleLocked = "chat.member.owner_role_locked";

    /// <summary>
    /// 附件不是你在聊天里上传的
    /// </summary>
    public const string AttachmentNotOwned = "chat.attachment.not_owned";

    /// <summary>
    /// 不能和自己单聊
    /// </summary>
    public const string SelfConversation = "chat.conversation.self";

    /// <summary>
    /// 群聊至少要有两名成员
    /// </summary>
    public const string GroupTooSmall = "chat.group.too_small";

    /// <summary>
    /// 只有群聊支持这个操作
    /// </summary>
    public const string GroupOnly = "chat.conversation.group_only";

    /// <summary>
    /// 单聊不支持这个操作
    /// </summary>
    public const string SingleUnsupported = "chat.conversation.single_unsupported";

    /// <summary>
    /// 群主不能退群或被移出
    /// </summary>
    public const string OwnerCannotLeave = "chat.group.owner_cannot_leave";

    /// <summary>
    /// 部门群的名称不能修改
    /// </summary>
    public const string DepartmentNameLocked = "chat.department.name_locked";

    /// <summary>
    /// 文本消息的正文不合法
    /// </summary>
    public const string ContentInvalid = "chat.message.content_invalid";

    /// <summary>
    /// 图片与文件消息没有附件
    /// </summary>
    public const string AttachmentsRequired = "chat.message.attachments_required";

    /// <summary>
    /// 语音消息不合法
    /// </summary>
    public const string VoiceInvalid = "chat.message.voice_invalid";

    /// <summary>
    /// 这种消息不能直接发送
    /// </summary>
    public const string TypeNotSendable = "chat.message.type_not_sendable";

    /// <summary>
    /// 提及的人太多
    /// </summary>
    public const string TooManyMentions = "chat.message.too_many_mentions";

    /// <summary>
    /// 只能提及会话成员
    /// </summary>
    public const string MentionNotMember = "chat.message.mention_not_member";

    /// <summary>
    /// 被回复的消息不可用
    /// </summary>
    public const string ReplyInvalid = "chat.message.reply_invalid";

    /// <summary>
    /// 过了撤回时限
    /// </summary>
    public const string RecallExpired = "chat.message.recall_expired";

    /// <summary>
    /// 过了编辑时限
    /// </summary>
    public const string EditExpired = "chat.message.edit_expired";

    /// <summary>
    /// 只有文本消息可以编辑
    /// </summary>
    public const string EditTextOnly = "chat.message.edit_text_only";

    /// <summary>
    /// 消息已撤回
    /// </summary>
    public const string Recalled = "chat.message.recalled";

    /// <summary>
    /// 表情无效
    /// </summary>
    public const string EmojiInvalid = "chat.reaction.emoji_invalid";

    /// <summary>
    /// 不能对自己这样做
    /// </summary>
    public const string SelfTarget = "chat.member.self_target";

    /// <summary>
    /// 角色只能是管理员或普通成员
    /// </summary>
    public const string RoleInvalid = "chat.member.role_invalid";

    /// <summary>
    /// 有用户不属于当前一侧
    /// </summary>
    public const string UsersOutsideSide = "chat.member.outside_side";

    /// <summary>
    /// 这条消息不能转发
    /// </summary>
    public const string NotForwardable = "chat.message.not_forwardable";

    /// <summary>
    /// 消息包含敏感词
    /// </summary>
    public const string SensitiveWord = "chat.message.sensitive";

    /// <summary>
    /// 聊天策略参数不合法
    /// </summary>
    public const string PolicyInvalid = "chat.policy.invalid";

    /// <summary>
    /// 不支持的筛选
    /// </summary>
    public const string FilterUnsupported = "chat.query.filter_unsupported";

    /// <summary>
    /// 不支持的排序
    /// </summary>
    public const string SortUnsupported = "chat.query.sort_unsupported";

    /// <summary>
    /// 置顶消息到了上限
    /// </summary>
    public const string PinLimit = "chat.message.pin_limit";

    /// <summary>
    /// 系统提示：建群
    /// </summary>
    public const string SystemGroupCreated = "chat.system.group_created";

    /// <summary>
    /// 系统提示：邀请入群
    /// </summary>
    public const string SystemMembersInvited = "chat.system.members_invited";

    /// <summary>
    /// 系统提示：退群
    /// </summary>
    public const string SystemMemberLeft = "chat.system.member_left";

    /// <summary>
    /// 系统提示：移出群聊
    /// </summary>
    public const string SystemMembersRemoved = "chat.system.members_removed";

    /// <summary>
    /// 系统提示：转让群主
    /// </summary>
    public const string SystemOwnerTransferred = "chat.system.owner_transferred";

    /// <summary>
    /// 系统提示：解散群
    /// </summary>
    public const string SystemGroupDissolved = "chat.system.group_dissolved";

    /// <summary>
    /// 会话预览：群公告
    /// </summary>
    public const string SystemAnnouncement = "chat.system.announcement";

    /// <summary>
    /// 系统提示里名字之间的分隔
    /// </summary>
    public const string SystemNamesSeparator = "chat.system.names_separator";

    /// <summary>
    /// 系统提示：名字太多时折叠
    /// </summary>
    public const string SystemNamesMore = "chat.system.names_more";

    /// <summary>
    /// 系统提示：没有名字时的人数
    /// </summary>
    public const string SystemNamesCount = "chat.system.names_count";

    /// <summary>
    /// 会话预览：一张图片
    /// </summary>
    public const string PreviewImage = "chat.preview.image";

    /// <summary>
    /// 会话预览：多张图片
    /// </summary>
    public const string PreviewImages = "chat.preview.images";

    /// <summary>
    /// 会话预览：语音
    /// </summary>
    public const string PreviewVoice = "chat.preview.voice";

    /// <summary>
    /// 会话预览：一个文件
    /// </summary>
    public const string PreviewFile = "chat.preview.file";

    /// <summary>
    /// 会话预览：多个文件
    /// </summary>
    public const string PreviewFiles = "chat.preview.files";

    /// <summary>
    /// 会话预览：已撤回
    /// </summary>
    public const string PreviewRecalled = "chat.preview.recalled";

    /// <summary>
    /// 会话预览：聊天记录
    /// </summary>
    public const string PreviewRecord = "chat.preview.record";

    /// <summary>
    /// 这个会话不能提及所有人
    /// </summary>
    public const string MentionAllUnsupported = "chat.message.mention_all_unsupported";

    /// <summary>
    /// 一次转发的消息得来自同一个会话
    /// </summary>
    public const string ForwardMixedConversations = "chat.message.forward_mixed";

    /// <summary>
    /// 合并转发的内容太多
    /// </summary>
    public const string RecordTooLarge = "chat.message.record_too_large";

    /// <summary>
    /// 操作 <c>chat-conversations.assistants</c> 的说明
    /// </summary>
    public const string AssistantsChatConversationDescription = "chat_conversations.assistants.description";

    /// <summary>
    /// 操作 <c>chat-conversations.open-assistant</c> 的说明
    /// </summary>
    public const string OpenAssistantChatConversationDescription = "chat_conversations.open_assistant.description";

    /// <summary>
    /// 操作 <c>chat-messages.reply-assistant</c> 的说明
    /// </summary>
    public const string ReplyAssistantChatMessageDescription = "chat_messages.reply_assistant.description";

    /// <summary>
    /// 这个会话不是助手会话
    /// </summary>
    public const string NotAssistantConversation = "chat.assistant.not_assistant";

    /// <summary>
    /// 会话里没有可回答的提问
    /// </summary>
    public const string NoQuestion = "chat.assistant.no_question";

    /// <summary>
    /// 助手不可用（AI 模块没有装配）
    /// </summary>
    public const string AssistantUnavailable = "chat.assistant.unavailable";

    /// <summary>
    /// 助手回复生成失败（参数 <c>reason</c>）
    /// </summary>
    public const string AssistantReplyFailed = "chat.assistant.reply_failed";

    /// <summary>
    /// 助手没有产生任何内容
    /// </summary>
    public const string AssistantReplyEmpty = "chat.assistant.reply_empty";

    /// <summary>
    /// 保留清理作业的标题
    /// </summary>
    public const string RetentionJobTitle = "jobs.chat.messages.cleanup.title";

    /// <summary>
    /// 保留清理作业的说明
    /// </summary>
    public const string RetentionJobDescription = "jobs.chat.messages.cleanup.description";

    /// <summary>
    /// 操作 <c>chat-calls.start</c> 的说明
    /// </summary>
    public const string StartChatCallDescription = "chat_calls.start.description";

    /// <summary>
    /// 操作 <c>chat-calls.accept</c> 的说明
    /// </summary>
    public const string AcceptChatCallDescription = "chat_calls.accept.description";

    /// <summary>
    /// 操作 <c>chat-calls.reject</c> 的说明
    /// </summary>
    public const string RejectChatCallDescription = "chat_calls.reject.description";

    /// <summary>
    /// 操作 <c>chat-calls.hangup</c> 的说明
    /// </summary>
    public const string HangupChatCallDescription = "chat_calls.hangup.description";

    /// <summary>
    /// 操作 <c>chat-calls.active</c> 的说明
    /// </summary>
    public const string ActiveChatCallDescription = "chat_calls.active.description";

    /// <summary>
    /// 操作 <c>chat-calls.ice-servers</c> 的说明
    /// </summary>
    public const string IceServersChatCallDescription = "chat_calls.ice_servers.description";

    /// <summary>
    /// 操作 <c>chat-calls.join</c> 的说明
    /// </summary>
    public const string JoinChatCallDescription = "chat_calls.join.description";

    /// <summary>
    /// 操作 <c>chat-calls.ongoing</c> 的说明
    /// </summary>
    public const string OngoingChatCallDescription = "chat_calls.ongoing.description";

    /// <summary>
    /// 操作 <c>chat-calls.settings</c> 的说明
    /// </summary>
    public const string SettingsChatCallDescription = "chat_calls.settings.description";

    /// <summary>
    /// 这个会话不能发起通话
    /// </summary>
    public const string CallUnsupported = "chat.call.unsupported";

    /// <summary>
    /// 群通话至少要邀请一名成员
    /// </summary>
    public const string CallInviteesRequired = "chat.call.invitees_required";

    /// <summary>
    /// 只能邀请这个会话的成员
    /// </summary>
    public const string CallInviteeNotMember = "chat.call.invitee_not_member";

    /// <summary>
    /// 通话人数超过上限（参数 <c>max</c>）
    /// </summary>
    public const string CallTooManyParticipants = "chat.call.too_many";

    /// <summary>
    /// 这个会话里已经有一通电话
    /// </summary>
    public const string CallInProgress = "chat.call.in_progress";

    /// <summary>
    /// 邀请的人都不在线或正在通话中
    /// </summary>
    public const string CallNobodyAvailable = "chat.call.nobody_available";

    /// <summary>
    /// 通话人数已满
    /// </summary>
    public const string CallFull = "chat.call.full";

    /// <summary>
    /// 这通电话已经结束
    /// </summary>
    public const string CallEnded = "chat.call.ended";

    /// <summary>
    /// 通话记录不能撤回
    /// </summary>
    public const string CallRecordLocked = "chat.call.record_locked";

    /// <summary>
    /// 通话不存在
    /// </summary>
    public const string CallNotFound = "chat.call.not_found";

    /// <summary>
    /// 不是这通电话的参与者
    /// </summary>
    public const string NotCallParticipant = "chat.call.not_participant";

    /// <summary>
    /// 自己正在另一通电话里
    /// </summary>
    public const string CallerBusy = "chat.call.caller_busy";

    /// <summary>
    /// 对方正在通话中
    /// </summary>
    public const string CalleeBusy = "chat.call.callee_busy";

    /// <summary>
    /// 对方不在线
    /// </summary>
    public const string CalleeOffline = "chat.call.callee_offline";

    /// <summary>
    /// 这通电话已经被接听或已结束
    /// </summary>
    public const string CallNotRinging = "chat.call.not_ringing";

    /// <summary>
    /// 会话预览：语音通话
    /// </summary>
    public const string PreviewVoiceCall = "chat.preview.voice_call";

    /// <summary>
    /// 会话预览：视频通话
    /// </summary>
    public const string PreviewVideoCall = "chat.preview.video_call";

    /// <summary>
    /// 通话收尾作业的标题
    /// </summary>
    public const string CallSweepJobTitle = "jobs.chat.calls.sweep.title";

    /// <summary>
    /// 通话收尾作业的说明
    /// </summary>
    public const string CallSweepJobDescription = "jobs.chat.calls.sweep.description";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(ChatMessages)));

    /// <summary>
    /// 消息目录
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
