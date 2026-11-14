// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat.Operations.ChatConversations;
using XiHan.BasicApp.Chat.Queries;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Operations.ChatAudit;

/// <summary>
/// 聊天审计的消息列表
/// </summary>
/// <remarks>
/// <c>POST /api/chat-audit:list</c>。当前一侧全部会话的消息，不要求是成员；筛选与排序见 <see cref="ChatAuditQueries"/>
/// </remarks>
/// <param name="queries">审计查询</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatAuditRead)]
public sealed class ListChatAuditMessages(ChatAuditQueries queries) : IOperation<PageRequest, PageResult<ChatAuditMessageResponse>>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-audit.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<ChatAuditMessageResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 聊天审计的通话记录
/// </summary>
/// <remarks>
/// <c>POST /api/chat-audit:list-calls</c>。当前一侧全部会话的通话，不要求是成员；只有状态与参与者，筛选与排序见 <see cref="ChatAuditQueries"/>
/// </remarks>
/// <param name="queries">审计查询</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = ChatPermissions.ChatAuditRead)]
public sealed class ListChatAuditCalls(ChatAuditQueries queries) : IOperation<PageRequest, PageResult<ChatAuditCallResponse>>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-audit.list-calls";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<ChatAuditCallResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.CallsAsync(request, context);
    }
}

/// <summary>
/// 聊天审计的消息详情
/// </summary>
/// <remarks>
/// <c>GET /api/chat-audit/{id}</c>（方案 H4）。消息、所在会话、附件、提及、回复与表情回应；撤回与编辑前的正文不另留
/// </remarks>
/// <param name="queries">审计查询</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatAuditRead)]
public sealed class GetChatAuditMessage(ChatAuditQueries queries) : IOperation<GetChatAuditMessageRequest, ChatAuditMessageDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-audit.get";

    /// <inheritdoc />
    public async ValueTask<Result<ChatAuditMessageDetailResponse>> HandleAsync(GetChatAuditMessageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await queries.GetAsync(context, request.Id).ConfigureAwait(false);
    }
}

/// <summary>
/// 聊天审计的会话成员
/// </summary>
/// <remarks>
/// <c>GET /api/chat-audit:members?conversationId=</c>（方案 H4）。会话与它现在的成员，不要求是成员
/// </remarks>
/// <param name="queries">审计查询</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = ChatPermissions.ChatAuditRead)]
public sealed class ListChatAuditMembers(ChatAuditQueries queries) : IOperation<ChatConversationRequest, ChatAuditMembersResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-audit.members";

    /// <inheritdoc />
    public async ValueTask<Result<ChatAuditMembersResponse>> HandleAsync(ChatConversationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await queries.MembersAsync(context, request.ConversationId).ConfigureAwait(false);
    }
}
