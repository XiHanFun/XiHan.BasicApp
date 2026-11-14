// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Chat.Domain.Entities;

namespace XiHan.BasicApp.Chat.Operations.ChatConversations;

/// <summary>
/// 没有参数的请求
/// </summary>
public sealed record ChatEmptyRequest;

/// <summary>
/// 按会话的请求
/// </summary>
/// <param name="ConversationId">会话</param>
public sealed record ChatConversationRequest(Guid ConversationId);

/// <summary>
/// <c>chat-conversations.open-single</c> 的请求
/// </summary>
/// <param name="PeerUserId">对端用户</param>
public sealed record OpenSingleConversationRequest(Guid PeerUserId);

/// <summary>
/// <c>chat-conversations.create-group</c> 的请求
/// </summary>
/// <param name="ConversationName">群名</param>
/// <param name="MemberUserIds">成员（不含也会自动加上群主自己）</param>
public sealed record CreateGroupConversationRequest(
    [Required, StringLength(100, MinimumLength = 1)] string ConversationName,
    [Required, MaxLength(500)] IReadOnlyList<Guid> MemberUserIds);

/// <summary>
/// <c>chat-conversations.open-department</c> 的请求
/// </summary>
/// <param name="DepartmentId">部门</param>
public sealed record OpenDepartmentConversationRequest(Guid DepartmentId);

/// <summary>
/// 按会话与一批用户的请求（加人、移出）
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="UserIds">用户</param>
public sealed record ChatConversationUsersRequest(Guid ConversationId, [Required, MinLength(1), MaxLength(500)] IReadOnlyList<Guid> UserIds);

/// <summary>
/// <c>chat-conversations.update-info</c> 的请求
/// </summary>
/// <remarks>
/// 只改给了的字段：群名给空白不改（部门群不能改名），公告、描述与头像给空串即清空
/// </remarks>
/// <param name="ConversationId">会话</param>
/// <param name="ConversationName">群名</param>
/// <param name="Announcement">群公告</param>
/// <param name="Description">群描述</param>
/// <param name="Avatar">头像：文件主键（须是自己在聊天里上传的）或直链</param>
public sealed record UpdateConversationInfoRequest(
    Guid ConversationId,
    [StringLength(100)] string? ConversationName = null,
    [StringLength(2000)] string? Announcement = null,
    [StringLength(500)] string? Description = null,
    [StringLength(500)] string? Avatar = null);

/// <summary>
/// <c>chat-conversations.transfer-owner</c> 的请求
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="NewOwnerUserId">新群主（须是现有成员）</param>
public sealed record TransferOwnerRequest(Guid ConversationId, Guid NewOwnerUserId);

/// <summary>
/// <c>chat-conversations.silence-members</c> 的请求
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="UserIds">成员</param>
/// <param name="IsSilenced">禁言为真，解除为假</param>
public sealed record SilenceMembersRequest(Guid ConversationId, [Required, MinLength(1), MaxLength(500)] IReadOnlyList<Guid> UserIds, bool IsSilenced);

/// <summary>
/// <c>chat-conversations.set-member-role</c> 的请求
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="UserId">成员</param>
/// <param name="MemberRole">新角色（只能是管理员或普通成员）</param>
public sealed record SetMemberRoleRequest(Guid ConversationId, Guid UserId, ChatMemberRole MemberRole);

/// <summary>
/// <c>chat-conversations.pin</c> 的请求
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="IsPinned">置顶为真，取消为假</param>
public sealed record PinConversationRequest(Guid ConversationId, bool IsPinned);

/// <summary>
/// <c>chat-conversations.mute</c> 的请求
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="IsMuted">免打扰为真，取消为假</param>
public sealed record MuteConversationRequest(Guid ConversationId, bool IsMuted);

/// <summary>
/// <c>chat-conversations.mark-read</c> 的请求
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="UpToMessageId">读到的消息；为空即读到最新</param>
public sealed record MarkReadRequest(Guid ConversationId, Guid? UpToMessageId = null);

