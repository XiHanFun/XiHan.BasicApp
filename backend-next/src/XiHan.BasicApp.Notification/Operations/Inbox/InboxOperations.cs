// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Notification.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Operations.Inbox;

/// <summary>
/// 分页列出本人的收件箱。
/// </summary>
/// <remarks>
/// <c>POST /api/inbox:list</c>（我的消息页与顶栏的通知中心都读它）。
/// </remarks>
/// <param name="queries">收件箱查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ListInbox(InboxQueries queries) : IOperation<PageRequest, PageResult<InboxItemResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "inbox.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<InboxItemResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 本人未读的横幅：<c>GET /api/inbox:banners</c>。
/// </summary>
/// <param name="queries">收件箱查询。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ListInboxBanners(InboxQueries queries) : IOperation<InboxShowcaseRequest, InboxItemsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "inbox.banners";

    /// <inheritdoc />
    public async ValueTask<Result<InboxItemsResponse>> HandleAsync(InboxShowcaseRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return new InboxItemsResponse(await queries.ShowcaseAsync(context, InboxShowcase.Banners).ConfigureAwait(false));
    }
}

/// <summary>
/// 本人还没弹过的弹窗：<c>GET /api/inbox:popups</c>。
/// </summary>
/// <param name="queries">收件箱查询。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ListInboxPopups(InboxQueries queries) : IOperation<InboxShowcaseRequest, InboxItemsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "inbox.popups";

    /// <inheritdoc />
    public async ValueTask<Result<InboxItemsResponse>> HandleAsync(InboxShowcaseRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return new InboxItemsResponse(await queries.ShowcaseAsync(context, InboxShowcase.Popups).ConfigureAwait(false));
    }
}

/// <summary>
/// 本人未读的强制阅读。
/// </summary>
/// <remarks>
/// <c>GET /api/inbox:mandatory</c>（路由守卫据此拦截）。
/// </remarks>
/// <param name="queries">收件箱查询。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ListInboxMandatory(InboxQueries queries) : IOperation<InboxShowcaseRequest, InboxItemsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "inbox.mandatory";

    /// <inheritdoc />
    public async ValueTask<Result<InboxItemsResponse>> HandleAsync(InboxShowcaseRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return new InboxItemsResponse(await queries.ShowcaseAsync(context, InboxShowcase.Mandatory).ConfigureAwait(false));
    }
}

/// <summary>
/// 标已读。
/// </summary>
/// <remarks>
/// <c>POST /api/inbox:mark-read</c>；不是本人的逐项 <c>NOTIFICATION_NOTFOUND_002</c>。
/// </remarks>
/// <param name="service">收件箱服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class MarkInboxRead(InboxService service) : IOperation<InboxIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "inbox.mark-read";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(InboxIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.MarkReadAsync(context, request.Ids).ConfigureAwait(false);
    }
}

/// <summary>
/// 全部已读。
/// </summary>
/// <remarks>
/// <c>POST /api/inbox:mark-all-read</c>（只标此刻可见的）。
/// </remarks>
/// <param name="service">收件箱服务。</param>
/// <param name="queries">收件箱查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class MarkInboxAllRead(InboxService service, InboxQueries queries) : IOperation<MarkAllInboxReadRequest, InboxCountResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "inbox.mark-all-read";

    /// <inheritdoc />
    public async ValueTask<Result<InboxCountResponse>> HandleAsync(MarkAllInboxReadRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var visible = await queries.VisibleUnreadAsync(context).ConfigureAwait(false);
        return new InboxCountResponse(await service.MarkAllReadAsync(context, visible).ConfigureAwait(false));
    }
}

/// <summary>
/// 确认：<c>POST /api/inbox/{id}:confirm</c>（同时标已读）。
/// </summary>
/// <param name="service">收件箱服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ConfirmInbox(InboxService service) : IOperation<InboxIdRequest, InboxCountResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "inbox.confirm";

    /// <inheritdoc />
    public async ValueTask<Result<InboxCountResponse>> HandleAsync(InboxIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindMineAsync(context, request.Id).ConfigureAwait(false);
        if (!found.TryGetValue(out var row))
        {
            return found.Error!;
        }

        await service.ConfirmAsync(context, row).ConfigureAwait(false);
        return new InboxCountResponse(1);
    }
}

/// <summary>
/// 记下弹窗已显示。
/// </summary>
/// <remarks>
/// <c>POST /api/inbox/{id}:mark-popup-shown</c>。
/// </remarks>
/// <param name="service">收件箱服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class MarkInboxPopupShown(InboxService service) : IOperation<InboxIdRequest, InboxCountResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "inbox.mark-popup-shown";

    /// <inheritdoc />
    public async ValueTask<Result<InboxCountResponse>> HandleAsync(InboxIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindMineAsync(context, request.Id).ConfigureAwait(false);
        if (!found.TryGetValue(out var row))
        {
            return found.Error!;
        }

        await service.MarkPopupShownAsync(context, row).ConfigureAwait(false);
        return new InboxCountResponse(1);
    }
}

/// <summary>
/// 从本人收件箱里删除。
/// </summary>
/// <remarks>
/// <c>DELETE /api/inbox?ids=</c>（方案 5.11 拍板补上的 <c>inbox.delete</c>，4.x 没有）。
/// </remarks>
/// <param name="service">收件箱服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class DeleteInbox(InboxService service) : IOperation<InboxIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "inbox.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(InboxIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.DeleteAsync(context, request.Ids).ConfigureAwait(false);
    }
}
