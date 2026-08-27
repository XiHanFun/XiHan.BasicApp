// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Chat.Domain.DomainServices;
using XiHan.BasicApp.Chat.Domain.Entities;

namespace XiHan.BasicApp.Chat.Tests;

/// <summary>
/// 聊天群治理领域不变量测试：建群、部门群同步、成员增减、群主转让、角色与禁言、群信息更新，
/// 以及会话成员写入唯一收口处的租户作用域校验。
/// </summary>
/// <remarks>
/// 群治理是聊天里唯一带「权限矩阵」的部分：谁能改什么由成员角色 + 会话类型两维决定，
/// 任何一格判反都直接构成越权。这里对每条规则同时给出通过路径与拒绝路径，并断言拒绝时
/// 不留下任何副作用（成员没删、消息没发、人数没变）。
/// </remarks>
public sealed class ChatExtraDomainGovernanceTests
{
    /// <summary>
    /// 建群成功时：会话为群聊、发起者为群主、其余人为普通成员、人数按去重后的成员数落库，并追加建群系统提示。
    /// </summary>
    [Fact]
    public async Task CreateGroupConversationAsync_ShouldSeatOwnerAndAppendCreateNotice()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "张三");
        _ = fixture.AddUser(2, "李四");
        _ = fixture.AddUser(3, "王五");

        var result = await fixture.Service.CreateGroupConversationAsync(
            new ChatGroupCreateCommand(OwnerUserId: 1, ConversationName: "项目群", MemberUserIds: [2, 3, 2, 0, -1]));

        Assert.Equal(ChatConversationType.Group, result.Conversation.ConversationType);
        Assert.Equal("项目群", result.Conversation.ConversationName);
        Assert.Equal(1L, result.Conversation.OwnerUserId);
        Assert.Equal(3, result.Conversation.MemberCount);
        Assert.Equal([1L, 2L, 3L], result.RecipientUserIds.Order().ToArray());
        Assert.Equal(ChatMemberRole.Owner, fixture.FindMember(result.Conversation.BasicId, 1)!.MemberRole);
        Assert.Equal(ChatMemberRole.Member, fixture.FindMember(result.Conversation.BasicId, 2)!.MemberRole);
        Assert.Equal(ChatMemberRole.Member, fixture.FindMember(result.Conversation.BasicId, 3)!.MemberRole);

        Assert.NotNull(result.SystemMessage);
        Assert.Equal(ChatMessageType.System, result.SystemMessage.MessageType);
        Assert.Equal(0L, result.SystemMessage.SenderUserId);
        Assert.Null(result.SystemMessage.SenderUserName);
        Assert.Equal("张三 创建了群聊", result.SystemMessage.Content);
        Assert.Equal("张三 创建了群聊", result.Conversation.LastMessagePreview);
        Assert.Equal(result.SystemMessage.BasicId, result.Conversation.LastMessageId);
    }

    /// <summary>
    /// 群聊名称为空、纯空白或超过 100 字符必须被拒绝，且不得落下任何会话行。
    /// </summary>
    /// <param name="conversationName">待校验的群聊名称。</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateGroupConversationAsync_BlankNameShouldReject(string? conversationName)
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "张三");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateGroupConversationAsync(
                new ChatGroupCreateCommand(1, conversationName!, [2])));

        Assert.Contains("群聊名称不能为空", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.Conversations);
    }

    /// <summary>
    /// 群聊名称超长必须被拒绝（边界：100 字符放行，101 字符拒绝）。
    /// </summary>
    [Fact]
    public async Task CreateGroupConversationAsync_NameLengthShouldBeBoundedAt100()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "张三");
        _ = fixture.AddUser(2, "李四");

        var atLimit = await fixture.Service.CreateGroupConversationAsync(
            new ChatGroupCreateCommand(1, new string('名', 100), [2]));
        Assert.Equal(100, atLimit.Conversation.ConversationName!.Length);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateGroupConversationAsync(
                new ChatGroupCreateCommand(1, new string('名', 101), [2])));
        Assert.Contains("不能超过 100 个字符", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 只有群主一人（成员去重后不足 2 人）不成群，必须被拒绝。
    /// </summary>
    [Fact]
    public async Task CreateGroupConversationAsync_SingleMemberShouldReject()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "张三");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateGroupConversationAsync(
                new ChatGroupCreateCommand(1, "只有我", [1, 1, 0])));

        Assert.Contains("至少需要 2 名成员", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.Conversations);
    }

    /// <summary>
    /// 群主主键非法必须抛越界异常，而不是继续走建群流程。
    /// </summary>
    [Fact]
    public async Task CreateGroupConversationAsync_NonPositiveOwnerShouldReject()
    {
        var fixture = new ChatExtraDomainFixture();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreateGroupConversationAsync(
                new ChatGroupCreateCommand(0, "项目群", [2, 3])));
    }

    /// <summary>
    /// 成员名单为 null 必须抛参数空异常（命令对象合法但字段缺失）。
    /// </summary>
    [Fact]
    public async Task CreateGroupConversationAsync_NullMemberListShouldThrow()
    {
        var fixture = new ChatExtraDomainFixture();

        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(
            () => fixture.Service.CreateGroupConversationAsync(
                new ChatGroupCreateCommand(1, "项目群", null!)));
    }

    /// <summary>
    /// 群主用户不存在必须被拒绝，且不得建出无主群。
    /// </summary>
    [Fact]
    public async Task CreateGroupConversationAsync_MissingOwnerUserShouldReject()
    {
        var fixture = new ChatExtraDomainFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateGroupConversationAsync(
                new ChatGroupCreateCommand(1, "项目群", [2])));

        Assert.Contains("用户 1 不存在", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.Conversations);
    }

    /// <summary>
    /// 首次进入部门群：按部门名建群、部门成员全量入群、人数与实际成员行一致。
    /// </summary>
    [Fact]
    public async Task GetOrCreateDepartmentConversationAsync_FirstOpenShouldCreateAndSyncMembers()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "张三");
        _ = fixture.AddUser(2, "李四");
        _ = fixture.AddDepartment(50, "研发部", 1, 2);

        var result = await fixture.Service.GetOrCreateDepartmentConversationAsync(
            new ChatDepartmentConversationCommand(DepartmentId: 50, OperatorUserId: 1));

        Assert.True(result.Created);
        Assert.Equal(ChatConversationType.Department, result.Conversation.ConversationType);
        Assert.Equal("研发部", result.Conversation.ConversationName);
        Assert.Equal(50L, result.Conversation.DepartmentId);
        Assert.Equal(2, result.Conversation.MemberCount);
        Assert.NotNull(fixture.FindMember(result.Conversation.BasicId, 1));
        Assert.NotNull(fixture.FindMember(result.Conversation.BasicId, 2));
        Assert.Null(result.Conversation.OwnerUserId);
    }

    /// <summary>
    /// 再次进入部门群必须复用既有会话，只补齐尚未入群的部门成员，离开部门的老成员保留不动。
    /// </summary>
    [Fact]
    public async Task GetOrCreateDepartmentConversationAsync_SecondOpenShouldOnlyAppendMissingMembers()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "张三");
        _ = fixture.AddUser(2, "李四");
        _ = fixture.AddUser(9, "旧同事");
        _ = fixture.AddDepartment(50, "研发部", 1, 2);
        var existing = fixture.AddConversation(100, ChatConversationType.Department, departmentId: 50);
        _ = fixture.AddMember(100, 1);
        _ = fixture.AddMember(100, 9);

        var result = await fixture.Service.GetOrCreateDepartmentConversationAsync(
            new ChatDepartmentConversationCommand(50, OperatorUserId: 1));

        Assert.False(result.Created);
        Assert.Same(existing, result.Conversation);
        Assert.Equal(3, result.Conversation.MemberCount);
        Assert.NotNull(fixture.FindMember(100, 2));
        Assert.NotNull(fixture.FindMember(100, 9));
        Assert.Single(fixture.AddedMembers);
    }

    /// <summary>
    /// 部门不存在必须被拒绝。
    /// </summary>
    [Fact]
    public async Task GetOrCreateDepartmentConversationAsync_MissingDepartmentShouldReject()
    {
        var fixture = new ChatExtraDomainFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.GetOrCreateDepartmentConversationAsync(
                new ChatDepartmentConversationCommand(50, 1)));

        Assert.Contains("部门不存在", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 非本部门成员不得进入部门群，且不得因此建出部门群。
    /// </summary>
    [Fact]
    public async Task GetOrCreateDepartmentConversationAsync_OutsiderShouldReject()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddDepartment(50, "研发部", 1, 2);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.GetOrCreateDepartmentConversationAsync(
                new ChatDepartmentConversationCommand(50, OperatorUserId: 8)));

        Assert.Contains("仅本部门成员可进入部门群", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.Conversations);
    }

    /// <summary>
    /// 单聊与部门群都不支持添加成员，只有自建群聊可以。
    /// </summary>
    /// <param name="conversationType">被检查的会话类型。</param>
    [Theory]
    [InlineData(ChatConversationType.Single)]
    [InlineData(ChatConversationType.Department)]
    [InlineData(ChatConversationType.Assistant)]
    public async Task AddMembersAsync_NonGroupConversationShouldReject(ChatConversationType conversationType)
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, conversationType);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.AddMembersAsync(new ChatMemberAddCommand(100, 1, [2])));

        Assert.Contains("仅群聊支持添加成员", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.AddedMembers);
    }

    /// <summary>
    /// 非会话成员发起加人必须被拒绝（与「是成员但不是管理者」区分不同的提示）。
    /// </summary>
    [Fact]
    public async Task AddMembersAsync_NonMemberOperatorShouldReject()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.AddMembersAsync(new ChatMemberAddCommand(100, OperatorUserId: 9, [2])));

        Assert.Contains("仅会话成员可操作", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 普通成员无权加人。
    /// </summary>
    [Fact]
    public async Task AddMembersAsync_PlainMemberOperatorShouldReject()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);
        _ = fixture.AddMember(100, 2);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.AddMembersAsync(new ChatMemberAddCommand(100, OperatorUserId: 2, [3])));

        Assert.Contains("仅群主或管理员可管理成员", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.AddedMembers);
    }

    /// <summary>
    /// 待加的人已经全在群里时必须是空操作：不加行、不改人数、不发系统提示。
    /// </summary>
    [Fact]
    public async Task AddMembersAsync_AllAlreadyMembersShouldBeNoOp()
    {
        var fixture = new ChatExtraDomainFixture();
        var conversation = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        conversation.MemberCount = 2;
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);
        _ = fixture.AddMember(100, 2);

        var result = await fixture.Service.AddMembersAsync(new ChatMemberAddCommand(100, 1, [1, 2]));

        Assert.Null(result.SystemMessage);
        Assert.Equal(2, result.Conversation.MemberCount);
        Assert.Equal([1L, 2L], result.RecipientUserIds.Order().ToArray());
        Assert.Empty(fixture.AddedMembers);
        Assert.Empty(fixture.AddedMessages);
    }

    /// <summary>
    /// 管理员加人成功时：新成员为普通成员、人数累加、系统提示按邀请文案生成，收件人含新老成员。
    /// </summary>
    [Fact]
    public async Task AddMembersAsync_AdminShouldAppendMembersAndInviteNotice()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(2, "李四");
        _ = fixture.AddUser(3, "王五");
        var conversation = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        conversation.MemberCount = 2;
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);
        _ = fixture.AddMember(100, 2, ChatMemberRole.Admin);

        var result = await fixture.Service.AddMembersAsync(new ChatMemberAddCommand(100, OperatorUserId: 2, [3]));

        Assert.Equal(3, result.Conversation.MemberCount);
        Assert.Equal(ChatMemberRole.Member, fixture.FindMember(100, 3)!.MemberRole);
        Assert.NotNull(result.SystemMessage);
        Assert.Equal("李四 邀请 王五 加入群聊", result.SystemMessage.Content);
        Assert.Equal([1L, 2L, 3L], result.RecipientUserIds.Order().ToArray());
    }

    /// <summary>
    /// 一次加入超过 3 人时，系统提示必须折叠为「前三人 等 N 人」而不是罗列全部。
    /// </summary>
    [Fact]
    public async Task AddMembersAsync_MoreThanThreeJoinersShouldFoldNames()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "群主");
        _ = fixture.AddUser(3, "甲");
        _ = fixture.AddUser(4, "乙");
        _ = fixture.AddUser(5, "丙");
        _ = fixture.AddUser(6, "丁");
        _ = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);

        var result = await fixture.Service.AddMembersAsync(new ChatMemberAddCommand(100, 1, [3, 4, 5, 6]));

        Assert.NotNull(result.SystemMessage);
        Assert.Equal("群主 邀请 甲、乙、丙 等 4 人 加入群聊", result.SystemMessage.Content);
    }

    /// <summary>
    /// 主动退群不需要管理权限，文案为「退出群聊」，且被移出者不在推送收件人内。
    /// </summary>
    [Fact]
    public async Task RemoveMemberAsync_SelfLeaveShouldNotRequireManagePermission()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(2, "李四");
        var conversation = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        conversation.MemberCount = 2;
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);
        _ = fixture.AddMember(100, 2);

        var result = await fixture.Service.RemoveMemberAsync(new ChatMemberRemoveCommand(100, 2, 2));

        Assert.Equal(1, result.Conversation.MemberCount);
        Assert.Null(fixture.FindMember(100, 2));
        Assert.NotNull(result.SystemMessage);
        Assert.Equal("李四 退出群聊", result.SystemMessage.Content);
        Assert.Equal([1L], result.RecipientUserIds.ToArray());
    }

    /// <summary>
    /// 单聊与部门群不支持成员移除/退群。
    /// </summary>
    /// <param name="conversationType">被检查的会话类型。</param>
    [Theory]
    [InlineData(ChatConversationType.Single)]
    [InlineData(ChatConversationType.Department)]
    public async Task RemoveMemberAsync_NonGroupConversationShouldReject(ChatConversationType conversationType)
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, conversationType);
        _ = fixture.AddMember(100, 2);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.RemoveMemberAsync(new ChatMemberRemoveCommand(100, 2, 2)));

        Assert.Contains("仅群聊支持成员移除/退群", exception.Message, StringComparison.Ordinal);
        Assert.NotNull(fixture.FindMember(100, 2));
    }

    /// <summary>
    /// 被移出者本就不是会话成员时必须报「不是会话成员」，而不是静默成功。
    /// </summary>
    [Fact]
    public async Task RemoveMemberAsync_TargetNotMemberShouldReject()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.RemoveMemberAsync(new ChatMemberRemoveCommand(100, 1, 7)));

        Assert.Contains("该用户不是会话成员", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 成员软删失败时必须整体失败，不得继续递减人数或追加系统提示。
    /// </summary>
    [Fact]
    public async Task RemoveMemberAsync_DeleteFailureShouldAbortWithoutSideEffect()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(2, "李四");
        var conversation = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        conversation.MemberCount = 2;
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);
        _ = fixture.AddMember(100, 2);
        fixture.MemberDeleteSucceeds = false;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.RemoveMemberAsync(new ChatMemberRemoveCommand(100, 1, 2)));

        Assert.Contains("成员移除失败", exception.Message, StringComparison.Ordinal);
        Assert.Equal(2, conversation.MemberCount);
        Assert.Empty(fixture.AddedMessages);
    }

    /// <summary>
    /// 人数冗余递减必须有下限 0，不得出现负数成员数。
    /// </summary>
    [Fact]
    public async Task RemoveMemberAsync_MemberCountShouldNeverGoNegative()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(2, "李四");
        var conversation = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        conversation.MemberCount = 0;
        _ = fixture.AddMember(100, 2);

        var result = await fixture.Service.RemoveMemberAsync(new ChatMemberRemoveCommand(100, 2, 2));

        Assert.Equal(0, result.Conversation.MemberCount);
    }

    /// <summary>
    /// 转让群主成功时：新旧成员角色对调、会话群主指针改写、系统提示记录移交事实。
    /// </summary>
    [Fact]
    public async Task TransferOwnerAsync_ShouldSwapRolesAndRewriteOwnerPointer()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "张三");
        _ = fixture.AddUser(2, "李四");
        _ = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);
        _ = fixture.AddMember(100, 2);

        var result = await fixture.Service.TransferOwnerAsync(new ChatOwnerTransferCommand(100, 1, 2));

        Assert.Equal(2L, result.Conversation.OwnerUserId);
        Assert.Equal(ChatMemberRole.Member, fixture.FindMember(100, 1)!.MemberRole);
        Assert.Equal(ChatMemberRole.Owner, fixture.FindMember(100, 2)!.MemberRole);
        Assert.NotNull(result.SystemMessage);
        Assert.Equal("张三 已将群主移交给 李四", result.SystemMessage.Content);
    }

    /// <summary>
    /// 只有群聊、只有群主能转让，且不能转给自己、不能转给非成员。
    /// </summary>
    [Fact]
    public async Task TransferOwnerAsync_ShouldRejectEveryIllegalCombination()
    {
        var single = new ChatExtraDomainFixture();
        _ = single.AddConversation(100, ChatConversationType.Single);
        var notGroup = await Assert.ThrowsAsync<InvalidOperationException>(
            () => single.Service.TransferOwnerAsync(new ChatOwnerTransferCommand(100, 1, 2)));
        Assert.Contains("仅群聊支持转让群主", notGroup.Message, StringComparison.Ordinal);

        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "张三");
        _ = fixture.AddUser(2, "李四");
        _ = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);
        _ = fixture.AddMember(100, 2);

        var notOwner = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.TransferOwnerAsync(new ChatOwnerTransferCommand(100, OperatorUserId: 2, NewOwnerUserId: 1)));
        Assert.Contains("仅群主可转让群主", notOwner.Message, StringComparison.Ordinal);

        var toSelf = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.TransferOwnerAsync(new ChatOwnerTransferCommand(100, 1, 1)));
        Assert.Contains("不能把群主转让给自己", toSelf.Message, StringComparison.Ordinal);

        var toOutsider = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.TransferOwnerAsync(new ChatOwnerTransferCommand(100, 1, 9)));
        Assert.Contains("新群主必须是会话成员", toOutsider.Message, StringComparison.Ordinal);

        Assert.Equal(1L, fixture.Conversations[100].OwnerUserId);
        Assert.Equal(ChatMemberRole.Owner, fixture.FindMember(100, 1)!.MemberRole);
    }

    /// <summary>
    /// 群主设置管理员成功时角色落库；重复设置同一角色时不再发起写操作。
    /// </summary>
    [Fact]
    public async Task SetMemberRoleAsync_OwnerShouldPromoteAndStayIdempotent()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);
        var target = fixture.AddMember(100, 2);

        _ = await fixture.Service.SetMemberRoleAsync(
            new ChatMemberRoleCommand(100, 1, 2, ChatMemberRole.Admin));
        Assert.Equal(ChatMemberRole.Admin, target.MemberRole);

        _ = await fixture.Service.SetMemberRoleAsync(
            new ChatMemberRoleCommand(100, 1, 2, ChatMemberRole.Admin));

        fixture.MemberRepository.Verify(
            value => value.UpdateAsync(target, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 角色设置的四条拒绝路径：非群聊、非群主、改自己、目标角色越界。
    /// </summary>
    [Fact]
    public async Task SetMemberRoleAsync_ShouldRejectEveryIllegalCombination()
    {
        var department = new ChatExtraDomainFixture();
        _ = department.AddConversation(100, ChatConversationType.Department);
        var notGroup = await Assert.ThrowsAsync<InvalidOperationException>(
            () => department.Service.SetMemberRoleAsync(new ChatMemberRoleCommand(100, 1, 2, ChatMemberRole.Admin)));
        Assert.Contains("仅群聊支持设置管理员", notGroup.Message, StringComparison.Ordinal);

        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);
        _ = fixture.AddMember(100, 2, ChatMemberRole.Admin);

        var notOwner = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SetMemberRoleAsync(new ChatMemberRoleCommand(100, 2, 1, ChatMemberRole.Member)));
        Assert.Contains("仅群主可设置管理员", notOwner.Message, StringComparison.Ordinal);

        var self = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SetMemberRoleAsync(new ChatMemberRoleCommand(100, 1, 1, ChatMemberRole.Member)));
        Assert.Contains("不能修改自己的角色", self.Message, StringComparison.Ordinal);

        var toOwner = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SetMemberRoleAsync(new ChatMemberRoleCommand(100, 1, 2, ChatMemberRole.Owner)));
        Assert.Contains("只能在管理员与普通成员之间切换", toOwner.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 群主的角色不得被改写：即便是群主自己转成员也要先走转让流程。
    /// </summary>
    /// <remarks>群主与会话上的 OwnerUserId 指针必须同源，绕开转让直接改角色会让两者不一致。</remarks>
    [Fact]
    public async Task SetMemberRoleAsync_TargetOwnerRowShouldBeProtected()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);
        var strayOwnerRow = fixture.AddMember(100, 2, ChatMemberRole.Owner);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SetMemberRoleAsync(new ChatMemberRoleCommand(100, 1, 2, ChatMemberRole.Member)));

        Assert.Contains("不能修改群主的角色", exception.Message, StringComparison.Ordinal);
        Assert.Equal(ChatMemberRole.Owner, strayOwnerRow.MemberRole);
    }

    /// <summary>
    /// 管理员可禁言普通成员；解除禁言走同一入口，重复设置同一状态不再写库。
    /// </summary>
    [Fact]
    public async Task SetMemberSilenceAsync_AdminShouldToggleSilenceIdempotently()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);
        _ = fixture.AddMember(100, 2, ChatMemberRole.Admin);
        var target = fixture.AddMember(100, 3);

        var silenced = await fixture.Service.SetMemberSilenceAsync(
            new ChatMemberSilenceCommand(100, OperatorUserId: 2, TargetUserId: 3, IsSilenced: true));
        Assert.True(target.IsSilenced);
        Assert.Null(silenced.SystemMessage);
        Assert.Equal([1L, 2L, 3L], silenced.RecipientUserIds.Order().ToArray());

        _ = await fixture.Service.SetMemberSilenceAsync(new ChatMemberSilenceCommand(100, 2, 3, IsSilenced: true));
        fixture.MemberRepository.Verify(
            value => value.UpdateAsync(target, It.IsAny<CancellationToken>()),
            Times.Once);

        _ = await fixture.Service.SetMemberSilenceAsync(new ChatMemberSilenceCommand(100, 2, 3, IsSilenced: false));
        Assert.False(target.IsSilenced);
    }

    /// <summary>
    /// 单聊没有治理层级，不支持禁言。
    /// </summary>
    [Fact]
    public async Task SetMemberSilenceAsync_SingleConversationShouldReject()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, ChatConversationType.Single);
        _ = fixture.AddMember(100, 1);
        var target = fixture.AddMember(100, 2);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SetMemberSilenceAsync(new ChatMemberSilenceCommand(100, 1, 2, true)));

        Assert.Contains("单聊不支持禁言", exception.Message, StringComparison.Ordinal);
        Assert.False(target.IsSilenced);
    }

    /// <summary>
    /// 群主与管理员不可被禁言，管理动作只对普通成员生效。
    /// </summary>
    /// <param name="targetRole">被禁言目标的角色。</param>
    [Theory]
    [InlineData(ChatMemberRole.Owner)]
    [InlineData(ChatMemberRole.Admin)]
    public async Task SetMemberSilenceAsync_ManagerTargetShouldReject(ChatMemberRole targetRole)
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);
        var target = fixture.AddMember(100, 2, targetRole);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SetMemberSilenceAsync(new ChatMemberSilenceCommand(100, 1, 2, true)));

        Assert.Contains("不能禁言群主或管理员", exception.Message, StringComparison.Ordinal);
        Assert.False(target.IsSilenced);
    }

    /// <summary>
    /// 更新群信息：改名/描述/头像即时生效，公告变更额外以操作人身份追加一条公告消息。
    /// </summary>
    [Fact]
    public async Task UpdateConversationInfoAsync_AnnouncementChangeShouldAppendOwnedNotice()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "张三");
        _ = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);

        var result = await fixture.Service.UpdateConversationInfoAsync(
            new ChatConversationInfoUpdateCommand(100, 1, "新群名", "本周五发版", "冲刺群", "avatar.png"));

        Assert.Equal("新群名", result.Conversation.ConversationName);
        Assert.Equal("本周五发版", result.Conversation.Announcement);
        Assert.Equal("冲刺群", result.Conversation.Description);
        Assert.Equal("avatar.png", result.Conversation.Avatar);
        Assert.NotNull(result.SystemMessage);
        Assert.Equal(ChatMessageType.System, result.SystemMessage.MessageType);
        Assert.Equal(1L, result.SystemMessage.SenderUserId);
        Assert.Equal("张三", result.SystemMessage.SenderUserName);
        Assert.Equal("本周五发版", result.SystemMessage.Content);
        Assert.Equal("[群公告] 本周五发版", result.Conversation.LastMessagePreview);
    }

    /// <summary>
    /// 公告内容没变时不得重复刷屏；显式传空串清空公告同样不发提示。
    /// </summary>
    [Fact]
    public async Task UpdateConversationInfoAsync_UnchangedOrClearedAnnouncementShouldNotNotify()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "张三");
        var conversation = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        conversation.Announcement = "本周五发版";
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);

        var unchanged = await fixture.Service.UpdateConversationInfoAsync(
            new ChatConversationInfoUpdateCommand(100, 1, null, "本周五发版", null));
        Assert.Null(unchanged.SystemMessage);

        var cleared = await fixture.Service.UpdateConversationInfoAsync(
            new ChatConversationInfoUpdateCommand(100, 1, null, "", null));
        Assert.Null(cleared.SystemMessage);
        Assert.Null(cleared.Conversation.Announcement);
        Assert.Empty(fixture.AddedMessages);
    }

    /// <summary>
    /// 传 null 的字段一律保持原值，避免"没填就清空"。
    /// </summary>
    [Fact]
    public async Task UpdateConversationInfoAsync_NullFieldsShouldKeepCurrentValues()
    {
        var fixture = new ChatExtraDomainFixture();
        var conversation = fixture.AddConversation(100, ChatConversationType.Group, ownerUserId: 1);
        conversation.ConversationName = "原群名";
        conversation.Announcement = "原公告";
        conversation.Description = "原描述";
        conversation.Avatar = "原头像";
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);

        var result = await fixture.Service.UpdateConversationInfoAsync(
            new ChatConversationInfoUpdateCommand(100, 1, null, null, null));

        Assert.Equal("原群名", result.Conversation.ConversationName);
        Assert.Equal("原公告", result.Conversation.Announcement);
        Assert.Equal("原描述", result.Conversation.Description);
        Assert.Equal("原头像", result.Conversation.Avatar);
        Assert.Null(result.SystemMessage);
    }

    /// <summary>
    /// 单聊没有群信息可编辑；部门群名称随部门同步，不许手动改。
    /// </summary>
    [Fact]
    public async Task UpdateConversationInfoAsync_SingleAndDepartmentNameShouldBeProtected()
    {
        var single = new ChatExtraDomainFixture();
        _ = single.AddConversation(100, ChatConversationType.Single);
        var singleReject = await Assert.ThrowsAsync<InvalidOperationException>(
            () => single.Service.UpdateConversationInfoAsync(
                new ChatConversationInfoUpdateCommand(100, 1, "新名", null, null)));
        Assert.Contains("单聊没有可编辑的群信息", singleReject.Message, StringComparison.Ordinal);

        var department = new ChatExtraDomainFixture();
        var conversation = department.AddConversation(100, ChatConversationType.Department, departmentId: 50);
        conversation.ConversationName = "研发部";
        _ = department.AddMember(100, 1, ChatMemberRole.Owner);
        var departmentReject = await Assert.ThrowsAsync<InvalidOperationException>(
            () => department.Service.UpdateConversationInfoAsync(
                new ChatConversationInfoUpdateCommand(100, 1, "改名", null, null)));
        Assert.Contains("部门群名称随部门同步", departmentReject.Message, StringComparison.Ordinal);
        Assert.Equal("研发部", conversation.ConversationName);
    }

    /// <summary>
    /// 部门群允许改公告与描述，只是名称受保护。
    /// </summary>
    [Fact]
    public async Task UpdateConversationInfoAsync_DepartmentAnnouncementShouldStillBeEditable()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "张三");
        _ = fixture.AddConversation(100, ChatConversationType.Department, departmentId: 50);
        _ = fixture.AddMember(100, 1, ChatMemberRole.Owner);

        var result = await fixture.Service.UpdateConversationInfoAsync(
            new ChatConversationInfoUpdateCommand(100, 1, null, "部门例会改到周三", "研发部群"));

        Assert.Equal("部门例会改到周三", result.Conversation.Announcement);
        Assert.Equal("研发部群", result.Conversation.Description);
        Assert.NotNull(result.SystemMessage);
    }

    /// <summary>
    /// 会话不存在时统一报「会话不存在」，会话主键非法时抛越界异常。
    /// </summary>
    [Fact]
    public async Task GovernanceCommands_ShouldValidateConversationIdBeforeAnything()
    {
        var fixture = new ChatExtraDomainFixture();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.AddMembersAsync(new ChatMemberAddCommand(0, 1, [2])));

        var missing = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.AddMembersAsync(new ChatMemberAddCommand(404, 1, [2])));
        Assert.Contains("会话不存在", missing.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 平台作用域下不得把租户归属用户拉进会话，否则会造出对方永远看不到的死关系。
    /// </summary>
    /// <remarks>
    /// 成员行的 TenantId 取自建行时的上下文而非用户自身归属：不拦就会写出一行 TenantId=0 的成员记录，
    /// 该用户在自己的租户里看不到这个会话，只会收到推送。
    /// </remarks>
    [Fact]
    public async Task AddMemberRows_PlatformScopeShouldRejectTenantOwnedUser()
    {
        var fixture = new ChatExtraDomainFixture { TenantId = null };
        _ = fixture.AddUser(1, "张三");
        _ = fixture.AddUser(2, "李四", tenantId: 7);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.GetOrCreateSingleConversationAsync(new ChatSingleConversationCommand(1, 2)));

        Assert.Contains("属于租户，不能加入平台会话", exception.Message, StringComparison.Ordinal);
        Assert.Contains("李四", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.AddedMembers);
    }

    /// <summary>
    /// 租户 0 与「无租户上下文」等价，都按平台作用域校验。
    /// </summary>
    [Fact]
    public async Task AddMemberRows_TenantZeroShouldBeTreatedAsPlatformScope()
    {
        var fixture = new ChatExtraDomainFixture { TenantId = 0 };
        _ = fixture.AddUser(1, "张三");
        _ = fixture.AddUser(2, "李四", tenantId: 7);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.GetOrCreateSingleConversationAsync(new ChatSingleConversationCommand(1, 2)));

        Assert.Contains("不能加入平台会话", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 租户作用域下只收该租户的成员，缺任一成员关系即整体拒绝。
    /// </summary>
    [Fact]
    public async Task AddMemberRows_TenantScopeShouldRejectUsersOutsideCurrentTenant()
    {
        var fixture = new ChatExtraDomainFixture { TenantId = 7 };
        _ = fixture.AddUser(1, "张三", tenantId: 7);
        _ = fixture.AddUser(2, "李四", tenantId: 7);
        fixture.AddTenantUser(7, 1);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.GetOrCreateSingleConversationAsync(new ChatSingleConversationCommand(1, 2)));

        Assert.Contains("不属于当前租户的用户", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.AddedMembers);
    }

    /// <summary>
    /// 租户作用域下双方都是该租户成员时必须放行并建出成员行。
    /// </summary>
    [Fact]
    public async Task AddMemberRows_TenantScopeShouldAllowTenantMembers()
    {
        var fixture = new ChatExtraDomainFixture { TenantId = 7 };
        _ = fixture.AddUser(1, "张三", tenantId: 7);
        _ = fixture.AddUser(2, "李四", tenantId: 7);
        fixture.AddTenantUser(7, 1);
        fixture.AddTenantUser(7, 2);

        var result = await fixture.Service.GetOrCreateSingleConversationAsync(new ChatSingleConversationCommand(1, 2));

        Assert.True(result.Created);
        Assert.Equal(2, fixture.AddedMembers.Count);
    }

    /// <summary>
    /// 已取消的令牌必须在动手写任何数据之前抛出取消异常。
    /// </summary>
    [Fact]
    public async Task GovernanceCommands_CancelledTokenShouldThrowBeforeAnyWrite()
    {
        var fixture = new ChatExtraDomainFixture();
        _ = fixture.AddUser(1, "张三");
        _ = fixture.AddUser(2, "李四");
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.CreateGroupConversationAsync(
                new ChatGroupCreateCommand(1, "项目群", [2]), source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.GetOrCreateSingleConversationAsync(
                new ChatSingleConversationCommand(1, 2), source.Token));

        Assert.Empty(fixture.Conversations);
        Assert.Empty(fixture.AddedMembers);
    }

    /// <summary>
    /// 全部群治理命令对 null 命令对象都必须抛参数空异常。
    /// </summary>
    [Fact]
    public async Task GovernanceCommands_NullCommandShouldThrow()
    {
        var fixture = new ChatExtraDomainFixture();

        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.CreateGroupConversationAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.GetOrCreateDepartmentConversationAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.AddMembersAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.RemoveMemberAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.TransferOwnerAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.SetMemberRoleAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.SetMemberSilenceAsync(null!));
        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => fixture.Service.UpdateConversationInfoAsync(null!));
    }
}