/// <summary>
/// <c>chat-conversations.user-options</c> 的请求
/// </summary>
/// <remarks>
/// 按显示名排序，<paramref name="Skip"/> 与 <paramref name="Limit"/> 逐页往下取
/// </remarks>
/// <param name="Keyword">关键字（匹配用户名与显示名）</param>
/// <param name="Limit">条数上限，缺省 50</param>
/// <param name="DepartmentId">只看这个部门及其全部下级里的人</param>
/// <param name="Skip">跳过前面的条数，缺省 0</param>
public sealed record ChatUserOptionsRequest(
    [StringLength(100)] string? Keyword = null,
    [Range(1, 500)] int? Limit = null,
    Guid? DepartmentId = null,
    [Range(0, 100000)] int? Skip = null);

/// <summary>
/// 打开或建立会话的结果
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="ConversationType">会话类型</param>
/// <param name="ConversationName">会话名称</param>
/// <param name="Created">是否新建</param>
public sealed record ChatConversationOpenResponse(Guid ConversationId, ChatConversationType ConversationType, string? ConversationName, bool Created);

/// <summary>
/// 我的会话列表项
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="ConversationType">会话类型</param>
/// <param name="DisplayName">显示名（单聊是对端的用户名）</param>
/// <param name="Avatar">头像：单聊是对端的头像文件，群与部门群是文件主键或直链（文件主键经 <c>chat-messages.presign</c> 凭会话换地址）</param>
/// <param name="PeerUserId">单聊的对端用户</param>
/// <param name="DepartmentId">部门群对应的部门</param>
/// <param name="AssistantId">助手会话对应的助手</param>
/// <param name="MemberCount">成员数</param>
/// <param name="MemberRole">我的角色</param>
/// <param name="UnreadCount">我的未读数</param>
/// <param name="IsMuted">我设了免打扰</param>
/// <param name="IsPinned">我置顶了它</param>
/// <param name="IsSilenced">我被禁言</param>
/// <param name="Announcement">群公告</param>
/// <param name="Description">群描述</param>
/// <param name="LastMessageAt">最后一条消息的时间（UTC）</param>
/// <param name="LastMessagePreview">最后一条消息的预览</param>
/// <param name="LastSeq">最大消息序号</param>
/// <param name="PeerOnline">单聊的对端此刻在线；别的会话类型恒为假</param>
public sealed record ChatConversationListItem(
    Guid ConversationId,
    ChatConversationType ConversationType,
    string DisplayName,
    string? Avatar,
    Guid? PeerUserId,
    Guid? DepartmentId,
    Guid? AssistantId,
    int MemberCount,
    ChatMemberRole MemberRole,
    int UnreadCount,
    bool IsMuted,
    bool IsPinned,
    bool IsSilenced,
    string? Announcement,
    string? Description,
    DateTimeOffset? LastMessageAt,
    string? LastMessagePreview,
    long LastSeq,
    bool PeerOnline);

/// <summary>
/// 会话成员
/// </summary>
/// <param name="UserId">用户</param>
/// <param name="UserName">用户名</param>
/// <param name="DisplayName">显示名</param>
/// <param name="MemberRole">角色</param>
/// <param name="IsSilenced">被禁言</param>
/// <param name="JoinedAt">入会时间（UTC）</param>
/// <param name="AvatarFileId">头像文件（前端按它换签名地址显示）</param>
/// <param name="IsOnline">此刻在线</param>
public sealed record ChatMemberItem(Guid UserId, string? UserName, string? DisplayName, ChatMemberRole MemberRole, bool IsSilenced, DateTimeOffset JoinedAt, Guid? AvatarFileId, bool IsOnline);

/// <summary>
/// 成员的已读位
/// </summary>
/// <param name="UserId">用户</param>
/// <param name="LastReadMessageId">已读到的消息</param>
/// <param name="LastReadSeq">已读到的消息序号（没读过为 0）</param>
public sealed record ChatReadPosition(Guid UserId, Guid? LastReadMessageId, long LastReadSeq);

