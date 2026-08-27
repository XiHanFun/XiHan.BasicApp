// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Chat.Application.AppServices;
using XiHan.BasicApp.Chat.Application.Dtos;
using XiHan.BasicApp.Chat.Application.Services;
using XiHan.BasicApp.Chat.Domain.DomainServices;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Chat.Tests;

/// <summary>
/// 聊天命令应用服务测试：当前用户解析、敏感词前置拦截、领域命令组装，以及落库后的实时扇出口径。
/// </summary>
/// <remarks>
/// 应用服务这一层不做业务判定，它只负责三件容易写错又不会报错的事：把当前登录用户塞进命令
/// （错了就变成替别人发言）、在落库前跑敏感词（顺序反了就先落库再拦截）、按结果把变更推给谁
/// （推多了泄露、推少了对端界面不刷新）。这里对每条都给出可验证的断言。
/// </remarks>
public sealed class ChatExtraAppServiceTests
{
    private const long CurrentUserId = 42;

    /// <summary>
    /// 打开单聊：会话主键与"是否新建"必须原样来自领域结果，不得由应用层自行推断。
    /// </summary>
    [Fact]
    public async Task OpenSingleConversationAsync_ShouldProjectDomainResultAsIs()
    {
        var context = new AppServiceContext();
        var conversation = CreateConversation(100, ChatConversationType.Single);
        context.Domain
            .Setup(value => value.GetOrCreateSingleConversationAsync(
                It.IsAny<ChatSingleConversationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatConversationCommandResult(conversation, Created: false));

        var dto = await context.Service.OpenSingleConversationAsync(new ChatSingleConversationOpenDto { PeerUserId = 7 });

        Assert.Equal(100, dto.ConversationId);
        Assert.False(dto.Created);
        context.Domain.Verify(
            value => value.GetOrCreateSingleConversationAsync(
                It.Is<ChatSingleConversationCommand>(command => command.UserId == CurrentUserId && command.PeerUserId == 7),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 打开部门群：操作者必须锁定为当前登录用户，部门主键取自入参。
    /// </summary>
    [Fact]
    public async Task OpenDepartmentConversationAsync_ShouldBindCurrentUserAsOperator()
    {
        var context = new AppServiceContext();
        var conversation = CreateConversation(100, ChatConversationType.Department);
        context.Domain
            .Setup(value => value.GetOrCreateDepartmentConversationAsync(
                It.IsAny<ChatDepartmentConversationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatConversationCommandResult(conversation, Created: true));

        var dto = await context.Service.OpenDepartmentConversationAsync(
            new ChatDepartmentConversationOpenDto { DepartmentId = 50 });

        Assert.True(dto.Created);
        context.Domain.Verify(
            value => value.GetOrCreateDepartmentConversationAsync(
                It.Is<ChatDepartmentConversationCommand>(command =>
                    command.DepartmentId == 50 && command.OperatorUserId == CurrentUserId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 建群成功必须同时推会话变更与建群系统提示，返回的 DTO 一律标记为新建。
    /// </summary>
    [Fact]
    public async Task CreateGroupConversationAsync_ShouldPushChangeAndSystemMessage()
    {
        var context = new AppServiceContext();
        var conversation = CreateConversation(100, ChatConversationType.Group);
        var systemMessage = CreateMessage(200, 100, senderUserId: 0, ChatMessageType.System, "张三 创建了群聊");
        context.Domain
            .Setup(value => value.CreateGroupConversationAsync(
                It.IsAny<ChatGroupCreateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatGovernanceResult(conversation, systemMessage, [1, 2]));

        var dto = await context.Service.CreateGroupConversationAsync(
            new ChatGroupCreateDto { ConversationName = "项目群", MemberUserIds = [1, 2] });

        Assert.True(dto.Created);
        context.Push.Verify(
            value => value.PushConversationChangedAsync(100, "created", It.Is<IReadOnlyList<long>>(ids => ids.Count == 2)),
            Times.Once);
        context.Push.Verify(
            value => value.PushMessageAsync(
                It.Is<ChatMessageItemDto>(message => message.MessageId == 200),
                conversation,
                It.IsAny<IReadOnlyList<long>>()),
            Times.Once);
    }

    /// <summary>
    /// 治理结果没有系统提示时只推会话变更，不得凭空造出一条消息推送。
    /// </summary>
    [Fact]
    public async Task AddMembersAsync_WithoutSystemMessageShouldOnlyPushConversationChange()
    {
        var context = new AppServiceContext();
        var conversation = CreateConversation(100, ChatConversationType.Group);
        context.Domain
            .Setup(value => value.AddMembersAsync(It.IsAny<ChatMemberAddCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatGovernanceResult(conversation, SystemMessage: null, [1, 2]));

        await context.Service.AddMembersAsync(new ChatMemberAddDto { ConversationId = 100, UserIds = [2] });

        context.Push.Verify(
            value => value.PushConversationChangedAsync(100, "member-added", It.IsAny<IReadOnlyList<long>>()),
            Times.Once);
        context.Push.Verify(
            value => value.PushMessageAsync(It.IsAny<ChatMessageItemDto>(), It.IsAny<SysChatConversation>(), It.IsAny<IReadOnlyList<long>>()),
            Times.Never);
    }

    /// <summary>
    /// 移除成员时被移出者不在剩余收件人内，必须单独收到一次会话变更，否则其会话列表不会收敛。
    /// </summary>
    [Fact]
    public async Task RemoveMemberAsync_ShouldNotifyRemovedUserSeparately()
    {
        var context = new AppServiceContext();
        var conversation = CreateConversation(100, ChatConversationType.Group);
        context.Domain
            .Setup(value => value.RemoveMemberAsync(It.IsAny<ChatMemberRemoveCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatGovernanceResult(conversation, SystemMessage: null, [1, 2]));

        await context.Service.RemoveMemberAsync(new ChatMemberRemoveDto { ConversationId = 100, UserId = 9 });

        context.Push.Verify(
            value => value.PushConversationChangedAsync(100, "member-removed", It.Is<IReadOnlyList<long>>(ids => ids.Count == 2)),
            Times.Once);
        context.Push.Verify(
            value => value.PushConversationChangedAsync(100, "member-removed", It.Is<IReadOnlyList<long>>(ids => ids.Count == 1 && ids[0] == 9)),
            Times.Once);
    }

    /// <summary>
    /// 敏感词拦截必须发生在落库之前：命中时领域服务与推送都不得被调用。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_SensitiveWordShouldBlockBeforePersist()
    {
        var context = new AppServiceContext();
        context.Guard
            .Setup(value => value.EnsureAllowedAsync("赌博广告", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("消息包含敏感词，已被拦截。"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.SendMessageAsync(new ChatMessageSendDto
            {
                ConversationId = 100,
                MessageType = ChatMessageType.Text,
                Content = "赌博广告"
            }));

        Assert.Contains("敏感词", exception.Message, StringComparison.Ordinal);
        context.Domain.Verify(
            value => value.SendMessageAsync(It.IsAny<ChatMessageSendCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        context.Push.Verify(
            value => value.PushMessageAsync(It.IsAny<ChatMessageItemDto>(), It.IsAny<SysChatConversation>(), It.IsAny<IReadOnlyList<long>>()),
            Times.Never);
    }

    /// <summary>
    /// 发送消息成功后必须把消息 DTO 与会话摘要一并推给全体收件人（含发送者本人，用于多端回显）。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_ShouldPushProjectedMessageToRecipients()
    {
        var context = new AppServiceContext();
        var conversation = CreateConversation(100, ChatConversationType.Single);
        var message = CreateMessage(200, 100, CurrentUserId, ChatMessageType.Text, "hello");
        context.Domain
            .Setup(value => value.SendMessageAsync(It.IsAny<ChatMessageSendCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatMessageSendResult(message, conversation, [CurrentUserId, 7]));

        var dto = await context.Service.SendMessageAsync(new ChatMessageSendDto
        {
            ConversationId = 100,
            MessageType = ChatMessageType.Text,
            Content = "hello",
            ClientMessageId = "client-1"
        });

        Assert.Equal(200, dto.MessageId);
        Assert.Equal("hello", dto.Content);
        context.Guard.Verify(value => value.EnsureAllowedAsync("hello", It.IsAny<CancellationToken>()), Times.Once);
        context.Domain.Verify(
            value => value.SendMessageAsync(
                It.Is<ChatMessageSendCommand>(command =>
                    command.ConversationId == 100
                    && command.SenderUserId == CurrentUserId
                    && command.ClientMessageId == "client-1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        context.Push.Verify(
            value => value.PushMessageAsync(
                It.Is<ChatMessageItemDto>(item => item.MessageId == 200),
                conversation,
                It.Is<IReadOnlyList<long>>(ids => ids.Count == 2)),
            Times.Once);
    }

    /// <summary>
    /// 撤回消息必须按领域结果里的会话与消息主键推送撤回事件。
    /// </summary>
    [Fact]
    public async Task RecallMessageAsync_ShouldPushRecalledWithDomainIdentifiers()
    {
        var context = new AppServiceContext();
        var message = CreateMessage(200, 100, CurrentUserId, ChatMessageType.Text, null);
        message.IsRecalled = true;
        context.Domain
            .Setup(value => value.RecallMessageAsync(It.IsAny<ChatMessageRecallCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatMessageRecallResult(message, [1, 2]));

        await context.Service.RecallMessageAsync(200);

        context.Domain.Verify(
            value => value.RecallMessageAsync(
                It.Is<ChatMessageRecallCommand>(command => command.MessageId == 200 && command.OperatorUserId == CurrentUserId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        context.Push.Verify(
            value => value.PushRecalledAsync(100, 200, It.Is<IReadOnlyList<long>>(ids => ids.Count == 2)),
            Times.Once);
    }

    /// <summary>
    /// 编辑消息同样要先过敏感词，成功后推送新正文与编辑时间。
    /// </summary>
    [Fact]
    public async Task EditMessageAsync_ShouldGuardContentThenPushEdited()
    {
        var context = new AppServiceContext();
        var editedTime = DateTimeOffset.UtcNow;
        var message = CreateMessage(200, 100, CurrentUserId, ChatMessageType.Text, "改后正文");
        message.EditedTime = editedTime;
        context.Domain
            .Setup(value => value.EditMessageAsync(It.IsAny<ChatMessageEditCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatMessageEditResult(message, [1, 2]));

        var dto = await context.Service.EditMessageAsync(new ChatMessageEditDto { MessageId = 200, Content = "改后正文" });

        Assert.Equal("改后正文", dto.Content);
        Assert.Equal(editedTime, dto.EditedTime);
        context.Guard.Verify(value => value.EnsureAllowedAsync("改后正文", It.IsAny<CancellationToken>()), Times.Once);
        context.Push.Verify(
            value => value.PushMessageEditedAsync(100, 200, "改后正文", editedTime, It.IsAny<IReadOnlyList<long>>()),
            Times.Once);
    }

    /// <summary>
    /// 表情回应结果只回传 Added 标记，同时把增量推给全体成员。
    /// </summary>
    [Fact]
    public async Task ToggleReactionAsync_ShouldReturnAddedFlagAndPushDelta()
    {
        var context = new AppServiceContext();
        context.Domain
            .Setup(value => value.ToggleReactionAsync(It.IsAny<ChatReactionToggleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatReactionToggleResult(100, 200, CurrentUserId, "张三", "👍", Added: true, [1, 2]));

        var dto = await context.Service.ToggleReactionAsync(new ChatReactionToggleDto { MessageId = 200, Emoji = "👍" });

        Assert.True(dto.Added);
        context.Push.Verify(
            value => value.PushReactionChangedAsync(100, 200, "👍", CurrentUserId, "张三", true, It.IsAny<IReadOnlyList<long>>()),
            Times.Once);
    }

    /// <summary>
    /// Pin 与取消 Pin 共用领域命令，区别只在 Pin 标志；两者都推同一个会话变更类型。
    /// </summary>
    [Fact]
    public async Task PinAndUnpinMessageAsync_ShouldDifferOnlyByPinFlag()
    {
        var context = new AppServiceContext();
        var message = CreateMessage(200, 100, CurrentUserId, ChatMessageType.Text, "hi");
        context.Domain
            .Setup(value => value.SetMessagePinAsync(It.IsAny<ChatMessagePinCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatMessagePinResult(message, [1, 2]));

        await context.Service.PinMessageAsync(new ChatMessagePinDto { MessageId = 200 });
        await context.Service.UnpinMessageAsync(new ChatMessagePinDto { MessageId = 200 });

        context.Domain.Verify(
            value => value.SetMessagePinAsync(
                It.Is<ChatMessagePinCommand>(command => command.MessageId == 200 && command.Pin),
                It.IsAny<CancellationToken>()),
            Times.Once);
        context.Domain.Verify(
            value => value.SetMessagePinAsync(
                It.Is<ChatMessagePinCommand>(command => command.MessageId == 200 && !command.Pin),
                It.IsAny<CancellationToken>()),
            Times.Once);
        context.Push.Verify(
            value => value.PushConversationChangedAsync(100, "pinned-changed", It.IsAny<IReadOnlyList<long>>()),
            Times.Exactly(2));
    }

    /// <summary>
    /// 会话置顶与免打扰是个人维度设置，只能推给当前用户本人做多端同步，不得广播给其他成员。
    /// </summary>
    [Fact]
    public async Task ToggleConversationSettings_ShouldPushOnlyToCurrentUser()
    {
        var context = new AppServiceContext();
        context.Domain
            .Setup(value => value.TogglePinConversationAsync(It.IsAny<ChatMemberToggleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        context.Domain
            .Setup(value => value.ToggleMuteConversationAsync(It.IsAny<ChatMemberToggleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var pinned = await context.Service.TogglePinConversationAsync(new ChatConversationToggleDto { ConversationId = 100 });
        var muted = await context.Service.ToggleMuteConversationAsync(new ChatConversationToggleDto { ConversationId = 100 });

        Assert.True(pinned.IsOn);
        Assert.False(muted.IsOn);
        context.Push.Verify(
            value => value.PushConversationChangedAsync(
                100,
                "member-setting-changed",
                It.Is<IReadOnlyList<long>>(ids => ids.Count == 1 && ids[0] == CurrentUserId)),
            Times.Exactly(2));
    }

    /// <summary>
    /// 标记已读必须把已读位扇出给全体成员，群已读回执依赖这条推送刷新。
    /// </summary>
    [Fact]
    public async Task MarkReadAsync_ShouldFanOutReadPositionToAllMembers()
    {
        var context = new AppServiceContext();
        context.Domain
            .Setup(value => value.MarkConversationReadAsync(It.IsAny<ChatMarkReadCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatMarkReadResult(100, CurrentUserId, 500, [1, 2, 3]));

        await context.Service.MarkReadAsync(new ChatMarkReadDto { ConversationId = 100, UpToMessageId = 500 });

        context.Push.Verify(
            value => value.PushReadPositionChangedAsync(
                100, CurrentUserId, 500, It.Is<IReadOnlyList<long>>(ids => ids.Count == 3)),
            Times.Once);
    }

    /// <summary>
    /// 群治理类命令各自使用固定的会话变更类型，前端按类型决定刷新范围。
    /// </summary>
    [Fact]
    public async Task GovernanceCommands_ShouldUseDistinctChangeTypes()
    {
        var context = new AppServiceContext();
        var conversation = CreateConversation(100, ChatConversationType.Group);
        var governance = new ChatGovernanceResult(conversation, SystemMessage: null, [1, 2]);
        context.Domain
            .Setup(value => value.UpdateConversationInfoAsync(It.IsAny<ChatConversationInfoUpdateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(governance);
        context.Domain
            .Setup(value => value.TransferOwnerAsync(It.IsAny<ChatOwnerTransferCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(governance);
        context.Domain
            .Setup(value => value.SetMemberSilenceAsync(It.IsAny<ChatMemberSilenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(governance);
        context.Domain
            .Setup(value => value.SetMemberRoleAsync(It.IsAny<ChatMemberRoleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(governance);

        await context.Service.UpdateConversationInfoAsync(new ChatConversationInfoUpdateDto { ConversationId = 100 });
        await context.Service.TransferOwnerAsync(new ChatOwnerTransferDto { ConversationId = 100, NewOwnerUserId = 2 });
        await context.Service.SetMemberSilenceAsync(new ChatMemberSilenceDto { ConversationId = 100, UserId = 2, IsSilenced = true });
        await context.Service.SetMemberRoleAsync(new ChatMemberRoleDto { ConversationId = 100, UserId = 2, MemberRole = ChatMemberRole.Admin });

        foreach (var changeType in new[] { "info-changed", "owner-transferred", "member-silenced", "member-role-changed" })
        {
            context.Push.Verify(
                value => value.PushConversationChangedAsync(100, changeType, It.IsAny<IReadOnlyList<long>>()),
                Times.Once);
        }
    }

    /// <summary>
    /// 全部带入参的端点对 null 入参都必须抛参数空异常，不得把 null 传进领域层。
    /// </summary>
    [Fact]
    public async Task Endpoints_NullInputShouldThrow()
    {
        var context = new AppServiceContext();

        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.OpenSingleConversationAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.CreateGroupConversationAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.OpenDepartmentConversationAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.AddMembersAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.RemoveMemberAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.SendMessageAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.EditMessageAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.ToggleReactionAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.PinMessageAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.UnpinMessageAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.TogglePinConversationAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.ToggleMuteConversationAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.MarkReadAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.UpdateConversationInfoAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.TransferOwnerAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.SetMemberSilenceAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.SetMemberRoleAsync(null!));
    }

    /// <summary>
    /// 拿不到当前登录用户时必须直接失败，绝不允许用默认值 0 去替某个"用户"发言或建会话。
    /// </summary>
    [Fact]
    public async Task Endpoints_WithoutAuthenticatedUserShouldThrow()
    {
        var context = new AppServiceContext(currentUserId: null);

        var open = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.OpenSingleConversationAsync(new ChatSingleConversationOpenDto { PeerUserId = 7 }));
        Assert.Contains("当前用户未登录", open.Message, StringComparison.Ordinal);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.SendMessageAsync(new ChatMessageSendDto { ConversationId = 100, Content = "hi" }));
        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.MarkReadAsync(new ChatMarkReadDto { ConversationId = 100 }));
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Service.RecallMessageAsync(200));

        context.Domain.Verify(
            value => value.SendMessageAsync(It.IsAny<ChatMessageSendCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 已取消的令牌必须在进入领域层之前就抛出取消异常。
    /// </summary>
    [Fact]
    public async Task Endpoints_CancelledTokenShouldThrowBeforeDomainCall()
    {
        var context = new AppServiceContext();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.OpenSingleConversationAsync(new ChatSingleConversationOpenDto { PeerUserId = 7 }, source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.SendMessageAsync(new ChatMessageSendDto { ConversationId = 100, Content = "hi" }, source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.RecallMessageAsync(200, source.Token));

        context.Domain.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 创建带主键的会话实体。
    /// </summary>
    /// <param name="id">会话主键。</param>
    /// <param name="conversationType">会话类型。</param>
    /// <returns>会话实体。</returns>
    private static SysChatConversation CreateConversation(long id, ChatConversationType conversationType)
    {
        var conversation = new SysChatConversation { ConversationType = conversationType };
        ChatExtraDomainFixture.SetEntityId(conversation, id);
        return conversation;
    }

    /// <summary>
    /// 创建带主键的消息实体。
    /// </summary>
    /// <param name="id">消息主键。</param>
    /// <param name="conversationId">会话主键。</param>
    /// <param name="senderUserId">发送人用户主键。</param>
    /// <param name="messageType">消息类型。</param>
    /// <param name="content">消息正文。</param>
    /// <returns>消息实体。</returns>
    private static SysChatMessage CreateMessage(
        long id,
        long conversationId,
        long senderUserId,
        ChatMessageType messageType,
        string? content)
    {
        var message = new SysChatMessage
        {
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            MessageType = messageType,
            Content = content,
            CreatedTime = DateTimeOffset.UtcNow
        };
        ChatExtraDomainFixture.SetEntityId(message, id);
        return message;
    }

    /// <summary>
    /// 命令应用服务及其四个协作者替身。
    /// </summary>
    private sealed class AppServiceContext
    {
        /// <summary>
        /// 组装应用服务与替身。
        /// </summary>
        /// <param name="currentUserId">当前登录用户主键；null 表示未登录。</param>
        public AppServiceContext(long? currentUserId = CurrentUserId)
        {
            Domain = new Mock<IChatDomainService>();
            Push = new Mock<IChatRealtimePushService>();
            Guard = new Mock<IChatSensitiveWordGuard>();
            CurrentUser = new Mock<ICurrentUser>();
            CurrentUser.SetupGet(value => value.UserId).Returns(currentUserId);
            Service = new ChatAppService(Domain.Object, Push.Object, Guard.Object, CurrentUser.Object);
        }

        /// <summary>被测命令应用服务。</summary>
        public ChatAppService Service { get; }

        /// <summary>领域服务替身。</summary>
        public Mock<IChatDomainService> Domain { get; }

        /// <summary>实时推送服务替身。</summary>
        public Mock<IChatRealtimePushService> Push { get; }

        /// <summary>敏感词守卫替身。</summary>
        public Mock<IChatSensitiveWordGuard> Guard { get; }

        /// <summary>当前用户替身。</summary>
        public Mock<ICurrentUser> CurrentUser { get; }
    }
}
