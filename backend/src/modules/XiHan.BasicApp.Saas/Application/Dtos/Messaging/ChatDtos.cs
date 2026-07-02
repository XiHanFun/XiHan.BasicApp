#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChatDtos
// Guid:4a9c8d1e-3daf-4ed7-bc86-7c8d9e0f1a2b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 打开单聊会话 DTO
/// </summary>
public sealed class ChatSingleConversationOpenDto
{
    /// <summary>
    /// 对端用户ID
    /// </summary>
    public long PeerUserId { get; set; }
}

/// <summary>
/// 创建群聊 DTO
/// </summary>
public sealed class ChatGroupCreateDto
{
    /// <summary>
    /// 群聊名称
    /// </summary>
    public string ConversationName { get; set; } = string.Empty;

    /// <summary>
    /// 初始成员用户ID集合（自动包含创建者为群主）
    /// </summary>
    public List<long> MemberUserIds { get; set; } = [];
}

/// <summary>
/// 打开部门群 DTO
/// </summary>
public sealed class ChatDepartmentConversationOpenDto
{
    /// <summary>
    /// 部门ID
    /// </summary>
    public long DepartmentId { get; set; }
}

/// <summary>
/// 添加群成员 DTO
/// </summary>
public sealed class ChatMemberAddDto
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public long ConversationId { get; set; }

    /// <summary>
    /// 待添加用户ID集合
    /// </summary>
    public List<long> UserIds { get; set; } = [];
}

/// <summary>
/// 移除群成员/退群 DTO
/// </summary>
public sealed class ChatMemberRemoveDto
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public long ConversationId { get; set; }

    /// <summary>
    /// 被移出用户ID（等于当前用户即主动退群）
    /// </summary>
    public long UserId { get; set; }
}

/// <summary>
/// 发送聊天消息 DTO
/// </summary>
public sealed class ChatMessageSendDto
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public long ConversationId { get; set; }

    /// <summary>
    /// 消息类型
    /// </summary>
    public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;

    /// <summary>
    /// 消息内容（文本正文；图片/文件为可选说明）
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 关联文件ID（图片/文件消息必填）
    /// </summary>
    public long? FileId { get; set; }

    /// <summary>
    /// 文件名（冗余快照）
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// 客户端消息ID（乐观上屏去重）
    /// </summary>
    public string? ClientMessageId { get; set; }
}

/// <summary>
/// 标记会话已读 DTO
/// </summary>
public sealed class ChatMarkReadDto
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public long ConversationId { get; set; }

    /// <summary>
    /// 已读位（读到的最后一条消息ID，可空）
    /// </summary>
    public long? UpToMessageId { get; set; }
}

/// <summary>
/// 消息历史查询 DTO（游标分页）
/// </summary>
public sealed class ChatMessageHistoryQueryDto
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public long ConversationId { get; set; }

    /// <summary>
    /// 游标：取该消息ID之前（不含）的历史；空取最新一页
    /// </summary>
    public long? BeforeMessageId { get; set; }

    /// <summary>
    /// 每页条数（1-100，默认 20）
    /// </summary>
    public int Take { get; set; } = 20;
}

/// <summary>
/// 会话摘要 DTO（打开/创建会话返回）
/// </summary>
public sealed class ChatConversationDto
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public long ConversationId { get; set; }

    /// <summary>
    /// 会话类型
    /// </summary>
    public ChatConversationType ConversationType { get; set; }

    /// <summary>
    /// 会话名称（单聊为空）
    /// </summary>
    public string? ConversationName { get; set; }

    /// <summary>
    /// 是否本次新建
    /// </summary>
    public bool Created { get; set; }
}

/// <summary>
/// 会话列表项 DTO
/// </summary>
public sealed class ChatConversationListItemDto
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public long ConversationId { get; set; }

    /// <summary>
    /// 会话类型
    /// </summary>
    public ChatConversationType ConversationType { get; set; }

    /// <summary>
    /// 展示名称（单聊=对端用户名；群聊/部门群=会话名）
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 会话头像（单聊=对端头像）
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 单聊对端用户ID
    /// </summary>
    public long? PeerUserId { get; set; }

    /// <summary>
    /// 部门ID（部门群）
    /// </summary>
    public long? DepartmentId { get; set; }

    /// <summary>
    /// 成员数量
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// 我在会话中的角色
    /// </summary>
    public ChatMemberRole MemberRole { get; set; }

    /// <summary>
    /// 我的未读消息数
    /// </summary>
    public int UnreadCount { get; set; }

    /// <summary>
    /// 是否免打扰
    /// </summary>
    public bool IsMuted { get; set; }

    /// <summary>
    /// 最后一条消息时间
    /// </summary>
    public DateTimeOffset? LastMessageTime { get; set; }

    /// <summary>
    /// 最后一条消息预览
    /// </summary>
    public string? LastMessagePreview { get; set; }
}

/// <summary>
/// 聊天消息项 DTO
/// </summary>
public sealed class ChatMessageItemDto
{
    /// <summary>
    /// 消息ID
    /// </summary>
    public long MessageId { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public long ConversationId { get; set; }

    /// <summary>
    /// 发送人用户ID
    /// </summary>
    public long SenderUserId { get; set; }

    /// <summary>
    /// 发送人用户名（快照）
    /// </summary>
    public string? SenderUserName { get; set; }

    /// <summary>
    /// 消息类型
    /// </summary>
    public ChatMessageType MessageType { get; set; }

    /// <summary>
    /// 消息内容（已撤回为空）
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 关联文件ID
    /// </summary>
    public long? FileId { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// 是否已撤回
    /// </summary>
    public bool IsRecalled { get; set; }

    /// <summary>
    /// 客户端消息ID
    /// </summary>
    public string? ClientMessageId { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}

/// <summary>
/// 消息历史结果 DTO
/// </summary>
public sealed class ChatMessageHistoryResultDto
{
    /// <summary>
    /// 消息项（按时间正序，便于前端直接渲染）
    /// </summary>
    public List<ChatMessageItemDto> Items { get; set; } = [];

    /// <summary>
    /// 是否还有更早的历史
    /// </summary>
    public bool HasMore { get; set; }
}

/// <summary>
/// 会话成员项 DTO
/// </summary>
public sealed class ChatMemberItemDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 成员角色
    /// </summary>
    public ChatMemberRole MemberRole { get; set; }

    /// <summary>
    /// 入群时间
    /// </summary>
    public DateTimeOffset JoinTime { get; set; }
}