/// <summary>
/// 部门树的节点
/// </summary>
/// <param name="DepartmentId">部门</param>
/// <param name="DepartmentName">部门名称</param>
/// <param name="Children">下级部门</param>
public sealed record ChatDepartmentNode(Guid DepartmentId, string DepartmentName, IReadOnlyList<ChatDepartmentNode> Children);

/// <summary>
/// 可选的用户
/// </summary>
/// <param name="UserId">用户</param>
/// <param name="UserName">用户名</param>
/// <param name="DisplayName">显示名</param>
/// <param name="AvatarFileId">头像文件（前端按它换签名地址显示）</param>
/// <param name="DepartmentName">主部门的名称；没有任职或在平台一侧为空</param>
/// <param name="IsOnline">此刻在线</param>
public sealed record ChatUserOption(Guid UserId, string UserName, string DisplayName, Guid? AvatarFileId, string? DepartmentName, bool IsOnline);

/// <summary>
/// 做完了的会话
/// </summary>
/// <param name="ConversationId">会话</param>
public sealed record ChatConversationIdResponse(Guid ConversationId);

/// <summary>
/// 个人开关的新状态
/// </summary>
/// <param name="IsOn">开为真</param>
public sealed record ChatToggleResponse(bool IsOn);

/// <summary>
/// 我的会话（置顶优先，再按最后消息时间倒序）
/// </summary>
/// <param name="Items">会话</param>
public sealed record ChatConversationsResponse(IReadOnlyList<ChatConversationListItem> Items);

/// <summary>
/// 会话成员（按角色再按入会时间）
/// </summary>
/// <param name="Items">成员</param>
public sealed record ChatMembersResponse(IReadOnlyList<ChatMemberItem> Items);

/// <summary>
/// 成员的已读位
/// </summary>
/// <param name="Items">已读位</param>
public sealed record ChatReadPositionsResponse(IReadOnlyList<ChatReadPosition> Items);

/// <summary>
/// 当前一侧启用的部门树
/// </summary>
/// <param name="Items">顶层部门</param>
public sealed record ChatDepartmentTreeResponse(IReadOnlyList<ChatDepartmentNode> Items);

/// <summary>
/// 可选的用户
/// </summary>
/// <param name="Items">用户</param>
/// <param name="HasMore">后面还有</param>
public sealed record ChatUserOptionsResponse(IReadOnlyList<ChatUserOption> Items, bool HasMore);

/// <summary>
/// <c>chat-conversations.assistants</c> 的请求（没有参数）
/// </summary>
public sealed record ChatAssistantsRequest;

/// <summary>
/// 可用的助手
/// </summary>
/// <param name="Id">助手 ID</param>
/// <param name="Code">编码</param>
/// <param name="Name">名称</param>
/// <param name="Avatar">头像地址</param>
/// <param name="Description">简介</param>
/// <param name="IsDefault">是否缺省助手</param>
public sealed record ChatAssistantItem(Guid Id, string Code, string Name, string? Avatar, string? Description, bool IsDefault);

/// <summary>
/// <c>chat-conversations.assistants</c> 的响应
/// </summary>
/// <param name="Items">可用的助手</param>
public sealed record ChatAssistantsResponse(IReadOnlyList<ChatAssistantItem> Items);

/// <summary>
/// <c>chat-conversations.open-assistant</c> 的请求
/// </summary>
/// <param name="AssistantId">助手 ID</param>
public sealed record OpenAssistantConversationRequest(Guid AssistantId);

/// <summary>
/// 打开助手会话的结果
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="AssistantId">助手 ID</param>
/// <param name="AssistantName">助手名称</param>
/// <param name="Avatar">头像地址</param>
/// <param name="Created">是否新建</param>
public sealed record ChatAssistantOpenResponse(Guid ConversationId, Guid AssistantId, string AssistantName, string? Avatar, bool Created);
