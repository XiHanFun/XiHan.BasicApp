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

    private readonly IChatConversationRepository _conversationRepository;

    private readonly IChatConversationMemberRepository _memberRepository;

    private readonly IChatMessageRepository _messageRepository;

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
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IUserDepartmentRepository userDepartmentRepository)
    {
        _conversationRepository = conversationRepository;
        _memberRepository = memberRepository;
        _messageRepository = messageRepository;
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
        var senderMember = await _memberRepository.GetMemberAsync(conversation.BasicId, command.SenderUserId, cancellationToken)
            ?? throw new InvalidOperationException("仅会话成员可发送消息。");

        var content = ValidateMessagePayload(command);
        var sender = await GetUserOrThrowAsync(command.SenderUserId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var message = await _messageRepository.AddAsync(new SysChatMessage
        {
            ConversationId = conversation.BasicId,
            SenderUserId = command.SenderUserId,
            SenderUserName = sender.UserName,
            MessageType = command.MessageType,
            Content = content,
            FileId = command.FileId,
            FileName = Optional(command.FileName, 200),
            FileSize = command.FileSize,
            ClientMessageId = Optional(command.ClientMessageId, 50)
        }, cancellationToken);

        // 会话最后消息冗余 + 其余成员未读自增（发送者已读位前移）
        conversation.LastMessageId = message.BasicId;
        conversation.LastMessageTime = now;
        conversation.LastMessagePreview = BuildPreview(command.MessageType, content, command.FileName);
        _ = await _conversationRepository.UpdateAsync(conversation, cancellationToken);
        _ = await _memberRepository.IncrementUnreadAsync(conversation.BasicId, command.SenderUserId, cancellationToken);

        senderMember.LastReadMessageId = message.BasicId;
        senderMember.LastReadTime = now;
        _ = await _memberRepository.UpdateAsync(senderMember, cancellationToken);

        var members = await _memberRepository.GetByConversationIdAsync(conversation.BasicId, cancellationToken);
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
    public async Task MarkConversationReadAsync(ChatMarkReadCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var member = await _memberRepository.GetMemberAsync(command.ConversationId, command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("仅会话成员可标记已读。");

        member.UnreadCount = 0;
        member.LastReadTime = DateTimeOffset.UtcNow;
        if (command.UpToMessageId is { } upTo && upTo > 0)
        {
            member.LastReadMessageId = upTo;
        }

        _ = await _memberRepository.UpdateAsync(member, cancellationToken);
    }

    private static string BuildPairKey(long userId, long peerUserId)
    {
        var (min, max) = userId < peerUserId ? (userId, peerUserId) : (peerUserId, userId);
        return $"{min}_{max}";
    }

    private static string BuildPreview(ChatMessageType messageType, string? content, string? fileName)
    {
        var preview = messageType switch
        {
            ChatMessageType.Image => "[图片]",
            ChatMessageType.File => $"[文件] {fileName}".TrimEnd(),
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
                if (command.FileId is not > 0)
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
