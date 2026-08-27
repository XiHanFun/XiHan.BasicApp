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
/// 聊天领域服务的内存态测试夹具：用可断言的内存集合替身驱动全部仓储，不触碰任何真实数据源。
/// </summary>
/// <remarks>
/// 领域服务的绝大多数不变量都是「读一批行 → 判定 → 写回若干行」的组合，用逐个 Setup 的替身写起来
/// 噪音极大且容易把被测逻辑写进测试里。这里改成一份内存镜像：仓储替身读写同一批集合，
/// 用例只需要摆好初始状态，断言就能直接落在集合的最终形态上。
/// </remarks>
internal sealed class ChatExtraDomainFixture
{
    private long _nextConversationId = 1000;
    private long _nextMemberId = 3000;
    private long _nextMessageId = 5000;
    private long _nextReactionId = 7000;

    /// <summary>
    /// 创建夹具并把全部仓储替身接到内存集合上。
    /// </summary>
    public ChatExtraDomainFixture()
    {
        ConversationRepository = new Mock<IChatConversationRepository>();
        MemberRepository = new Mock<IChatConversationMemberRepository>();
        MessageRepository = new Mock<IChatMessageRepository>();
        ReactionRepository = new Mock<IChatMessageReactionRepository>();
        UserRepository = new Mock<IUserRepository>();
        DepartmentRepository = new Mock<IDepartmentRepository>();
        UserDepartmentRepository = new Mock<IUserDepartmentRepository>();
        TenantUserRepository = new Mock<ITenantUserRepository>();
        CurrentTenant = new Mock<ICurrentTenant>();

        SetupConversationRepository();
        SetupMemberRepository();
        SetupMessageRepository();
        SetupReactionRepository();
        SetupUserRepository();
        SetupOrganizationRepositories();
        CurrentTenant.SetupGet(value => value.Id).Returns(() => TenantId);

        Service = new ChatDomainService(
            ConversationRepository.Object,
            MemberRepository.Object,
            MessageRepository.Object,
            ReactionRepository.Object,
            UserRepository.Object,
            DepartmentRepository.Object,
            UserDepartmentRepository.Object,
            TenantUserRepository.Object,
            CurrentTenant.Object);
    }

    /// <summary>被测领域服务。</summary>
    public ChatDomainService Service { get; }

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

    /// <summary>部门仓储替身。</summary>
    public Mock<IDepartmentRepository> DepartmentRepository { get; }

    /// <summary>用户部门关系仓储替身。</summary>
    public Mock<IUserDepartmentRepository> UserDepartmentRepository { get; }

    /// <summary>租户成员仓储替身。</summary>
    public Mock<ITenantUserRepository> TenantUserRepository { get; }

    /// <summary>当前租户上下文替身。</summary>
    public Mock<ICurrentTenant> CurrentTenant { get; }

    /// <summary>当前租户主键；null 或 0 表示平台作用域。</summary>
    public long? TenantId { get; set; }

    /// <summary>成员软删是否成功（用于覆盖「成员移除失败」路径）。</summary>
    public bool MemberDeleteSucceeds { get; set; } = true;

    /// <summary>会话表内存镜像。</summary>
    public Dictionary<long, SysChatConversation> Conversations { get; } = [];

    /// <summary>会话成员表内存镜像。</summary>
    public List<SysChatConversationMember> Members { get; } = [];

    /// <summary>消息表内存镜像。</summary>
    public Dictionary<long, SysChatMessage> Messages { get; } = [];

    /// <summary>表情回应表内存镜像。</summary>
    public List<SysChatMessageReaction> Reactions { get; } = [];

    /// <summary>用户表内存镜像。</summary>
    public Dictionary<long, SysUser> Users { get; } = [];

    /// <summary>部门表内存镜像。</summary>
    public Dictionary<long, SysDepartment> Departments { get; } = [];

    /// <summary>部门到成员用户主键的映射。</summary>
    public Dictionary<long, List<long>> DepartmentUserIds { get; } = [];

    /// <summary>租户成员关系内存镜像。</summary>
    public List<SysTenantUser> TenantUsers { get; } = [];

    /// <summary>本次用例中新落库的消息（按写入顺序）。</summary>
    public List<SysChatMessage> AddedMessages { get; } = [];

    /// <summary>本次用例中新落库的成员行（按写入顺序）。</summary>
    public List<SysChatConversationMember> AddedMembers { get; } = [];

    /// <summary>
    /// 登记一个平台归属用户。
    /// </summary>
    /// <param name="id">用户主键。</param>
    /// <param name="userName">用户名。</param>
    /// <param name="tenantId">用户归属租户；0 为平台归属。</param>
    /// <returns>用户实体。</returns>
    public SysUser AddUser(long id, string userName, long tenantId = 0)
    {
        var user = new SysUser { UserName = userName, TenantId = tenantId };
        SetEntityId(user, id);
        Users[id] = user;
        return user;
    }

