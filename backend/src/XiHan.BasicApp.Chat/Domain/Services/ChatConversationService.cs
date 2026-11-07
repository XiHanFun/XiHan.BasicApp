// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Operations.ChatConversations;
using XiHan.BasicApp.Contracts.Assistants;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Domain.Services;

/// <summary>
/// 会话与群治理（作用域）
/// </summary>
/// <remarks>
/// 规则照 4.x 的 <c>ChatDomainService</c>：单聊按两人唯一；建群含群主至少两人、发起人为群主；部门群只有本部门的成员能进、打开时补齐部门成员，没有群主与管理员（方案 H3，与 4.x 同）；
/// 群治理（加人、移出、改信息、转让群主、禁言、设管理员）只在群聊，群主不能退群或被移出，没有解散。治理产生的系统提示与会话变更在提交之后推给成员
/// 平台没有部门（6.0 的部门是租户库实体），部门群只在租户一侧
/// </remarks>
/// <param name="store">按侧读写聊天表</param>
/// <param name="departments">部门仓储</param>
/// <param name="messages">消息服务（系统提示）</param>
/// <param name="texts">系统提示文案</param>
/// <param name="push">推送</param>
/// <param name="ids">标识生成器</param>
/// <param name="clock">时间源</param>
public sealed class ChatConversationService(
    ChatStore store,
    IRepository<SysDepartment, Guid> departments,
    ChatMessageService messages,
    ChatTexts texts,
    ChatPush push,
    IIdGenerator<Guid> ids,
    TimeProvider clock)
{
    /// <summary>
    /// 打开单聊，没有即建
    /// </summary>
    public async Task<Result<ChatConversationOpenResponse>> OpenSingleAsync(InvocationContext context, ChatCaller caller, Guid peerUserId)
    {
        if (peerUserId == caller.UserId)
        {
            return Error.Validation(ChatErrors.SelfConversation, ChatMessages.SelfConversation);
        }

        var pairKey = ChatRules.PairKeyOf(caller.UserId, peerUserId);
        var existing = await store.Conversations(caller.Side).Where(c => c.PairKey == pairKey).Take(1).ToListAsync(context.CancellationToken).ConfigureAwait(false);
        if (existing.Count > 0)
        {
            return OpenedOf(existing[0], created: false);
        }

        var people = await store.RequireUsersInSideAsync(caller.Side, [caller.UserId, peerUserId], context.CancellationToken).ConfigureAwait(false);
        if (people.IsFailure)
        {
            return people.Error!;
        }

        var conversation = new SysChatConversation { Id = ids.NewId(), TenantId = caller.Side, Type = ChatConversationType.Single, PairKey = pairKey, MemberCount = 2 };
        await store.Rows.InsertAsync(conversation, context.CancellationToken).ConfigureAwait(false);
        await AddMemberRowsAsync(caller.Side, conversation.Id, [caller.UserId, peerUserId], ownerUserId: null, context.CancellationToken).ConfigureAwait(false);
        return OpenedOf(conversation, created: true);
    }

    /// <summary>
    /// 打开与助手的会话，没有即建
    /// </summary>
    /// <remarks>
    /// 只有本人一个成员（照 4.x）
    /// </remarks>
    /// <returns>会话与是否新建</returns>
    public async Task<Result<(SysChatConversation Conversation, bool Created)>> OpenAssistantAsync(InvocationContext context, ChatCaller caller, ChatAssistantInfo assistant)
    {
        var pairKey = ChatRules.AssistantPairKeyOf(assistant.Id, caller.UserId);
        var existing = await store.Conversations(caller.Side).Where(c => c.PairKey == pairKey).Take(1).ToListAsync(context.CancellationToken).ConfigureAwait(false);
        if (existing.Count > 0)
        {
            return (existing[0], false);
        }

        var conversation = new SysChatConversation { Id = ids.NewId(), TenantId = caller.Side, Type = ChatConversationType.Assistant, Name = assistant.Name, Avatar = assistant.Avatar, AssistantId = assistant.Id, PairKey = pairKey, MemberCount = 1 };
        await store.Rows.InsertAsync(conversation, context.CancellationToken).ConfigureAwait(false);
        await AddMemberRowsAsync(caller.Side, conversation.Id, [caller.UserId], ownerUserId: null, context.CancellationToken).ConfigureAwait(false);
        return (conversation, true);
    }

    /// <summary>
    /// 建群：发起人为群主，追加建群提示
    /// </summary>
    public async Task<Result<ChatConversationOpenResponse>> CreateGroupAsync(InvocationContext context, ChatCaller caller, string name, IReadOnlyList<Guid> memberUserIds)
    {
        var memberIds = memberUserIds.Where(static id => id != Guid.Empty).Append(caller.UserId).Distinct().ToList();
        if (memberIds.Count < 2)
        {
            return Error.Validation(ChatErrors.GroupTooSmall, ChatMessages.GroupTooSmall, MessageArguments.Of(("min", 2)));
        }

        var people = await store.RequireUsersInSideAsync(caller.Side, memberIds, context.CancellationToken).ConfigureAwait(false);
        if (!people.TryGetValue(out var users))
        {
            return people.Error!;
        }

        var conversation = new SysChatConversation { Id = ids.NewId(), TenantId = caller.Side, Type = ChatConversationType.Group, Name = name.Trim(), OwnerUserId = caller.UserId, MemberCount = memberIds.Count };
        await store.Rows.InsertAsync(conversation, context.CancellationToken).ConfigureAwait(false);
        await AddMemberRowsAsync(caller.Side, conversation.Id, memberIds, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        var text = texts.Render(context, ChatMessages.SystemGroupCreated, ("name", users[caller.UserId].UserName));
        var system = await messages.AppendSystemAsync(context, caller.Side, conversation.Id, text, text, senderUserId: null, senderName: null).ConfigureAwait(false);
        if (!system.TryGetValue(out var appended))
        {
            return system.Error!;
        }

        await push.ChangedAsync(caller.Side, conversation.Id, ChatRealtimeMethods.ChangeTypes.Created, memberIds, context.CancellationToken).ConfigureAwait(false);
        await push.MessageAsync(caller.Side, ChatStore.ItemOf(appended.Message), appended.Conversation, memberIds, context.CancellationToken).ConfigureAwait(false);
        return OpenedOf(appended.Conversation, created: true);
    }

    /// <summary>
    /// 打开部门群，没有即建，并把部门现有的成员补进群
    /// </summary>
    /// <remarks>
    /// 只有本部门的成员能进（照 4.x）；离开部门的成员保留会话历史、不在这里移除（移除随部门归属事件）
    /// </remarks>
    public async Task<Result<ChatConversationOpenResponse>> OpenDepartmentAsync(InvocationContext context, ChatCaller caller, Guid departmentId)
    {
        if (!(await departments.FindAsync(departmentId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var department))
        {
            return Error.NotFound(ChatErrors.DepartmentNotFound, ChatMessages.DepartmentNotFound, MessageArguments.Of(("id", departmentId)));
        }

        var departmentUsers = (await TenantScopedQuery.For<SysUserDepartment>(context).Where(m => m.DepartmentId == departmentId).Select(static m => m.UserId).ToListAsync(context.CancellationToken).ConfigureAwait(false)).Distinct().ToList();
        if (!departmentUsers.Contains(caller.UserId))
        {
            return Error.Denied(ChatErrors.NotInDepartment, ChatMessages.NotInDepartment);
        }

        var conversation = await DepartmentConversationAsync(caller.Side, departmentId, context.CancellationToken).ConfigureAwait(false);
        var created = conversation is null;
        if (conversation is null)
        {
            conversation = new SysChatConversation { Id = ids.NewId(), TenantId = caller.Side, Type = ChatConversationType.Department, Name = department.Name, DepartmentId = departmentId };
            await store.Rows.InsertAsync(conversation, context.CancellationToken).ConfigureAwait(false);
        }

        var existing = (await store.MemberIdsAsync(caller.Side, conversation.Id, context.CancellationToken).ConfigureAwait(false)).ToHashSet();
        var missing = departmentUsers.Where(id => !existing.Contains(id)).ToList();
        if (missing.Count > 0)
        {
            var people = await store.RequireUsersInSideAsync(caller.Side, missing, context.CancellationToken).ConfigureAwait(false);
            if (people.IsFailure)
            {
                return people.Error!;
            }

            await AddMemberRowsAsync(caller.Side, conversation.Id, missing, ownerUserId: null, context.CancellationToken).ConfigureAwait(false);
        }

        if (created || missing.Count > 0)
        {
            conversation.MemberCount = existing.Count + missing.Count;
            await store.Rows.UpdateAsync(conversation, context.CancellationToken).ConfigureAwait(false);
        }

        return OpenedOf(conversation, created);
    }

    /// <summary>
    /// 加群成员（群主或管理员，只在群聊）
    /// </summary>
    /// <returns>请求里的用户都在群里时成功（已在群里的也算成功）</returns>
    public async Task<Result<BatchResult>> AddMembersAsync(InvocationContext context, ChatCaller caller, Guid conversationId, IReadOnlyList<Guid> userIds)
    {
        var governed = await GroupForManagerAsync(context, caller, conversationId).ConfigureAwait(false);
        if (governed.IsFailure)
        {
            return governed.Error!;
        }

        var existing = (await store.MemberIdsAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false)).ToHashSet();
        var requested = userIds.Where(static id => id != Guid.Empty).Distinct().ToList();
        var missing = requested.Where(id => !existing.Contains(id)).ToList();
        if (missing.Count == 0)
        {
            await push.ChangedAsync(caller.Side, conversationId, ChatRealtimeMethods.ChangeTypes.MemberAdded, existing, context.CancellationToken).ConfigureAwait(false);
            return new BatchResult(requested, []);
        }

        var people = await store.RequireUsersInSideAsync(caller.Side, [.. missing, caller.UserId], context.CancellationToken).ConfigureAwait(false);
        if (!people.TryGetValue(out var users))
        {
            return people.Error!;
        }

        await AddMemberRowsAsync(caller.Side, conversationId, missing, ownerUserId: null, context.CancellationToken).ConfigureAwait(false);
        var text = texts.Render(context, ChatMessages.SystemMembersInvited, ("operator", users[caller.UserId].UserName), ("names", texts.Names(context, [.. missing.Select(id => users[id].UserName)])));
        var recipients = existing.Concat(missing).ToList();
        var announced = await AnnounceAsync(context, caller, conversationId, text, ChatRealtimeMethods.ChangeTypes.MemberAdded, recipients, recipients.Count).ConfigureAwait(false);
        return announced.IsFailure ? announced.Error! : new BatchResult(requested, []);
    }

    /// <summary>
    /// 移出群成员（群主或管理员，只在群聊）：逐个成功或失败
    /// </summary>
    /// <remarks>
    /// 不是成员、群主、自己（自己用退群）逐项失败；移出的人一并写一条系统提示，剩下的成员收到会话变更与提示，被移出的人只收会话变更
    /// </remarks>
    public async Task<Result<BatchResult>> RemoveMembersAsync(InvocationContext context, ChatCaller caller, Guid conversationId, IReadOnlyList<Guid> userIds)
    {
        var governed = await GroupForManagerAsync(context, caller, conversationId).ConfigureAwait(false);
        if (!governed.TryGetValue(out var conversation))
        {
            return governed.Error!;
        }

        var current = (await store.MembersAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static m => m.UserId);
        var removed = new List<SysChatConversationMember>();
        var failed = new List<BatchFailure>();
        foreach (var userId in userIds.Distinct())
        {
            if (userId == caller.UserId)
            {
                failed.Add(new BatchFailure(userId, ChatErrors.SelfTarget, ChatMessages.SelfTarget.L()));
            }
            else if (!current.TryGetValue(userId, out var member))
            {
                failed.Add(new BatchFailure(userId, ChatErrors.NotMember, ChatMessages.NotMember.L()));
            }
            else if (conversation.OwnerUserId == userId)
            {
                failed.Add(new BatchFailure(userId, ChatErrors.OwnerCannotLeave, ChatMessages.OwnerCannotLeave.L()));
            }
            else
            {
                removed.Add(member);
            }
        }

        if (removed.Count == 0)
        {
            return new BatchResult([], failed);
        }

        foreach (var member in removed)
        {
            await store.Rows.SoftDeleteAsync(member, context.CancellationToken).ConfigureAwait(false);
        }

        var users = await store.UsersAsync([caller.UserId, .. removed.Select(static m => m.UserId)], context.CancellationToken).ConfigureAwait(false);
        var text = texts.Render(context, ChatMessages.SystemMembersRemoved, ("operator", users.GetValueOrDefault(caller.UserId)?.UserName), ("names", texts.Names(context, [.. removed.Select(m => users.GetValueOrDefault(m.UserId)?.UserName)])));
        var removedIds = removed.Select(static m => m.UserId).ToList();
        var remaining = current.Keys.Except(removedIds).ToList();
        var announced = await AnnounceAsync(context, caller, conversationId, text, ChatRealtimeMethods.ChangeTypes.MemberRemoved, remaining, remaining.Count).ConfigureAwait(false);
        if (announced.IsFailure)
        {
            return announced.Error!;
        }

        await push.ChangedAsync(caller.Side, conversationId, ChatRealtimeMethods.ChangeTypes.MemberRemoved, removedIds, context.CancellationToken).ConfigureAwait(false);
        return new BatchResult(removedIds, failed);
    }

    /// <summary>
    /// 退群（只在群聊，群主须先转让）
    /// </summary>
    public async Task<Result> LeaveAsync(InvocationContext context, ChatCaller caller, Guid conversationId)
    {
        var found = await store.ConversationAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var conversation))
        {
            return found.Error!;
        }

        if (conversation.Type != ChatConversationType.Group)
        {
            return Error.Validation(ChatErrors.GroupOnly, ChatMessages.GroupOnly);
        }

        if (conversation.OwnerUserId == caller.UserId)
        {
            return Error.Validation(ChatErrors.OwnerCannotLeave, ChatMessages.OwnerCannotLeave);
        }

        var member = await store.RequireMemberAsync(caller.Side, conversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (!member.TryGetValue(out var leaving))
        {
            return member.Error!;
        }

        await store.Rows.SoftDeleteAsync(leaving, context.CancellationToken).ConfigureAwait(false);
        var users = await store.UsersAsync([caller.UserId], context.CancellationToken).ConfigureAwait(false);
        var text = texts.Render(context, ChatMessages.SystemMemberLeft, ("name", users.GetValueOrDefault(caller.UserId)?.UserName));
        var remaining = await store.MemberIdsAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false);
        var announced = await AnnounceAsync(context, caller, conversationId, text, ChatRealtimeMethods.ChangeTypes.MemberRemoved, remaining, remaining.Count).ConfigureAwait(false);
        if (announced.IsFailure)
        {
            return announced.Error!;
        }

        await push.ChangedAsync(caller.Side, conversationId, ChatRealtimeMethods.ChangeTypes.MemberRemoved, [caller.UserId], context.CancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 改群信息
    /// </summary>
    /// <remarks>
    /// 群主或管理员；部门群不能改名，公告变了追加以操作人身份发出的公告
    /// </remarks>
    public async Task<Result> UpdateInfoAsync(InvocationContext context, ChatCaller caller, UpdateConversationInfoRequest request)
    {
        var found = await store.ConversationAsync(caller.Side, request.ConversationId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var conversation))
        {
            return found.Error!;
        }

        if (conversation.Type == ChatConversationType.Single)
        {
            return Error.Validation(ChatErrors.SingleUnsupported, ChatMessages.SingleUnsupported);
        }

        var manager = await store.RequireManagerAsync(caller.Side, conversation.Id, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (manager.IsFailure)
        {
            return manager.Error!;
        }

        if (!string.IsNullOrWhiteSpace(request.ConversationName))
        {
            if (conversation.Type == ChatConversationType.Department)
            {
                return Error.Validation(ChatErrors.DepartmentNameLocked, ChatMessages.DepartmentNameLocked);
            }

            conversation.Name = request.ConversationName.Trim();
        }

        var announcementChanged = false;
        if (request.Announcement is not null)
        {
            var announcement = ChatRules.Optional(request.Announcement, 2000);
            announcementChanged = !string.Equals(conversation.Announcement, announcement, StringComparison.Ordinal);
            conversation.Announcement = announcement;
        }

        if (request.Description is not null)
        {
            conversation.Description = ChatRules.Optional(request.Description, 500);
        }

        if (request.Avatar is not null)
        {
            var avatar = ChatRules.Optional(request.Avatar, 500);
            if (Guid.TryParse(avatar, out var avatarFileId))
            {
                // 文件主键统一存小写连字符形式，换地址时按它与会话头像比对
                avatar = avatarFileId.ToString();
            }

            var checkedAvatar = await messages.EnsureAvatarAsync(context, caller, conversation.Avatar, avatar).ConfigureAwait(false);
            if (checkedAvatar.IsFailure)
            {
                return checkedAvatar.Error!;
            }

            conversation.Avatar = avatar;
        }

        await store.Rows.UpdateAsync(conversation, context.CancellationToken).ConfigureAwait(false);
        var recipients = await store.MemberIdsAsync(caller.Side, conversation.Id, context.CancellationToken).ConfigureAwait(false);
        await push.ChangedAsync(caller.Side, conversation.Id, ChatRealtimeMethods.ChangeTypes.InfoChanged, recipients, context.CancellationToken).ConfigureAwait(false);
        if (announcementChanged && conversation.Announcement is { } text)
        {
            // 公告以操作人的身份进消息流（前端渲染为带发布人的公告卡片），正文是公告全文
            var users = await store.UsersAsync([caller.UserId], context.CancellationToken).ConfigureAwait(false);
            var preview = texts.Render(context, ChatMessages.SystemAnnouncement, ("text", ChatRules.Truncate(text, 60)));
            var system = await messages.AppendSystemAsync(context, caller.Side, conversation.Id, text, preview, caller.UserId, users.GetValueOrDefault(caller.UserId)?.UserName).ConfigureAwait(false);
            if (!system.TryGetValue(out var appended))
            {
                return system.Error!;
            }

            await push.MessageAsync(caller.Side, ChatStore.ItemOf(appended.Message), appended.Conversation, recipients, context.CancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    /// <summary>
    /// 转让群主
    /// </summary>
    /// <remarks>
    /// 只在群聊、只有群主；旧群主降为普通成员
    /// </remarks>
    public async Task<Result> TransferOwnerAsync(InvocationContext context, ChatCaller caller, Guid conversationId, Guid newOwnerUserId)
    {
        var found = await store.ConversationAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var conversation))
        {
            return found.Error!;
        }

        if (conversation.Type != ChatConversationType.Group)
        {
            return Error.Validation(ChatErrors.GroupOnly, ChatMessages.GroupOnly);
        }

        if (conversation.OwnerUserId != caller.UserId)
        {
            return Error.Denied(ChatErrors.OwnerRequired, ChatMessages.OwnerRequired);
        }

        if (newOwnerUserId == caller.UserId)
        {
            return Error.Validation(ChatErrors.SelfTarget, ChatMessages.SelfTarget);
        }

        var oldOwner = await store.RequireMemberAsync(caller.Side, conversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        var newOwner = await store.RequireMemberAsync(caller.Side, conversationId, newOwnerUserId, context.CancellationToken).ConfigureAwait(false);
        if (!oldOwner.TryGetValue(out var from) || !newOwner.TryGetValue(out var to))
        {
            return oldOwner.IsFailure ? oldOwner.Error! : newOwner.Error!;
        }

        from.Role = ChatMemberRole.Member;
        to.Role = ChatMemberRole.Owner;
        await store.Rows.UpdateRangeAsync([from, to], context.CancellationToken).ConfigureAwait(false);
        conversation.OwnerUserId = newOwnerUserId;
        await store.Rows.UpdateAsync(conversation, context.CancellationToken).ConfigureAwait(false);
        var users = await store.UsersAsync([caller.UserId, newOwnerUserId], context.CancellationToken).ConfigureAwait(false);
        var text = texts.Render(context, ChatMessages.SystemOwnerTransferred, ("from", users.GetValueOrDefault(caller.UserId)?.UserName), ("to", users.GetValueOrDefault(newOwnerUserId)?.UserName));
        var recipients = await store.MemberIdsAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false);
        var announced = await AnnounceAsync(context, caller, conversationId, text, ChatRealtimeMethods.ChangeTypes.OwnerTransferred, recipients, memberCount: null).ConfigureAwait(false);
        return announced.IsFailure ? announced.Error! : Result.Success();
    }

    /// <summary>
    /// 禁言或解除：逐个成功或失败
    /// </summary>
    /// <remarks>
    /// 群聊与部门群的群主或管理员；群主与管理员不能禁言
    /// </remarks>
    public async Task<Result<BatchResult>> SilenceAsync(InvocationContext context, ChatCaller caller, Guid conversationId, IReadOnlyList<Guid> userIds, bool isSilenced)
    {
        var found = await store.ConversationAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var conversation))
        {
            return found.Error!;
        }

        if (conversation.Type == ChatConversationType.Single)
        {
            return Error.Validation(ChatErrors.SingleUnsupported, ChatMessages.SingleUnsupported);
        }

        var manager = await store.RequireManagerAsync(caller.Side, conversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (manager.IsFailure)
        {
            return manager.Error!;
        }

        var current = await store.MembersAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false);
        var byUser = current.ToDictionary(static m => m.UserId);
        var changed = new List<SysChatConversationMember>();
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var userId in userIds.Distinct())
        {
            if (!byUser.TryGetValue(userId, out var target))
            {
                failed.Add(new BatchFailure(userId, ChatErrors.NotMember, ChatMessages.NotMember.L()));
                continue;
            }

            if (target.Role is ChatMemberRole.Owner or ChatMemberRole.Admin)
            {
                failed.Add(new BatchFailure(userId, ChatErrors.SilenceProtected, ChatMessages.SilenceProtected.L()));
                continue;
            }

            succeeded.Add(userId);
            if (target.IsSilenced != isSilenced)
            {
                target.IsSilenced = isSilenced;
                changed.Add(target);
            }
        }

        await store.Rows.UpdateRangeAsync(changed, context.CancellationToken).ConfigureAwait(false);
        if (succeeded.Count > 0)
        {
            await push.ChangedAsync(caller.Side, conversationId, ChatRealtimeMethods.ChangeTypes.MemberSilenced, [.. current.Select(static m => m.UserId)], context.CancellationToken).ConfigureAwait(false);
        }

        return new BatchResult(succeeded, failed);
    }

    /// <summary>
    /// 设管理员或取消（只在群聊、只有群主，在管理员与普通成员之间切换）
    /// </summary>
    public async Task<Result<ChatMemberItem>> SetRoleAsync(InvocationContext context, ChatCaller caller, Guid conversationId, Guid userId, ChatMemberRole role)
    {
        var found = await store.ConversationAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var conversation))
        {
            return found.Error!;
        }

        if (conversation.Type != ChatConversationType.Group)
        {
            return Error.Validation(ChatErrors.GroupOnly, ChatMessages.GroupOnly);
        }

        if (conversation.OwnerUserId != caller.UserId)
        {
            return Error.Denied(ChatErrors.OwnerRequired, ChatMessages.OwnerRequired);
        }

        if (userId == caller.UserId)
        {
            return Error.Validation(ChatErrors.SelfTarget, ChatMessages.SelfTarget);
        }

        if (role is not (ChatMemberRole.Admin or ChatMemberRole.Member))
        {
            return Error.Validation(ChatErrors.RoleInvalid, ChatMessages.RoleInvalid);
        }

        var member = await store.RequireMemberAsync(caller.Side, conversationId, userId, context.CancellationToken).ConfigureAwait(false);
        if (!member.TryGetValue(out var target))
        {
            return member.Error!;
        }

        if (target.Role == ChatMemberRole.Owner)
        {
            return Error.Denied(ChatErrors.OwnerRoleLocked, ChatMessages.OwnerRoleLocked);
        }

        if (target.Role != role)
        {
            target.Role = role;
            await store.Rows.UpdateAsync(target, context.CancellationToken).ConfigureAwait(false);
        }

        var recipients = await store.MemberIdsAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false);
        await push.ChangedAsync(caller.Side, conversationId, ChatRealtimeMethods.ChangeTypes.MemberRoleChanged, recipients, context.CancellationToken).ConfigureAwait(false);
        var users = await store.UsersAsync([userId], context.CancellationToken).ConfigureAwait(false);
        var user = users.GetValueOrDefault(userId);
        return new ChatMemberItem(userId, user?.UserName, user?.DisplayName, target.Role, target.IsSilenced, target.JoinedAt);
    }

    /// <summary>
    /// 会话置顶或免打扰（个人设置，只推本人的其他端）
    /// </summary>
    public async Task<Result<ChatToggleResponse>> SetPersonalAsync(InvocationContext context, ChatCaller caller, Guid conversationId, bool? isPinned, bool? isMuted)
    {
        var member = await store.RequireMemberAsync(caller.Side, conversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (!member.TryGetValue(out var mine))
        {
            return member.Error!;
        }

        mine.IsPinned = isPinned ?? mine.IsPinned;
        mine.IsMuted = isMuted ?? mine.IsMuted;
        await store.Rows.UpdateAsync(mine, context.CancellationToken).ConfigureAwait(false);
        await push.ChangedAsync(caller.Side, conversationId, ChatRealtimeMethods.ChangeTypes.MemberSettingChanged, [caller.UserId], context.CancellationToken).ConfigureAwait(false);
        return new ChatToggleResponse(isPinned.HasValue ? mine.IsPinned : mine.IsMuted);
    }

    /// <summary>
    /// 部门对应的部门群
    /// </summary>
    public async Task<SysChatConversation?> DepartmentConversationAsync(string side, Guid departmentId, CancellationToken cancellationToken)
    {
        var found = await store.Conversations(side).Where(c => c.DepartmentId == departmentId && c.Type == ChatConversationType.Department).OrderBy(c => c.CreatedAt).Take(1).ToListAsync(cancellationToken).ConfigureAwait(false);
        return found.Count == 0 ? null : found[0];
    }

    /// <summary>
    /// 加成员行
    /// </summary>
    public Task AddMemberRowsAsync(string side, Guid conversationId, IReadOnlyCollection<Guid> userIds, Guid? ownerUserId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var rows = userIds.Select(userId => new SysChatConversationMember
        {
            Id = ids.NewId(),
            TenantId = side,
            ConversationId = conversationId,
            UserId = userId,
            Role = ownerUserId == userId ? ChatMemberRole.Owner : ChatMemberRole.Member,
            JoinedAt = now,
        }).ToList();
        return store.Rows.InsertRangeAsync(rows, cancellationToken);
    }

    /// <summary>
    /// 重算会话的成员数
    /// </summary>
    public async Task<Result<SysChatConversation>> RecountAsync(string side, Guid conversationId, CancellationToken cancellationToken)
    {
        var found = await store.ConversationAsync(side, conversationId, cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var conversation))
        {
            return found;
        }

        conversation.MemberCount = (await store.MemberIdsAsync(side, conversationId, cancellationToken).ConfigureAwait(false)).Count;
        await store.Rows.UpdateAsync(conversation, cancellationToken).ConfigureAwait(false);
        return conversation;
    }

    private static ChatConversationOpenResponse OpenedOf(SysChatConversation conversation, bool created) => new(conversation.Id, conversation.Type, conversation.Name, created);

    /// <summary>
    /// 群聊里的治理：会话存在、是群聊、调用方是群主或管理员
    /// </summary>
    private async Task<Result<SysChatConversation>> GroupForManagerAsync(InvocationContext context, ChatCaller caller, Guid conversationId)
    {
        var found = await store.ConversationAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var conversation))
        {
            return found;
        }

        if (conversation.Type != ChatConversationType.Group)
        {
            return Error.Validation(ChatErrors.GroupOnly, ChatMessages.GroupOnly);
        }

        var manager = await store.RequireManagerAsync(caller.Side, conversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        return manager.IsFailure ? manager.Error! : conversation;
    }

    /// <summary>
    /// 治理之后：写中性的系统提示（可同时重算成员数），推会话变更与提示给接收人
    /// </summary>
    private async Task<Result> AnnounceAsync(InvocationContext context, ChatCaller caller, Guid conversationId, string text, string changeType, IReadOnlyCollection<Guid> recipients, int? memberCount)
    {
        var system = await messages.AppendSystemAsync(context, caller.Side, conversationId, text, text, senderUserId: null, senderName: null, memberCount).ConfigureAwait(false);
        if (!system.TryGetValue(out var appended))
        {
            return system.Error!;
        }

        await push.ChangedAsync(caller.Side, conversationId, changeType, recipients, context.CancellationToken).ConfigureAwait(false);
        await push.MessageAsync(caller.Side, ChatStore.ItemOf(appended.Message), appended.Conversation, recipients, context.CancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
