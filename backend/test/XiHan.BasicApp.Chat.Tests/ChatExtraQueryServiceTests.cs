// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using System.Linq.Expressions;
using System.Reflection;
using XiHan.BasicApp.Chat.Application.Dtos;
using XiHan.BasicApp.Chat.Application.QueryServices;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Domain.Repositories;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Chat.Tests;

/// <summary>
/// 聊天查询应用服务测试：会话列表的对端解析与排序、历史与搜索的分页边界、
/// 成员与已读位投影、部门树的作用域收窄，以及审计查询的入参校验。
/// </summary>
/// <remarks>
/// 聊天查询的数据边界不是数据范围过滤，而是「当前用户是会话成员」这一条：
/// 任何一个查询端点漏掉这道校验，都等于把别人的私聊历史开放给了任意持 chat:read 的账号。
/// 本文件对每个查询端点都断言这道校验，并覆盖分页取数的边界值。
/// </remarks>
public sealed class ChatExtraQueryServiceTests
{
    private const long CurrentUserId = 1;

    /// <summary>
    /// 当前用户一个会话都没有时必须直接返回空列表，不再去查会话表。
    /// </summary>
    [Fact]
    public async Task GetMyConversationsAsync_WithoutMembershipShouldShortCircuit()
    {
        var context = new QueryServiceContext();

        var items = await context.Service.GetMyConversationsAsync();

        Assert.Empty(items);
        context.ConversationRepository.Verify(
            value => value.GetListAsync(It.IsAny<Expression<Func<SysChatConversation, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 单聊的展示名与头像取对端用户，群聊取会话自身字段；对端解析不到时回落为「未知用户」。
    /// </summary>
    [Fact]
    public async Task GetMyConversationsAsync_ShouldResolveDisplayNameByConversationType()
    {
        var context = new QueryServiceContext();
        context.AddUser(2, "李四", avatar: "peer.png");
        var single = context.AddConversation(100, ChatConversationType.Single);
        var group = context.AddConversation(200, ChatConversationType.Group);
        group.ConversationName = "项目群";
        group.Avatar = "group.png";
        var orphanSingle = context.AddConversation(300, ChatConversationType.Single);
        context.AddMember(100, CurrentUserId);
        context.AddMember(100, 2);
        context.AddMember(200, CurrentUserId);
        context.AddMember(300, CurrentUserId);
        context.AddMember(300, 999);

        var items = await context.Service.GetMyConversationsAsync();
        var byId = items.ToDictionary(item => item.ConversationId);

        Assert.Equal("李四", byId[100].DisplayName);
        Assert.Equal("peer.png", byId[100].Avatar);
        Assert.Equal(2L, byId[100].PeerUserId);
        Assert.Equal("项目群", byId[200].DisplayName);
        Assert.Equal("group.png", byId[200].Avatar);
        Assert.Null(byId[200].PeerUserId);
        Assert.Equal("未知用户", byId[300].DisplayName);
        _ = single;
        _ = orphanSingle;
    }

    /// <summary>
    /// 会话列表排序口径：置顶优先，其次最后消息时间倒序，最后按会话主键倒序兜底。
    /// </summary>
    [Fact]
    public async Task GetMyConversationsAsync_ShouldOrderByPinnedThenLastMessageTime()
    {
        var context = new QueryServiceContext();
        var now = DateTimeOffset.UtcNow;
        context.AddConversation(100, ChatConversationType.Group).LastMessageTime = now.AddMinutes(-1);
        context.AddConversation(200, ChatConversationType.Group).LastMessageTime = now;
        context.AddConversation(300, ChatConversationType.Group).LastMessageTime = null;
        context.AddConversation(400, ChatConversationType.Group).LastMessageTime = null;
        context.AddMember(100, CurrentUserId).IsPinned = false;
        context.AddMember(200, CurrentUserId).IsPinned = false;
        context.AddMember(300, CurrentUserId).IsPinned = true;
        context.AddMember(400, CurrentUserId).IsPinned = false;

        var items = await context.Service.GetMyConversationsAsync();

        Assert.Equal([300L, 200L, 100L, 400L], items.Select(item => item.ConversationId).ToArray());
    }

    /// <summary>
    /// 成员行指向的会话已不存在时必须跳过该行，而不是抛异常让整张列表打不开。
    /// </summary>
    [Fact]
    public async Task GetMyConversationsAsync_ShouldSkipDanglingMembership()
    {
        var context = new QueryServiceContext();
        _ = context.AddConversation(100, ChatConversationType.Group);
        context.AddMember(100, CurrentUserId);
        context.AddMember(777, CurrentUserId);

        var items = await context.Service.GetMyConversationsAsync();

        Assert.Equal(100L, Assert.Single(items).ConversationId);
    }

    /// <summary>
    /// 会话列表必须带出个人维度状态（未读、免打扰、禁言、角色）与会话冗余字段。
    /// </summary>
    [Fact]
    public async Task GetMyConversationsAsync_ShouldCarryPerMemberStateAndConversationRedundancy()
    {
        var context = new QueryServiceContext();
        var conversation = context.AddConversation(100, ChatConversationType.Group);
        conversation.ConversationName = "项目群";
        conversation.MemberCount = 9;
        conversation.Announcement = "公告";
        conversation.Description = "描述";
        conversation.LastMessagePreview = "最后一条";
        var member = context.AddMember(100, CurrentUserId, ChatMemberRole.Admin);
        member.UnreadCount = 5;
        member.IsMuted = true;
        member.IsSilenced = true;

        var item = Assert.Single(await context.Service.GetMyConversationsAsync());

        Assert.Equal(9, item.MemberCount);
        Assert.Equal(ChatMemberRole.Admin, item.MemberRole);
        Assert.Equal(5, item.UnreadCount);
        Assert.True(item.IsMuted);
        Assert.True(item.IsSilenced);
        Assert.Equal("公告", item.Announcement);
        Assert.Equal("描述", item.Description);
        Assert.Equal("最后一条", item.LastMessagePreview);
    }

    /// <summary>
    /// 每个查询端点都必须先校验「当前用户是会话成员」，非成员一律拒绝。
    /// </summary>
    [Fact]
    public async Task QueryEndpoints_NonMemberShouldBeRejected()
    {
        var context = new QueryServiceContext();
        _ = context.AddConversation(100, ChatConversationType.Group);

        var history = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.GetMessageHistoryAsync(new ChatMessageHistoryQueryDto { ConversationId = 100 }));
        Assert.Contains("仅会话成员可查看", history.Message, StringComparison.Ordinal);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.GetMessageSearchAsync(new ChatMessageSearchQueryDto { ConversationId = 100, Keyword = "x" }));
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Service.GetMembersAsync(100));
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Service.GetReadPositionsAsync(100));
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Service.GetPinnedMessagesAsync(100));

        context.MessageRepository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 会话主键非法必须抛越界异常，不得当作"查不到"静默返回空。
    /// </summary>
    /// <param name="conversationId">非法的会话主键。</param>
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task QueryEndpoints_NonPositiveConversationIdShouldThrow(long conversationId)
    {
        var context = new QueryServiceContext();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => context.Service.GetMembersAsync(conversationId));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.GetMessageHistoryAsync(new ChatMessageHistoryQueryDto { ConversationId = conversationId }));
    }

    /// <summary>
    /// 未登录时全部查询端点都必须失败，绝不按用户 0 去解析会话成员身份。
    /// </summary>
    [Fact]
    public async Task QueryEndpoints_WithoutAuthenticatedUserShouldThrow()
    {
        var context = new QueryServiceContext(currentUserId: null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Service.GetMyConversationsAsync());
        Assert.Contains("当前用户未登录", exception.Message, StringComparison.Ordinal);
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Service.GetMembersAsync(100));
    }

    /// <summary>
    /// 历史每页条数被夹在 1..100：超上限按 100 取、非正数按 1 取，且都要多取一条判断是否还有更早历史。
    /// </summary>
    /// <param name="requestedTake">入参给出的每页条数。</param>
    /// <param name="expectedRepositoryTake">仓储实际应收到的取数（含多取的一条）。</param>
    [Theory]
    [InlineData(1000, 101)]
    [InlineData(100, 101)]
    [InlineData(0, 2)]
    [InlineData(-5, 2)]
    [InlineData(20, 21)]
    public async Task GetMessageHistoryAsync_TakeShouldBeClampedBetweenOneAndHundred(int requestedTake, int expectedRepositoryTake)
    {
        var context = new QueryServiceContext();
        _ = context.AddConversation(100, ChatConversationType.Group);
        context.AddMember(100, CurrentUserId);

        _ = await context.Service.GetMessageHistoryAsync(
            new ChatMessageHistoryQueryDto { ConversationId = 100, Take = requestedTake });

        context.MessageRepository.Verify(
            value => value.GetHistoryAsync(100, null, expectedRepositoryTake, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 历史游标分页：多取的一条只用于判断 HasMore，不进结果；返回项按消息主键正序便于直接渲染。
    /// </summary>
    [Fact]
    public async Task GetMessageHistoryAsync_ShouldTrimProbeRowAndReturnAscendingItems()
    {
        var context = new QueryServiceContext();
        _ = context.AddConversation(100, ChatConversationType.Group);
        context.AddMember(100, CurrentUserId);
        context.MessageRepository
            .Setup(value => value.GetHistoryAsync(100, It.IsAny<long?>(), 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                context.CreateMessage(400, 100),
                context.CreateMessage(300, 100),
                context.CreateMessage(200, 100),
                context.CreateMessage(100, 100)
            ]);

        var result = await context.Service.GetMessageHistoryAsync(
            new ChatMessageHistoryQueryDto { ConversationId = 100, Take = 3 });

        Assert.True(result.HasMore);
        Assert.Equal([200L, 300L, 400L], result.Items.Select(item => item.MessageId).ToArray());
    }

    /// <summary>
    /// 取回的条数没超过每页上限时 HasMore 必须为假。
    /// </summary>
    [Fact]
    public async Task GetMessageHistoryAsync_WithoutProbeRowShouldReportNoMore()
    {
        var context = new QueryServiceContext();
        _ = context.AddConversation(100, ChatConversationType.Group);
        context.AddMember(100, CurrentUserId);
        context.MessageRepository
            .Setup(value => value.GetHistoryAsync(100, It.IsAny<long?>(), 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync([context.CreateMessage(200, 100)]);

        var result = await context.Service.GetMessageHistoryAsync(
            new ChatMessageHistoryQueryDto { ConversationId = 100, Take = 3 });

        Assert.False(result.HasMore);
        Assert.Equal(200L, Assert.Single(result.Items).MessageId);
    }

    /// <summary>
    /// 历史结果必须把表情回应按消息主键聚合带出，一次查询解决整页。
    /// </summary>
    [Fact]
    public async Task GetMessageHistoryAsync_ShouldAttachReactionsByMessage()
    {
        var context = new QueryServiceContext();
        _ = context.AddConversation(100, ChatConversationType.Group);
        context.AddMember(100, CurrentUserId);
        context.MessageRepository
            .Setup(value => value.GetHistoryAsync(100, It.IsAny<long?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([context.CreateMessage(200, 100), context.CreateMessage(201, 100)]);
        context.ReactionRepository
            .Setup(value => value.GetByMessageIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new SysChatMessageReaction { ConversationId = 100, MessageId = 200, UserId = 2, UserName = "李四", Emoji = "👍" }
            ]);

        var result = await context.Service.GetMessageHistoryAsync(
            new ChatMessageHistoryQueryDto { ConversationId = 100, Take = 20 });

        var withReaction = result.Items.Single(item => item.MessageId == 200);
        var withoutReaction = result.Items.Single(item => item.MessageId == 201);
        Assert.Equal("👍", Assert.Single(withReaction.Reactions).Emoji);
        Assert.Empty(withoutReaction.Reactions);
    }

    /// <summary>
    /// 定位模式优先于游标：以目标消息为中心，向前多取一条用于判断是否还有更早历史。
    /// </summary>
    [Fact]
    public async Task GetMessageHistoryAsync_AroundModeShouldTakeHalfPageOnEachSide()
    {
        var context = new QueryServiceContext();
        _ = context.AddConversation(100, ChatConversationType.Group);
        context.AddMember(100, CurrentUserId);
        context.MessageRepository
            .Setup(value => value.GetAroundAsync(100, 300, 11, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([context.CreateMessage(300, 100)]);

        var result = await context.Service.GetMessageHistoryAsync(
            new ChatMessageHistoryQueryDto { ConversationId = 100, BeforeMessageId = 999, AroundMessageId = 300, Take = 20 });

        Assert.False(result.HasMore);
        Assert.Equal(300L, Assert.Single(result.Items).MessageId);
        context.MessageRepository.Verify(
            value => value.GetHistoryAsync(It.IsAny<long>(), It.IsAny<long?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 定位模式下前向命中超过半页时，必须裁掉多余的更早消息并报告还有更多。
    /// </summary>
    [Fact]
    public async Task GetMessageHistoryAsync_AroundModeShouldTrimExtraEarlierRows()
    {
        var context = new QueryServiceContext();
        _ = context.AddConversation(100, ChatConversationType.Group);
        context.AddMember(100, CurrentUserId);
        context.MessageRepository
            .Setup(value => value.GetAroundAsync(100, 300, 2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                context.CreateMessage(100, 100),
                context.CreateMessage(200, 100),
                context.CreateMessage(300, 100)
            ]);

        var result = await context.Service.GetMessageHistoryAsync(
            new ChatMessageHistoryQueryDto { ConversationId = 100, AroundMessageId = 300, Take = 2 });

        Assert.True(result.HasMore);
        Assert.Equal([200L, 300L], result.Items.Select(item => item.MessageId).ToArray());
    }

    /// <summary>
    /// 搜索关键字为空或纯空白时直接返回空结果，不打扰数据库。
    /// </summary>
    /// <param name="keyword">搜索关键字。</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetMessageSearchAsync_BlankKeywordShouldReturnEmptyWithoutQuery(string? keyword)
    {
        var context = new QueryServiceContext();
        _ = context.AddConversation(100, ChatConversationType.Group);
        context.AddMember(100, CurrentUserId);

        var result = await context.Service.GetMessageSearchAsync(
            new ChatMessageSearchQueryDto { ConversationId = 100, Keyword = keyword! });

        Assert.Empty(result.Items);
        Assert.False(result.HasMore);
        context.MessageRepository.Verify(
            value => value.SearchAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 搜索关键字必须去掉两端空白后下推，每页条数夹在 1..50 且多取一条判断是否还有更多。
    /// </summary>
    [Fact]
    public async Task GetMessageSearchAsync_ShouldTrimKeywordAndClampTakeAtFifty()
    {
        var context = new QueryServiceContext();
        _ = context.AddConversation(100, ChatConversationType.Group);
        context.AddMember(100, CurrentUserId);
        context.MessageRepository
            .Setup(value => value.SearchAsync(100, "发版", It.IsAny<long?>(), 51, It.IsAny<CancellationToken>()))
            .ReturnsAsync([context.CreateMessage(300, 100), context.CreateMessage(200, 100)]);

        var result = await context.Service.GetMessageSearchAsync(
            new ChatMessageSearchQueryDto { ConversationId = 100, Keyword = "  发版  ", Take = 999 });

        Assert.False(result.HasMore);
        Assert.Equal([300L, 200L], result.Items.Select(item => item.MessageId).ToArray());
    }

    /// <summary>
    /// 成员列表按「群主 → 管理员 → 成员」再按入群时间排序，用户名由用户表批量解析。
    /// </summary>
    [Fact]
    public async Task GetMembersAsync_ShouldOrderByRoleThenJoinTimeAndResolveUserNames()
    {
        var context = new QueryServiceContext();
        _ = context.AddConversation(100, ChatConversationType.Group);
        context.AddUser(1, "我");
        context.AddUser(2, "李四");
        context.AddUser(3, "王五");
        var now = DateTimeOffset.UtcNow;
        context.AddMember(100, 3, ChatMemberRole.Member).JoinTime = now.AddDays(-1);
        context.AddMember(100, 2, ChatMemberRole.Admin).JoinTime = now.AddDays(-2);
        context.AddMember(100, CurrentUserId, ChatMemberRole.Owner).JoinTime = now.AddDays(-3);
        context.AddMember(100, 9, ChatMemberRole.Member).JoinTime = now;

        var members = await context.Service.GetMembersAsync(100);

        Assert.Equal([1L, 2L, 3L, 9L], members.Select(member => member.UserId).ToArray());
        Assert.Equal("王五", members[2].UserName);
        Assert.Null(members[3].UserName);
    }

    /// <summary>
    /// 已读位列表逐成员投影用户主键与最后已读消息，供群已读回执渲染。
    /// </summary>
    [Fact]
    public async Task GetReadPositionsAsync_ShouldProjectEveryMemberPosition()
    {
        var context = new QueryServiceContext();
        _ = context.AddConversation(100, ChatConversationType.Group);
        context.AddMember(100, CurrentUserId).LastReadMessageId = 500;
        context.AddMember(100, 2);

        var positions = await context.Service.GetReadPositionsAsync(100);

        Assert.Equal(2, positions.Count);
        Assert.Equal(500L, positions.Single(item => item.UserId == CurrentUserId).LastReadMessageId);
        Assert.Null(positions.Single(item => item.UserId == 2).LastReadMessageId);
    }

    /// <summary>
    /// 置顶消息列表原样投影仓储给出的顺序（按 Pin 时间倒序由仓储保证）。
    /// </summary>
    [Fact]
    public async Task GetPinnedMessagesAsync_ShouldProjectRepositoryOrder()
    {
        var context = new QueryServiceContext();
        _ = context.AddConversation(100, ChatConversationType.Group);
        context.AddMember(100, CurrentUserId);
        context.MessageRepository
            .Setup(value => value.GetPinnedAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([context.CreateMessage(300, 100), context.CreateMessage(200, 100)]);

        var pinned = await context.Service.GetPinnedMessagesAsync(100);

        Assert.Equal([300L, 200L], pinned.Select(item => item.MessageId).ToArray());
    }

    /// <summary>
    /// 部门树只列出当前作用域内启用的部门，并按排序值组装父子层级。
    /// </summary>
    /// <remarks>
    /// 不复用通用部门树端点：那条按「读共享」口径会在平台态列出全部租户的部门，
    /// 选中即建出跨作用域会话（写入期会被拒），候选人必须与聊天的严格隔离口径一致。
    /// </remarks>
    [Fact]
    public async Task GetDepartmentTreeAsync_ShouldScopeByTenantAndNestChildren()
    {
        var context = new QueryServiceContext { TenantId = 7 };
        context.AddDepartment(1, "总部", tenantId: 7, parentId: null, sort: 2);
        context.AddDepartment(2, "研发部", tenantId: 7, parentId: 1, sort: 1);
        context.AddDepartment(3, "测试部", tenantId: 7, parentId: 1, sort: 0);
        context.AddDepartment(4, "分公司", tenantId: 7, parentId: null, sort: 1);
        context.AddDepartment(5, "别的租户部门", tenantId: 8, parentId: null, sort: 0);
        context.AddDepartment(6, "停用部门", tenantId: 7, parentId: null, sort: 0, status: EnableStatus.Disabled);

        var tree = await context.Service.GetDepartmentTreeAsync();

        Assert.Equal([4L, 1L], tree.Select(node => node.BasicId).ToArray());
        Assert.Empty(tree[0].Children);
        Assert.Equal([3L, 2L], tree[1].Children.Select(node => node.BasicId).ToArray());
    }

    /// <summary>
    /// 平台作用域（无租户上下文）按租户 0 收窄部门候选。
    /// </summary>
    [Fact]
    public async Task GetDepartmentTreeAsync_PlatformScopeShouldUseTenantZero()
    {
        var context = new QueryServiceContext { TenantId = null };
        context.AddDepartment(1, "平台部门", tenantId: 0, parentId: null, sort: 0);
        context.AddDepartment(2, "租户部门", tenantId: 7, parentId: null, sort: 0);

        var tree = await context.Service.GetDepartmentTreeAsync();

        Assert.Equal(1L, Assert.Single(tree).BasicId);
    }

    /// <summary>
    /// 全部带入参的查询端点对 null 入参都必须抛参数空异常。
    /// </summary>
    [Fact]
    public async Task QueryEndpoints_NullInputShouldThrow()
    {
        var context = new QueryServiceContext();

        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.GetMessageHistoryAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.GetMessageSearchAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Service.GetUserOptionsAsync(null!));
    }

    /// <summary>
    /// 已取消的令牌必须在查询下推之前抛出取消异常。
    /// </summary>
    [Fact]
    public async Task QueryEndpoints_CancelledTokenShouldThrow()
    {
        var context = new QueryServiceContext();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Service.GetMyConversationsAsync(source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Service.GetMembersAsync(100, source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.GetMessageHistoryAsync(new ChatMessageHistoryQueryDto { ConversationId = 100 }, source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Service.GetDepartmentTreeAsync(source.Token));
    }

    /// <summary>
    /// 审计查询的时间区间必须自洽：开始时间晚于结束时间时直接抛越界异常，不下推到数据库。
    /// </summary>
    [Fact]
    public async Task GetChatMessagePageAsync_InvertedTimeRangeShouldThrow()
    {
        var service = new ChatAuditQueryService(new Mock<ISqlSugarClientResolver>(MockBehavior.Strict).Object);
        var now = DateTimeOffset.UtcNow;

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetChatMessagePageAsync(new ChatAuditPageQueryDto
            {
                CreatedTimeStart = now,
                CreatedTimeEnd = now.AddDays(-1)
            }));
    }

    /// <summary>
    /// 只给了半边时间区间时不触发区间校验（由数据库侧单边过滤），但仍不得越过 null 与取消校验。
    /// </summary>
    [Fact]
    public async Task GetChatMessagePageAsync_NullInputAndCancellationShouldThrowBeforeDatabaseAccess()
    {
        var resolver = new Mock<ISqlSugarClientResolver>(MockBehavior.Strict);
        var service = new ChatAuditQueryService(resolver.Object);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => service.GetChatMessagePageAsync(null!));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetChatMessagePageAsync(new ChatAuditPageQueryDto(), source.Token));

        resolver.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 审计列表的文件名摘要：多附件以中文逗号连接，空白名被剔除，无有效附件返回 null。
    /// </summary>
    /// <param name="attachmentsJson">消息行上的附件 JSON。</param>
    /// <param name="expected">期望的文件名摘要。</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData("[]", null)]
    [InlineData("坏数据", null)]
    [InlineData("[{\"fileId\":1,\"fileName\":\"a.png\"}]", "a.png")]
    [InlineData("[{\"fileId\":1,\"fileName\":\"a.png\"},{\"fileId\":2,\"fileName\":\"b.pdf\"}]", "a.png，b.pdf")]
    [InlineData("[{\"fileId\":1,\"fileName\":\"  \"}]", null)]
    public void BuildAttachmentSummary_ShouldJoinValidFileNames(string? attachmentsJson, string? expected)
    {
        var method = typeof(ChatAuditQueryService).GetMethod(
            "BuildAttachmentSummary",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("未找到附件摘要方法。");

        var summary = (string?)method.Invoke(null, [attachmentsJson]);

        Assert.Equal(expected, summary, StringComparer.Ordinal);
    }

    /// <summary>
    /// 聊天查询应用服务及其全部协作者替身，仓储读写同一批内存集合。
    /// </summary>
    private sealed class QueryServiceContext
    {
        private long _nextMemberId = 3000;

        /// <summary>
        /// 组装查询服务与替身。
        /// </summary>
        /// <param name="currentUserId">当前登录用户主键；null 表示未登录。</param>
        public QueryServiceContext(long? currentUserId = CurrentUserId)
        {
            ConversationRepository = new Mock<IChatConversationRepository>();
            MemberRepository = new Mock<IChatConversationMemberRepository>();
            MessageRepository = new Mock<IChatMessageRepository>();
            ReactionRepository = new Mock<IChatMessageReactionRepository>();
            UserRepository = new Mock<IUserRepository>();
            SuperAdminProtector = new Mock<ISuperAdminProtector>();
            DepartmentRepository = new Mock<IDepartmentRepository>();
            TenantUserRepository = new Mock<ITenantUserRepository>();
            CurrentTenant = new Mock<ICurrentTenant>();
            var currentUser = new Mock<ICurrentUser>();
            currentUser.SetupGet(value => value.UserId).Returns(currentUserId);
            CurrentTenant.SetupGet(value => value.Id).Returns(() => TenantId);

            ConversationRepository
                .Setup(value => value.GetListAsync(It.IsAny<Expression<Func<SysChatConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysChatConversation, bool>> predicate, CancellationToken _) =>
                    Conversations.Values.Where(predicate.Compile()).ToList());
            MemberRepository
                .Setup(value => value.GetByUserIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long userId, CancellationToken _) => Members.Where(member => member.UserId == userId).ToList());
            MemberRepository
                .Setup(value => value.GetByConversationIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long conversationId, CancellationToken _) =>
                    Members.Where(member => member.ConversationId == conversationId).ToList());
            MemberRepository
                .Setup(value => value.GetMemberAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long conversationId, long userId, CancellationToken _) =>
                    Members.Find(member => member.ConversationId == conversationId && member.UserId == userId));
            MemberRepository
                .Setup(value => value.GetListAsync(It.IsAny<Expression<Func<SysChatConversationMember, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysChatConversationMember, bool>> predicate, CancellationToken _) =>
                    Members.Where(predicate.Compile()).ToList());
            MessageRepository
                .Setup(value => value.GetHistoryAsync(It.IsAny<long>(), It.IsAny<long?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            MessageRepository
                .Setup(value => value.GetPinnedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            ReactionRepository
                .Setup(value => value.GetByMessageIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            UserRepository
                .Setup(value => value.GetListAsync(It.IsAny<Expression<Func<SysUser, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysUser, bool>> predicate, CancellationToken _) =>
                    Users.Values.Where(predicate.Compile()).ToList());
            DepartmentRepository
                .Setup(value => value.GetListAsync(It.IsAny<Expression<Func<SysDepartment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysDepartment, bool>> predicate, CancellationToken _) =>
                    Departments.Values.Where(predicate.Compile()).ToList());

            Service = new ChatQueryService(
                ConversationRepository.Object,
                MemberRepository.Object,
                MessageRepository.Object,
                ReactionRepository.Object,
                UserRepository.Object,
                currentUser.Object,
                SuperAdminProtector.Object,
                DepartmentRepository.Object,
                TenantUserRepository.Object,
                CurrentTenant.Object);
        }

        /// <summary>被测查询应用服务。</summary>
        public ChatQueryService Service { get; }

        /// <summary>会话仓储替身。</summary>
        public Mock<IChatConversationRepository> ConversationRepository { get; }

        /// <summary>会话成员仓储替身。</summary>
        public Mock<IChatConversationMemberRepository> MemberRepository { get; }

        /// <summary>消息仓储替身。</summary>
        public Mock<IChatMessageRepository> MessageRepository { get; }

        /// <summary>表情回应仓储替身。</summary>
        public Mock<IChatMessageReactionRepository> ReactionRepository { get; }

        /// <summary>用户仓储替身。</summary>
        public Mock<IUserRepository> UserRepository { get; }

        /// <summary>超管保护器替身。</summary>
        public Mock<ISuperAdminProtector> SuperAdminProtector { get; }

        /// <summary>部门仓储替身。</summary>
        public Mock<IDepartmentRepository> DepartmentRepository { get; }

        /// <summary>租户成员仓储替身。</summary>
        public Mock<ITenantUserRepository> TenantUserRepository { get; }

        /// <summary>当前租户上下文替身。</summary>
        public Mock<ICurrentTenant> CurrentTenant { get; }

        /// <summary>当前租户主键；null 表示平台作用域。</summary>
        public long? TenantId { get; init; }

        /// <summary>会话表内存镜像。</summary>
        public Dictionary<long, SysChatConversation> Conversations { get; } = [];

        /// <summary>会话成员表内存镜像。</summary>
        public List<SysChatConversationMember> Members { get; } = [];

        /// <summary>用户表内存镜像。</summary>
        public Dictionary<long, SysUser> Users { get; } = [];

        /// <summary>部门表内存镜像。</summary>
        public Dictionary<long, SysDepartment> Departments { get; } = [];

        /// <summary>
        /// 登记一个会话。
        /// </summary>
        /// <param name="id">会话主键。</param>
        /// <param name="conversationType">会话类型。</param>
        /// <returns>会话实体。</returns>
        public SysChatConversation AddConversation(long id, ChatConversationType conversationType)
        {
            var conversation = new SysChatConversation { ConversationType = conversationType };
            ChatExtraDomainFixture.SetEntityId(conversation, id);
            Conversations[id] = conversation;
            return conversation;
        }

        /// <summary>
        /// 登记一条会话成员行。
        /// </summary>
        /// <param name="conversationId">会话主键。</param>
        /// <param name="userId">用户主键。</param>
        /// <param name="role">成员角色。</param>
        /// <returns>成员实体。</returns>
        public SysChatConversationMember AddMember(long conversationId, long userId, ChatMemberRole role = ChatMemberRole.Member)
        {
            var member = new SysChatConversationMember
            {
                ConversationId = conversationId,
                UserId = userId,
                MemberRole = role,
                JoinTime = DateTimeOffset.UtcNow
            };
            ChatExtraDomainFixture.SetEntityId(member, ++_nextMemberId);
            Members.Add(member);
            return member;
        }

        /// <summary>
        /// 登记一个用户。
        /// </summary>
        /// <param name="id">用户主键。</param>
        /// <param name="userName">用户名。</param>
        /// <param name="avatar">头像。</param>
        /// <returns>用户实体。</returns>
        public SysUser AddUser(long id, string userName, string? avatar = null)
        {
            var user = new SysUser { UserName = userName, Avatar = avatar };
            ChatExtraDomainFixture.SetEntityId(user, id);
            Users[id] = user;
            return user;
        }

        /// <summary>
        /// 登记一个部门。
        /// </summary>
        /// <param name="id">部门主键。</param>
        /// <param name="departmentName">部门名称。</param>
        /// <param name="tenantId">归属租户。</param>
        /// <param name="parentId">父部门主键。</param>
        /// <param name="sort">排序值。</param>
        /// <param name="status">启用状态。</param>
        /// <returns>部门实体。</returns>
        public SysDepartment AddDepartment(
            long id,
            string departmentName,
            long tenantId,
            long? parentId,
            int sort,
            EnableStatus status = EnableStatus.Enabled)
        {
            var department = new SysDepartment
            {
                TenantId = tenantId,
                DepartmentName = departmentName,
                ParentId = parentId,
                Sort = sort,
                Status = status
            };
            ChatExtraDomainFixture.SetEntityId(department, id);
            Departments[id] = department;
            return department;
        }

        /// <summary>
        /// 创建带主键的消息实体。
        /// </summary>
        /// <param name="id">消息主键。</param>
        /// <param name="conversationId">会话主键。</param>
        /// <returns>消息实体。</returns>
        public SysChatMessage CreateMessage(long id, long conversationId)
        {
            var message = new SysChatMessage
            {
                ConversationId = conversationId,
                SenderUserId = 2,
                MessageType = ChatMessageType.Text,
                Content = $"消息{id}",
                CreatedTime = DateTimeOffset.UtcNow
            };
            ChatExtraDomainFixture.SetEntityId(message, id);
            return message;
        }
    }
}