    /// <summary>
    /// 登记一个会话。
    /// </summary>
    /// <param name="id">会话主键。</param>
    /// <param name="conversationType">会话类型。</param>
    /// <param name="ownerUserId">群主用户主键。</param>
    /// <param name="departmentId">部门主键。</param>
    /// <param name="pairKey">配对键。</param>
    /// <returns>会话实体。</returns>
    public SysChatConversation AddConversation(
        long id,
        ChatConversationType conversationType,
        long? ownerUserId = null,
        long? departmentId = null,
        string? pairKey = null)
    {
        var conversation = new SysChatConversation
        {
            ConversationType = conversationType,
            OwnerUserId = ownerUserId,
            DepartmentId = departmentId,
            PairKey = pairKey
        };
        SetEntityId(conversation, id);
        Conversations[id] = conversation;
        return conversation;
    }

    /// <summary>
    /// 登记一条会话成员行。
    /// </summary>
    /// <param name="conversationId">会话主键。</param>
    /// <param name="userId">用户主键。</param>
    /// <param name="role">成员角色。</param>
    /// <param name="silenced">是否被禁言。</param>
    /// <returns>成员实体。</returns>
    public SysChatConversationMember AddMember(
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
        SetEntityId(member, ++_nextMemberId);
        Members.Add(member);
        return member;
    }

    /// <summary>
    /// 登记一条消息。
    /// </summary>
    /// <param name="id">消息主键。</param>
    /// <param name="conversationId">会话主键。</param>
    /// <param name="senderUserId">发送人用户主键。</param>
    /// <param name="messageType">消息类型。</param>
    /// <param name="content">消息正文。</param>
    /// <param name="createdTime">发送时间；默认为当前时间。</param>
    /// <returns>消息实体。</returns>
    public SysChatMessage AddMessage(
        long id,
        long conversationId,
        long senderUserId,
        ChatMessageType messageType = ChatMessageType.Text,
        string? content = "hi",
        DateTimeOffset? createdTime = null)
    {
        var message = new SysChatMessage
        {
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            SenderUserName = Users.TryGetValue(senderUserId, out var sender) ? sender.UserName : null,
            MessageType = messageType,
            Content = content,
            CreatedTime = createdTime ?? DateTimeOffset.UtcNow
        };
        SetEntityId(message, id);
        Messages[id] = message;
        return message;
    }

    /// <summary>
    /// 登记一个部门及其成员。
    /// </summary>
    /// <param name="id">部门主键。</param>
    /// <param name="departmentName">部门名称。</param>
    /// <param name="userIds">部门成员用户主键。</param>
    /// <returns>部门实体。</returns>
    public SysDepartment AddDepartment(long id, string departmentName, params long[] userIds)
    {
        var department = new SysDepartment { DepartmentName = departmentName };
        SetEntityId(department, id);
        Departments[id] = department;
        DepartmentUserIds[id] = [.. userIds];
        return department;
    }

    /// <summary>
    /// 登记一条租户成员关系。
    /// </summary>
    /// <param name="tenantId">租户主键。</param>
    /// <param name="userId">用户主键。</param>
    public void AddTenantUser(long tenantId, long userId)
    {
        var membership = new SysTenantUser { TenantId = tenantId, UserId = userId };
        SetEntityId(membership, tenantId * 1000 + userId);
        TenantUsers.Add(membership);
    }

    /// <summary>
    /// 取会话内某用户的成员行（可能已被移除）。
    /// </summary>
    /// <param name="conversationId">会话主键。</param>
    /// <param name="userId">用户主键。</param>
    /// <returns>成员实体；不存在返回 null。</returns>
    public SysChatConversationMember? FindMember(long conversationId, long userId)
    {
        return Members.Find(member => member.ConversationId == conversationId && member.UserId == userId);
    }

