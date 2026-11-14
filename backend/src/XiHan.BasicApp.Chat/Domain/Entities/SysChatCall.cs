// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Chat.Domain.Entities;

/// <summary>
/// 聊天通话（租户库实体）
/// </summary>
/// <remarks>
/// 一次语音或视频通话的状态：媒体走浏览器之间的 WebRTC，服务端只记谁叫了谁、接没接、几时开始几时结束，不经手也不保存音视频
/// 单独成表而不是只留一条消息：响铃与通话中是会变的状态，多端要同步（一端接听其余端停铃），刷新页面要能认回来，忙线也按它判断；结束后才往会话里追加一条通话记录消息（<see cref="MessageId"/>）
/// 落租户库、跟着会话走：两侧都能通话，独立库的租户记在它自己的库里
/// 参与者单独成表（<see cref="SysChatCallParticipant"/>）：每个人各有应答与进出时间，群通话是多人各自进出
/// 状态的改动都先对会话行加锁再改：两个人同时互相呼叫、两个端同时接听都在锁里排出先后
/// 没有删除入口：通话记录跟消息一样是会话的历史；状态几秒一变，不进差异日志
/// </remarks>
[SugarTable(TableName = "sys_chat_call", TableDescription = "聊天通话（租户库实体）")]
[SugarIndex("ix_sys_chat_call_tenant_id_conversation_id", nameof(TenantId), OrderByType.Asc, nameof(ConversationId), OrderByType.Asc)]
[SugarIndex("ix_sys_chat_call_tenant_id_status", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysChatCall : TenantScopedAuditedEntity
{
    /// <summary>
    /// 会话
    /// </summary>
    [SugarColumn(ColumnName = "conversation_id", IsNullable = false, ColumnDescription = "会话")]
    public Guid ConversationId { get; set; }

    /// <summary>
    /// 发起人
    /// </summary>
    [SugarColumn(ColumnName = "initiator_user_id", IsNullable = false, ColumnDescription = "发起人")]
    public Guid InitiatorUserId { get; set; }

    /// <summary>
    /// 媒体
    /// </summary>
    [SugarColumn(ColumnName = "media", IsNullable = false, ColumnDescription = "媒体")]
    public ChatCallMedia Media { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public ChatCallStatus Status { get; set; }

    /// <summary>
    /// 结束的原因
    /// </summary>
    /// <remarks>
    /// 没结束为空
    /// </remarks>
    [SugarColumn(ColumnName = "end_reason", IsNullable = true, ColumnDescription = "结束的原因")]
    public ChatCallEndReason? EndReason { get; set; }

    /// <summary>
    /// 响铃到几时（UTC）
    /// </summary>
    /// <remarks>
    /// 发起时按策略的响铃秒数定下，过了还没人接即未接听
    /// </remarks>
    [SugarColumn(ColumnName = "ring_expires_at", IsNullable = false, ColumnDescription = "响铃到几时（UTC）")]
    public DateTimeOffset RingExpiresAt { get; set; }

    /// <summary>
    /// 接通时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "answered_at", IsNullable = true, ColumnDescription = "接通时间（UTC）")]
    public DateTimeOffset? AnsweredAt { get; set; }

    /// <summary>
    /// 结束时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "ended_at", IsNullable = true, ColumnDescription = "结束时间（UTC）")]
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>
    /// 结束后追加到会话里的通话记录消息
    /// </summary>
    [SugarColumn(ColumnName = "message_id", IsNullable = true, ColumnDescription = "结束后追加到会话里的通话记录消息")]
    public Guid? MessageId { get; set; }
}

/// <summary>
/// 通话的媒体
/// </summary>
public enum ChatCallMedia
{
    /// <summary>
    /// 语音
    /// </summary>
    Voice = 1,

    /// <summary>
    /// 视频
    /// </summary>
    Video = 2,

    /// <summary>
    /// 屏幕共享
    /// </summary>
    /// <remarks>
    /// 发起人一上来就共享屏幕、带着麦克风；别人按语音进，进来之后也能共享自己的屏幕
    /// </remarks>
    Screen = 3,
}

/// <summary>
/// 通话的状态
/// </summary>
public enum ChatCallStatus
{
    /// <summary>
    /// 响铃中
    /// </summary>
    Ringing = 1,

    /// <summary>
    /// 通话中
    /// </summary>
    Ongoing = 2,

    /// <summary>
    /// 已结束
    /// </summary>
    Ended = 3,
}

/// <summary>
/// 通话结束的原因
/// </summary>
public enum ChatCallEndReason
{
    /// <summary>
    /// 接通后挂断
    /// </summary>
    Completed = 1,

    /// <summary>
    /// 发起人在接通前取消
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// 对方拒绝
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// 响铃到点没人接
    /// </summary>
    Missed = 4,

    /// <summary>
    /// 接通后有人掉线，由系统结束
    /// </summary>
    Interrupted = 5,
}
