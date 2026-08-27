// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Chat.Application.Contracts;
using XiHan.BasicApp.Chat.Application.Dtos;
using XiHan.BasicApp.Chat.Application.Mappers;
using XiHan.BasicApp.Chat.Application.Services;
using XiHan.BasicApp.Chat.Domain.DomainServices;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

using XiHan.BasicApp.Chat.Domain.Permissions;

namespace XiHan.BasicApp.Chat.Application.AppServices;

/// <summary>
/// 聊天命令应用服务
/// </summary>
/// <remarks>
/// 持久化经领域服务（UoW 内），实时推送在落库后 best-effort 点发（失败不回滚）。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "在线聊天")]
public sealed class ChatAppService
    : ChatApplicationService, IChatAppService
{
    private readonly IChatDomainService _chatDomainService;

    private readonly IChatRealtimePushService _pushService;

    private readonly IChatSensitiveWordGuard _sensitiveWordGuard;

    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatAppService(
        IChatDomainService chatDomainService,
        IChatRealtimePushService pushService,
        IChatSensitiveWordGuard sensitiveWordGuard,
        ICurrentUser currentUser)
    {
        _chatDomainService = chatDomainService;
        _pushService = pushService;
        _sensitiveWordGuard = sensitiveWordGuard;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 打开单聊会话（不存在则创建）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Read)]
    public async Task<ChatConversationDto> OpenSingleConversationAsync(ChatSingleConversationOpenDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.GetOrCreateSingleConversationAsync(
            new ChatSingleConversationCommand(GetCurrentUserIdOrThrow(), input.PeerUserId), cancellationToken);
        return ChatApplicationMapper.ToConversationDto(result.Conversation, result.Created);
    }

    /// <summary>
    /// 创建群聊
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Manage)]
    public async Task<ChatConversationDto> CreateGroupConversationAsync(ChatGroupCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.CreateGroupConversationAsync(
            new ChatGroupCreateCommand(GetCurrentUserIdOrThrow(), input.ConversationName, input.MemberUserIds), cancellationToken);
        await PushGovernanceAsync(result, "created");
        return ChatApplicationMapper.ToConversationDto(result.Conversation, created: true);
    }

    /// <summary>
    /// 打开部门群（不存在则创建，成员按部门归属同步）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Read)]
    public async Task<ChatConversationDto> OpenDepartmentConversationAsync(ChatDepartmentConversationOpenDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.GetOrCreateDepartmentConversationAsync(
            new ChatDepartmentConversationCommand(input.DepartmentId, GetCurrentUserIdOrThrow()), cancellationToken);
        return ChatApplicationMapper.ToConversationDto(result.Conversation, result.Created);
    }

    /// <summary>
    /// 添加群成员
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Manage)]
    public async Task AddMembersAsync(ChatMemberAddDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.AddMembersAsync(
            new ChatMemberAddCommand(input.ConversationId, GetCurrentUserIdOrThrow(), input.UserIds), cancellationToken);
        await PushGovernanceAsync(result, "member-added");
    }

    /// <summary>
    /// 移除群成员/退群
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Manage)]
    public async Task RemoveMemberAsync(ChatMemberRemoveDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.RemoveMemberAsync(
            new ChatMemberRemoveCommand(input.ConversationId, GetCurrentUserIdOrThrow(), input.UserId), cancellationToken);
        // 剩余成员收到系统提示与成员变更；被移出者只收会话变更（其会话列表随之收敛）
        await PushGovernanceAsync(result, "member-removed");
        await _pushService.PushConversationChangedAsync(input.ConversationId, "member-removed", [input.UserId]);
    }

    /// <summary>
    /// 发送消息（落库后向会话成员实时推送）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Send)]
    public async Task<ChatMessageItemDto> SendMessageAsync(ChatMessageSendDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _sensitiveWordGuard.EnsureAllowedAsync(input.Content, cancellationToken);
        var result = await _chatDomainService.SendMessageAsync(
            ChatApplicationMapper.ToSendCommand(input, GetCurrentUserIdOrThrow()), cancellationToken);
        var messageDto = ChatApplicationMapper.ToMessageItemDto(result.Message);
        await _pushService.PushMessageAsync(messageDto, result.Conversation, result.RecipientUserIds);
        return messageDto;
    }

    /// <summary>
    /// 撤回消息（仅本人限时）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Send)]
    public async Task RecallMessageAsync(long messageId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.RecallMessageAsync(
            new ChatMessageRecallCommand(messageId, GetCurrentUserIdOrThrow()), cancellationToken);
        await _pushService.PushRecalledAsync(result.Message.ConversationId, result.Message.BasicId, result.RecipientUserIds);
    }

    /// <summary>
    /// 编辑消息（仅文本、仅本人、限时窗口）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Send)]
    public async Task<ChatMessageItemDto> EditMessageAsync(ChatMessageEditDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _sensitiveWordGuard.EnsureAllowedAsync(input.Content, cancellationToken);
        var result = await _chatDomainService.EditMessageAsync(
            new ChatMessageEditCommand(input.MessageId, GetCurrentUserIdOrThrow(), input.Content), cancellationToken);
        await _pushService.PushMessageEditedAsync(
            result.Message.ConversationId, result.Message.BasicId, result.Message.Content, result.Message.EditedTime, result.RecipientUserIds);
        return ChatApplicationMapper.ToMessageItemDto(result.Message);
    }

    /// <summary>
    /// 表情回应 toggle（已存在则取消，否则新增）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Send)]
    public async Task<ChatReactionToggleResultDto> ToggleReactionAsync(ChatReactionToggleDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.ToggleReactionAsync(
            new ChatReactionToggleCommand(input.MessageId, GetCurrentUserIdOrThrow(), input.Emoji), cancellationToken);
        await _pushService.PushReactionChangedAsync(
            result.ConversationId, result.MessageId, result.Emoji, result.UserId, result.UserName, result.Added, result.RecipientUserIds);
        return new ChatReactionToggleResultDto { Added = result.Added };
    }

    /// <summary>
    /// Pin 消息（单聊双方皆可，群仅群主/管理员；每会话有上限）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Send)]
    public async Task PinMessageAsync(ChatMessagePinDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.SetMessagePinAsync(
            new ChatMessagePinCommand(input.MessageId, GetCurrentUserIdOrThrow(), Pin: true), cancellationToken);
        await _pushService.PushConversationChangedAsync(result.Message.ConversationId, "pinned-changed", result.RecipientUserIds);
    }

    /// <summary>
    /// 取消 Pin 消息
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Send)]
    public async Task UnpinMessageAsync(ChatMessagePinDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.SetMessagePinAsync(
            new ChatMessagePinCommand(input.MessageId, GetCurrentUserIdOrThrow(), Pin: false), cancellationToken);
        await _pushService.PushConversationChangedAsync(result.Message.ConversationId, "pinned-changed", result.RecipientUserIds);
    }

    /// <summary>
    /// 会话置顶 toggle（个人维度）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Read)]
    public async Task<ChatToggleStateDto> TogglePinConversationAsync(ChatConversationToggleDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var isOn = await _chatDomainService.TogglePinConversationAsync(
            new ChatMemberToggleCommand(input.ConversationId, userId), cancellationToken);
        // 仅推给本人：多端同步个人会话设置
        await _pushService.PushConversationChangedAsync(input.ConversationId, "member-setting-changed", [userId]);
        return new ChatToggleStateDto { IsOn = isOn };
    }

    /// <summary>
    /// 会话免打扰 toggle（个人维度）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Read)]
    public async Task<ChatToggleStateDto> ToggleMuteConversationAsync(ChatConversationToggleDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var isOn = await _chatDomainService.ToggleMuteConversationAsync(
            new ChatMemberToggleCommand(input.ConversationId, userId), cancellationToken);
        await _pushService.PushConversationChangedAsync(input.ConversationId, "member-setting-changed", [userId]);
        return new ChatToggleStateDto { IsOn = isOn };
    }

    /// <summary>
    /// 标记会话已读
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Read)]
    public async Task MarkReadAsync(ChatMarkReadDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.MarkConversationReadAsync(
            new ChatMarkReadCommand(input.ConversationId, GetCurrentUserIdOrThrow(), input.UpToMessageId), cancellationToken);
        // 已读位扇出全体成员：群已读回执/单聊已读状态实时刷新
        await _pushService.PushReadPositionChangedAsync(result.ConversationId, result.UserId, result.LastReadMessageId, result.RecipientUserIds);
    }

    /// <summary>
    /// 更新群信息（群主/管理员；部门群名称禁改；公告变更追加系统提示）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Manage)]
    public async Task UpdateConversationInfoAsync(ChatConversationInfoUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.UpdateConversationInfoAsync(
            new ChatConversationInfoUpdateCommand(input.ConversationId, GetCurrentUserIdOrThrow(), input.ConversationName, input.Announcement, input.Description, input.Avatar),
            cancellationToken);
        await PushGovernanceAsync(result, "info-changed");
    }

    /// <summary>
    /// 转让群主（仅群聊群主）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Manage)]
    public async Task TransferOwnerAsync(ChatOwnerTransferDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.TransferOwnerAsync(
            new ChatOwnerTransferCommand(input.ConversationId, GetCurrentUserIdOrThrow(), input.NewOwnerUserId), cancellationToken);
        await PushGovernanceAsync(result, "owner-transferred");
    }

    /// <summary>
    /// 成员禁言/解除（群主与管理员）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Manage)]
    public async Task SetMemberSilenceAsync(ChatMemberSilenceDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.SetMemberSilenceAsync(
            new ChatMemberSilenceCommand(input.ConversationId, GetCurrentUserIdOrThrow(), input.UserId, input.IsSilenced), cancellationToken);
        await PushGovernanceAsync(result, "member-silenced");
    }

    /// <summary>
    /// 设置成员角色（仅群主；Admin ↔ Member）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(ChatPermissionCodes.Manage)]
    public async Task SetMemberRoleAsync(ChatMemberRoleDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.SetMemberRoleAsync(
            new ChatMemberRoleCommand(input.ConversationId, GetCurrentUserIdOrThrow(), input.UserId, input.MemberRole), cancellationToken);
        await PushGovernanceAsync(result, "member-role-changed");
    }

    /// <summary>
    /// 群治理推送：会话变更通知 + 可选系统提示消息（时间线实时可见）
    /// </summary>
    private async Task PushGovernanceAsync(ChatGovernanceResult result, string changeType)
    {
        await _pushService.PushConversationChangedAsync(result.Conversation.BasicId, changeType, result.RecipientUserIds);
        if (result.SystemMessage is not null)
        {
            await _pushService.PushMessageAsync(
                ChatApplicationMapper.ToMessageItemDto(result.SystemMessage), result.Conversation, result.RecipientUserIds);
        }
    }

    private long GetCurrentUserIdOrThrow()
    {
        return _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
    }
}
