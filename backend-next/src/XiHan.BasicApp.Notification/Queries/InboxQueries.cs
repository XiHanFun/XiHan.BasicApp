// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Notification.Operations.Inbox;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Queries;

/// <summary>
/// 横幅、弹窗与强制阅读的取法。
/// </summary>
public enum InboxShowcase
{
    /// <summary>
    /// 未读的横幅。
    /// </summary>
    Banners = 0,

    /// <summary>
    /// 还没弹过的弹窗。
    /// </summary>
    Popups = 1,

    /// <summary>
    /// 未读的强制阅读。
    /// </summary>
    Mandatory = 2,
}

/// <summary>
/// 本人收件箱的查询。
/// </summary>
/// <remarks>
/// 只看当前一侧、通知已发布且未删除、此刻在可见期内（开始时间已到、未到期；4.x 不看开始时间）。
/// 收件箱与通知在同一个租户库里，按通知 ID 联表。
/// </remarks>
/// <param name="unitOfWork">工作单元。</param>
/// <param name="clock">时钟。</param>
public sealed class InboxQueries(SqlSugarUnitOfWork unitOfWork, TimeProvider clock)
{
    private const string FieldStatus = "status";
    private const string FieldNotificationType = "notificationType";
    private const string FieldPending = "pending";
    private const string FieldPublishedAt = "publishedAt";

    /// <summary>
    /// 横幅、弹窗与强制阅读一次至多取多少条。
    /// </summary>
    public const int MaxShowcase = 100;

    // 可空时间列与 OR 一起判空时 SqlSugar 会译丢条件，统一换成「为空取哨兵值」再比较（4.x 同）
    private static readonly DateTimeOffset _sentinel = DateTimeOffset.UnixEpoch;

    /// <summary>
    /// 分页列出本人的收件箱。
    /// </summary>
    /// <remarks>
    /// 关键字匹配标题与正文；筛选接受阅读状态与类型的 <c>eq / in</c>、<c>pending eq true</c>（未读或需要确认而未确认）；
    /// 排序只接受发布时间，缺省倒序。
    /// </remarks>
    /// <param name="request">分页请求。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>一页收件箱。</returns>
    public async ValueTask<Result<PageResult<InboxItemResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = Visible(context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where((u, n) => n.Title.Contains(keyword) || n.Content!.Contains(keyword));
        }

        foreach (var filter in request.Filters)
        {
            var value = filter.Value ?? string.Empty;
            var sentinel = _sentinel;
            ISugarQueryable<SysUserNotification, SysNotification>? filtered = (filter.Field, filter.Operator) switch
            {
                (FieldStatus, "eq" or "in") when Enums.Of<UserNotificationStatus>(value) is { } statuses => query.Where((u, n) => statuses.Contains(u.Status)),
                (FieldNotificationType, "eq" or "in") when Enums.Of<NotificationType>(value) is { } types => query.Where((u, n) => types.Contains(n.NotificationType)),
                (FieldPending, "eq") when bool.TryParse(value, out var pending) && pending => query.Where((u, n) => u.Status == UserNotificationStatus.Unread || (n.NeedConfirm && SqlFunc.IsNull(u.ConfirmedAt, sentinel) == sentinel)),
                _ => null,
            };
            if (filtered is null)
            {
                return Error.Validation(NotificationErrors.QueryUnsupported, NotificationMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));
            }

