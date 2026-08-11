// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using System.Linq.Expressions;
using System.Reflection;
using XiHan.BasicApp.Chat.Domain.DomainServices;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Chat.Tests;

/// <summary>
/// 聊天领域服务不变量测试，覆盖单聊/助手会话唯一定位、成员资格校验、消息撤回和群成员治理。
/// </summary>
public sealed class ChatDomainServiceTests
{
    /// <summary>
    /// 同一对用户已有单聊会话时必须返回既有会话，不得重建。
    /// </summary>
    [Fact]
    public async Task GetOrCreateSingleConversationAsync_ExistingPairShouldReturnWithoutCreate()
    {
        var fixture = CreateFixture(CreateUser(1, "张三"), CreateUser(2, "李四"));
        var existing = CreateConversation(100, ChatConversationType.Single, pairKey: "1_2");
        fixture.ConversationRepository
            .Setup(value => value.GetByPairKeyAsync("1_2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await fixture.Service.GetOrCreateSingleConversationAsync(
            new ChatSingleConversationCommand(UserId: 2, PeerUserId: 1));

        Assert.False(result.Created);
        Assert.Same(existing, result.Conversation);
        fixture.ConversationRepository.Verify(
            value => value.GetByPairKeyAsync("1_2", It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.ConversationRepository.Verify(
            value => value.AddAsync(It.IsAny<SysChatConversation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 首次单聊必须按「小ID_大ID」写入配对键，并为双方各建一条成员记录。
    /// </summary>
    [Fact]
    public async Task GetOrCreateSingleConversationAsync_NewPairShouldCreateWithNormalizedPairKey()
    {
        var fixture = CreateFixture(CreateUser(1, "张三"), CreateUser(2, "李四"));

        var result = await fixture.Service.GetOrCreateSingleConversationAsync(
            new ChatSingleConversationCommand(UserId: 2, PeerUserId: 1));

        Assert.True(result.Created);
        Assert.Equal("1_2", result.Conversation.PairKey);
        Assert.Equal(ChatConversationType.Single, result.Conversation.ConversationType);
        Assert.Equal(2, result.Conversation.MemberCount);
        fixture.MemberRepository.Verify(
            value => value.AddAsync(It.IsAny<SysChatConversationMember>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    /// <summary>
    /// 与自己建立单聊必须被拒绝。
    /// </summary>
    [Fact]
    public async Task GetOrCreateSingleConversationAsync_SelfPairShouldReject()
    {
        var fixture = CreateFixture(CreateUser(1, "张三"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.GetOrCreateSingleConversationAsync(
                new ChatSingleConversationCommand(UserId: 1, PeerUserId: 1)));

        Assert.Contains("不能与自己", exception.Message, StringComparison.Ordinal);
        fixture.ConversationRepository.Verify(
            value => value.AddAsync(It.IsAny<SysChatConversation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 首次创建助手会话必须写入「ai:{助手}:{用户}」配对键、会话类型为助手且成员只有用户本人。
    /// </summary>
    [Fact]
    public async Task GetOrCreateAssistantConversationAsync_ShouldBuildAssistantPairKeyAndType()
    {
        var fixture = CreateFixture(CreateUser(2, "李四"));
        SysChatConversationMember? addedMember = null;
        fixture.MemberRepository
            .Setup(value => value.AddAsync(It.IsAny<SysChatConversationMember>(), It.IsAny<CancellationToken>()))
            .Callback((SysChatConversationMember member, CancellationToken _) => addedMember = member)
            .ReturnsAsync((SysChatConversationMember member, CancellationToken _) => member);

        var result = await fixture.Service.GetOrCreateAssistantConversationAsync(
            new ChatAssistantConversationCommand(UserId: 2, AssistantId: 9, AssistantName: "小曦", Avatar: null));

        Assert.True(result.Created);
        Assert.Equal("ai:9:2", result.Conversation.PairKey);
        Assert.Equal(ChatConversationType.Assistant, result.Conversation.ConversationType);
        Assert.Equal(9, result.Conversation.AssistantId);
        Assert.Equal(1, result.Conversation.MemberCount);
        Assert.Equal("小曦", result.Conversation.ConversationName);
        Assert.NotNull(addedMember);
        Assert.Equal(2, addedMember.UserId);
        fixture.ConversationRepository.Verify(
            value => value.GetByPairKeyAsync("ai:9:2", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 同一用户对同一助手已有会话时必须返回既有会话，不得重建。
    /// </summary>
    [Fact]
    public async Task GetOrCreateAssistantConversationAsync_ExistingShouldReturnWithoutCreate()
    {
        var fixture = CreateFixture(CreateUser(2, "李四"));
        var existing = CreateConversation(100, ChatConversationType.Assistant, pairKey: "ai:9:2");
        fixture.ConversationRepository
            .Setup(value => value.GetByPairKeyAsync("ai:9:2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await fixture.Service.GetOrCreateAssistantConversationAsync(
            new ChatAssistantConversationCommand(UserId: 2, AssistantId: 9, AssistantName: "小曦", Avatar: null));

        Assert.False(result.Created);
        Assert.Same(existing, result.Conversation);
        fixture.ConversationRepository.Verify(
            value => value.AddAsync(It.IsAny<SysChatConversation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 非会话成员发送消息必须被拒绝，且不得写入消息。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_NonMemberShouldReject()
    {
        var fixture = CreateFixture(CreateUser(5, "张三"), CreateUser(6, "李四"));
        var conversation = CreateConversation(100, ChatConversationType.Group);
        fixture.ConversationRepository
            .Setup(value => value.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        fixture.MemberRepository
            .Setup(value => value.GetByConversationIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateMember(100, 5)]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, SenderUserId: 6, ChatMessageType.Text, "hello", null, null)));

        Assert.Contains("仅会话成员可发送消息", exception.Message, StringComparison.Ordinal);
        fixture.MessageRepository.Verify(
            value => value.AddAsync(It.IsAny<SysChatMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 被禁言的成员发送消息必须被拒绝。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_SilencedMemberShouldReject()
    {
        var fixture = CreateFixture(CreateUser(5, "张三"));
        var conversation = CreateConversation(100, ChatConversationType.Group);
        fixture.ConversationRepository
            .Setup(value => value.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        fixture.MemberRepository
            .Setup(value => value.GetByConversationIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateMember(100, 5, silenced: true)]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, SenderUserId: 5, ChatMessageType.Text, "hello", null, null)));

        Assert.Contains("禁言", exception.Message, StringComparison.Ordinal);
        fixture.MessageRepository.Verify(
            value => value.AddAsync(It.IsAny<SysChatMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 成员发送文本消息必须落库、刷新会话最后消息冗余、给其余成员未读加一并前移发送者已读位。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_MemberTextShouldPersistAndFanOut()
    {
        var fixture = CreateFixture(CreateUser(5, "张三"), CreateUser(6, "李四"));
        var conversation = CreateConversation(100, ChatConversationType.Group);
        var senderMember = CreateMember(100, 5);
        fixture.ConversationRepository
            .Setup(value => value.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        fixture.MemberRepository
            .Setup(value => value.GetByConversationIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([senderMember, CreateMember(100, 6)]);

        var result = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, SenderUserId: 5, ChatMessageType.Text, "hello", null, "client-1"));

        Assert.Equal("hello", result.Message.Content);
        Assert.Equal(100, result.Message.ConversationId);
        Assert.Equal("张三", result.Message.SenderUserName);
        Assert.Equal(result.Message.BasicId, conversation.LastMessageId);
        Assert.Equal("hello", conversation.LastMessagePreview);
        Assert.Equal([5, 6], result.RecipientUserIds);
        Assert.Equal(result.Message.BasicId, senderMember.LastReadMessageId);
        fixture.MemberRepository.Verify(
            value => value.IncrementUnreadAsync(100, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 非会话成员标记已读必须被拒绝。
    /// </summary>
    [Fact]
    public async Task MarkConversationReadAsync_NonMemberShouldReject()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.MarkConversationReadAsync(
                new ChatMarkReadCommand(ConversationId: 100, UserId: 9, UpToMessageId: null)));

        Assert.Contains("仅会话成员可标记已读", exception.Message, StringComparison.Ordinal);
        fixture.MemberRepository.Verify(
            value => value.UpdateAsync(It.IsAny<SysChatConversationMember>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 撤回他人消息必须被拒绝。
    /// </summary>
    [Fact]
    public async Task RecallMessageAsync_OthersMessageShouldReject()
    {
        var fixture = CreateFixture();
        var message = CreateMessage(200, conversationId: 100, senderUserId: 5, "hi", DateTimeOffset.UtcNow);
        fixture.MessageRepository
            .Setup(value => value.GetByIdAsync(200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.RecallMessageAsync(new ChatMessageRecallCommand(MessageId: 200, OperatorUserId: 6)));

        Assert.Contains("仅可撤回自己发送的消息", exception.Message, StringComparison.Ordinal);
        fixture.MessageRepository.Verify(
            value => value.UpdateAsync(It.IsAny<SysChatMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 撤回自己的消息必须置标清空内容并保留行，不得物理删除；撤回最后一条时同步刷新会话预览。
    /// </summary>
    [Fact]
    public async Task RecallMessageAsync_ShouldMarkRecalledWithoutDelete()
    {
        var fixture = CreateFixture();
        var message = CreateMessage(200, conversationId: 100, senderUserId: 5, "hi", DateTimeOffset.UtcNow);
        var conversation = CreateConversation(100, ChatConversationType.Single);
        conversation.LastMessageId = 200;
        fixture.MessageRepository
            .Setup(value => value.GetByIdAsync(200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        fixture.ConversationRepository
            .Setup(value => value.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var result = await fixture.Service.RecallMessageAsync(
            new ChatMessageRecallCommand(MessageId: 200, OperatorUserId: 5));

        Assert.True(result.Message.IsRecalled);
        Assert.NotNull(result.Message.RecallTime);
        Assert.Null(result.Message.Content);
        Assert.Equal("[消息已撤回]", conversation.LastMessagePreview);
        fixture.MessageRepository.Verify(
            value => value.UpdateAsync(message, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.MessageRepository.Verify(
            value => value.DeleteAsync(It.IsAny<SysChatMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.MessageRepository.Verify(
            value => value.DeleteByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 超出撤回时间窗口的消息必须拒绝撤回。
    /// </summary>
    [Fact]
    public async Task RecallMessageAsync_BeyondWindowShouldReject()
    {
        var fixture = CreateFixture();
        var message = CreateMessage(200, conversationId: 100, senderUserId: 5, "hi", DateTimeOffset.UtcNow.AddMinutes(-10));
        fixture.MessageRepository
            .Setup(value => value.GetByIdAsync(200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.RecallMessageAsync(new ChatMessageRecallCommand(MessageId: 200, OperatorUserId: 5)));

        Assert.Contains("2 分钟", exception.Message, StringComparison.Ordinal);
        Assert.False(message.IsRecalled);
        fixture.MessageRepository.Verify(
            value => value.UpdateAsync(It.IsAny<SysChatMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 普通成员移除他人必须被拒绝，仅群主或管理员可管理成员。
    /// </summary>
    [Fact]
    public async Task RemoveMemberAsync_NonManagerOperatorShouldReject()
    {
        var fixture = CreateFixture();
        var conversation = CreateConversation(100, ChatConversationType.Group, ownerUserId: 5);
        fixture.ConversationRepository
            .Setup(value => value.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        fixture.MemberRepository
            .Setup(value => value.GetMemberAsync(100, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMember(100, 7, ChatMemberRole.Member));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.RemoveMemberAsync(
                new ChatMemberRemoveCommand(ConversationId: 100, OperatorUserId: 7, UserId: 6)));

        Assert.Contains("仅群主或管理员可管理成员", exception.Message, StringComparison.Ordinal);
        fixture.MemberRepository.Verify(
            value => value.DeleteAsync(It.IsAny<SysChatConversationMember>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 群主不能退群或被移出。
    /// </summary>
    [Fact]
    public async Task RemoveMemberAsync_OwnerShouldNotBeRemoved()
    {
        var fixture = CreateFixture();
        var conversation = CreateConversation(100, ChatConversationType.Group, ownerUserId: 5);
        fixture.ConversationRepository
            .Setup(value => value.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.RemoveMemberAsync(
                new ChatMemberRemoveCommand(ConversationId: 100, OperatorUserId: 5, UserId: 5)));

        Assert.Contains("群主不能退群或被移出", exception.Message, StringComparison.Ordinal);
        fixture.MemberRepository.Verify(
            value => value.DeleteAsync(It.IsAny<SysChatConversationMember>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 管理员移除普通成员必须软删成员记录、递减成员数并追加系统提示消息。
    /// </summary>
    [Fact]
    public async Task RemoveMemberAsync_AdminShouldRemoveMemberAndAppendSystemMessage()
    {
        var fixture = CreateFixture(CreateUser(6, "李四"), CreateUser(7, "王五"));
        var conversation = CreateConversation(100, ChatConversationType.Group, ownerUserId: 5);
        conversation.MemberCount = 3;
        var target = CreateMember(100, 6);
        fixture.ConversationRepository
            .Setup(value => value.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        fixture.MemberRepository
            .Setup(value => value.GetMemberAsync(100, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMember(100, 7, ChatMemberRole.Admin));
        fixture.MemberRepository
            .Setup(value => value.GetMemberAsync(100, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        fixture.MemberRepository
            .Setup(value => value.DeleteAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        fixture.MemberRepository
            .Setup(value => value.GetByConversationIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateMember(100, 5, ChatMemberRole.Owner), CreateMember(100, 7, ChatMemberRole.Admin)]);

        var result = await fixture.Service.RemoveMemberAsync(
            new ChatMemberRemoveCommand(ConversationId: 100, OperatorUserId: 7, UserId: 6));

        Assert.Equal(2, result.Conversation.MemberCount);
        Assert.NotNull(result.SystemMessage);
        Assert.Equal(ChatMessageType.System, result.SystemMessage.MessageType);
        Assert.Equal("王五 将 李四 移出群聊", result.SystemMessage.Content);
        Assert.Equal([5, 7], result.RecipientUserIds);
        fixture.MemberRepository.Verify(
            value => value.DeleteAsync(target, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 创建领域服务与全部仓储替身，默认平台租户上下文并回填新增实体主键。
    /// </summary>
    /// <param name="users">用户仓储可解析的用户集合。</param>
    /// <returns>聊天领域测试夹具。</returns>
    private static DomainFixture CreateFixture(params SysUser[] users)
    {
        var conversationRepository = new Mock<IChatConversationRepository>();
        conversationRepository
            .Setup(value => value.AddAsync(It.IsAny<SysChatConversation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatConversation conversation, CancellationToken _) =>
            {
                SetEntityId(conversation, 1001);
                return conversation;
            });
        conversationRepository
            .Setup(value => value.UpdateAsync(It.IsAny<SysChatConversation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatConversation conversation, CancellationToken _) => conversation);

        var memberId = 3000L;
        var memberRepository = new Mock<IChatConversationMemberRepository>();
        memberRepository
            .Setup(value => value.AddAsync(It.IsAny<SysChatConversationMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatConversationMember member, CancellationToken _) =>
            {
                SetEntityId(member, ++memberId);
                return member;
            });
        memberRepository
            .Setup(value => value.UpdateAsync(It.IsAny<SysChatConversationMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatConversationMember member, CancellationToken _) => member);
        memberRepository
            .Setup(value => value.IncrementUnreadAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        memberRepository
            .Setup(value => value.GetByConversationIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var messageRepository = new Mock<IChatMessageRepository>();
        messageRepository
            .Setup(value => value.AddAsync(It.IsAny<SysChatMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatMessage message, CancellationToken _) =>
            {
                SetEntityId(message, 5001);
                return message;
            });
        messageRepository
            .Setup(value => value.UpdateAsync(It.IsAny<SysChatMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatMessage message, CancellationToken _) => message);

        var reactionRepository = new Mock<IChatMessageReactionRepository>();

        var userRepository = new Mock<IUserRepository>();
        foreach (var user in users)
        {
            userRepository
                .Setup(value => value.GetByIdAsync(user.BasicId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
        }

        userRepository
            .Setup(value => value.GetListAsync(It.IsAny<Expression<Func<SysUser, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. users]);

        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(value => value.Id).Returns((long?)null);

        var service = new ChatDomainService(
            conversationRepository.Object,
            memberRepository.Object,
            messageRepository.Object,
            reactionRepository.Object,
            userRepository.Object,
            new Mock<IDepartmentRepository>().Object,
            new Mock<IUserDepartmentRepository>().Object,
            new Mock<ITenantUserRepository>().Object,
            currentTenant.Object);
        return new DomainFixture(service, conversationRepository, memberRepository, messageRepository, userRepository);
    }

    /// <summary>
    /// 创建平台归属用户实体。
    /// </summary>
    private static SysUser CreateUser(long id, string userName)
    {
        var user = new SysUser
        {
            UserName = userName,
            TenantId = 0
        };
        SetEntityId(user, id);
        return user;
    }

    /// <summary>
    /// 创建指定类型的会话实体。
    /// </summary>
    private static SysChatConversation CreateConversation(
        long id,
        ChatConversationType conversationType,
        long? ownerUserId = null,
        string? pairKey = null)
    {
        var conversation = new SysChatConversation
        {
            ConversationType = conversationType,
            OwnerUserId = ownerUserId,
            PairKey = pairKey,
            MemberCount = 2
        };
        SetEntityId(conversation, id);
        return conversation;
    }

    /// <summary>
    /// 创建会话成员实体。
    /// </summary>
    private static SysChatConversationMember CreateMember(
        long conversationId,
        long userId,
        ChatMemberRole role = ChatMemberRole.Member,
        bool silenced = false)
    {
        var member = new SysChatConversationMember
        {
            ConversationId = conversationId,
            UserId = userId,
            MemberRole = role,
            IsSilenced = silenced,
            JoinTime = DateTimeOffset.UtcNow
        };
        SetEntityId(member, conversationId * 100 + userId);
        return member;
    }

    /// <summary>
    /// 创建指定发送时间的文本消息实体。
    /// </summary>
    private static SysChatMessage CreateMessage(
        long id,
        long conversationId,
        long senderUserId,
        string? content,
        DateTimeOffset createdTime)
    {
        var message = new SysChatMessage
        {
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            MessageType = ChatMessageType.Text,
            Content = content,
            CreatedTime = createdTime
        };
        SetEntityId(message, id);
        return message;
    }

    /// <summary>
    /// 模拟 ORM 回填受保护的实体主键。
    /// </summary>
    private static void SetEntityId(object entity, long id)
    {
        var property = entity.GetType().GetProperty(
            "BasicId",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("未找到实体主键属性。");
        property.SetValue(entity, id);
    }

    /// <summary>
    /// 聊天领域测试依赖集合。
    /// </summary>
    private sealed record DomainFixture(
        ChatDomainService Service,
        Mock<IChatConversationRepository> ConversationRepository,
        Mock<IChatConversationMemberRepository> MemberRepository,
        Mock<IChatMessageRepository> MessageRepository,
        Mock<IUserRepository> UserRepository);
}
