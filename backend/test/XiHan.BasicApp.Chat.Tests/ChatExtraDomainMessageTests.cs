// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Chat.Domain.DomainServices;
using XiHan.BasicApp.Chat.Domain.Entities;

namespace XiHan.BasicApp.Chat.Tests;

/// <summary>
/// 聊天消息领域不变量测试：各消息类型的载荷校验与预览生成、@ 名单与回复快照、
/// 编辑与表情回应、消息置顶上限、已读位推进，以及 AI 助手回复的独立写入路径。
/// </summary>
/// <remarks>
/// 消息表是只追加的：一条内容不合规的消息落库后没有业务删除入口，只能等保留期清理。
/// 因此每种消息类型的载荷校验都必须在写入前把关，本文件对每条校验分支同时断言异常提示与
/// 「没有落下任何消息行」。
/// </remarks>
public sealed class ChatExtraDomainMessageTests
{
    /// <summary>
    /// 文本消息正文为空、纯空白或 null 必须被拒绝，且不得落库。
    /// </summary>
    /// <param name="content">待校验的正文。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public async Task SendMessageAsync_BlankTextShouldReject(string? content)
    {
        var fixture = CreateSingleChat();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, 1, ChatMessageType.Text, content, null, null)));

        Assert.Contains("文本消息内容不能为空", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.AddedMessages);
    }

    /// <summary>
    /// 文本正文长度上限是 4000：正好 4000 放行，4001 拒绝。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_TextLengthShouldBeBoundedAt4000()
    {
        var fixture = CreateSingleChat();

        var result = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, 1, ChatMessageType.Text, new string('字', 4000), null, null));
        Assert.Equal(4000, result.Message.Content!.Length);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, 1, ChatMessageType.Text, new string('字', 4001), null, null)));
        Assert.Contains("不能超过 4000 个字符", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 文本正文两端空白必须被裁掉后落库，预览与正文保持一致。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_TextShouldBeTrimmedBeforePersist()
    {
        var fixture = CreateSingleChat();

        var result = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "  你好  ", null, null));

        Assert.Equal("你好", result.Message.Content);
        Assert.Equal("你好", result.Conversation.LastMessagePreview);
    }

    /// <summary>
    /// 超长文本的会话预览必须截断到 200 字符，不把 4000 字塞进会话列表。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_LongTextPreviewShouldBeTruncatedTo200()
    {
        var fixture = CreateSingleChat();

        var result = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, 1, ChatMessageType.Text, new string('字', 1000), null, null));

        Assert.Equal(1000, result.Message.Content!.Length);
        Assert.Equal(200, result.Conversation.LastMessagePreview!.Length);
    }

    /// <summary>
    /// 语音消息必须且只能带一个有效音频附件：零个、两个、文件主键非法都要拒绝。
    /// </summary>
    /// <param name="attachmentCount">附件数量。</param>
    /// <param name="fileId">首个附件的文件主键。</param>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 1)]
    [InlineData(1, 0)]
    public async Task SendMessageAsync_VoiceAttachmentCardinalityShouldReject(int attachmentCount, long fileId)
    {
        var fixture = CreateSingleChat();
        var attachments = Enumerable.Range(0, attachmentCount)
            .Select(index => new ChatMessageAttachment(index == 0 ? fileId : 2, $"voice{index}.m4a", 100, 10))
            .ToList();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, 1, ChatMessageType.Voice, null, attachments, null)));

        Assert.Contains("语音消息必须且只能关联一个音频文件", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.AddedMessages);
    }

    /// <summary>
    /// 语音消息缺时长或时长非正必须被拒绝。
    /// </summary>
    /// <param name="durationSeconds">附件带的时长秒数；null 表示未给出。</param>
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SendMessageAsync_VoiceWithoutDurationShouldReject(int? durationSeconds)
    {
        var fixture = CreateSingleChat();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(
                    100, 1, ChatMessageType.Voice, null,
                    [new ChatMessageAttachment(9, "voice.m4a", 100, durationSeconds)], null)));

        Assert.Contains("语音消息必须带时长", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 语音时长上限是 60 秒：正好 60 放行，61 拒绝。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_VoiceDurationShouldBeBoundedAt60Seconds()
    {
        var fixture = CreateSingleChat();

        var result = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(
                100, 1, ChatMessageType.Voice, "这段说明应被丢弃",
                [new ChatMessageAttachment(9, "voice.m4a", 100, 60)], null));

        // 语音无正文：说明文字要么没有、要么该走文本消息，留着只会和气泡内播放器抢位置
        Assert.Null(result.Message.Content);
        Assert.Equal("[语音] 60\"", result.Conversation.LastMessagePreview);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(
                    100, 1, ChatMessageType.Voice, null,
                    [new ChatMessageAttachment(9, "voice.m4a", 100, 61)], null)));
        Assert.Contains("不能超过 60 秒", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 图片与文件消息必须关联至少一个有效文件，缺附件或附件主键非法都要拒绝。
    /// </summary>
    /// <param name="messageType">消息类型。</param>
    [Theory]
    [InlineData(ChatMessageType.Image)]
    [InlineData(ChatMessageType.File)]
    public async Task SendMessageAsync_MediaWithoutValidAttachmentShouldReject(ChatMessageType messageType)
    {
        var fixture = CreateSingleChat();

        var empty = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, 1, messageType, "说明", null, null)));
        Assert.Contains("图片/文件消息必须关联文件", empty.Message, StringComparison.Ordinal);

        var invalid = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(
                    100, 1, messageType, "说明", [new ChatMessageAttachment(0, "a.png", 1)], null)));
        Assert.Contains("图片/文件消息必须关联文件", invalid.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.AddedMessages);
    }

    /// <summary>
    /// 图片与文件消息的会话预览必须按数量给出占位文案，单个文件带出文件名。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_MediaPreviewShouldReflectAttachmentCount()
    {
        var fixture = CreateSingleChat();

        var singleImage = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, 1, ChatMessageType.Image, null, [new ChatMessageAttachment(1, "a.png", 1)], null));
        Assert.Equal("[图片]", singleImage.Conversation.LastMessagePreview);

        var multiImage = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(
                100, 1, ChatMessageType.Image, null,
                [new ChatMessageAttachment(1, "a.png", 1), new ChatMessageAttachment(2, "b.png", 1)], null));
        Assert.Equal("[图片] 2张", multiImage.Conversation.LastMessagePreview);

        var singleFile = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, 1, ChatMessageType.File, null, [new ChatMessageAttachment(3, "报表.xlsx", 1)], null));
        Assert.Equal("[文件] 报表.xlsx", singleFile.Conversation.LastMessagePreview);

        var multiFile = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(
                100, 1, ChatMessageType.File, null,
                [new ChatMessageAttachment(3, "a.xlsx", 1), new ChatMessageAttachment(4, "b.xlsx", 1)], null));
        Assert.Equal("[文件] 2个", multiFile.Conversation.LastMessagePreview);
    }

    /// <summary>
    /// 图片/文件消息的正文是可选说明：空白说明必须落成 null 而不是空串。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_MediaBlankCaptionShouldBecomeNull()
    {
        var fixture = CreateSingleChat();

        var result = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, 1, ChatMessageType.Image, "   ", [new ChatMessageAttachment(1, "a.png", 1)], null));

        Assert.Null(result.Message.Content);
        Assert.NotNull(result.Message.Attachments);
    }

    /// <summary>
    /// 系统提示消息只能由服务端生成，客户端不得直接发送。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_SystemTypeShouldReject()
    {
        var fixture = CreateSingleChat();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, 1, ChatMessageType.System, "伪造的系统提示", null, null)));

        Assert.Contains("系统提示消息由服务端生成", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.AddedMessages);
    }

    /// <summary>
    /// 未定义的消息类型必须抛越界异常，不得按默认分支静默落库。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_UndefinedMessageTypeShouldReject()
    {
        var fixture = CreateSingleChat();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, 1, (ChatMessageType)77, "内容", null, null)));
        Assert.Empty(fixture.AddedMessages);
    }

    /// <summary>
    /// 助手回复类型不在客户端可发送的分支内，同样落到未定义分支被拒绝。
    /// </summary>
    /// <remarks>助手回复只能经 AppendAssistantMessageAsync 写入，那条路径不校验成员也不计未读。</remarks>
    [Fact]
    public async Task SendMessageAsync_AssistantTypeShouldNotBeSendableByUser()
    {
        var fixture = CreateSingleChat();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, 1, ChatMessageType.Assistant, "冒充助手", null, null)));
    }

    /// <summary>
    /// @ 名单超过 20 人必须被拒绝。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_TooManyMentionsShouldReject()
    {
        var fixture = CreateSingleChat();
        var mentions = Enumerable.Range(1, 21).Select(id => (long)id).ToList();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "hi", null, null, null, mentions)));

        Assert.Contains("最多 @ 20 人", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 只能 @ 会话成员，@ 到群外的人必须整条消息被拒绝。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_MentionOutsiderShouldReject()
    {
        var fixture = CreateSingleChat();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "hi", null, null, null, [9])));

        Assert.Contains("仅可 @ 会话成员", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.AddedMessages);
    }

    /// <summary>
    /// @ 名单必须去重、剔除非正数，并以逗号串落库；名单为空时落 null。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_MentionsShouldBeDedupedAndJoined()
    {
        var fixture = CreateSingleChat();

        var mentioned = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "hi", null, null, null, [2, 2, 0, -1, 1]));
        Assert.Equal("2,1", mentioned.Message.MentionedUserIds);

        var none = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "hi", null, null, null, []));
        Assert.Null(none.Message.MentionedUserIds);
    }

    /// <summary>
    /// 回复消息必须生成「发送人: 内容」快照；被回复消息不存在、跨会话或已撤回一律拒绝。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_ReplyPreviewShouldSnapshotOriginalAndRejectIllegalTargets()
    {
        var fixture = CreateSingleChat();
        _ = fixture.AddMessage(200, conversationId: 100, senderUserId: 2, content: "原始内容");

        var replied = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "收到", null, null, 200));
        Assert.Equal(200L, replied.Message.ReplyToMessageId);
        Assert.Equal("李四: 原始内容", replied.Message.ReplyPreview);

        var missing = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "收到", null, null, 999)));
        Assert.Contains("被回复的消息不存在", missing.Message, StringComparison.Ordinal);

        _ = fixture.AddMessage(300, conversationId: 777, senderUserId: 2, content: "别的会话");
        var crossConversation = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "收到", null, null, 300)));
        Assert.Contains("仅可回复本会话内的消息", crossConversation.Message, StringComparison.Ordinal);

        var recalledOriginal = fixture.AddMessage(400, conversationId: 100, senderUserId: 2, content: null);
        recalledOriginal.IsRecalled = true;
        var recalled = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "收到", null, null, 400)));
        Assert.Contains("被回复的消息已撤回", recalled.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 回复目标为 0 或负数视为普通消息，不生成回复快照。
    /// </summary>
    /// <param name="replyToMessageId">被回复消息主键。</param>
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task SendMessageAsync_NonPositiveReplyTargetShouldBeIgnored(long replyToMessageId)
    {
        var fixture = CreateSingleChat();

        var result = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "hi", null, null, replyToMessageId));

        Assert.Null(result.Message.ReplyToMessageId);
        Assert.Null(result.Message.ReplyPreview);
    }

    /// <summary>
    /// 客户端消息ID 必须裁剪空白并截断到 50 字符，否则会超出列长度。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_ClientMessageIdShouldBeTrimmedAndTruncated()
    {
        var fixture = CreateSingleChat();

        var normal = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "hi", null, "  client-1  "));
        Assert.Equal("client-1", normal.Message.ClientMessageId);

        var overlong = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "hi", null, new string('c', 80)));
        Assert.Equal(50, overlong.Message.ClientMessageId!.Length);

        var blank = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "hi", null, "   "));
        Assert.Null(blank.Message.ClientMessageId);
    }

    /// <summary>
    /// 发消息只给除发送者之外的成员加未读，发送者自己的未读不得增加。
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_UnreadShouldSkipSender()
    {
        var fixture = CreateSingleChat();
        var sender = fixture.FindMember(100, 1)!;
        var peer = fixture.FindMember(100, 2)!;

        _ = await fixture.Service.SendMessageAsync(
            new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "hi", null, null));

        Assert.Equal(0, sender.UnreadCount);
        Assert.Equal(1, peer.UnreadCount);
        fixture.MemberRepository.Verify(
            value => value.IncrementUnreadAsync(100, 1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 撤回已撤回的消息必须被拒绝，不得二次改写撤回时间。
    /// </summary>
    [Fact]
    public async Task RecallMessageAsync_AlreadyRecalledShouldReject()
    {
        var fixture = CreateSingleChat();
        var message = fixture.AddMessage(200, 100, senderUserId: 1);
        message.IsRecalled = true;
        message.RecallTime = DateTimeOffset.UtcNow.AddMinutes(-1);
        var originalRecallTime = message.RecallTime;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.RecallMessageAsync(new ChatMessageRecallCommand(200, 1)));

        Assert.Contains("消息已撤回", exception.Message, StringComparison.Ordinal);
        Assert.Equal(originalRecallTime, message.RecallTime);
    }

    /// <summary>
    /// 撤回的不是最后一条消息时，会话预览不得被改成撤回占位。
    /// </summary>
    [Fact]
    public async Task RecallMessageAsync_NonLastMessageShouldKeepConversationPreview()
    {
        var fixture = CreateSingleChat();
        var conversation = fixture.Conversations[100];
        conversation.LastMessageId = 999;
        conversation.LastMessagePreview = "最后一条";
        _ = fixture.AddMessage(200, 100, senderUserId: 1);

        var result = await fixture.Service.RecallMessageAsync(new ChatMessageRecallCommand(200, 1));

        Assert.True(result.Message.IsRecalled);
        Assert.Equal("最后一条", conversation.LastMessagePreview);
    }

    /// <summary>
    /// 撤回不存在的消息必须报「消息不存在」；消息主键非法抛越界异常。
    /// </summary>
    [Fact]
    public async Task RecallMessageAsync_MissingOrInvalidMessageIdShouldReject()
    {
        var fixture = CreateSingleChat();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.RecallMessageAsync(new ChatMessageRecallCommand(0, 1)));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.RecallMessageAsync(new ChatMessageRecallCommand(404, 1)));
        Assert.Contains("消息不存在", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 编辑消息成功时改写正文、置编辑时间，并在其为最后一条时同步刷新会话预览。
    /// </summary>
    [Fact]
    public async Task EditMessageAsync_ShouldRewriteContentAndStampEditedTime()
    {
        var fixture = CreateSingleChat();
        var message = fixture.AddMessage(200, 100, senderUserId: 1, content: "原文");
        fixture.Conversations[100].LastMessageId = 200;

        var result = await fixture.Service.EditMessageAsync(new ChatMessageEditCommand(200, 1, "  改后正文  "));

        Assert.Equal("改后正文", result.Message.Content);
        Assert.NotNull(result.Message.EditedTime);
        Assert.Equal("改后正文", fixture.Conversations[100].LastMessagePreview);
        Assert.Equal([1L, 2L], result.RecipientUserIds.Order().ToArray());
        _ = message;
    }

    /// <summary>
    /// 编辑消息的全部拒绝路径：消息不存在、非本人、已撤回、非文本、超窗口、非成员、被禁言。
    /// </summary>
    [Fact]
    public async Task EditMessageAsync_ShouldRejectEveryIllegalCase()
    {
        var fixture = CreateSingleChat();

        var missing = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EditMessageAsync(new ChatMessageEditCommand(404, 1, "新内容")));
        Assert.Contains("消息不存在", missing.Message, StringComparison.Ordinal);

        _ = fixture.AddMessage(201, 100, senderUserId: 2, content: "别人的");
        var notMine = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EditMessageAsync(new ChatMessageEditCommand(201, 1, "新内容")));
        Assert.Contains("仅可编辑自己发送的消息", notMine.Message, StringComparison.Ordinal);

        var recalled = fixture.AddMessage(202, 100, senderUserId: 1);
        recalled.IsRecalled = true;
        var recalledReject = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EditMessageAsync(new ChatMessageEditCommand(202, 1, "新内容")));
        Assert.Contains("已撤回的消息不能编辑", recalledReject.Message, StringComparison.Ordinal);

        _ = fixture.AddMessage(203, 100, senderUserId: 1, messageType: ChatMessageType.Image, content: null);
        var notText = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EditMessageAsync(new ChatMessageEditCommand(203, 1, "新内容")));
        Assert.Contains("仅文本消息支持编辑", notText.Message, StringComparison.Ordinal);

        _ = fixture.AddMessage(204, 100, senderUserId: 1, createdTime: DateTimeOffset.UtcNow.AddMinutes(-6));
        var expired = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EditMessageAsync(new ChatMessageEditCommand(204, 1, "新内容")));
        Assert.Contains("仅可编辑 5 分钟内发送的消息", expired.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 已不在会话内的人不能编辑历史消息；仍在会话但被禁言同样不能编辑。
    /// </summary>
    [Fact]
    public async Task EditMessageAsync_NonMemberOrSilencedShouldReject()
    {
        var outside = new ChatExtraDomainFixture();
        _ = outside.AddUser(1, "张三");
        _ = outside.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = outside.AddMessage(200, 100, senderUserId: 1, content: "原文");
        var notMember = await Assert.ThrowsAsync<InvalidOperationException>(
            () => outside.Service.EditMessageAsync(new ChatMessageEditCommand(200, 1, "新内容")));
        Assert.Contains("仅会话成员可编辑消息", notMember.Message, StringComparison.Ordinal);

        var fixture = CreateSingleChat();
        fixture.FindMember(100, 1)!.IsSilenced = true;
        var message = fixture.AddMessage(200, 100, senderUserId: 1, content: "原文");
        var silenced = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EditMessageAsync(new ChatMessageEditCommand(200, 1, "新内容")));
        Assert.Contains("你已被禁言，暂时不能编辑消息", silenced.Message, StringComparison.Ordinal);
        Assert.Equal("原文", message.Content);
    }

    /// <summary>
    /// 编辑后的正文同样受 4000 字上限与非空约束。
    /// </summary>
    [Fact]
    public async Task EditMessageAsync_BlankOrOverlongContentShouldReject()
    {
        var fixture = CreateSingleChat();
        _ = fixture.AddMessage(200, 100, senderUserId: 1, content: "原文");

        var blank = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EditMessageAsync(new ChatMessageEditCommand(200, 1, "   ")));
        Assert.Contains("文本消息内容不能为空", blank.Message, StringComparison.Ordinal);

        var overlong = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EditMessageAsync(new ChatMessageEditCommand(200, 1, new string('字', 4001))));
        Assert.Contains("不能超过 4000 个字符", overlong.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 表情回应是 toggle：首次落库为新增，同一 (消息, 用户, 表情) 再来一次即取消。
    /// </summary>
    [Fact]
    public async Task ToggleReactionAsync_ShouldAddThenRemoveSameTriple()
    {
        var fixture = CreateSingleChat();
        _ = fixture.AddMessage(200, 100, senderUserId: 2);

        var added = await fixture.Service.ToggleReactionAsync(new ChatReactionToggleCommand(200, 1, " 👍 "));
        Assert.True(added.Added);
        Assert.Equal("👍", added.Emoji);
        Assert.Equal(200L, added.MessageId);
        Assert.Equal(100L, added.ConversationId);
        Assert.Equal(1L, added.UserId);
        Assert.Equal("张三", added.UserName);
        var stored = Assert.Single(fixture.Reactions);
        Assert.Equal("👍", stored.Emoji);
        Assert.Equal(100L, stored.ConversationId);
        Assert.Equal("张三", stored.UserName);

        var removed = await fixture.Service.ToggleReactionAsync(new ChatReactionToggleCommand(200, 1, "👍"));
        Assert.False(removed.Added);
        Assert.Empty(fixture.Reactions);
    }

    /// <summary>
    /// 表情为空、纯空白或超过 16 个码元一律视为无效回应，且在查消息之前就被拦下。
    /// </summary>
    /// <param name="emoji">待校验的表情。</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678901234567")]
    public async Task ToggleReactionAsync_InvalidEmojiShouldReject(string emoji)
    {
        var fixture = CreateSingleChat();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.ToggleReactionAsync(new ChatReactionToggleCommand(200, 1, emoji)));

        Assert.Contains("表情回应无效", exception.Message, StringComparison.Ordinal);
        fixture.MessageRepository.Verify(
            value => value.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 已撤回的消息不能被回应；非会话成员也不能回应。
    /// </summary>
    [Fact]
    public async Task ToggleReactionAsync_RecalledMessageOrNonMemberShouldReject()
    {
        var fixture = CreateSingleChat();
        var recalled = fixture.AddMessage(200, 100, senderUserId: 2);
        recalled.IsRecalled = true;
        var recalledReject = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.ToggleReactionAsync(new ChatReactionToggleCommand(200, 1, "👍")));
        Assert.Contains("已撤回的消息不能回应", recalledReject.Message, StringComparison.Ordinal);

        _ = fixture.AddUser(9, "路人");
        _ = fixture.AddMessage(201, 100, senderUserId: 2);
        var outsider = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.ToggleReactionAsync(new ChatReactionToggleCommand(201, 9, "👍")));
        Assert.Contains("仅会话成员可回应消息", outsider.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.Reactions);
    }

    /// <summary>
    /// 单聊双方都可以置顶消息，不受群角色约束。
    /// </summary>
    [Fact]
    public async Task SetMessagePinAsync_SingleConversationShouldAllowEitherParty()
    {
        var fixture = CreateSingleChat();
        _ = fixture.AddMessage(200, 100, senderUserId: 1);

        var result = await fixture.Service.SetMessagePinAsync(new ChatMessagePinCommand(200, OperatorUserId: 2, Pin: true));

        Assert.True(result.Message.IsPinned);
        Assert.Equal(2L, result.Message.PinnedByUserId);
        Assert.NotNull(result.Message.PinnedTime);
    }

    /// <summary>
    /// 群聊内只有群主与管理员可以置顶消息，普通成员被拒绝。
    /// </summary>
    [Fact]
    public async Task SetMessagePinAsync_GroupPlainMemberShouldReject()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);
        _ = fixture.AddMember(100, 2);
        var message = fixture.AddMessage(200, 100, senderUserId: 2);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SetMessagePinAsync(new ChatMessagePinCommand(200, 2, true)));

        Assert.Contains("仅群主或管理员可置顶消息", exception.Message, StringComparison.Ordinal);
        Assert.False(message.IsPinned);
    }

    /// <summary>
    /// 已撤回的消息不能置顶；非会话成员不能操作置顶。
    /// </summary>
    [Fact]
    public async Task SetMessagePinAsync_RecalledOrNonMemberShouldReject()
    {
        var fixture = CreateSingleChat();
        var recalled = fixture.AddMessage(200, 100, senderUserId: 1);
        recalled.IsRecalled = true;
        var recalledReject = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SetMessagePinAsync(new ChatMessagePinCommand(200, 1, true)));
        Assert.Contains("已撤回的消息不能置顶", recalledReject.Message, StringComparison.Ordinal);

        _ = fixture.AddMessage(201, 100, senderUserId: 1);
        var outsider = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SetMessagePinAsync(new ChatMessagePinCommand(201, 9, true)));
        Assert.Contains("仅会话成员可操作", outsider.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 每个会话最多置顶 10 条消息，第 11 条必须被拒绝。
    /// </summary>
    [Fact]
    public async Task SetMessagePinAsync_ShouldEnforceTenPinnedMessagesPerConversation()
    {
        var fixture = CreateSingleChat();
        for (var index = 0; index < 10; index++)
        {
            var pinned = fixture.AddMessage(300 + index, 100, senderUserId: 1);
            pinned.IsPinned = true;
        }

        _ = fixture.AddMessage(400, 100, senderUserId: 1);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SetMessagePinAsync(new ChatMessagePinCommand(400, 1, true)));

        Assert.Contains("最多置顶 10 条消息", exception.Message, StringComparison.Ordinal);
        Assert.False(fixture.Messages[400].IsPinned);
    }

    /// <summary>
    /// 取消置顶必须清空 Pin 操作人与时间；对本就未置顶的消息取消是空操作。
    /// </summary>
    [Fact]
    public async Task SetMessagePinAsync_UnpinShouldClearPinMetadataAndStayIdempotent()
    {
        var fixture = CreateSingleChat();
        var message = fixture.AddMessage(200, 100, senderUserId: 1);
        message.IsPinned = true;
        message.PinnedByUserId = 1;
        message.PinnedTime = DateTimeOffset.UtcNow;

        var unpinned = await fixture.Service.SetMessagePinAsync(new ChatMessagePinCommand(200, 1, Pin: false));
        Assert.False(unpinned.Message.IsPinned);
        Assert.Null(unpinned.Message.PinnedByUserId);
        Assert.Null(unpinned.Message.PinnedTime);

        _ = await fixture.Service.SetMessagePinAsync(new ChatMessagePinCommand(200, 1, Pin: false));
        fixture.MessageRepository.Verify(
            value => value.UpdateAsync(message, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 会话置顶与免打扰都是个人维度的 toggle，返回值即新状态。
    /// </summary>
    [Fact]
    public async Task ToggleConversationSettings_ShouldFlipPerMemberFlags()
    {
        var fixture = CreateSingleChat();
        var member = fixture.FindMember(100, 1)!;

        Assert.True(await fixture.Service.TogglePinConversationAsync(new ChatMemberToggleCommand(100, 1)));
        Assert.True(member.IsPinned);
        Assert.False(await fixture.Service.TogglePinConversationAsync(new ChatMemberToggleCommand(100, 1)));
        Assert.False(member.IsPinned);

        Assert.True(await fixture.Service.ToggleMuteConversationAsync(new ChatMemberToggleCommand(100, 1)));
        Assert.True(member.IsMuted);
        Assert.False(await fixture.Service.ToggleMuteConversationAsync(new ChatMemberToggleCommand(100, 1)));
        Assert.False(member.IsMuted);
    }

    /// <summary>
    /// 非会话成员不得设置会话置顶或免打扰，两条路径给出各自的提示。
    /// </summary>
    [Fact]
    public async Task ToggleConversationSettings_NonMemberShouldRejectWithSpecificMessage()
    {
        var fixture = CreateSingleChat();

        var pin = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.TogglePinConversationAsync(new ChatMemberToggleCommand(100, 9)));
        Assert.Contains("仅会话成员可置顶会话", pin.Message, StringComparison.Ordinal);

        var mute = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.ToggleMuteConversationAsync(new ChatMemberToggleCommand(100, 9)));
        Assert.Contains("仅会话成员可设置免打扰", mute.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 不带已读位标记已读时，已读位取会话最后一条消息（语义为「读到最新」），并清零未读。
    /// </summary>
    [Fact]
    public async Task MarkConversationReadAsync_NullPositionShouldAdvanceToConversationTail()
    {
        var fixture = CreateSingleChat();
        var member = fixture.FindMember(100, 1)!;
        member.UnreadCount = 7;
        fixture.Conversations[100].LastMessageId = 500;

        var result = await fixture.Service.MarkConversationReadAsync(new ChatMarkReadCommand(100, 1, null));

        Assert.Equal(0, member.UnreadCount);
        Assert.NotNull(member.LastReadTime);
        Assert.Equal(500L, member.LastReadMessageId);
        Assert.Equal(500L, result.LastReadMessageId);
        Assert.Equal([1L, 2L], result.RecipientUserIds.Order().ToArray());
    }

    /// <summary>
    /// 显式已读位优先于会话尾部，且已读位只进不退。
    /// </summary>
    /// <remarks>多端并发标记已读时，落后的那一端不得把已读位拖回去。</remarks>
    [Fact]
    public async Task MarkConversationReadAsync_ReadPositionShouldNeverRegress()
    {
        var fixture = CreateSingleChat();
        var member = fixture.FindMember(100, 1)!;
        fixture.Conversations[100].LastMessageId = 500;

        _ = await fixture.Service.MarkConversationReadAsync(new ChatMarkReadCommand(100, 1, 400));
        Assert.Equal(400L, member.LastReadMessageId);

        _ = await fixture.Service.MarkConversationReadAsync(new ChatMarkReadCommand(100, 1, 300));
        Assert.Equal(400L, member.LastReadMessageId);

        _ = await fixture.Service.MarkConversationReadAsync(new ChatMarkReadCommand(100, 1, 450));
        Assert.Equal(450L, member.LastReadMessageId);
    }

    /// <summary>
    /// 会话还没有任何消息时，标记已读只清未读、不写出已读位。
    /// </summary>
    [Fact]
    public async Task MarkConversationReadAsync_EmptyConversationShouldLeaveReadPositionNull()
    {
        var fixture = CreateSingleChat();
        var member = fixture.FindMember(100, 1)!;
        member.UnreadCount = 3;

        var result = await fixture.Service.MarkConversationReadAsync(new ChatMarkReadCommand(100, 1, null));

        Assert.Equal(0, member.UnreadCount);
        Assert.Null(result.LastReadMessageId);
    }

    /// <summary>
    /// 助手回复走独立写入路径：不校验成员、不加未读，消息类型为助手回复且发送人是助手主键。
    /// </summary>
    [Fact]
    public async Task AppendAssistantMessageAsync_ShouldPersistWithoutUnreadFanOut()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, ChatConversationType.Assistant, pairKey: "ai:9:1");
        _ = fixture.AddMember(100, 1);

        var result = await fixture.Service.AppendAssistantMessageAsync(
            new ChatAssistantMessageCommand(100, AssistantId: 9, AssistantName: "小曦", Content: "这是回复"));

        Assert.Equal(ChatMessageType.Assistant, result.Message.MessageType);
        Assert.Equal(9L, result.Message.SenderUserId);
        Assert.Equal("小曦", result.Message.SenderUserName);
        Assert.Equal("这是回复", result.Message.Content);
        Assert.Equal("这是回复", result.Conversation.LastMessagePreview);
        Assert.Equal(result.Message.BasicId, result.Conversation.LastMessageId);
        Assert.Equal([1L], result.RecipientUserIds.ToArray());
        fixture.MemberRepository.Verify(
            value => value.IncrementUnreadAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 助手回复超长时截断而不是整条丢弃：模型已经生成完，丢掉整条回复代价更大。
    /// </summary>
    [Fact]
    public async Task AppendAssistantMessageAsync_OverlongContentShouldBeTruncatedNotRejected()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, ChatConversationType.Assistant);
        _ = fixture.AddMember(100, 1);

        var result = await fixture.Service.AppendAssistantMessageAsync(
            new ChatAssistantMessageCommand(100, 9, "小曦", new string('答', 25000)));

        Assert.Equal(20000, result.Message.Content!.Length);
        Assert.Equal(200, result.Conversation.LastMessagePreview!.Length);
    }

    /// <summary>
    /// 助手回复只能追加到助手会话，普通会话必须被拒绝；助手主键与内容同样受校验。
    /// </summary>
    [Fact]
    public async Task AppendAssistantMessageAsync_ShouldRejectIllegalTargetAndPayload()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = fixture.AddConversation(200, ChatConversationType.Assistant);

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.AppendAssistantMessageAsync(new ChatAssistantMessageCommand(200, 0, "小曦", "回复")));

        var notAssistant = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.AppendAssistantMessageAsync(new ChatAssistantMessageCommand(100, 9, "小曦", "回复")));
        Assert.Contains("仅助手会话可追加助手回复", notAssistant.Message, StringComparison.Ordinal);

        var blank = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.AppendAssistantMessageAsync(new ChatAssistantMessageCommand(200, 9, "小曦", "   ")));
        Assert.Contains("助手回复内容不能为空", blank.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.AddedMessages);
    }

    /// <summary>
    /// 助手会话名称为空或超长必须被拒绝，助手头像空白时落 null。
    /// </summary>
    [Fact]
    public async Task GetOrCreateAssistantConversationAsync_ShouldValidateNameAndNormalizeAvatar()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "张三");

        var blank = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.GetOrCreateAssistantConversationAsync(
                new ChatAssistantConversationCommand(1, 9, "  ", null)));
        Assert.Contains("助手名称不能为空", blank.Message, StringComparison.Ordinal);

        var result = await fixture.Service.GetOrCreateAssistantConversationAsync(
            new ChatAssistantConversationCommand(1, 9, "  小曦  ", "   "));
        Assert.Equal("小曦", result.Conversation.ConversationName);
        Assert.Null(result.Conversation.Avatar);
    }

    /// <summary>
    /// 助手会话与用户会话的主键都必须为正数，否则抛越界异常。
    /// </summary>
    [Fact]
    public async Task GetOrCreateAssistantConversationAsync_NonPositiveIdsShouldReject()
    {
        var fixture = new ChatExtraDomainFixture();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.GetOrCreateAssistantConversationAsync(
                new ChatAssistantConversationCommand(0, 9, "小曦", null)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.GetOrCreateAssistantConversationAsync(
                new ChatAssistantConversationCommand(1, 0, "小曦", null)));
    }

    /// <summary>
    /// 全部消息类命令对 null 命令对象都必须抛参数空异常。
    /// </summary>
    [Fact]
    public async Task MessageCommands_NullCommandShouldThrow()
    {
        var fixture = CreateSingleChat();

        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.SendMessageAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.RecallMessageAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.EditMessageAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.ToggleReactionAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.SetMessagePinAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.TogglePinConversationAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.ToggleMuteConversationAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.MarkConversationReadAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.AppendAssistantMessageAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.GetOrCreateAssistantConversationAsync(null!));
    }

    /// <summary>
    /// 已取消的令牌必须在写入消息之前抛出取消异常。
    /// </summary>
    [Fact]
    public async Task MessageCommands_CancelledTokenShouldThrowBeforeAnyWrite()
    {
        var fixture = CreateSingleChat();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.SendMessageAsync(
                new ChatMessageSendCommand(100, 1, ChatMessageType.Text, "hi", null, null), source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.MarkConversationReadAsync(new ChatMarkReadCommand(100, 1, null), source.Token));

        Assert.Empty(fixture.AddedMessages);
    }

    /// <summary>
    /// 构造一个双人单聊夹具：会话 100，成员为张三(1) 与 李四(2)。
    /// </summary>
    /// <returns>已就绪的领域测试夹具。</returns>
    private static ChatExtraDomainFixture CreateSingleChat()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "张三");
        _ = fixture.AddUser(2, "李四");
        _ = fixture.AddConversation(100, ChatConversationType.Single, pairKey: "1_2");
        _ = fixture.AddMember(100, 1);
        _ = fixture.AddMember(100, 2);
        return fixture;
    }
}
