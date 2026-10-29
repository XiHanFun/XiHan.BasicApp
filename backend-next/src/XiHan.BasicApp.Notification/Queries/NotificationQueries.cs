// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Operations.Notifications;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Queries;

/// <summary>
/// 通知公告的查询：列表（当前一侧）、阅读统计与接收人。
/// </summary>
/// <param name="rows">按侧读写。</param>
/// <param name="directory">用户目录（接收人显示名；没有实现即只给 ID）。</param>
public sealed class NotificationQueries(SideRows rows, IEnumerable<IUserDirectory> directory)
{
    private const string FieldNotificationType = "notificationType";
    private const string FieldPriority = "priority";
    private const string FieldTargetType = "targetType";
    private const string FieldIsPublished = "isPublished";
    private const string FieldCreatedAt = "createdAt";
    private const string FieldPublishedAt = "publishedAt";

    private readonly IUserDirectory? _directory = directory.FirstOrDefault();

    /// <summary>
    /// 分页列出。关键字匹配标题与正文；筛选接受类型、优先级、目标的 <c>eq / in</c>，是否已发布的 <c>eq</c>，创建与发布时间的 <c>ge / le</c>；
    /// 排序接受创建时间、发布时间与优先级，缺省按创建时间倒序。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>一页通知。</returns>
    public async ValueTask<Result<PageResult<NotificationResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = rows.Query<SysNotification>(SideRows.SideOf(context));
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(n => n.Title.Contains(keyword) || n.Content!.Contains(keyword));
        }

        foreach (var filter in request.Filters)
        {
            var value = filter.Value ?? string.Empty;
            ISugarQueryable<SysNotification>? filtered = (filter.Field, filter.Operator) switch
            {
                (FieldNotificationType, "eq" or "in") when Enums.Of<NotificationType>(value) is { } types => query.Where(n => types.Contains(n.NotificationType)),
                (FieldPriority, "eq" or "in") when Enums.Of<NotificationPriority>(value) is { } priorities => query.Where(n => priorities.Contains(n.Priority)),
                (FieldTargetType, "eq" or "in") when Enums.Of<NotificationTargetType>(value) is { } targets => query.Where(n => targets.Contains(n.TargetType)),
                (FieldIsPublished, "eq") when bool.TryParse(value, out var published) => query.Where(n => n.IsPublished == published),
                (FieldCreatedAt, "ge") when DateTimeOffset.TryParse(value, out var from) => query.Where(n => n.CreatedAt >= from),
                (FieldCreatedAt, "le") when DateTimeOffset.TryParse(value, out var to) => query.Where(n => n.CreatedAt <= to),
                (FieldPublishedAt, "ge") when DateTimeOffset.TryParse(value, out var from) => query.Where(n => n.PublishedAt >= from),
                (FieldPublishedAt, "le") when DateTimeOffset.TryParse(value, out var to) => query.Where(n => n.PublishedAt <= to),
                _ => null,
            };
            if (filtered is null)
            {
                return Error.Validation(NotificationErrors.QueryUnsupported, NotificationMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));
            }

            query = filtered;
        }

        if (request.Sorts.Count == 0)
        {
            query = query.OrderBy(n => n.CreatedAt, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldCreatedAt => query.OrderBy(n => n.CreatedAt, direction),
                FieldPublishedAt => query.OrderBy(n => n.PublishedAt, direction),
                FieldPriority => query.OrderBy(n => n.Priority, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(NotificationErrors.QueryUnsupported, NotificationMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var page = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<NotificationResponse>(page.Select(NotificationResponse.From).ToList(), total.Value, request.Page);
    }

    /// <summary>
    /// 阅读统计（按收件箱里还留着的那些算）。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="notification">通知。</param>
    /// <returns>统计。</returns>
    public async Task<NotificationReadStatsResponse> ReadStatsAsync(InvocationContext context, SysNotification notification)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(notification);
        var inbox = await rows.Query<SysUserNotification>(SideRows.SideOf(context))
            .Where(u => u.NotificationId == notification.Id)
            .Select(static u => new InboxState { Status = u.Status, ConfirmedAt = u.ConfirmedAt })
            .ToListAsync(context.CancellationToken).ConfigureAwait(false);
        var read = inbox.Count(static u => u.Status == UserNotificationStatus.Read);
        return new NotificationReadStatsResponse(notification.Id, inbox.Count, read, inbox.Count - read, notification.NeedConfirm, inbox.Count(static u => u.ConfirmedAt is not null));
    }

    /// <summary>
    /// 分页列出接收人（送达时间倒序）。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="notification">已发布的通知。</param>
    /// <param name="filter">范围。</param>
    /// <param name="page">分页。</param>
    /// <returns>一页接收人。</returns>
    public async Task<PageResult<NotificationRecipientResponse>> RecipientsAsync(InvocationContext context, SysNotification notification, NotificationRecipientFilter filter, PageInfo page)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(page);
        var sentinel = DateTimeOffset.UnixEpoch;
        var query = rows.Query<SysUserNotification>(SideRows.SideOf(context)).Where(u => u.NotificationId == notification.Id);
        query = filter switch
        {
            NotificationRecipientFilter.Unread => query.Where(u => u.Status == UserNotificationStatus.Unread),
            NotificationRecipientFilter.Read => query.Where(u => u.Status == UserNotificationStatus.Read),
            NotificationRecipientFilter.Unconfirmed => query.Where(u => SqlFunc.IsNull(u.ConfirmedAt, sentinel) == sentinel),
            _ => query,
        };
        var total = new RefAsync<int>();
        var items = await query.OrderBy(u => u.CreatedAt, OrderByType.Desc).ToPageListAsync(page.Index, page.Size, total, context.CancellationToken).ConfigureAwait(false);
        var names = _directory is null
            ? new Dictionary<Guid, UserSummary>()
            : await _directory.FindAsync(items.Select(static u => u.UserId).ToList(), context.CancellationToken).ConfigureAwait(false);
        return new PageResult<NotificationRecipientResponse>(
            items.Select(u =>
            {
                names.TryGetValue(u.UserId, out var user);
                return new NotificationRecipientResponse(u.UserId, user?.UserName ?? string.Empty, user?.DisplayName ?? string.Empty, u.Status, u.ReadAt, u.ConfirmedAt, u.CreatedAt);
            }).ToList(),
            total.Value,
            page);
    }

    /// <summary>
    /// 统计用的投影。
    /// </summary>
    private sealed class InboxState
    {
        public UserNotificationStatus Status { get; set; }

        public DateTimeOffset? ConfirmedAt { get; set; }
    }
}
