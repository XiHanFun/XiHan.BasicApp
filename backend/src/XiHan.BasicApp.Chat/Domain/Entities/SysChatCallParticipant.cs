// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Chat.Domain.Entities;

/// <summary>
/// 通话的参与者（租户库实体）
/// </summary>
/// <remarks>
/// 通话里的每个人一行：各自的应答（受邀、已加入、拒绝、未接、已离开）与进出时间，发起人建通话时即已加入
/// 忙线按它判断：一个人同一时刻只在一个通话里，受邀或已加入的行就是他正占着的通话
/// 只记用户、不记是哪条连接：信令按用户点发，没接这通电话的端自己忽略，连接断了重连不用改库
/// （租户, 通话, 用户）部分唯一：同一通话里一个人只有一行
/// 状态几秒一变、没有要追溯的字段改动，不进差异日志
/// </remarks>
[SugarTable(TableName = "sys_chat_call_participant", TableDescription = "通话的参与者（租户库实体）")]
[ActiveUniqueIndex(nameof(TenantId), nameof(CallId), nameof(UserId), Name = "ux_sys_chat_call_participant_tenant_id_call_id_user_id")]
[SugarIndex("ix_sys_chat_call_participant_tenant_id_user_id_status", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysChatCallParticipant : TenantScopedAuditedEntity
{
    /// <summary>
    /// 通话
    /// </summary>
    [SugarColumn(ColumnName = "call_id", IsNullable = false, ColumnDescription = "通话")]
    public Guid CallId { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public ChatCallParticipantStatus Status { get; set; }

    /// <summary>
    /// 加入时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "joined_at", IsNullable = true, ColumnDescription = "加入时间（UTC）")]
    public DateTimeOffset? JoinedAt { get; set; }

    /// <summary>
    /// 离开时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "left_at", IsNullable = true, ColumnDescription = "离开时间（UTC）")]
    public DateTimeOffset? LeftAt { get; set; }
}

/// <summary>
/// 参与者在通话里的状态
/// </summary>
public enum ChatCallParticipantStatus
{
    /// <summary>
    /// 受邀，正在响铃
    /// </summary>
    Invited = 1,

    /// <summary>
    /// 已加入
    /// </summary>
    Joined = 2,

    /// <summary>
    /// 拒绝了
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// 没接
    /// </summary>
    Missed = 4,

    /// <summary>
    /// 已离开
    /// </summary>
    Left = 5,
}
