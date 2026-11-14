// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Chat;

/// <summary>
/// 聊天模块的错误码
/// </summary>
/// <remarks>
/// 格式为 <c>CHAT_&lt;KIND&gt;_&lt;NNN&gt;</c>，规则不满足即返回失败
/// </remarks>
public static class ChatErrors
{
    /// <summary>
    /// 会话不存在
    /// </summary>
    public const string ConversationNotFound = "CHAT_NOTFOUND_001";

    /// <summary>
    /// 消息不存在
    /// </summary>
    public const string MessageNotFound = "CHAT_NOTFOUND_002";

    /// <summary>
    /// 用户不存在
    /// </summary>
    public const string UserNotFound = "CHAT_NOTFOUND_003";

    /// <summary>
    /// 部门不存在
    /// </summary>
    public const string DepartmentNotFound = "CHAT_NOTFOUND_004";

    /// <summary>
    /// 附件不存在或不在这个会话里
    /// </summary>
    public const string AttachmentNotFound = "CHAT_NOTFOUND_005";

    /// <summary>
    /// 不是会话成员
    /// </summary>
    public const string NotMember = "CHAT_DENIED_001";

    /// <summary>
    /// 要群主或管理员
    /// </summary>
    public const string ManagerRequired = "CHAT_DENIED_002";

    /// <summary>
    /// 要群主
    /// </summary>
    public const string OwnerRequired = "CHAT_DENIED_003";

    /// <summary>
    /// 被禁言
    /// </summary>
    public const string Silenced = "CHAT_DENIED_004";

    /// <summary>
    /// 不是自己发的消息
    /// </summary>
    public const string NotSender = "CHAT_DENIED_005";

    /// <summary>
    /// 不在这个部门
    /// </summary>
    public const string NotInDepartment = "CHAT_DENIED_006";

    /// <summary>
    /// 不能禁言群主或管理员
    /// </summary>
    public const string SilenceProtected = "CHAT_DENIED_007";

    /// <summary>
    /// 不能改群主的角色
    /// </summary>
    public const string OwnerRoleLocked = "CHAT_DENIED_008";

    /// <summary>
    /// 附件不是自己在聊天里上传的
    /// </summary>
    public const string AttachmentNotOwned = "CHAT_DENIED_009";

    /// <summary>
    /// 不能和自己单聊
    /// </summary>
    public const string SelfConversation = "CHAT_VALIDATION_001";

    /// <summary>
    /// 群聊人数不够
    /// </summary>
    public const string GroupTooSmall = "CHAT_VALIDATION_002";

    /// <summary>
    /// 只有群聊支持
    /// </summary>
    public const string GroupOnly = "CHAT_VALIDATION_003";

    /// <summary>
    /// 单聊不支持
    /// </summary>
    public const string SingleUnsupported = "CHAT_VALIDATION_004";

    /// <summary>
    /// 群主不能退群或被移出
    /// </summary>
    public const string OwnerCannotLeave = "CHAT_VALIDATION_005";

    /// <summary>
    /// 部门群不能改名
    /// </summary>
    public const string DepartmentNameLocked = "CHAT_VALIDATION_006";

    /// <summary>
    /// 文本消息的正文不合法
    /// </summary>
    public const string ContentInvalid = "CHAT_VALIDATION_007";

    /// <summary>
    /// 图片与文件消息没有附件
    /// </summary>
    public const string AttachmentsRequired = "CHAT_VALIDATION_008";

    /// <summary>
    /// 语音消息不合法
    /// </summary>
    public const string VoiceInvalid = "CHAT_VALIDATION_009";

    /// <summary>
    /// 系统提示与助手回复不能直接发送
    /// </summary>
    public const string TypeNotSendable = "CHAT_VALIDATION_010";

    /// <summary>
    /// 提及的人太多
    /// </summary>
    public const string TooManyMentions = "CHAT_VALIDATION_011";

    /// <summary>
    /// 提及了非成员
    /// </summary>
    public const string MentionNotMember = "CHAT_VALIDATION_012";

    /// <summary>
    /// 回复的消息不可用
    /// </summary>
    public const string ReplyInvalid = "CHAT_VALIDATION_013";

    /// <summary>
    /// 过了撤回时限
    /// </summary>
    public const string RecallExpired = "CHAT_VALIDATION_014";

    /// <summary>
    /// 过了编辑时限
    /// </summary>
    public const string EditExpired = "CHAT_VALIDATION_015";

    /// <summary>
    /// 只有文本消息能编辑
    /// </summary>
    public const string EditTextOnly = "CHAT_VALIDATION_016";

