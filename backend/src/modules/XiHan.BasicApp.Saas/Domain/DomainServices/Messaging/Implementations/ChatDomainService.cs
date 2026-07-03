#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChatDomainService
// Guid:1a5b4c7d-9c04-4bc4-ea53-4f5a6b7c8d9e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 聊天领域服务实现
/// </summary>
public sealed class ChatDomainService : IChatDomainService
{
    /// <summary>
    /// 撤回时间窗口（分钟）
    /// </summary>
    private const int RecallWindowMinutes = 2;

    /// <summary>
    /// 文本消息最大长度
    /// </summary>
    private const int MaxContentLength = 4000;

    /// <summary>
    /// 会话最后消息预览最大长度
    /// </summary>
    private const int MaxPreviewLength = 200;

    /// <summary>
    /// 编辑时间窗口（分钟）
    /// </summary>
    private const int EditWindowMinutes = 5;

    /// <summary>
    /// 每会话 Pin 消息上限
    /// </summary>
    private const int MaxPinnedMessages = 10;

    /// <summary>
    /// 单条消息 @ 人数上限
    /// </summary>
    private const int MaxMentionCount = 20;

    /// <summary>
    /// 表情回应 emoji 最大长度（Unicode 码元）
    /// </summary>
    private const int MaxReactionEmojiLength = 16;

    /// <summary>
    /// 回复快照最大长度
    /// </summary>
    private const int MaxReplyPreviewLength = 300;

    private readonly IChatConversationRepository _conversationRepository;

    private readonly IChatConversationMemberRepository _memberRepository;

    private readonly IChatMessageRepository _messageRepository;

    private readonly IChatMessageReactionRepository _reactionRepository;

    private readonly IUserRepository _userRepository;

    private readonly IDepartmentRepository _departmentRepository;

    private readonly IUserDepartmentRepository _userDepartmentRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatDomainService(
        IChatConversationRepository conversationRepository,
        IChatConversationMemberRepository memberRepository,
        IChatMessageRepository messageRepository,
        IChatMessageReactionRepository reactionRepository,
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IUserDepartmentRepository userDepartmentRepository)
    {
        _conversationRepository = conversationRepository;
        _memberRepository = memberRepository;
        _messageRepository = messageRepository;
        _reactionRepository = reactionRepository;
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _userDepartmentRepository = userDepartmentRepository;
    }

    /// <inheritdoc />
    public async Task<ChatConversationCommandResult> GetOrCreateSingleConversationAsync(ChatSingleConversationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.UserId, "用户主键必须大于 0。");
        EnsureId(command.PeerUserId, "对端用户主键必须大于 0。");
        if (command.UserId == command.PeerUserId)
        {
            throw new InvalidOperationException("不能与自己建立单聊会话。");
        }

        var pairKey = BuildPairKey(command.UserId, command.PeerUserId);
        var existing = await _conversationRepository.GetByPairKeyAsync(pairKey, cancellationToken);
        if (existing is not null)
        {
            return new ChatConversationCommandResult(existing, Created: false);
        }

        _ = await GetUserOrThrowAsync(command.PeerUserId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var conversation = await _conversationRepository.AddAsync(new SysChatConversation
        {
            ConversationType = ChatConversationType.Single,
            PairKey = pairKey,
            MemberCount = 2
        }, cancellationToken);

        await AddMemberRowsAsync(conversation.BasicId, [command.UserId, command.PeerUserId], ownerUserId: null, now, cancellationToken);
        return new ChatConversationCommandResult(conversation, Created: true);
    }

    /// <inheritdoc />
    public async Task<ChatConversationCommandResult> CreateGroupConversationAsync(ChatGroupCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.OwnerUserId, "群主用户主键必须大于 0。");
        var name = Required(command.ConversationName, 100, "群聊名称不能为空且不能超过 100 个字符。");

        var memberIds = NormalizeMemberIds(command.MemberUserIds, command.OwnerUserId);
        if (memberIds.Count < 2)
        {
            throw new InvalidOperationException("群聊至少需要 2 名成员（含群主）。");
        }

        var now = DateTimeOffset.UtcNow;
        var conversation = await _conversationRepository.AddAsync(new SysChatConversation
        {
            ConversationType = ChatConversationType.Group,
            ConversationName = name,
            OwnerUserId = command.OwnerUserId,
            MemberCount = memberIds.Count
        }, cancellationToken);

