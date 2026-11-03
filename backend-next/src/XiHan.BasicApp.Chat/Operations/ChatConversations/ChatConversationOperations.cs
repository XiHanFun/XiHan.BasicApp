// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.BasicApp.Chat.Queries;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Operations.ChatConversations;

/// <summary>
/// 我的会话
/// </summary>
/// <remarks>
/// <c>GET /api/chat-conversations:mine</c>（4.x <c>ChatQuery/MyConversations</c>）。置顶优先，再按最后消息时间倒序；单聊显示对端的用户名
/// </remarks>
/// <param name="queries">聊天的查询</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsRead)]
public sealed class ListMyChatConversations(ChatQueries queries) : IOperation<ChatEmptyRequest, ChatConversationsResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.mine";

    /// <inheritdoc />
    public async ValueTask<Result<ChatConversationsResponse>> HandleAsync(ChatEmptyRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await queries.MineAsync(context, me).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 会话成员
/// </summary>
/// <remarks>
/// <c>GET /api/chat-conversations:members?conversationId=</c>（4.x <c>ChatQuery/Members</c>）。只给成员看
/// </remarks>
/// <param name="queries">聊天的查询</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsRead)]
public sealed class ListChatConversationMembers(ChatQueries queries) : IOperation<ChatConversationRequest, ChatMembersResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.members";

    /// <inheritdoc />
    public async ValueTask<Result<ChatMembersResponse>> HandleAsync(ChatConversationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await queries.MembersAsync(context, me, request.ConversationId).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 成员的已读位
/// </summary>
/// <remarks>
/// <c>GET /api/chat-conversations:read-positions?conversationId=</c>（4.x <c>ChatQuery/ReadPositions</c>）。群的已读回执与单聊的对端已读
/// </remarks>
/// <param name="queries">聊天的查询</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsRead)]
public sealed class ListChatReadPositions(ChatQueries queries) : IOperation<ChatConversationRequest, ChatReadPositionsResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.read-positions";

    /// <inheritdoc />
    public async ValueTask<Result<ChatReadPositionsResponse>> HandleAsync(ChatConversationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await queries.ReadPositionsAsync(context, me, request.ConversationId).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 发起部门群用的部门树
/// </summary>
/// <remarks>
/// <c>GET /api/chat-conversations:department-tree</c>（4.x <c>ChatQuery/DepartmentTree</c>）。当前一侧启用的部门，不用组织的部门读权限；平台没有部门
/// </remarks>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsRead)]
public sealed class ChatDepartmentTree : IOperation<ChatEmptyRequest, ChatDepartmentTreeResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.department-tree";

    /// <inheritdoc />
    public async ValueTask<Result<ChatDepartmentTreeResponse>> HandleAsync(ChatEmptyRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await ChatQueries.DepartmentTreeAsync(context).ConfigureAwait(false);
    }
}

/// <summary>
/// 发起聊天与加人用的选人
/// </summary>
/// <remarks>
/// <c>GET /api/chat-conversations:user-options?keyword=&amp;limit=</c>（4.x <c>ChatQuery/UserOptions</c>）。当前一侧的正常账号，不用身份的用户读权限
/// </remarks>
/// <param name="queries">聊天的查询</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsRead)]
public sealed class ChatUserOptions(ChatQueries queries) : IOperation<ChatUserOptionsRequest, ChatUserOptionsResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.user-options";

    /// <inheritdoc />
    public async ValueTask<Result<ChatUserOptionsResponse>> HandleAsync(ChatUserOptionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await queries.UserOptionsAsync(context, me, request).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 打开单聊
/// </summary>
/// <remarks>
/// <c>POST /api/chat-conversations:open-single</c>（4.x <c>Chat/OpenSingleConversation</c>）。同一对用户本侧只有一个单聊，没有即建；对端须属于当前一侧
/// </remarks>
/// <param name="service">会话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsRead)]
public sealed class OpenSingleChatConversation(ChatConversationService service) : IOperation<OpenSingleConversationRequest, ChatConversationOpenResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.open-single";

    /// <inheritdoc />
    public async ValueTask<Result<ChatConversationOpenResponse>> HandleAsync(OpenSingleConversationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.OpenSingleAsync(context, me, request.PeerUserId).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 可用的助手
/// </summary>
/// <remarks>
/// <c>GET /api/chat-conversations:assistants</c>（4.x <c>ChatAssistant/Available</c>）。AI 关掉时 <c>CHAT_UNAVAILABLE_001</c>，前端据此隐藏助手入口
/// </remarks>
/// <param name="service">助手服务</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsRead)]
public sealed class ChatConversationAssistants(ChatAssistantService service) : IOperation<ChatAssistantsRequest, ChatAssistantsResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.assistants";

    /// <inheritdoc />
    public async ValueTask<Result<ChatAssistantsResponse>> HandleAsync(ChatAssistantsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.ListAsync(context).ConfigureAwait(false);
    }
}

/// <summary>
/// 打开与助手的会话
/// </summary>
/// <remarks>
/// <c>POST /api/chat-conversations:open-assistant</c>（4.x <c>ChatAssistant/OpenConversation</c>）。一个用户对一个助手只有一个会话，没有即建、发开场白；助手不存在或已停用照 AI 的错误码；AI 关掉时 <c>CHAT_UNAVAILABLE_001</c>
/// </remarks>
/// <param name="service">助手服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsRead)]
public sealed class OpenAssistantChatConversation(ChatAssistantService service) : IOperation<OpenAssistantConversationRequest, ChatAssistantOpenResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.open-assistant";

    /// <inheritdoc />
    public async ValueTask<Result<ChatAssistantOpenResponse>> HandleAsync(OpenAssistantConversationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.OpenAsync(context, me, request.AssistantId).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 建群
/// </summary>
/// <remarks>
/// <c>POST /api/chat-conversations:create-group</c>（4.x <c>Chat/GroupConversation</c>）。含群主至少两人、发起人为群主，追加建群提示并推给成员
/// </remarks>
/// <param name="service">会话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsManage)]
public sealed class CreateGroupChatConversation(ChatConversationService service) : IOperation<CreateGroupConversationRequest, ChatConversationOpenResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.create-group";

    /// <inheritdoc />
    public async ValueTask<Result<ChatConversationOpenResponse>> HandleAsync(CreateGroupConversationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.CreateGroupAsync(context, me, request.ConversationName, request.MemberUserIds).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 打开部门群
/// </summary>
/// <remarks>
/// <c>POST /api/chat-conversations:open-department</c>（4.x <c>Chat/OpenDepartmentConversation</c>）。只有本部门的成员能进，没有即建，打开时把部门成员补进群；部门群没有群主与管理员
/// </remarks>
/// <param name="service">会话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsRead)]
public sealed class OpenDepartmentChatConversation(ChatConversationService service) : IOperation<OpenDepartmentConversationRequest, ChatConversationOpenResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.open-department";

    /// <inheritdoc />
    public async ValueTask<Result<ChatConversationOpenResponse>> HandleAsync(OpenDepartmentConversationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.OpenDepartmentAsync(context, me, request.DepartmentId).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 加群成员
/// </summary>
/// <remarks>
/// <c>POST /api/chat-conversations:add-members</c>（4.x <c>Chat/Members</c>）。只在群聊、群主或管理员；新成员须属于当前一侧，追加邀请提示
/// </remarks>
/// <param name="service">会话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsManage)]
public sealed class AddChatConversationMembers(ChatConversationService service) : IOperation<ChatConversationUsersRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.add-members";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(ChatConversationUsersRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.AddMembersAsync(context, me, request.ConversationId, request.UserIds).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 移出群成员
/// </summary>
/// <remarks>
/// <c>POST /api/chat-conversations:remove-members</c>（4.x <c>Chat/Member</c> 单个）。只在群聊、群主或管理员；不是成员、群主与自己逐项失败
/// </remarks>
/// <param name="service">会话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsManage)]
public sealed class RemoveChatConversationMembers(ChatConversationService service) : IOperation<ChatConversationUsersRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.remove-members";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(ChatConversationUsersRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.RemoveMembersAsync(context, me, request.ConversationId, request.UserIds).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 退群
/// </summary>
/// <remarks>
/// <c>POST /api/chat-conversations:leave</c>（4.x <c>Chat/LeaveConversation</c>）。只在群聊，群主须先转让
/// </remarks>
/// <param name="service">会话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsRead)]
public sealed class LeaveChatConversation(ChatConversationService service) : IOperation<ChatConversationRequest, ChatConversationIdResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.leave";

    /// <inheritdoc />
    public async ValueTask<Result<ChatConversationIdResponse>> HandleAsync(ChatConversationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        if (!caller.TryGetValue(out var me))
        {
            return caller.Error!;
        }

        var left = await service.LeaveAsync(context, me, request.ConversationId).ConfigureAwait(false);
        return left.IsFailure ? left.Error! : new ChatConversationIdResponse(request.ConversationId);
    }
}

/// <summary>
/// 改群信息
/// </summary>
/// <remarks>
/// <c>POST /api/chat-conversations:update-info</c>（4.x <c>Chat/ConversationInfo</c>）。群主或管理员；部门群不能改名，公告变了追加以操作人身份发出的公告
/// </remarks>
/// <param name="service">会话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsManage)]
public sealed class UpdateChatConversationInfo(ChatConversationService service) : IOperation<UpdateConversationInfoRequest, ChatConversationIdResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.update-info";

    /// <inheritdoc />
    public async ValueTask<Result<ChatConversationIdResponse>> HandleAsync(UpdateConversationInfoRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        if (!caller.TryGetValue(out var me))
        {
            return caller.Error!;
        }

        var updated = await service.UpdateInfoAsync(context, me, request).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : new ChatConversationIdResponse(request.ConversationId);
    }
}

/// <summary>
/// 转让群主
/// </summary>
/// <remarks>
/// <c>POST /api/chat-conversations:transfer-owner</c>（4.x <c>Chat/TransferOwner</c>）。只在群聊、只有群主，新群主须是成员
/// </remarks>
/// <param name="service">会话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsManage)]
public sealed class TransferChatConversationOwner(ChatConversationService service) : IOperation<TransferOwnerRequest, ChatConversationIdResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.transfer-owner";

    /// <inheritdoc />
    public async ValueTask<Result<ChatConversationIdResponse>> HandleAsync(TransferOwnerRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        if (!caller.TryGetValue(out var me))
        {
            return caller.Error!;
        }

        var transferred = await service.TransferOwnerAsync(context, me, request.ConversationId, request.NewOwnerUserId).ConfigureAwait(false);
        return transferred.IsFailure ? transferred.Error! : new ChatConversationIdResponse(request.ConversationId);
    }
}

/// <summary>
/// 禁言或解除
/// </summary>
/// <remarks>
/// <c>POST /api/chat-conversations:silence-members</c>（4.x <c>Chat/SetMemberSilence</c> 单个）。群聊与部门群的群主或管理员；不是成员、群主与管理员逐项失败
/// </remarks>
/// <param name="service">会话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsManage)]
public sealed class SilenceChatConversationMembers(ChatConversationService service) : IOperation<SilenceMembersRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.silence-members";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(SilenceMembersRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.SilenceAsync(context, me, request.ConversationId, request.UserIds, request.IsSilenced).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 设管理员或取消
/// </summary>
/// <remarks>
/// <c>POST /api/chat-conversations:set-member-role</c>（4.x <c>Chat/SetMemberRole</c>）。只在群聊、只有群主，在管理员与普通成员之间切换
/// </remarks>
/// <param name="service">会话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsManage)]
public sealed class SetChatConversationMemberRole(ChatConversationService service) : IOperation<SetMemberRoleRequest, ChatMemberItem>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.set-member-role";

    /// <inheritdoc />
    public async ValueTask<Result<ChatMemberItem>> HandleAsync(SetMemberRoleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.SetRoleAsync(context, me, request.ConversationId, request.UserId, request.MemberRole).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 会话置顶（个人设置）
/// </summary>
/// <remarks>
/// <c>POST /api/chat-conversations:pin</c>（4.x <c>Chat/TogglePinConversation</c> 是切换，6.0 请求带开关值）。只推本人的其他端
/// </remarks>
/// <param name="service">会话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsRead)]
public sealed class PinChatConversation(ChatConversationService service) : IOperation<PinConversationRequest, ChatToggleResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.pin";

    /// <inheritdoc />
    public async ValueTask<Result<ChatToggleResponse>> HandleAsync(PinConversationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.SetPersonalAsync(context, me, request.ConversationId, request.IsPinned, isMuted: null).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 会话免打扰（个人设置）
/// </summary>
/// <remarks>
/// <c>POST /api/chat-conversations:mute</c>（4.x <c>Chat/ToggleMuteConversation</c> 是切换，6.0 请求带开关值）。只推本人的其他端
/// </remarks>
/// <param name="service">会话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsRead)]
public sealed class MuteChatConversation(ChatConversationService service) : IOperation<MuteConversationRequest, ChatToggleResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.mute";

    /// <inheritdoc />
    public async ValueTask<Result<ChatToggleResponse>> HandleAsync(MuteConversationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.SetPersonalAsync(context, me, request.ConversationId, isPinned: null, request.IsMuted).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 标记已读
/// </summary>
/// <remarks>
/// <c>POST /api/chat-conversations:mark-read</c>（4.x <c>Chat/MarkRead</c>）。未读清零、已读位前移（不回退，没给即读到最新），已读位推给全体成员
/// </remarks>
/// <param name="service">消息服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatConversationsRead)]
public sealed class MarkChatConversationRead(ChatMessageService service) : IOperation<MarkReadRequest, ChatReadPosition>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-conversations.mark-read";

    /// <inheritdoc />
    public async ValueTask<Result<ChatReadPosition>> HandleAsync(MarkReadRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.MarkReadAsync(context, me, request.ConversationId, request.UpToMessageId).ConfigureAwait(false) : caller.Error!;
    }
}
