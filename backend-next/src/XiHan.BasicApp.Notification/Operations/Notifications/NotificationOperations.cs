// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Notification.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Notification.Operations.Notifications;

/// <summary>
/// 分页列出当前一侧的通知公告：<c>POST /api/notifications:list</c>。
/// </summary>
/// <param name="queries">通知查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.NotificationsRead)]
public sealed class ListNotifications(NotificationQueries queries) : IOperation<PageRequest, PageResult<NotificationResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "notifications.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<NotificationResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 取一条通知：<c>GET /api/notifications/{id}</c>。
/// </summary>
/// <param name="service">通知服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.NotificationsRead)]
public sealed class GetNotification(NotificationService service) : IOperation<NotificationIdRequest, NotificationResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "notifications.get";

    /// <inheritdoc />
    public async ValueTask<Result<NotificationResponse>> HandleAsync(NotificationIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(context, request.Id).ConfigureAwait(false);
        return found.TryGetValue(out var notification) ? NotificationResponse.From(notification) : found.Error!;
    }
}

/// <summary>
/// 新建通知：<c>POST /api/notifications</c>。缺省存为草稿；<c>publishImmediately</c> 即建好就发布，这时另要持有发布权限（<c>NOTIFICATION_DENIED_001</c>）。
/// </summary>
/// <param name="service">通知服务。</param>
/// <param name="permissions">权限检查器（判定立即发布）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.NotificationsCreate)]
public sealed class CreateNotification(NotificationService service, IPermissionChecker permissions) : IOperation<CreateNotificationRequest, NotificationResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "notifications.create";

    /// <inheritdoc />
    public async ValueTask<Result<NotificationResponse>> HandleAsync(CreateNotificationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (request.PublishImmediately && !await permissions.HasPermissionAsync(context.GetSecurity(), NotificationPermissions.NotificationsPublish, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Denied(NotificationErrors.PublishDenied, NotificationMessages.PublishDenied);
        }

        var created = await service.CreateAsync(context, request.ToDraft()).ConfigureAwait(false);
        if (!created.TryGetValue(out var notification))
        {
            return created.Error!;
        }

        if (request.PublishImmediately)
        {
            var published = await service.PublishAsync(context, notification).ConfigureAwait(false);
            if (published.IsFailure)
            {
                return published.Error!;
            }
        }

        return NotificationResponse.From(notification);
    }
}

/// <summary>
/// 修改草稿：<c>PUT /api/notifications/{id}</c>。整体覆盖，已发布的 <c>NOTIFICATION_CONFLICT_001</c>。
/// </summary>
/// <param name="service">通知服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.NotificationsUpdate)]
public sealed class UpdateNotification(NotificationService service) : IOperation<UpdateNotificationRequest, NotificationResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "notifications.update";

    /// <inheritdoc />
    public async ValueTask<Result<NotificationResponse>> HandleAsync(UpdateNotificationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(context, request.Id).ConfigureAwait(false);
        if (!found.TryGetValue(out var notification))
        {
            return found.Error!;
        }

        var updated = await service.UpdateAsync(context, notification, request.ToDraft()).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : NotificationResponse.From(notification);
    }
}

/// <summary>
/// 批量删除：<c>DELETE /api/notifications?ids=</c>。已发布的同时从接收人的收件箱里移除。
/// </summary>
/// <param name="service">通知服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.NotificationsDelete)]
public sealed class DeleteNotifications(NotificationService service) : IOperation<NotificationIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "notifications.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(NotificationIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.DeleteAsync(context, request.Ids).ConfigureAwait(false);
    }
}

/// <summary>
/// 发布：<c>POST /api/notifications/{id}:publish</c>。解析接收人、写收件箱，提交后实时推送；重复发布 <c>NOTIFICATION_CONFLICT_001</c>，找不到接收人 <c>NOTIFICATION_VALIDATION_004</c>。
/// </summary>
/// <param name="service">通知服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.NotificationsPublish)]
public sealed class PublishNotification(NotificationService service) : IOperation<NotificationIdRequest, NotificationDeliveryResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "notifications.publish";

    /// <inheritdoc />
    public async ValueTask<Result<NotificationDeliveryResponse>> HandleAsync(NotificationIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(context, request.Id).ConfigureAwait(false);
        if (!found.TryGetValue(out var notification))
        {
            return found.Error!;
        }

        var published = await service.PublishAsync(context, notification).ConfigureAwait(false);
        return published.TryGetValue(out var count) ? new NotificationDeliveryResponse(notification.Id, count, notification.PublishedAt!.Value) : published.Error!;
    }
}

/// <summary>
/// 提醒：<c>POST /api/notifications/{id}:remind</c>。再推给未读或未确认的接收人；草稿 <c>NOTIFICATION_CONFLICT_002</c>。
/// </summary>
/// <param name="service">通知服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.NotificationsPublish)]
public sealed class RemindNotification(NotificationService service) : IOperation<NotificationIdRequest, NotificationDeliveryResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "notifications.remind";

    /// <inheritdoc />
    public async ValueTask<Result<NotificationDeliveryResponse>> HandleAsync(NotificationIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(context, request.Id).ConfigureAwait(false);
        if (!found.TryGetValue(out var notification))
        {
            return found.Error!;
        }

        var reminded = await service.RemindAsync(context, notification).ConfigureAwait(false);
        return reminded.TryGetValue(out var count) ? new NotificationDeliveryResponse(notification.Id, count, notification.PublishedAt!.Value) : reminded.Error!;
    }
}

/// <summary>
/// 阅读统计：<c>GET /api/notifications/{id}:read-stats</c>。
/// </summary>
/// <param name="service">通知服务。</param>
/// <param name="queries">通知查询。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = NotificationPermissions.NotificationsRead)]
public sealed class GetNotificationReadStats(NotificationService service, NotificationQueries queries) : IOperation<NotificationIdRequest, NotificationReadStatsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "notifications.read-stats";

    /// <inheritdoc />
    public async ValueTask<Result<NotificationReadStatsResponse>> HandleAsync(NotificationIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(context, request.Id).ConfigureAwait(false);
        if (!found.TryGetValue(out var notification))
        {
            return found.Error!;
        }

        return await queries.ReadStatsAsync(context, notification).ConfigureAwait(false);
    }
}

/// <summary>
/// 接收人：<c>POST /api/notifications/{id}:recipients</c>。分页列出接收人与阅读、确认状态（4.x 的「未读用户」）；草稿 <c>NOTIFICATION_CONFLICT_002</c>。
/// </summary>
/// <param name="service">通知服务。</param>
/// <param name="queries">通知查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = NotificationPermissions.NotificationsRead)]
public sealed class ListNotificationRecipients(NotificationService service, NotificationQueries queries) : IOperation<NotificationRecipientsRequest, PageResult<NotificationRecipientResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "notifications.recipients";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<NotificationRecipientResponse>>> HandleAsync(NotificationRecipientsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(context, request.Id).ConfigureAwait(false);
        if (!found.TryGetValue(out var notification))
        {
            return found.Error!;
        }

        if (!notification.IsPublished)
        {
            return Error.Conflict(NotificationErrors.NotPublished, NotificationMessages.NotPublished, MessageArguments.Of(("id", notification.Id)));
        }

        return await queries.RecipientsAsync(context, notification, request.Filter, request.Page ?? PageInfo.Default).ConfigureAwait(false);
    }
}