        await AddMemberRowsAsync(conversation.BasicId, memberIds, command.OwnerUserId, now, cancellationToken);
        return new ChatConversationCommandResult(conversation, Created: true);
    }

    /// <inheritdoc />
    public async Task<ChatConversationCommandResult> GetOrCreateDepartmentConversationAsync(ChatDepartmentConversationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.DepartmentId, "部门主键必须大于 0。");
        EnsureId(command.OperatorUserId, "操作者用户主键必须大于 0。");

        var department = await _departmentRepository.GetByIdAsync(command.DepartmentId, cancellationToken)
            ?? throw new InvalidOperationException("部门不存在。");

        var departmentUserIds = await _userDepartmentRepository.GetUserIdsByDepartmentIdsAsync([command.DepartmentId], cancellationToken);
        var memberIds = departmentUserIds.Distinct().Where(id => id > 0).ToList();
        if (!memberIds.Contains(command.OperatorUserId))
        {
            throw new InvalidOperationException("仅本部门成员可进入部门群。");
        }

        var now = DateTimeOffset.UtcNow;
        var conversation = await _conversationRepository.GetByDepartmentIdAsync(command.DepartmentId, cancellationToken);
        var created = conversation is null;
        conversation ??= await _conversationRepository.AddAsync(new SysChatConversation
        {
            ConversationType = ChatConversationType.Department,
            ConversationName = department.DepartmentName,
            DepartmentId = command.DepartmentId,
            MemberCount = 0
        }, cancellationToken);

        // 成员同步：补齐部门内未入群成员（离开部门的成员保留会话历史，不强制移除）
        var existingMembers = await _memberRepository.GetByConversationIdAsync(conversation.BasicId, cancellationToken);
        var existingUserIds = existingMembers.Select(member => member.UserId).ToHashSet();
        var missing = memberIds.Where(id => !existingUserIds.Contains(id)).ToList();
        if (missing.Count > 0)
        {
            await AddMemberRowsAsync(conversation.BasicId, missing, ownerUserId: null, now, cancellationToken);
        }

        conversation.MemberCount = existingUserIds.Count + missing.Count;
        conversation = await _conversationRepository.UpdateAsync(conversation, cancellationToken);
        return new ChatConversationCommandResult(conversation, created);
    }

    /// <inheritdoc />
    public async Task<ChatConversationCommandResult> AddMembersAsync(ChatMemberAddCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var conversation = await GetConversationOrThrowAsync(command.ConversationId, cancellationToken);
        if (conversation.ConversationType != ChatConversationType.Group)
        {
            throw new InvalidOperationException("仅群聊支持添加成员。");
        }

        await EnsureCanManageMembersAsync(conversation, command.OperatorUserId, cancellationToken);

        var candidateIds = NormalizeMemberIds(command.UserIds, includeUserId: null);
        var existingMembers = await _memberRepository.GetByConversationIdAsync(conversation.BasicId, cancellationToken);
        var existingUserIds = existingMembers.Select(member => member.UserId).ToHashSet();
        var missing = candidateIds.Where(id => !existingUserIds.Contains(id)).ToList();
        if (missing.Count == 0)
        {
            return new ChatConversationCommandResult(conversation, Created: false);
        }

        foreach (var userId in missing)
        {
            _ = await GetUserOrThrowAsync(userId, cancellationToken);
        }

        await AddMemberRowsAsync(conversation.BasicId, missing, ownerUserId: null, DateTimeOffset.UtcNow, cancellationToken);
        conversation.MemberCount = existingUserIds.Count + missing.Count;
        conversation = await _conversationRepository.UpdateAsync(conversation, cancellationToken);
        return new ChatConversationCommandResult(conversation, Created: false);
    }

    /// <inheritdoc />
    public async Task<ChatConversationCommandResult> RemoveMemberAsync(ChatMemberRemoveCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var conversation = await GetConversationOrThrowAsync(command.ConversationId, cancellationToken);
        if (conversation.ConversationType != ChatConversationType.Group)
        {
            throw new InvalidOperationException("仅群聊支持成员移除/退群。");
        }

        if (conversation.OwnerUserId == command.UserId)
        {
            throw new InvalidOperationException("群主不能退群或被移出，请先移交群主。");
        }

        // 非本人操作 = 管理动作，需群主/管理员权限
        if (command.OperatorUserId != command.UserId)
        {
            await EnsureCanManageMembersAsync(conversation, command.OperatorUserId, cancellationToken);
        }

        var member = await _memberRepository.GetMemberAsync(conversation.BasicId, command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("该用户不是会话成员。");

        if (!await _memberRepository.DeleteAsync(member, cancellationToken))
        {
            throw new InvalidOperationException("成员移除失败。");
        }

        conversation.MemberCount = Math.Max(0, conversation.MemberCount - 1);
        conversation = await _conversationRepository.UpdateAsync(conversation, cancellationToken);
        return new ChatConversationCommandResult(conversation, Created: false);
    }

    /// <inheritdoc />
    public async Task<ChatMessageSendResult> SendMessageAsync(ChatMessageSendCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var conversation = await GetConversationOrThrowAsync(command.ConversationId, cancellationToken);
        var members = await _memberRepository.GetByConversationIdAsync(conversation.BasicId, cancellationToken);
        var senderMember = members.FirstOrDefault(member => member.UserId == command.SenderUserId)
            ?? throw new InvalidOperationException("仅会话成员可发送消息。");

        if (senderMember.IsSilenced)
        {
            throw new InvalidOperationException("你已被禁言，暂时不能发送消息。");
        }

        var content = ValidateMessagePayload(command);
        var mentionedUserIds = ValidateMentions(command.MentionedUserIds, members);
        var replyPreview = await BuildReplyPreviewAsync(command.ReplyToMessageId, conversation.BasicId, cancellationToken);
        var sender = await GetUserOrThrowAsync(command.SenderUserId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var message = await _messageRepository.AddAsync(new SysChatMessage
        {
            ConversationId = conversation.BasicId,
            SenderUserId = command.SenderUserId,
            SenderUserName = sender.UserName,
            MessageType = command.MessageType,
            Content = content,
            Attachments = ChatMessageAttachments.Serialize(command.Attachments),
            ClientMessageId = Optional(command.ClientMessageId, 50),
            ReplyToMessageId = command.ReplyToMessageId is > 0 ? command.ReplyToMessageId : null,
            ReplyPreview = replyPreview,
            MentionedUserIds = mentionedUserIds.Count > 0 ? string.Join(',', mentionedUserIds) : null
        }, cancellationToken);

        // 会话最后消息冗余 + 其余成员未读自增（发送者已读位前移）
        conversation.LastMessageId = message.BasicId;
        conversation.LastMessageTime = now;
        conversation.LastMessagePreview = BuildPreview(command.MessageType, content, command.Attachments);
        _ = await _conversationRepository.UpdateAsync(conversation, cancellationToken);
        _ = await _memberRepository.IncrementUnreadAsync(conversation.BasicId, command.SenderUserId, cancellationToken);

        senderMember.LastReadMessageId = message.BasicId;
        senderMember.LastReadTime = now;
        _ = await _memberRepository.UpdateAsync(senderMember, cancellationToken);

        var recipientIds = members.Select(member => member.UserId).ToList();
        return new ChatMessageSendResult(message, conversation, recipientIds);
    }

    /// <inheritdoc />
    public async Task<ChatMessageRecallResult> RecallMessageAsync(ChatMessageRecallCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.MessageId, "消息主键必须大于 0。");
        var message = await _messageRepository.GetByIdAsync(command.MessageId, cancellationToken)
            ?? throw new InvalidOperationException("消息不存在。");

        if (message.SenderUserId != command.OperatorUserId)
        {
            throw new InvalidOperationException("仅可撤回自己发送的消息。");
        }

        if (message.IsRecalled)
        {
            throw new InvalidOperationException("消息已撤回。");
        }

        var now = DateTimeOffset.UtcNow;
        if (now - message.CreatedTime > TimeSpan.FromMinutes(RecallWindowMinutes))
        {
            throw new InvalidOperationException($"仅可撤回 {RecallWindowMinutes} 分钟内发送的消息。");
        }

        message.IsRecalled = true;
        message.RecallTime = now;
        message.Content = null;
        message = await _messageRepository.UpdateAsync(message, cancellationToken);

        // 撤回的是最后一条时同步刷新会话预览
        var conversation = await GetConversationOrThrowAsync(message.ConversationId, cancellationToken);
        if (conversation.LastMessageId == message.BasicId)
        {
            conversation.LastMessagePreview = "[消息已撤回]";
            _ = await _conversationRepository.UpdateAsync(conversation, cancellationToken);
        }

        var members = await _memberRepository.GetByConversationIdAsync(message.ConversationId, cancellationToken);
        var recipientIds = members.Select(member => member.UserId).ToList();
        return new ChatMessageRecallResult(message, recipientIds);
    }

    /// <inheritdoc />
    public async Task<ChatMessageEditResult> EditMessageAsync(ChatMessageEditCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.MessageId, "消息主键必须大于 0。");
        var message = await _messageRepository.GetByIdAsync(command.MessageId, cancellationToken)
            ?? throw new InvalidOperationException("消息不存在。");

        if (message.SenderUserId != command.OperatorUserId)
        {
            throw new InvalidOperationException("仅可编辑自己发送的消息。");
        }

        if (message.IsRecalled)
        {
            throw new InvalidOperationException("已撤回的消息不能编辑。");
        }

        if (message.MessageType != ChatMessageType.Text)
        {
            throw new InvalidOperationException("仅文本消息支持编辑。");
        }

        var now = DateTimeOffset.UtcNow;
        if (now - message.CreatedTime > TimeSpan.FromMinutes(EditWindowMinutes))
        {
            throw new InvalidOperationException($"仅可编辑 {EditWindowMinutes} 分钟内发送的消息。");
        }

        var editorMember = await GetMemberOrThrowAsync(message.ConversationId, command.OperatorUserId, "仅会话成员可编辑消息。", cancellationToken);
        if (editorMember.IsSilenced)
        {
            throw new InvalidOperationException("你已被禁言，暂时不能编辑消息。");
        }

        var content = Required(command.Content, MaxContentLength, $"文本消息内容不能为空且不能超过 {MaxContentLength} 个字符。");
        message.Content = content;
        message.EditedTime = now;
        message = await _messageRepository.UpdateAsync(message, cancellationToken);

        // 编辑的是最后一条时同步刷新会话预览
        var conversation = await GetConversationOrThrowAsync(message.ConversationId, cancellationToken);
        if (conversation.LastMessageId == message.BasicId)
        {
            conversation.LastMessagePreview = BuildPreview(message.MessageType, content, ChatMessageAttachments.Deserialize(message.Attachments));
            _ = await _conversationRepository.UpdateAsync(conversation, cancellationToken);
        }

        var members = await _memberRepository.GetByConversationIdAsync(message.ConversationId, cancellationToken);
        return new ChatMessageEditResult(message, [.. members.Select(member => member.UserId)]);
    }

    /// <inheritdoc />
    public async Task<ChatReactionToggleResult> ToggleReactionAsync(ChatReactionToggleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.MessageId, "消息主键必须大于 0。");
        var emoji = (command.Emoji ?? string.Empty).Trim();
        if (emoji.Length is 0 or > MaxReactionEmojiLength)
        {
            throw new InvalidOperationException("表情回应无效。");
        }

        var message = await _messageRepository.GetByIdAsync(command.MessageId, cancellationToken)
            ?? throw new InvalidOperationException("消息不存在。");
        if (message.IsRecalled)
        {
            throw new InvalidOperationException("已撤回的消息不能回应。");
        }

        var operatorUser = await GetUserOrThrowAsync(command.OperatorUserId, cancellationToken);
        _ = await GetMemberOrThrowAsync(message.ConversationId, command.OperatorUserId, "仅会话成员可回应消息。", cancellationToken);

        var existing = await _reactionRepository.GetAsync(command.MessageId, command.OperatorUserId, emoji, cancellationToken);
        bool added;
        if (existing is not null)
        {
            if (!await _reactionRepository.DeleteAsync(existing, cancellationToken))
            {
                throw new InvalidOperationException("取消回应失败。");
            }

            added = false;
        }
        else
        {
            _ = await _reactionRepository.AddAsync(new SysChatMessageReaction
            {
                ConversationId = message.ConversationId,
                MessageId = message.BasicId,
                UserId = command.OperatorUserId,
                UserName = operatorUser.UserName,
                Emoji = emoji
            }, cancellationToken);
            added = true;
        }

        var members = await _memberRepository.GetByConversationIdAsync(message.ConversationId, cancellationToken);
        return new ChatReactionToggleResult(
            message.ConversationId,
            message.BasicId,
            command.OperatorUserId,
            operatorUser.UserName,
            emoji,
            added,
            [.. members.Select(member => member.UserId)]);
    }

    /// <inheritdoc />
    public async Task<ChatMessagePinResult> SetMessagePinAsync(ChatMessagePinCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.MessageId, "消息主键必须大于 0。");
        var message = await _messageRepository.GetByIdAsync(command.MessageId, cancellationToken)
            ?? throw new InvalidOperationException("消息不存在。");

        var conversation = await GetConversationOrThrowAsync(message.ConversationId, cancellationToken);
        await EnsureCanPinAsync(conversation, command.OperatorUserId, cancellationToken);

        if (command.Pin)
        {
            if (message.IsRecalled)
            {
                throw new InvalidOperationException("已撤回的消息不能置顶。");
            }

            if (!message.IsPinned)
            {
                var pinned = await _messageRepository.GetPinnedAsync(conversation.BasicId, cancellationToken);
                if (pinned.Count >= MaxPinnedMessages)
                {
                    throw new InvalidOperationException($"每个会话最多置顶 {MaxPinnedMessages} 条消息。");
                }

                message.IsPinned = true;
                message.PinnedByUserId = command.OperatorUserId;
                message.PinnedTime = DateTimeOffset.UtcNow;
                message = await _messageRepository.UpdateAsync(message, cancellationToken);
            }
        }
        else if (message.IsPinned)
        {
            message.IsPinned = false;
            message.PinnedByUserId = null;
            message.PinnedTime = null;
            message = await _messageRepository.UpdateAsync(message, cancellationToken);
        }

        var members = await _memberRepository.GetByConversationIdAsync(conversation.BasicId, cancellationToken);
        return new ChatMessagePinResult(message, [.. members.Select(member => member.UserId)]);
    }

    /// <inheritdoc />
    public async Task<bool> TogglePinConversationAsync(ChatMemberToggleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var member = await GetMemberOrThrowAsync(command.ConversationId, command.UserId, "仅会话成员可置顶会话。", cancellationToken);
        member.IsPinned = !member.IsPinned;
        _ = await _memberRepository.UpdateAsync(member, cancellationToken);
        return member.IsPinned;
    }

    /// <inheritdoc />
    public async Task<bool> ToggleMuteConversationAsync(ChatMemberToggleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var member = await GetMemberOrThrowAsync(command.ConversationId, command.UserId, "仅会话成员可设置免打扰。", cancellationToken);
        member.IsMuted = !member.IsMuted;
        _ = await _memberRepository.UpdateAsync(member, cancellationToken);
        return member.IsMuted;
    }

    /// <inheritdoc />
    public async Task<ChatMarkReadResult> MarkConversationReadAsync(ChatMarkReadCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var member = await _memberRepository.GetMemberAsync(command.ConversationId, command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("仅会话成员可标记已读。");

        member.UnreadCount = 0;
        member.LastReadTime = DateTimeOffset.UtcNow;

        // 已读位推进：显式已读位优先；null 语义为「读到最新」（取会话最后消息）。均不允许回退
        var target = command.UpToMessageId is { } upTo && upTo > 0 ? upTo : (long?)null;
        if (target is null)
        {
            var conversation = await _conversationRepository.GetByIdAsync(command.ConversationId, cancellationToken);
            target = conversation?.LastMessageId;
        }

        if (target is { } advanceTo && advanceTo > (member.LastReadMessageId ?? 0))
        {
            member.LastReadMessageId = advanceTo;
        }

        _ = await _memberRepository.UpdateAsync(member, cancellationToken);

        var members = await _memberRepository.GetByConversationIdAsync(command.ConversationId, cancellationToken);
        return new ChatMarkReadResult(
            command.ConversationId,
            command.UserId,
            member.LastReadMessageId,
            [.. members.Select(item => item.UserId)]);
    }

    /// <inheritdoc />
    public async Task<ChatGovernanceResult> UpdateConversationInfoAsync(ChatConversationInfoUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var conversation = await GetConversationOrThrowAsync(command.ConversationId, cancellationToken);
        if (conversation.ConversationType == ChatConversationType.Single)
        {
            throw new InvalidOperationException("单聊没有可编辑的群信息。");
        }

        await EnsureCanManageMembersAsync(conversation, command.OperatorUserId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(command.ConversationName))
        {
            if (conversation.ConversationType == ChatConversationType.Department)
            {
                throw new InvalidOperationException("部门群名称随部门同步，不能手动修改。");
            }

            conversation.ConversationName = Required(command.ConversationName, 100, "群聊名称不能为空且不能超过 100 个字符。");
        }

        var announcementChanged = false;
        if (command.Announcement is not null)
        {
            var announcement = Optional(command.Announcement, 2000);
            announcementChanged = !string.Equals(conversation.Announcement, announcement, StringComparison.Ordinal);
            conversation.Announcement = announcement;
        }

        if (command.Description is not null)
        {
            conversation.Description = Optional(command.Description, 500);
        }

        if (command.Avatar is not null)
        {
            conversation.Avatar = Optional(command.Avatar, 500);
        }

        conversation = await _conversationRepository.UpdateAsync(conversation, cancellationToken);

        SysChatMessage? systemMessage = null;
        if (announcementChanged && !string.IsNullOrWhiteSpace(conversation.Announcement))
        {
            // 公告更新以「操作人身份」入流（前端渲染为带发布人的公告卡片），正文为公告全文
            var operatorUser = await GetUserOrThrowAsync(command.OperatorUserId, cancellationToken);
            systemMessage = await AppendSystemMessageAsync(
                conversation,
                conversation.Announcement,
                $"[群公告] {Truncate(conversation.Announcement, 60)}",
                command.OperatorUserId,
                operatorUser.UserName,
                cancellationToken);
        }

        var members = await _memberRepository.GetByConversationIdAsync(conversation.BasicId, cancellationToken);
        return new ChatGovernanceResult(conversation, systemMessage, [.. members.Select(member => member.UserId)]);
    }

    /// <inheritdoc />
    public async Task<ChatGovernanceResult> TransferOwnerAsync(ChatOwnerTransferCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var conversation = await GetConversationOrThrowAsync(command.ConversationId, cancellationToken);
        if (conversation.ConversationType != ChatConversationType.Group)
        {
            throw new InvalidOperationException("仅群聊支持转让群主。");
        }

        if (conversation.OwnerUserId != command.OperatorUserId)
        {
            throw new InvalidOperationException("仅群主可转让群主。");
        }

        if (command.NewOwnerUserId == command.OperatorUserId)
        {
            throw new InvalidOperationException("不能把群主转让给自己。");
        }

        var oldOwnerMember = await GetMemberOrThrowAsync(conversation.BasicId, command.OperatorUserId, "群主成员记录不存在。", cancellationToken);
        var newOwnerMember = await GetMemberOrThrowAsync(conversation.BasicId, command.NewOwnerUserId, "新群主必须是会话成员。", cancellationToken);
        var newOwnerUser = await GetUserOrThrowAsync(command.NewOwnerUserId, cancellationToken);
        var oldOwnerUser = await GetUserOrThrowAsync(command.OperatorUserId, cancellationToken);

        oldOwnerMember.MemberRole = ChatMemberRole.Member;
        newOwnerMember.MemberRole = ChatMemberRole.Owner;
        _ = await _memberRepository.UpdateAsync(oldOwnerMember, cancellationToken);
        _ = await _memberRepository.UpdateAsync(newOwnerMember, cancellationToken);

        conversation.OwnerUserId = command.NewOwnerUserId;
        conversation = await _conversationRepository.UpdateAsync(conversation, cancellationToken);

        var transferText = $"{oldOwnerUser.UserName} 已将群主移交给 {newOwnerUser.UserName}";
        var systemMessage = await AppendSystemMessageAsync(
            conversation, transferText, transferText, senderUserId: 0, senderUserName: null, cancellationToken);

        var members = await _memberRepository.GetByConversationIdAsync(conversation.BasicId, cancellationToken);
        return new ChatGovernanceResult(conversation, systemMessage, [.. members.Select(member => member.UserId)]);
    }

    /// <inheritdoc />
    public async Task<ChatGovernanceResult> SetMemberSilenceAsync(ChatMemberSilenceCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var conversation = await GetConversationOrThrowAsync(command.ConversationId, cancellationToken);
        if (conversation.ConversationType == ChatConversationType.Single)
        {
            throw new InvalidOperationException("单聊不支持禁言。");
        }

        await EnsureCanManageMembersAsync(conversation, command.OperatorUserId, cancellationToken);

        var target = await GetMemberOrThrowAsync(conversation.BasicId, command.TargetUserId, "该用户不是会话成员。", cancellationToken);
        if (target.MemberRole is ChatMemberRole.Owner or ChatMemberRole.Admin)
        {
            throw new InvalidOperationException("不能禁言群主或管理员。");
        }

        if (target.IsSilenced != command.IsSilenced)
        {
            target.IsSilenced = command.IsSilenced;
            _ = await _memberRepository.UpdateAsync(target, cancellationToken);
        }

        var members = await _memberRepository.GetByConversationIdAsync(conversation.BasicId, cancellationToken);
        return new ChatGovernanceResult(conversation, SystemMessage: null, [.. members.Select(member => member.UserId)]);
    }

    /// <inheritdoc />
    public async Task<ChatGovernanceResult> SetMemberRoleAsync(ChatMemberRoleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var conversation = await GetConversationOrThrowAsync(command.ConversationId, cancellationToken);
        if (conversation.ConversationType != ChatConversationType.Group)
        {
            throw new InvalidOperationException("仅群聊支持设置管理员。");
        }

        if (conversation.OwnerUserId != command.OperatorUserId)
        {
            throw new InvalidOperationException("仅群主可设置管理员。");
        }

        if (command.TargetUserId == command.OperatorUserId)
        {
            throw new InvalidOperationException("不能修改自己的角色。");
        }

        if (command.MemberRole is not (ChatMemberRole.Admin or ChatMemberRole.Member))
        {
            throw new InvalidOperationException("只能在管理员与普通成员之间切换。");
        }

        var target = await GetMemberOrThrowAsync(conversation.BasicId, command.TargetUserId, "该用户不是会话成员。", cancellationToken);
        if (target.MemberRole == ChatMemberRole.Owner)
        {
            throw new InvalidOperationException("不能修改群主的角色。");
        }

        if (target.MemberRole != command.MemberRole)
        {
            target.MemberRole = command.MemberRole;
            _ = await _memberRepository.UpdateAsync(target, cancellationToken);
        }

        var members = await _memberRepository.GetByConversationIdAsync(conversation.BasicId, cancellationToken);
        return new ChatGovernanceResult(conversation, SystemMessage: null, [.. members.Select(member => member.UserId)]);
    }

    /// <summary>
    /// 追加系统提示消息（刷新会话预览但不增加成员未读）。
    /// senderUserId=0 为中性时间线提示（居中样式）；带操作人身份则前端渲染为公告卡片等归属样式
    /// </summary>
    private async Task<SysChatMessage> AppendSystemMessageAsync(
        SysChatConversation conversation,
        string content,
        string preview,
        long senderUserId,
        string? senderUserName,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var message = await _messageRepository.AddAsync(new SysChatMessage
        {
            ConversationId = conversation.BasicId,
            SenderUserId = senderUserId,
            SenderUserName = senderUserName,
            MessageType = ChatMessageType.System,
            Content = Truncate(content, MaxContentLength)
        }, cancellationToken);

        conversation.LastMessageId = message.BasicId;
        conversation.LastMessageTime = now;
        conversation.LastMessagePreview = Truncate(preview, MaxPreviewLength);
        _ = await _conversationRepository.UpdateAsync(conversation, cancellationToken);
        return message;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string BuildPairKey(long userId, long peerUserId)
    {
        var (min, max) = userId < peerUserId ? (userId, peerUserId) : (peerUserId, userId);
        return $"{min}_{max}";
    }

    private static string BuildPreview(ChatMessageType messageType, string? content, IReadOnlyList<ChatMessageAttachment>? attachments)
    {
        var count = attachments?.Count ?? 0;
        var preview = messageType switch
        {
            ChatMessageType.Image => count > 1 ? $"[图片] {count}张" : "[图片]",
            ChatMessageType.File => count > 1 ? $"[文件] {count}个" : $"[文件] {attachments?.FirstOrDefault()?.FileName}".TrimEnd(),
            _ => content ?? string.Empty
        };
        return preview.Length <= MaxPreviewLength ? preview : preview[..MaxPreviewLength];
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static List<long> NormalizeMemberIds(IReadOnlyCollection<long> userIds, long? includeUserId)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        var ids = userIds.Where(id => id > 0).ToHashSet();
        if (includeUserId is { } include)
        {
            _ = ids.Add(include);
        }

        return [.. ids];
    }

    private static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string Required(string? value, int maxLength, string message)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
        {
            throw new InvalidOperationException(message);
        }

        return value.Trim();
    }

    private static string? ValidateMessagePayload(ChatMessageSendCommand command)
    {
        switch (command.MessageType)
        {
            case ChatMessageType.Text:
                var content = Required(command.Content, MaxContentLength, $"文本消息内容不能为空且不能超过 {MaxContentLength} 个字符。");
                return content;

            case ChatMessageType.Image:
            case ChatMessageType.File:
                if (command.Attachments is not { Count: > 0 } || command.Attachments.Any(attachment => attachment.FileId <= 0))
                {
                    throw new InvalidOperationException("图片/文件消息必须关联文件。");
                }

                return Optional(command.Content, MaxContentLength);

            case ChatMessageType.System:
                throw new InvalidOperationException("系统提示消息由服务端生成，不能直接发送。");

            default:
                throw new ArgumentOutOfRangeException(nameof(command), "消息类型无效。");
        }
    }

    private async Task AddMemberRowsAsync(long conversationId, IReadOnlyCollection<long> userIds, long? ownerUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var userId in userIds)
        {
            _ = await _memberRepository.AddAsync(new SysChatConversationMember
            {
                ConversationId = conversationId,
                UserId = userId,
                MemberRole = ownerUserId == userId ? ChatMemberRole.Owner : ChatMemberRole.Member,
                JoinTime = now
            }, cancellationToken);
        }
    }

    /// <summary>
    /// 校验 @ 名单：全部为会话成员、数量受限、去重去自身无效值
    /// </summary>
    private static List<long> ValidateMentions(IReadOnlyCollection<long>? mentionedUserIds, IReadOnlyList<SysChatConversationMember> members)
    {
        if (mentionedUserIds is not { Count: > 0 })
        {
            return [];
        }

        var ids = mentionedUserIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count > MaxMentionCount)
        {
            throw new InvalidOperationException($"单条消息最多 @ {MaxMentionCount} 人。");
        }

        var memberIds = members.Select(member => member.UserId).ToHashSet();
        return ids.Any(id => !memberIds.Contains(id))
            ? throw new InvalidOperationException("仅可 @ 会话成员。")
            : ids;
    }

    /// <summary>
    /// 构建回复快照「{发送人}: {内容}」（发送时校验被回复消息同会话且未撤回）
    /// </summary>
    private async Task<string?> BuildReplyPreviewAsync(long? replyToMessageId, long conversationId, CancellationToken cancellationToken)
    {
        if (replyToMessageId is not > 0)
        {
            return null;
        }

        var original = await _messageRepository.GetByIdAsync(replyToMessageId.Value, cancellationToken)
            ?? throw new InvalidOperationException("被回复的消息不存在。");
        if (original.ConversationId != conversationId)
        {
            throw new InvalidOperationException("仅可回复本会话内的消息。");
        }

        if (original.IsRecalled)
        {
            throw new InvalidOperationException("被回复的消息已撤回。");
        }

        var body = BuildPreview(original.MessageType, original.Content, ChatMessageAttachments.Deserialize(original.Attachments));
        var preview = $"{original.SenderUserName}: {body}";
        return preview.Length <= MaxReplyPreviewLength ? preview : preview[..MaxReplyPreviewLength];
    }

    /// <summary>
    /// Pin 权限：单聊双方皆可，群/部门群仅群主与管理员
    /// </summary>
    private async Task EnsureCanPinAsync(SysChatConversation conversation, long operatorUserId, CancellationToken cancellationToken)
    {
        var member = await GetMemberOrThrowAsync(conversation.BasicId, operatorUserId, "仅会话成员可操作。", cancellationToken);
        if (conversation.ConversationType != ChatConversationType.Single
            && member.MemberRole is not (ChatMemberRole.Owner or ChatMemberRole.Admin))
        {
            throw new InvalidOperationException("仅群主或管理员可置顶消息。");
        }
    }

    private async Task<SysChatConversationMember> GetMemberOrThrowAsync(long conversationId, long userId, string message, CancellationToken cancellationToken)
    {
        return await _memberRepository.GetMemberAsync(conversationId, userId, cancellationToken)
            ?? throw new InvalidOperationException(message);
    }

    private async Task EnsureCanManageMembersAsync(SysChatConversation conversation, long operatorUserId, CancellationToken cancellationToken)
    {
        var operatorMember = await _memberRepository.GetMemberAsync(conversation.BasicId, operatorUserId, cancellationToken)
            ?? throw new InvalidOperationException("仅会话成员可操作。");

        if (operatorMember.MemberRole is not (ChatMemberRole.Owner or ChatMemberRole.Admin))
        {
            throw new InvalidOperationException("仅群主或管理员可管理成员。");
        }
    }

    private async Task<SysChatConversation> GetConversationOrThrowAsync(long conversationId, CancellationToken cancellationToken)
    {
        EnsureId(conversationId, "会话主键必须大于 0。");
        return await _conversationRepository.GetByIdAsync(conversationId, cancellationToken)
            ?? throw new InvalidOperationException("会话不存在。");
    }

    private async Task<SysUser> GetUserOrThrowAsync(long userId, CancellationToken cancellationToken)
    {
        EnsureId(userId, "用户主键必须大于 0。");
        return await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"用户 {userId} 不存在。");
    }
}
