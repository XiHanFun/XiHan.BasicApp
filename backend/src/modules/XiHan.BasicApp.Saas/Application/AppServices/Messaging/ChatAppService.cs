#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChatAppService
// Guid:9f4b3c6d-8cfe-4dcc-ab01-2b3c4d5e6f7a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 聊天命令应用服务
/// </summary>
/// <remarks>
/// 持久化经领域服务（UoW 内），实时推送在落库后 best-effort 点发（失败不回滚）。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "在线聊天")]
public sealed class ChatAppService
    : SaasApplicationService, IChatAppService
{
    private readonly IChatDomainService _chatDomainService;

    private readonly IChatRealtimePushService _pushService;

    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatAppService(
        IChatDomainService chatDomainService,
        IChatRealtimePushService pushService,
        ICurrentUser currentUser)
    {
        _chatDomainService = chatDomainService;
        _pushService = pushService;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Read)]
    public async Task<ChatConversationDto> OpenSingleConversationAsync(ChatSingleConversationOpenDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.GetOrCreateSingleConversationAsync(
            new ChatSingleConversationCommand(GetCurrentUserIdOrThrow(), input.PeerUserId), cancellationToken);
        return ChatApplicationMapper.ToConversationDto(result.Conversation, result.Created);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Manage)]
    public async Task<ChatConversationDto> CreateGroupConversationAsync(ChatGroupCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.CreateGroupConversationAsync(
            new ChatGroupCreateCommand(GetCurrentUserIdOrThrow(), input.ConversationName, input.MemberUserIds), cancellationToken);
        await _pushService.PushConversationChangedAsync(result.Conversation.BasicId, "created", input.MemberUserIds);
        return ChatApplicationMapper.ToConversationDto(result.Conversation, result.Created);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Read)]
    public async Task<ChatConversationDto> OpenDepartmentConversationAsync(ChatDepartmentConversationOpenDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.GetOrCreateDepartmentConversationAsync(
            new ChatDepartmentConversationCommand(input.DepartmentId, GetCurrentUserIdOrThrow()), cancellationToken);
        return ChatApplicationMapper.ToConversationDto(result.Conversation, result.Created);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Manage)]
    public async Task AddMembersAsync(ChatMemberAddDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        _ = await _chatDomainService.AddMembersAsync(
            new ChatMemberAddCommand(input.ConversationId, GetCurrentUserIdOrThrow(), input.UserIds), cancellationToken);
        await _pushService.PushConversationChangedAsync(input.ConversationId, "member-added", input.UserIds);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Manage)]
    public async Task RemoveMemberAsync(ChatMemberRemoveDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        _ = await _chatDomainService.RemoveMemberAsync(
            new ChatMemberRemoveCommand(input.ConversationId, GetCurrentUserIdOrThrow(), input.UserId), cancellationToken);
        await _pushService.PushConversationChangedAsync(input.ConversationId, "member-removed", [input.UserId]);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Send)]
    public async Task<ChatMessageItemDto> SendMessageAsync(ChatMessageSendDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.SendMessageAsync(
            ChatApplicationMapper.ToSendCommand(input, GetCurrentUserIdOrThrow()), cancellationToken);
        var messageDto = ChatApplicationMapper.ToMessageItemDto(result.Message);
        await _pushService.PushMessageAsync(messageDto, result.Conversation, result.RecipientUserIds);
        return messageDto;
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Send)]
    public async Task RecallMessageAsync(long messageId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.RecallMessageAsync(
            new ChatMessageRecallCommand(messageId, GetCurrentUserIdOrThrow()), cancellationToken);
        await _pushService.PushRecalledAsync(result.Message.ConversationId, result.Message.BasicId, result.RecipientUserIds);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Send)]
    public async Task<ChatMessageItemDto> EditMessageAsync(ChatMessageEditDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.EditMessageAsync(
            new ChatMessageEditCommand(input.MessageId, GetCurrentUserIdOrThrow(), input.Content), cancellationToken);
        await _pushService.PushMessageEditedAsync(
            result.Message.ConversationId, result.Message.BasicId, result.Message.Content, result.Message.EditedTime, result.RecipientUserIds);
        return ChatApplicationMapper.ToMessageItemDto(result.Message);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Send)]
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

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Send)]
    public async Task PinMessageAsync(ChatMessagePinDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.SetMessagePinAsync(
            new ChatMessagePinCommand(input.MessageId, GetCurrentUserIdOrThrow(), Pin: true), cancellationToken);
        await _pushService.PushConversationChangedAsync(result.Message.ConversationId, "pinned-changed", result.RecipientUserIds);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Send)]
    public async Task UnpinMessageAsync(ChatMessagePinDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.SetMessagePinAsync(
            new ChatMessagePinCommand(input.MessageId, GetCurrentUserIdOrThrow(), Pin: false), cancellationToken);
        await _pushService.PushConversationChangedAsync(result.Message.ConversationId, "pinned-changed", result.RecipientUserIds);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Read)]
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

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Read)]
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

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Read)]
    public async Task MarkReadAsync(ChatMarkReadDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.MarkConversationReadAsync(
            new ChatMarkReadCommand(input.ConversationId, GetCurrentUserIdOrThrow(), input.UpToMessageId), cancellationToken);
        // 已读位扇出全体成员：群已读回执/单聊已读状态实时刷新
        await _pushService.PushReadPositionChangedAsync(result.ConversationId, result.UserId, result.LastReadMessageId, result.RecipientUserIds);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Manage)]
    public async Task UpdateConversationInfoAsync(ChatConversationInfoUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.UpdateConversationInfoAsync(
            new ChatConversationInfoUpdateCommand(input.ConversationId, GetCurrentUserIdOrThrow(), input.ConversationName, input.Announcement, input.Description),
            cancellationToken);
        await PushGovernanceAsync(result, "info-changed");
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Manage)]
    public async Task TransferOwnerAsync(ChatOwnerTransferDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.TransferOwnerAsync(
            new ChatOwnerTransferCommand(input.ConversationId, GetCurrentUserIdOrThrow(), input.NewOwnerUserId), cancellationToken);
        await PushGovernanceAsync(result, "owner-transferred");
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Chat.Manage)]
    public async Task SetMemberSilenceAsync(ChatMemberSilenceDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _chatDomainService.SetMemberSilenceAsync(
            new ChatMemberSilenceCommand(input.ConversationId, GetCurrentUserIdOrThrow(), input.UserId, input.IsSilenced), cancellationToken);
        await PushGovernanceAsync(result, "member-silenced");
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
