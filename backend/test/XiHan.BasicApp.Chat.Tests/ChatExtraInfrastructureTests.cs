// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using XiHan.BasicApp.Chat.Application.EventHandlers;
using XiHan.BasicApp.Chat.Application.Services;
using XiHan.BasicApp.Chat.Domain.Configurations;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Domain.Repositories;
using XiHan.BasicApp.Chat.Hubs;
using XiHan.BasicApp.Chat.Infrastructure.Repositories;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.BasicApp.Chat.Domain.Permissions;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using ChatSeeders = XiHan.BasicApp.Chat.Infrastructure.Seeders;

namespace XiHan.BasicApp.Chat.Tests;

/// <summary>
/// 聊天基础设施测试：部门归属变更的部门群同步、种子数据的执行顺序与命名、
/// 仓储的取消透传，以及 Hub 会话ID 的 fail-closed 解析。
/// </summary>
/// <remarks>
/// 这一层的共同特征是「出错也不报错」：事件处理器吞掉异常只记日志、种子器顺序错了只是菜单少几个、
/// Hub 解析放宽了只是把任意字符串当组名。全都需要显式断言才看得见。
/// </remarks>
public sealed class ChatExtraInfrastructureTests
{
    /// <summary>
    /// 部门群还没建出来时，部门归属变更必须是空操作，不得凭空建群或建成员行。
    /// </summary>
    [Fact]
    public async Task HandleEventAsync_WithoutDepartmentConversationShouldBeNoOp()
    {
        var context = new SyncHandlerContext();

        await context.Handler.HandleEventAsync(new UserDepartmentChangedDomainEvent(5, 50, isAssigned: true));

        context.MemberRepository.Verify(
            value => value.AddAsync(It.IsAny<SysChatConversationMember>(), It.IsAny<CancellationToken>()),
            Times.Never);
        context.Push.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 分配进部门时自动加入部门群：新增成员行、人数加一，并把变更推给入群后的全体成员。
    /// </summary>
    [Fact]
    public async Task HandleEventAsync_AssignedShouldJoinDepartmentGroupAndNotifyEveryone()
    {
        var context = new SyncHandlerContext();
        var conversation = context.AddDepartmentConversation(100, departmentId: 50, memberCount: 1);
        context.AddMember(100, 1);

        await context.Handler.HandleEventAsync(new UserDepartmentChangedDomainEvent(5, 50, isAssigned: true));

        var added = Assert.Single(context.Members, member => member.UserId == 5);
        Assert.Equal(100, added.ConversationId);
        Assert.Equal(ChatMemberRole.Member, added.MemberRole);
        Assert.Equal(2, conversation.MemberCount);
        context.Push.Verify(
            value => value.PushConversationChangedAsync(
                100, "department-joined", It.Is<IReadOnlyList<long>>(ids => ids.Count == 2 && ids.Contains(5L))),
            Times.Once);
    }

    /// <summary>
    /// 已经在部门群里的人再次被分配进部门必须是空操作，不得建出重复成员行。
    /// </summary>
    [Fact]
    public async Task HandleEventAsync_AlreadyMemberShouldNotDuplicate()
    {
        var context = new SyncHandlerContext();
        _ = context.AddDepartmentConversation(100, 50, memberCount: 1);
        context.AddMember(100, 5);

        await context.Handler.HandleEventAsync(new UserDepartmentChangedDomainEvent(5, 50, isAssigned: true));

        Assert.Single(context.Members);
        context.MemberRepository.Verify(
            value => value.AddAsync(It.IsAny<SysChatConversationMember>(), It.IsAny<CancellationToken>()),
            Times.Never);
        context.Push.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 移出部门时即时踢出部门群，并把变更同时推给剩余成员与被踢的人（其会话列表随之收敛）。
    /// </summary>
    [Fact]
    public async Task HandleEventAsync_UnassignedShouldKickAndNotifyIncludingRemovedUser()
    {
        var context = new SyncHandlerContext();
        var conversation = context.AddDepartmentConversation(100, 50, memberCount: 2);
        context.AddMember(100, 1);
        context.AddMember(100, 5);

        await context.Handler.HandleEventAsync(new UserDepartmentChangedDomainEvent(5, 50, isAssigned: false));

        Assert.DoesNotContain(context.Members, member => member.UserId == 5);
        Assert.Equal(1, conversation.MemberCount);
        context.Push.Verify(
            value => value.PushConversationChangedAsync(
                100,
                "department-kicked",
                It.Is<IReadOnlyList<long>>(ids => ids.Count == 2 && ids.Contains(1L) && ids.Contains(5L))),
            Times.Once);
    }

    /// <summary>
    /// 移出部门但本就不在部门群里时必须是空操作，人数不得被误减。
    /// </summary>
    [Fact]
    public async Task HandleEventAsync_UnassignedNonMemberShouldBeNoOp()
    {
        var context = new SyncHandlerContext();
        var conversation = context.AddDepartmentConversation(100, 50, memberCount: 1);
        context.AddMember(100, 1);

        await context.Handler.HandleEventAsync(new UserDepartmentChangedDomainEvent(5, 50, isAssigned: false));

        Assert.Equal(1, conversation.MemberCount);
        context.Push.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 人数冗余递减有下限 0，脏数据不得把成员数减成负值。
    /// </summary>
    [Fact]
    public async Task HandleEventAsync_MemberCountShouldNeverGoNegative()
    {
        var context = new SyncHandlerContext();
        var conversation = context.AddDepartmentConversation(100, 50, memberCount: 0);
        context.AddMember(100, 5);

        await context.Handler.HandleEventAsync(new UserDepartmentChangedDomainEvent(5, 50, isAssigned: false));

        Assert.Equal(0, conversation.MemberCount);
    }

    /// <summary>
    /// 同步失败必须被吞掉：部门归属变更是主流程，不能因为聊天侧出错而整体回滚。
    /// </summary>
    [Fact]
    public async Task HandleEventAsync_RepositoryFailureShouldNotBreakMainFlow()
    {
        var context = new SyncHandlerContext();
        context.ConversationRepository
            .Setup(value => value.GetByDepartmentIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("数据库炸了"));

        await context.Handler.HandleEventAsync(new UserDepartmentChangedDomainEvent(5, 50, isAssigned: true));

        context.Push.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 事件数据为 null 必须抛参数空异常，而不是被 try 吞成静默成功。
    /// </summary>
    [Fact]
    public async Task HandleEventAsync_NullEventShouldThrow()
    {
        var context = new SyncHandlerContext();

        _ = await Assert.ThrowsAnyAsync<ArgumentNullException>(() => context.Handler.HandleEventAsync(null!));
    }

    /// <summary>
    /// 聊天种子各在自己的阶段、用聊天模块的号段（+40）：权限目录先于菜单，参数与任务在平台数据阶段。
    /// </summary>
    /// <remarks>
    /// 菜单建立时要按权限码绑定可见性，权限目录必须先落库；各模块同一阶段错开号段，避免与其它模块同序。
    /// </remarks>
    [Fact]
    public void ChatSeeders_ShouldRunInTheirPhases()
    {
        var orderByType = CreateSeeders().ToDictionary(seeder => seeder.GetType().Name, seeder => seeder.Order, StringComparer.Ordinal);

        Assert.Equal(SeedOrders.PermissionCatalog + 40, orderByType[nameof(ChatSeeders.ChatPermissionCatalogSeeder)]);
        Assert.Equal(SeedOrders.Menus + 40, orderByType[nameof(ChatSeeders.ChatMenuSeeder)]);
        Assert.Equal(SeedOrders.PlatformData + 40, orderByType[nameof(ChatSeeders.ChatSettingSeeder)]);
        Assert.Equal(SeedOrders.PlatformData + 41, orderByType[nameof(ChatSeeders.ChatTaskSeeder)]);
    }

    /// <summary>
    /// 聊天权限目录：四个功能权限两侧生效，码与常量表一致。
    /// </summary>
    [Fact]
    public void ChatPermissionCatalog_ShouldDeclareFourFunctionalPermissionsOnBothSides()
    {
        var catalog = new ChatSeeders.ChatPermissionCatalogSeeder(
            new Mock<ISqlSugarClientResolver>().Object,
            new Mock<ILogger<ChatSeeders.ChatPermissionCatalogSeeder>>().Object,
            new Mock<IServiceProvider>().Object);

        Assert.Equal(ChatPermissionCodes.All, catalog.Permissions.Select(permission => permission.Code));
        Assert.Empty(catalog.Resources);
        Assert.All(catalog.Permissions, permission =>
        {
            Assert.Equal(PermissionSide.Both, permission.Side);
            Assert.Null(permission.Resource);
        });
        Assert.Equal(ChatPermissionCodes.Module, catalog.ModuleCode);
    }

    /// <summary>
    /// 每个聊天种子器的名称都必须带 [Chat] 前缀，运行日志里才能一眼分辨归属模块。
    /// </summary>
    [Fact]
    public void ChatSeeders_ShouldPrefixNameWithModuleTag()
    {
        var violations = CreateSeeders()
            .Where(seeder => !seeder.Name.StartsWith("[Chat]", StringComparison.Ordinal))
            .Select(seeder => $"{seeder.GetType().Name} = {seeder.Name}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"下列 {violations.Count} 个聊天种子器名称缺少 [Chat] 前缀：{string.Join("、", violations)}");
    }

    /// <summary>
    /// 聊天参数归在 chat 分组下：键以分组名打头、符合参数键格式，默认策略与清理任务、守卫的缺省口径一致。
    /// </summary>
    [Fact]
    public void ChatConfigKeys_ShouldStayGroupedAndValid()
    {
        Assert.Equal("chat", ChatConfigKeys.Group, StringComparer.Ordinal);
        Assert.Equal("chat.policy", ChatConfigKeys.Policy, StringComparer.Ordinal);
        Assert.StartsWith(ChatConfigKeys.Group + ".", ChatConfigKeys.Policy, StringComparison.Ordinal);
        Assert.Equal(ChatConfigKeys.Policy, SaasConfigKeys.Normalize(ChatConfigKeys.Policy), StringComparer.Ordinal);

        var defaults = new ChatPolicySettings();
        Assert.Equal(365, defaults.RetentionDays);
        Assert.Empty(defaults.SensitiveWords);
    }

    /// <summary>
    /// 聊天参数种子只有聊天策略一条：初始值与默认值都是策略类型的默认实例，5.3.0 合并旧参数时写入的默认值与之一字不差。
    /// </summary>
    [Fact]
    public void ChatSettingSeeder_ShouldSeedPolicyDefaultsConsistentWithUpgradeScript()
    {
        var seeder = new ChatSeeders.ChatSettingSeeder(
            new Mock<ISqlSugarClientResolver>().Object,
            new Mock<ILogger<ChatSeeders.ChatSettingSeeder>>().Object,
            new Mock<IServiceProvider>().Object);

        var setting = Assert.Single(seeder.Settings);
        var defaults = JsonSerializer.Serialize(new ChatPolicySettings(), SaasConfigurationService.JsonOptions);
        Assert.Equal(ChatConfigKeys.Policy, setting.Key, StringComparer.Ordinal);
        Assert.Equal(ConfigDataType.Json, setting.DataType);
        Assert.Equal(defaults, setting.Value, StringComparer.Ordinal);
        Assert.Equal(defaults, setting.DefaultValue, StringComparer.Ordinal);

        var script = File.ReadAllText(ResolveUpgradeScript("5.3.0"));
        Assert.Contains($"'{ChatConfigKeys.Policy}'", script, StringComparison.Ordinal);
        Assert.Contains($"'{defaults}'", script, StringComparison.Ordinal);
    }

    /// <summary>
    /// 仓储的每个查询方法都必须先响应取消，取消后不得再去取数据库连接。
    /// </summary>
    [Fact]
    public async Task ChatRepositories_CancelledTokenShouldThrowBeforeTouchingClient()
    {
        var resolver = new Mock<ISqlSugarClientResolver>();
        var messageRepository = new ChatMessageRepository(resolver.Object);
        var memberRepository = new ChatConversationMemberRepository(resolver.Object);
        var conversationRepository = new ChatConversationRepository(resolver.Object);
        var reactionRepository = new ChatMessageReactionRepository(resolver.Object);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        var token = source.Token;

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => messageRepository.GetHistoryAsync(1, null, 10, token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => messageRepository.GetPinnedAsync(1, token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => messageRepository.GetAroundAsync(1, 2, 3, 4, token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => messageRepository.SearchAsync(1, "k", null, 10, token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => memberRepository.GetMemberAsync(1, 2, token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => memberRepository.GetByConversationIdAsync(1, token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => memberRepository.GetByUserIdAsync(1, token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => memberRepository.IncrementUnreadAsync(1, 2, token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => conversationRepository.GetByPairKeyAsync("1_2", token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => conversationRepository.GetByDepartmentIdAsync(50, token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reactionRepository.GetAsync(1, 2, "👍", token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reactionRepository.GetByMessageIdsAsync([1], token));

        resolver.Verify(value => value.GetCurrentClient(), Times.Never);
    }

    /// <summary>
    /// 空的消息ID集合必须直接返回空回应，不下推一条 IN () 空条件查询。
    /// </summary>
    [Fact]
    public async Task GetByMessageIdsAsync_EmptyIdsShouldShortCircuit()
    {
        var resolver = new Mock<ISqlSugarClientResolver>();
        var repository = new ChatMessageReactionRepository(resolver.Object);

        var reactions = await repository.GetByMessageIdsAsync([]);

        Assert.Empty(reactions);
        resolver.Verify(value => value.GetCurrentClient(), Times.Never);
    }

    /// <summary>
    /// Hub 的会话ID 解析是 fail-closed 的：非纯数字、非正数、空值一律拒绝，组名不接受任意字符串。
    /// </summary>
    /// <param name="raw">客户端上送的会话ID 字符串。</param>
    /// <param name="expected">是否应被接受。</param>
    [Theory]
    [InlineData("1", true)]
    [InlineData("9007199254740993", true)]
    [InlineData("0", false)]
    [InlineData("-1", false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData(null, false)]
    [InlineData("1;drop", false)]
    [InlineData("1.5", false)]
    [InlineData("chat:conv:1", false)]
    public void TryParseConversationId_ShouldFailClosedOnAnythingButPositiveInteger(string? raw, bool expected)
    {
        var method = typeof(BasicAppChatHub).GetMethod(
            "TryParseConversationId",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("未找到会话ID解析方法。");
        object?[] args = [raw, 0L];

        var accepted = (bool)method.Invoke(null, args)!;

        Assert.Equal(expected, accepted);
        if (expected)
        {
            Assert.True((long)args[1]! > 0);
        }
    }

    private static string ResolveUpgradeScript(string version, [CallerFilePath] string testFilePath = "")
    {
        var testDirectory = Path.GetDirectoryName(testFilePath)
            ?? throw new InvalidOperationException("无法解析测试源文件目录。");

        return Path.GetFullPath(Path.Combine(
            testDirectory, "..", "..", "src", "main", "XiHan.BasicApp.WebHost", "UpdateScripts", version, $"{version}.sql"));
    }

    /// <summary>
    /// 创建五个聊天种子器实例（仅用于读取执行序号与名称，不触发任何播种）。
    /// </summary>
    /// <returns>种子器实例集合。</returns>
    private static IReadOnlyList<IDataSeeder> CreateSeeders()
    {
        var resolver = new Mock<ISqlSugarClientResolver>().Object;
        var serviceProvider = new Mock<IServiceProvider>().Object;

        return
        [
            new ChatSeeders.ChatPermissionCatalogSeeder(resolver, new Mock<ILogger<ChatSeeders.ChatPermissionCatalogSeeder>>().Object, serviceProvider),
            new ChatSeeders.ChatMenuSeeder(resolver, new Mock<ILogger<ChatSeeders.ChatMenuSeeder>>().Object, serviceProvider),
            new ChatSeeders.ChatTaskSeeder(resolver, new Mock<ILogger<ChatSeeders.ChatTaskSeeder>>().Object, serviceProvider),
            new ChatSeeders.ChatSettingSeeder(resolver, new Mock<ILogger<ChatSeeders.ChatSettingSeeder>>().Object, serviceProvider)
        ];
    }

    /// <summary>
    /// 部门群成员同步事件处理器及其协作者替身，仓储读写同一批内存集合。
    /// </summary>
    private sealed class SyncHandlerContext
    {
        private long _nextMemberId = 3000;

        /// <summary>
        /// 组装事件处理器与替身。
        /// </summary>
        public SyncHandlerContext()
        {
            ConversationRepository = new Mock<IChatConversationRepository>();
            MemberRepository = new Mock<IChatConversationMemberRepository>();
            Push = new Mock<IChatRealtimePushService>();

            ConversationRepository
                .Setup(value => value.GetByDepartmentIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long departmentId, CancellationToken _) =>
                    Conversations.Find(conversation => conversation.DepartmentId == departmentId));
            ConversationRepository
                .Setup(value => value.UpdateAsync(It.IsAny<SysChatConversation>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysChatConversation conversation, CancellationToken _) => conversation);
            MemberRepository
                .Setup(value => value.GetMemberAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long conversationId, long userId, CancellationToken _) =>
                    Members.Find(member => member.ConversationId == conversationId && member.UserId == userId));
            MemberRepository
                .Setup(value => value.GetByConversationIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long conversationId, CancellationToken _) =>
                    Members.Where(member => member.ConversationId == conversationId).ToList());
            MemberRepository
                .Setup(value => value.AddAsync(It.IsAny<SysChatConversationMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysChatConversationMember member, CancellationToken _) =>
                {
                    ChatExtraDomainFixture.SetEntityId(member, ++_nextMemberId);
                    Members.Add(member);
                    return member;
                });
            MemberRepository
                .Setup(value => value.DeleteAsync(It.IsAny<SysChatConversationMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysChatConversationMember member, CancellationToken _) => Members.Remove(member));

            Handler = new ChatDepartmentMemberSyncEventHandler(
                ConversationRepository.Object,
                MemberRepository.Object,
                Push.Object,
                new Mock<ILogger<ChatDepartmentMemberSyncEventHandler>>().Object);
        }

        /// <summary>被测事件处理器。</summary>
        public ChatDepartmentMemberSyncEventHandler Handler { get; }

        /// <summary>会话仓储替身。</summary>
        public Mock<IChatConversationRepository> ConversationRepository { get; }

        /// <summary>会话成员仓储替身。</summary>
        public Mock<IChatConversationMemberRepository> MemberRepository { get; }

        /// <summary>实时推送服务替身。</summary>
        public Mock<IChatRealtimePushService> Push { get; }

        /// <summary>会话表内存镜像。</summary>
        public List<SysChatConversation> Conversations { get; } = [];

        /// <summary>会话成员表内存镜像。</summary>
        public List<SysChatConversationMember> Members { get; } = [];

        /// <summary>
        /// 登记一个部门群。
        /// </summary>
        /// <param name="id">会话主键。</param>
        /// <param name="departmentId">部门主键。</param>
        /// <param name="memberCount">当前成员数冗余值。</param>
        /// <returns>会话实体。</returns>
        public SysChatConversation AddDepartmentConversation(long id, long departmentId, int memberCount)
        {
            var conversation = new SysChatConversation
            {
                ConversationType = ChatConversationType.Department,
                DepartmentId = departmentId,
                MemberCount = memberCount
            };
            ChatExtraDomainFixture.SetEntityId(conversation, id);
            Conversations.Add(conversation);
            return conversation;
        }

        /// <summary>
        /// 登记一条会话成员行。
        /// </summary>
        /// <param name="conversationId">会话主键。</param>
        /// <param name="userId">用户主键。</param>
        public void AddMember(long conversationId, long userId)
        {
            var member = new SysChatConversationMember
            {
                ConversationId = conversationId,
                UserId = userId,
                MemberRole = ChatMemberRole.Member,
                JoinTime = DateTimeOffset.UtcNow
            };
            ChatExtraDomainFixture.SetEntityId(member, ++_nextMemberId);
            Members.Add(member);
        }
    }
}