    /// <summary>
    /// 消息已撤回
    /// </summary>
    public const string Recalled = "CHAT_VALIDATION_017";

    /// <summary>
    /// 表情无效
    /// </summary>
    public const string EmojiInvalid = "CHAT_VALIDATION_018";

    /// <summary>
    /// 不能对自己这样做
    /// </summary>
    public const string SelfTarget = "CHAT_VALIDATION_019";

    /// <summary>
    /// 角色只能是管理员或普通成员
    /// </summary>
    public const string RoleInvalid = "CHAT_VALIDATION_020";

    /// <summary>
    /// 有用户不属于当前一侧
    /// </summary>
    public const string UsersOutsideSide = "CHAT_VALIDATION_021";

    /// <summary>
    /// 这条消息不能转发
    /// </summary>
    public const string NotForwardable = "CHAT_VALIDATION_022";

    /// <summary>
    /// 包含敏感词
    /// </summary>
    public const string SensitiveWord = "CHAT_VALIDATION_023";

    /// <summary>
    /// 聊天策略参数不合法
    /// </summary>
    public const string PolicyInvalid = "CHAT_VALIDATION_024";

    /// <summary>
    /// 不支持的筛选或排序
    /// </summary>
    public const string QueryUnsupported = "CHAT_VALIDATION_025";

    /// <summary>
    /// 置顶消息到了上限
    /// </summary>
    public const string PinLimit = "CHAT_CONFLICT_001";

    /// <summary>
    /// 这个会话不是助手会话
    /// </summary>
    public const string NotAssistantConversation = "CHAT_VALIDATION_026";

    /// <summary>
    /// 会话里没有可回答的提问
    /// </summary>
    public const string NoQuestion = "CHAT_VALIDATION_027";

    /// <summary>
    /// 助手不可用（AI 模块没有装配）
    /// </summary>
    public const string AssistantUnavailable = "CHAT_UNAVAILABLE_001";

    /// <summary>
    /// 助手回复生成失败
    /// </summary>
    public const string AssistantReplyFailed = "CHAT_UNAVAILABLE_002";

    /// <summary>
    /// 助手没有产生任何内容
    /// </summary>
    public const string AssistantReplyEmpty = "CHAT_UNAVAILABLE_003";

    /// <summary>
    /// 这个会话不能发起通话（助手会话）
    /// </summary>
    public const string CallUnsupported = "CHAT_VALIDATION_028";

    /// <summary>
    /// 通话记录不能撤回
    /// </summary>
    public const string CallRecordLocked = "CHAT_VALIDATION_029";

    /// <summary>
    /// 通话不存在
    /// </summary>
    public const string CallNotFound = "CHAT_NOTFOUND_006";

    /// <summary>
    /// 不是这通电话的参与者
    /// </summary>
    public const string NotCallParticipant = "CHAT_DENIED_010";

    /// <summary>
    /// 自己正在另一通电话里
    /// </summary>
    public const string CallerBusy = "CHAT_CONFLICT_002";

    /// <summary>
    /// 对方正在通话中
    /// </summary>
    public const string CalleeBusy = "CHAT_CONFLICT_003";

    /// <summary>
    /// 对方不在线
    /// </summary>
    public const string CalleeOffline = "CHAT_CONFLICT_004";

    /// <summary>
    /// 这通电话已经不在响铃
    /// </summary>
    public const string CallNotRinging = "CHAT_CONFLICT_005";

    /// <summary>
    /// 群通话没有邀请任何人
    /// </summary>
    public const string CallInviteesRequired = "CHAT_VALIDATION_030";

    /// <summary>
    /// 邀请了不是会话成员的人
    /// </summary>
    public const string CallInviteeNotMember = "CHAT_VALIDATION_031";

    /// <summary>
    /// 邀请的人数超过了通话的人数上限
    /// </summary>
    public const string CallTooManyParticipants = "CHAT_VALIDATION_032";

    /// <summary>
    /// 这个会话里已经有一通电话
    /// </summary>
    public const string CallInProgress = "CHAT_CONFLICT_006";

    /// <summary>
    /// 邀请的人都不在线或正在通话中
    /// </summary>
    public const string CallNobodyAvailable = "CHAT_CONFLICT_007";

    /// <summary>
    /// 通话人数已满
    /// </summary>
    public const string CallFull = "CHAT_CONFLICT_008";

    /// <summary>
    /// 这通电话已经结束
    /// </summary>
    public const string CallEnded = "CHAT_CONFLICT_009";
}