    /// <summary>
    /// 模拟 ORM 回填受保护的实体主键。
    /// </summary>
    /// <param name="entity">实体实例。</param>
    /// <param name="id">主键值。</param>
    public static void SetEntityId(object entity, long id)
    {
        var property = entity.GetType().GetProperty(
            "BasicId",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("未找到实体主键属性。");
        property.SetValue(entity, id);
    }

    /// <summary>
    /// 接通会话仓储替身。
    /// </summary>
    private void SetupConversationRepository()
    {
        ConversationRepository
            .Setup(value => value.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) => Conversations.GetValueOrDefault(id));
        ConversationRepository
            .Setup(value => value.GetByPairKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string pairKey, CancellationToken _) =>
                Conversations.Values.FirstOrDefault(item => string.Equals(item.PairKey, pairKey, StringComparison.Ordinal)));
        ConversationRepository
            .Setup(value => value.GetByDepartmentIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long departmentId, CancellationToken _) => Conversations.Values.FirstOrDefault(item =>
                item.DepartmentId == departmentId && item.ConversationType == ChatConversationType.Department));
        ConversationRepository
            .Setup(value => value.AddAsync(It.IsAny<SysChatConversation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatConversation conversation, CancellationToken _) =>
            {
                SetEntityId(conversation, ++_nextConversationId);
                Conversations[conversation.BasicId] = conversation;
                return conversation;
            });
        ConversationRepository
            .Setup(value => value.UpdateAsync(It.IsAny<SysChatConversation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatConversation conversation, CancellationToken _) => conversation);
    }

    /// <summary>
    /// 接通会话成员仓储替身。
    /// </summary>
    private void SetupMemberRepository()
    {
        MemberRepository
            .Setup(value => value.GetMemberAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long conversationId, long userId, CancellationToken _) => FindMember(conversationId, userId));
        MemberRepository
            .Setup(value => value.GetByConversationIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long conversationId, CancellationToken _) =>
                Members.Where(member => member.ConversationId == conversationId).ToList());
        MemberRepository
            .Setup(value => value.GetByUserIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long userId, CancellationToken _) => Members.Where(member => member.UserId == userId).ToList());
        MemberRepository
            .Setup(value => value.AddAsync(It.IsAny<SysChatConversationMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatConversationMember member, CancellationToken _) =>
            {
                SetEntityId(member, ++_nextMemberId);
                Members.Add(member);
                AddedMembers.Add(member);
                return member;
            });
        MemberRepository
            .Setup(value => value.UpdateAsync(It.IsAny<SysChatConversationMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatConversationMember member, CancellationToken _) => member);
        MemberRepository
            .Setup(value => value.DeleteAsync(It.IsAny<SysChatConversationMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatConversationMember member, CancellationToken _) =>
                MemberDeleteSucceeds && Members.Remove(member));
        MemberRepository
            .Setup(value => value.IncrementUnreadAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long conversationId, long exceptUserId, CancellationToken _) =>
            {
                var affected = Members
                    .Where(member => member.ConversationId == conversationId && member.UserId != exceptUserId)
                    .ToList();
                affected.ForEach(member => member.UnreadCount += 1);
                return affected.Count;
            });
    }

    /// <summary>
    /// 接通消息仓储替身。
    /// </summary>
    private void SetupMessageRepository()
    {
        MessageRepository
            .Setup(value => value.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) => Messages.GetValueOrDefault(id));
        MessageRepository
            .Setup(value => value.AddAsync(It.IsAny<SysChatMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatMessage message, CancellationToken _) =>
            {
                SetEntityId(message, ++_nextMessageId);
                Messages[message.BasicId] = message;
                AddedMessages.Add(message);
                return message;
            });
        MessageRepository
            .Setup(value => value.UpdateAsync(It.IsAny<SysChatMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatMessage message, CancellationToken _) => message);
        MessageRepository
            .Setup(value => value.GetPinnedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long conversationId, CancellationToken _) =>
                Messages.Values.Where(message => message.ConversationId == conversationId && message.IsPinned).ToList());
    }

    /// <summary>
    /// 接通表情回应仓储替身。
    /// </summary>
    private void SetupReactionRepository()
    {
        ReactionRepository
            .Setup(value => value.GetAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long messageId, long userId, string emoji, CancellationToken _) =>
                Reactions.Find(reaction => reaction.MessageId == messageId
                    && reaction.UserId == userId
                    && string.Equals(reaction.Emoji, emoji, StringComparison.Ordinal)));
        ReactionRepository
            .Setup(value => value.AddAsync(It.IsAny<SysChatMessageReaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatMessageReaction reaction, CancellationToken _) =>
            {
                SetEntityId(reaction, ++_nextReactionId);
                Reactions.Add(reaction);
                return reaction;
            });
        ReactionRepository
            .Setup(value => value.DeleteAsync(It.IsAny<SysChatMessageReaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysChatMessageReaction reaction, CancellationToken _) => Reactions.Remove(reaction));
    }

    /// <summary>
    /// 接通用户仓储替身。
    /// </summary>
    private void SetupUserRepository()
    {
        UserRepository
            .Setup(value => value.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) => Users.GetValueOrDefault(id));
        UserRepository
            .Setup(value => value.GetListAsync(It.IsAny<Expression<Func<SysUser, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SysUser, bool>> predicate, CancellationToken _) =>
                Users.Values.Where(predicate.Compile()).ToList());
    }

    /// <summary>
    /// 接通部门与租户成员仓储替身。
    /// </summary>
    private void SetupOrganizationRepositories()
    {
        DepartmentRepository
            .Setup(value => value.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) => Departments.GetValueOrDefault(id));
        UserDepartmentRepository
            .Setup(value => value.GetUserIdsByDepartmentIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<long> departmentIds, CancellationToken _) =>
                departmentIds.SelectMany(id => DepartmentUserIds.GetValueOrDefault(id) ?? []).ToList());
        TenantUserRepository
            .Setup(value => value.GetListAsync(It.IsAny<Expression<Func<SysTenantUser, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SysTenantUser, bool>> predicate, CancellationToken _) =>
                TenantUsers.Where(predicate.Compile()).ToList());
    }
}