            query = filtered;
        }

        var ascending = false;
        foreach (var sort in request.Sorts)
        {
            if (!string.Equals(sort.Field, FieldPublishedAt, StringComparison.Ordinal))
            {
                return Error.Validation(NotificationErrors.QueryUnsupported, NotificationMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            ascending = !sort.Descending;
        }

        var total = new RefAsync<int>();
        var page = await Project(query.OrderBy((u, n) => n.PublishedAt, ascending ? OrderByType.Asc : OrderByType.Desc))
            .ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<InboxItemResponse>(page.Select(static r => r.ToResponse()).ToList(), total.Value, request.Page);
    }

    /// <summary>
    /// 横幅、弹窗或强制阅读（发布时间倒序，至多 <see cref="MaxShowcase"/> 条）。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="showcase">取法。</param>
    /// <returns>条目。</returns>
    public async Task<IReadOnlyList<InboxItemResponse>> ShowcaseAsync(InvocationContext context, InboxShowcase showcase)
    {
        ArgumentNullException.ThrowIfNull(context);
        var sentinel = _sentinel;
        var query = Visible(context);
        query = showcase switch
        {
            InboxShowcase.Banners => query.Where((u, n) => n.IsBanner && u.Status == UserNotificationStatus.Unread),
            InboxShowcase.Popups => query.Where((u, n) => n.IsPopup && SqlFunc.IsNull(u.PopupShownAt, sentinel) == sentinel),
            _ => query.Where((u, n) => n.IsMandatory && u.Status == UserNotificationStatus.Unread),
        };
        var rows = await Project(query.OrderBy((u, n) => n.PublishedAt, OrderByType.Desc)).Take(MaxShowcase).ToListAsync(context.CancellationToken).ConfigureAwait(false);
        return rows.Select(static r => r.ToResponse()).ToList();
    }

    /// <summary>
    /// 此刻可见的未读条目 ID。
    /// </summary>
    /// <remarks>
    /// 全部已读只标这些：开始时间还没到的留着未读，到时再出现。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <returns>条目 ID。</returns>
    public async Task<IReadOnlyList<Guid>> VisibleUnreadAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await Visible(context).Where((u, n) => u.Status == UserNotificationStatus.Unread).Select((u, n) => u.Id).ToListAsync(context.CancellationToken).ConfigureAwait(false);
    }

    private ISugarQueryable<SysUserNotification, SysNotification> Visible(InvocationContext context)
    {
        var side = SideRows.SideOf(context);
        var me = NotificationService.CallerOf(context) ?? Guid.Empty;
        var now = clock.GetUtcNow();
        var past = now.AddYears(-100);
        var future = now.AddYears(100);
        return unitOfWork.ConnectionFor(typeof(SysUserNotification)).Queryable<SysUserNotification>()
            .InnerJoin<SysNotification>((u, n) => u.NotificationId == n.Id)
            .Where((u, n) => u.TenantId == side && !u.IsDeleted && u.UserId == me && n.TenantId == side && !n.IsDeleted && n.IsPublished)
            .Where((u, n) => SqlFunc.IsNull(n.StartsAt, past) <= now && SqlFunc.IsNull(n.ExpiresAt, future) > now);
    }

    private static ISugarQueryable<InboxRow> Project(ISugarQueryable<SysUserNotification, SysNotification> query) => query.Select((u, n) => new InboxRow
    {
        Id = u.Id,
        NotificationId = n.Id,
        Title = n.Title,
        Content = n.Content,
        ContentFormat = n.ContentFormat,
        NotificationType = n.NotificationType,
        Priority = n.Priority,
        TargetType = n.TargetType,
        Status = u.Status,
        PublishedAt = n.PublishedAt,
        ReadAt = u.ReadAt,
        ConfirmedAt = u.ConfirmedAt,
        NeedConfirm = n.NeedConfirm,
        IsMandatory = n.IsMandatory,
        IsBanner = n.IsBanner,
        IsPopup = n.IsPopup,
        Icon = n.Icon,
        Link = n.Link,
        ExpiresAt = n.ExpiresAt,
    });

    /// <summary>
    /// 联表的投影（SqlSugar 要可写属性）。
    /// </summary>
    private sealed class InboxRow
    {
        public Guid Id { get; set; }

        public Guid NotificationId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        public NotificationContentFormat ContentFormat { get; set; }

        public NotificationType NotificationType { get; set; }

        public NotificationPriority Priority { get; set; }

        public NotificationTargetType TargetType { get; set; }

        public UserNotificationStatus Status { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }

        public DateTimeOffset? ReadAt { get; set; }

        public DateTimeOffset? ConfirmedAt { get; set; }

        public bool NeedConfirm { get; set; }

        public bool IsMandatory { get; set; }

        public bool IsBanner { get; set; }

        public bool IsPopup { get; set; }

        public string? Icon { get; set; }

        public string? Link { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }

        public InboxItemResponse ToResponse() => new(
            Id, NotificationId, Title, Content, ContentFormat, NotificationType, Priority, Status, PublishedAt ?? DateTimeOffset.MinValue, ReadAt, ConfirmedAt,
            TargetType == NotificationTargetType.All, NeedConfirm, IsMandatory, IsBanner, IsPopup, Icon, Link, ExpiresAt);
    }
}
