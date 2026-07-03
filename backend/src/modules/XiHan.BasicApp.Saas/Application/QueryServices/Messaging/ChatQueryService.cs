#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChatQueryService
// Guid:0a5c4d7e-9daf-4edd-bc12-3c4d5e6f7a8b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 聊天查询应用服务
/// </summary>
/// <remarks>
/// 全部查询以"当前用户是会话成员"为边界（数据私密性由成员关系保证，非数据范围）。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "在线聊天")]
public sealed class ChatQueryService
    : SaasApplicationService, IChatQueryService
{
    private const int MaxHistoryTake = 100;

    private readonly IChatConversationRepository _conversationRepository;

    private readonly IChatConversationMemberRepository _memberRepository;

    private readonly IChatMessageRepository _messageRepository;

    private readonly IChatMessageReactionRepository _reactionRepository;

    private readonly IUserRepository _userRepository;

    private readonly ICurrentUser _currentUser;

    private readonly ISuperAdminProtector _superAdminProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatQueryService(
        IChatConversationRepository conversationRepository,
        IChatConversationMemberRepository memberRepository,
        IChatMessageRepository messageRepository,
        IChatMessageReactionRepository reactionRepository,
        IUserRepository userRepository,
        ICurrentUser currentUser,
        ISuperAdminProtector superAdminProtector)
    {
        _conversationRepository = conversationRepository;
        _memberRepository = memberRepository;
        _messageRepository = messageRepository;
        _reactionRepository = reactionRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _superAdminProtector = superAdminProtector;
    }

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.Chat.Read)]
    public async Task<List<ChatConversationListItemDto>> GetMyConversationsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = GetCurrentUserIdOrThrow();

        var myMembers = await _memberRepository.GetByUserIdAsync(userId, cancellationToken);
        if (myMembers.Count == 0)
        {
            return [];
        }

        var conversationIds = myMembers.Select(member => member.ConversationId).ToList();
        var conversations = await _conversationRepository.GetListAsync(
            conversation => conversationIds.Contains(conversation.BasicId), cancellationToken);
        var conversationMap = conversations.ToDictionary(conversation => conversation.BasicId);

        // 单聊：批量解析对端用户（一次成员查询 + 一次用户查询）
        var singleIds = conversations.Where(c => c.ConversationType == ChatConversationType.Single).Select(c => c.BasicId).ToList();
        var peerByConversation = new Dictionary<long, SysUser>();
        if (singleIds.Count > 0)
        {
            var allSingleMembers = await _memberRepository.GetListAsync(
                member => singleIds.Contains(member.ConversationId), cancellationToken);
            var peerMembers = allSingleMembers.Where(member => member.UserId != userId).ToList();
            var peerUserIds = peerMembers.Select(member => member.UserId).Distinct().ToList();
            var peerUsers = peerUserIds.Count > 0
                ? await _userRepository.GetListAsync(user => peerUserIds.Contains(user.BasicId), cancellationToken)
                : [];
            var peerUserMap = peerUsers.ToDictionary(user => user.BasicId);
            foreach (var peerMember in peerMembers)
            {
                if (peerUserMap.TryGetValue(peerMember.UserId, out var peerUser))
                {
                    peerByConversation[peerMember.ConversationId] = peerUser;
                }
            }
        }

        var items = new List<ChatConversationListItemDto>(myMembers.Count);
        foreach (var member in myMembers)
        {
            if (!conversationMap.TryGetValue(member.ConversationId, out var conversation))
            {
                continue;
            }

            var peer = peerByConversation.GetValueOrDefault(conversation.BasicId);
            items.Add(new ChatConversationListItemDto
            {
                ConversationId = conversation.BasicId,
                ConversationType = conversation.ConversationType,
                DisplayName = conversation.ConversationType == ChatConversationType.Single
                    ? peer?.UserName ?? "未知用户"
                    : conversation.ConversationName ?? string.Empty,
                Avatar = conversation.ConversationType == ChatConversationType.Single ? peer?.Avatar : conversation.Avatar,
                PeerUserId = conversation.ConversationType == ChatConversationType.Single ? peer?.BasicId : null,
                DepartmentId = conversation.DepartmentId,
                MemberCount = conversation.MemberCount,
                MemberRole = member.MemberRole,
                UnreadCount = member.UnreadCount,
                IsMuted = member.IsMuted,
                IsPinned = member.IsPinned,
                LastMessageTime = conversation.LastMessageTime,
                LastMessagePreview = conversation.LastMessagePreview
            });
        }

        // 置顶优先，再按最后消息时间倒序
        return [.. items
            .OrderByDescending(item => item.IsPinned)
            .ThenByDescending(item => item.LastMessageTime ?? DateTimeOffset.MinValue)
            .ThenByDescending(item => item.ConversationId)];
    }

    /// <inheritdoc />
    [HttpPost]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Read)]
    public async Task<ChatMessageHistoryResultDto> GetMessageHistoryAsync(ChatMessageHistoryQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureMemberAsync(input.ConversationId, cancellationToken);

        var take = Math.Clamp(input.Take, 1, MaxHistoryTake);
        // 多取一条判断是否还有更早历史
        var rows = await _messageRepository.GetHistoryAsync(input.ConversationId, input.BeforeMessageId, take + 1, cancellationToken);
        var hasMore = rows.Count > take;
        var page = rows.Take(take).OrderBy(message => message.BasicId).ToList();

        var items = await AttachReactionsAsync(page, cancellationToken);
        return new ChatMessageHistoryResultDto { Items = items, HasMore = hasMore };
    }

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.Chat.Read)]
    public async Task<List<ChatReadPositionDto>> GetReadPositionsAsync(long conversationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureMemberAsync(conversationId, cancellationToken);

        var members = await _memberRepository.GetByConversationIdAsync(conversationId, cancellationToken);
        return [.. members.Select(member => new ChatReadPositionDto
        {
            UserId = member.UserId,
            LastReadMessageId = member.LastReadMessageId
        })];
    }

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.Chat.Read)]
    public async Task<List<ChatMessageItemDto>> GetPinnedMessagesAsync(long conversationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureMemberAsync(conversationId, cancellationToken);

        var pinned = await _messageRepository.GetPinnedAsync(conversationId, cancellationToken);
        return [.. pinned.Select(message => ChatApplicationMapper.ToMessageItemDto(message))];
    }

    /// <summary>
    /// 批量带出消息的表情回应（一次查询按消息ID聚合）
    /// </summary>
    private async Task<List<ChatMessageItemDto>> AttachReactionsAsync(IReadOnlyList<SysChatMessage> messages, CancellationToken cancellationToken)
    {
        if (messages.Count == 0)
        {
            return [];
        }

        var messageIds = messages.Select(message => message.BasicId).ToList();
        var reactions = await _reactionRepository.GetByMessageIdsAsync(messageIds, cancellationToken);
        var reactionMap = reactions
            .GroupBy(reaction => reaction.MessageId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return [.. messages.Select(message =>
            ChatApplicationMapper.ToMessageItemDto(message, reactionMap.GetValueOrDefault(message.BasicId)))];
    }

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.Chat.Read)]
    public async Task<List<ChatMemberItemDto>> GetMembersAsync(long conversationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureMemberAsync(conversationId, cancellationToken);

        var members = await _memberRepository.GetByConversationIdAsync(conversationId, cancellationToken);
        var userIds = members.Select(member => member.UserId).Distinct().ToList();
        var users = userIds.Count > 0
            ? await _userRepository.GetListAsync(user => userIds.Contains(user.BasicId), cancellationToken)
            : [];
        var userMap = users.ToDictionary(user => user.BasicId);

        return [.. members
            .OrderBy(member => member.MemberRole)
            .ThenBy(member => member.JoinTime)
            .Select(member => ChatApplicationMapper.ToMemberItemDto(member, userMap.GetValueOrDefault(member.UserId)?.UserName))];
    }

    /// <inheritdoc />
    /// <remarks>
    /// 仅需聊天查看权限的轻量选人端点（发起单聊/建群/加成员）：
    /// 与用户管理 GetEnabledUsersAsync 同语义（固定启用用户 + 超管隐藏），
    /// 但权限门槛为 saas:chat:read，避免普通聊天用户因缺 saas:user:read 而 403。
    /// </remarks>
    [PermissionAuthorize(SaasPermissionCodes.Chat.Read)]
    public async Task<IReadOnlyList<UserSelectItemDto>> GetUserOptionsAsync(UserSelectQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = new BasicAppPRDto
        {
            Conditions = new QueryConditions()
        };
        request.Page.PageSize = Math.Clamp(input.Limit, 1, 500);
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysUser.UserName),
                nameof(SysUser.RealName),
                nameof(SysUser.NickName));
        }
        request.Conditions.AddFilter(nameof(SysUser.Status), EnableStatus.Enabled);
        request.Conditions.AddSort(nameof(SysUser.CreatedTime), SortDirection.Descending, 0);

        // 超管隐藏：非超管用户的选择项中排除超管用户（超管自身不受限）
        if (!_superAdminProtector.IsCurrentUserSuperAdmin())
        {
            var protectedUserIds = await _superAdminProtector.GetProtectedUserIdsAsync(cancellationToken);
            if (protectedUserIds.Count > 0)
            {
                request.Conditions.AddFilterIn<SysUser, long>(user => user.BasicId, protectedUserIds.Cast<object>(), QueryOperator.NotIn);
            }
        }

        var users = await _userRepository.GetPagedAsync(request, cancellationToken);
        return [.. users.Items.Select(UserApplicationMapper.ToSelectItemDto)];
    }

    private async Task EnsureMemberAsync(long conversationId, CancellationToken cancellationToken)
    {
        if (conversationId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(conversationId), "会话主键必须大于 0。");
        }

        var member = await _memberRepository.GetMemberAsync(conversationId, GetCurrentUserIdOrThrow(), cancellationToken);
        if (member is null)
        {
            throw new InvalidOperationException("仅会话成员可查看。");
        }
    }

    private long GetCurrentUserIdOrThrow()
    {
        return _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
    }
}
