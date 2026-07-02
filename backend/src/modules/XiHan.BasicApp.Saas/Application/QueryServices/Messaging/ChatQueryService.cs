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
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
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

    private readonly IUserRepository _userRepository;

    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatQueryService(
        IChatConversationRepository conversationRepository,
        IChatConversationMemberRepository memberRepository,
        IChatMessageRepository messageRepository,
        IUserRepository userRepository,
        ICurrentUser currentUser)
    {
        _conversationRepository = conversationRepository;
        _memberRepository = memberRepository;
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
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
                LastMessageTime = conversation.LastMessageTime,
                LastMessagePreview = conversation.LastMessagePreview
            });
        }

        return [.. items
            .OrderByDescending(item => item.LastMessageTime ?? DateTimeOffset.MinValue)
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

        var items = rows
            .Take(take)
            .OrderBy(message => message.BasicId)
            .Select(ChatApplicationMapper.ToMessageItemDto)
            .ToList();

        return new ChatMessageHistoryResultDto { Items = items, HasMore = hasMore };
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
