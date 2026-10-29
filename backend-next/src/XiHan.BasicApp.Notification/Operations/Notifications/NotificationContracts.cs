// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;

namespace XiHan.BasicApp.Notification.Operations.Notifications;

/// <summary>
/// 一条通知公告。
/// </summary>
/// <param name="Id">通知 ID。</param>
/// <param name="Title">标题。</param>
/// <param name="Content">正文。</param>
/// <param name="ContentFormat">正文格式。</param>
/// <param name="NotificationType">类型。</param>
/// <param name="Priority">优先级。</param>
/// <param name="Icon">图标。</param>
/// <param name="Link">链接。</param>
/// <param name="BusinessType">关联的业务类型。</param>
/// <param name="BusinessId">关联的业务 ID。</param>
/// <param name="TargetType">投递目标。</param>
/// <param name="TargetIds">目标 ID（角色、部门或用户）。</param>
/// <param name="NeedConfirm">需要确认。</param>
/// <param name="IsMandatory">强制阅读。</param>
/// <param name="IsBanner">横幅。</param>
/// <param name="IsPopup">登录后弹窗。</param>
/// <param name="StartsAt">可见起始时间。</param>
/// <param name="ExpiresAt">到期时间。</param>
/// <param name="IsPublished">是否已发布。</param>
/// <param name="PublishedAt">发布时间。</param>
/// <param name="PublisherId">发布人。</param>
/// <param name="RecipientCount">接收人数。</param>
/// <param name="Remark">备注。</param>
/// <param name="CreatedAt">创建时间。</param>
/// <param name="ModifiedAt">修改时间。</param>
public sealed record NotificationResponse(
    Guid Id,
    string Title,
    string? Content,
    NotificationContentFormat ContentFormat,
    NotificationType NotificationType,
    NotificationPriority Priority,
    string? Icon,
    string? Link,
    string? BusinessType,
    string? BusinessId,
    NotificationTargetType TargetType,
    IReadOnlyList<Guid> TargetIds,
    bool NeedConfirm,
    bool IsMandatory,
    bool IsBanner,
    bool IsPopup,
    DateTimeOffset? StartsAt,
    DateTimeOffset? ExpiresAt,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    Guid? PublisherId,
    int RecipientCount,
    string? Remark,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    /// <param name="n">通知。</param>
    /// <returns>响应。</returns>
    public static NotificationResponse From(SysNotification n)
    {
        ArgumentNullException.ThrowIfNull(n);
        return new NotificationResponse(
            n.Id, n.Title, n.Content, n.ContentFormat, n.NotificationType, n.Priority, n.Icon, n.Link, n.BusinessType, n.BusinessId,
            n.TargetType, NotificationTargets.Parse(n.TargetIds), n.NeedConfirm, n.IsMandatory, n.IsBanner, n.IsPopup, n.StartsAt, n.ExpiresAt,
            n.IsPublished, n.PublishedAt, n.PublisherId, n.RecipientCount, n.Remark, n.CreatedAt, n.ModifiedAt);
    }
}

/// <summary>
/// <c>notifications.create</c> 的请求。
/// </summary>
/// <param name="Title">标题。</param>
/// <param name="Content">正文。</param>
/// <param name="ContentFormat">正文格式（缺省 Markdown）。</param>
/// <param name="NotificationType">类型（缺省系统公告）。</param>
/// <param name="Priority">优先级（缺省普通）。</param>
/// <param name="TargetType">投递目标（缺省全部）。</param>
/// <param name="TargetIds">目标 ID（角色、部门或用户）；全部为空即可。</param>
/// <param name="Icon">图标。</param>
/// <param name="Link">链接。</param>
/// <param name="BusinessType">关联的业务类型。</param>
/// <param name="BusinessId">关联的业务 ID。</param>
/// <param name="NeedConfirm">需要确认。</param>
/// <param name="IsMandatory">强制阅读。</param>
/// <param name="IsBanner">横幅。</param>
/// <param name="IsPopup">登录后弹窗。</param>
/// <param name="StartsAt">可见起始时间。</param>
/// <param name="ExpiresAt">到期时间。</param>
/// <param name="Remark">备注。</param>
/// <param name="PublishImmediately">建好即发布（另需发布权限）。</param>
public sealed record CreateNotificationRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [StringLength(200_000)] string? Content = null,
    NotificationContentFormat ContentFormat = NotificationContentFormat.Markdown,
    NotificationType NotificationType = NotificationType.System,
    NotificationPriority Priority = NotificationPriority.Normal,
    NotificationTargetType TargetType = NotificationTargetType.All,
    [MaxLength(5000)] IReadOnlyList<Guid>? TargetIds = null,
    [StringLength(100)] string? Icon = null,
    [StringLength(500)] string? Link = null,
    [StringLength(100)] string? BusinessType = null,
    [StringLength(100)] string? BusinessId = null,
    bool NeedConfirm = false,
    bool IsMandatory = false,
    bool IsBanner = false,
    bool IsPopup = false,
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? ExpiresAt = null,
    [StringLength(500)] string? Remark = null,
    bool PublishImmediately = false)
{
    /// <summary>
    /// 转成草稿内容。
    /// </summary>
    /// <returns>草稿。</returns>
    public NotificationDraft ToDraft() => new(
        Title, Content, ContentFormat, NotificationType, Priority, TargetType, TargetIds ?? [], Icon, Link, BusinessType, BusinessId,
        NeedConfirm, IsMandatory, IsBanner, IsPopup, StartsAt, ExpiresAt, Remark);
}

/// <summary>
/// <c>notifications.update</c> 的请求：整体覆盖草稿。标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">通知 ID。</param>
/// <param name="Title">标题。</param>
/// <param name="Content">正文。</param>
/// <param name="ContentFormat">正文格式（缺省 Markdown）。</param>
/// <param name="NotificationType">类型（缺省系统公告）。</param>
/// <param name="Priority">优先级（缺省普通）。</param>
/// <param name="TargetType">投递目标（缺省全部）。</param>
/// <param name="TargetIds">目标 ID（角色、部门或用户）；全部为空即可。</param>
/// <param name="Icon">图标。</param>
/// <param name="Link">链接。</param>
/// <param name="BusinessType">关联的业务类型。</param>
/// <param name="BusinessId">关联的业务 ID。</param>
/// <param name="NeedConfirm">需要确认。</param>
/// <param name="IsMandatory">强制阅读。</param>
/// <param name="IsBanner">横幅。</param>
/// <param name="IsPopup">登录后弹窗。</param>
/// <param name="StartsAt">可见起始时间。</param>
/// <param name="ExpiresAt">到期时间。</param>
/// <param name="Remark">备注。</param>
public sealed record UpdateNotificationRequest(
    Guid Id,
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [StringLength(200_000)] string? Content = null,
    NotificationContentFormat ContentFormat = NotificationContentFormat.Markdown,
    NotificationType NotificationType = NotificationType.System,
    NotificationPriority Priority = NotificationPriority.Normal,
    NotificationTargetType TargetType = NotificationTargetType.All,
    [MaxLength(5000)] IReadOnlyList<Guid>? TargetIds = null,
    [StringLength(100)] string? Icon = null,
    [StringLength(500)] string? Link = null,
    [StringLength(100)] string? BusinessType = null,
    [StringLength(100)] string? BusinessId = null,
    bool NeedConfirm = false,
    bool IsMandatory = false,
    bool IsBanner = false,
    bool IsPopup = false,
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? ExpiresAt = null,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成草稿内容。
    /// </summary>
    /// <returns>草稿。</returns>
    public NotificationDraft ToDraft() => new(
        Title, Content, ContentFormat, NotificationType, Priority, TargetType, TargetIds ?? [], Icon, Link, BusinessType, BusinessId,
        NeedConfirm, IsMandatory, IsBanner, IsPopup, StartsAt, ExpiresAt, Remark);
}

/// <summary>
/// 按 ID 取一条通知的请求。标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">通知 ID。</param>
public sealed record NotificationIdRequest(Guid Id);

/// <summary>
/// 批量删除的请求。
/// </summary>
/// <param name="Ids">通知 ID，1 到 100 个。</param>
public sealed record NotificationIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// 发布与提醒的结果。
/// </summary>
/// <param name="Id">通知 ID。</param>
/// <param name="RecipientCount">这次送达的接收人数（发布：全部接收人；提醒：未读或未确认的）。</param>
/// <param name="PublishedAt">发布时间。</param>
public sealed record NotificationDeliveryResponse(Guid Id, int RecipientCount, DateTimeOffset PublishedAt);

/// <summary>
/// 阅读统计。
/// </summary>
/// <param name="Id">通知 ID。</param>
/// <param name="RecipientCount">接收人数（收件箱里还留着的）。</param>
/// <param name="ReadCount">已读。</param>
/// <param name="UnreadCount">未读。</param>
/// <param name="NeedConfirm">是否需要确认。</param>
/// <param name="ConfirmedCount">已确认。</param>
public sealed record NotificationReadStatsResponse(Guid Id, int RecipientCount, int ReadCount, int UnreadCount, bool NeedConfirm, int ConfirmedCount);

/// <summary>
/// 接收人列表要看的范围。
/// </summary>
public enum NotificationRecipientFilter
{
    /// <summary>
    /// 全部接收人。
    /// </summary>
    All = 0,

    /// <summary>
    /// 未读的。
    /// </summary>
    Unread = 1,

    /// <summary>
    /// 已读的。
    /// </summary>
    Read = 2,

    /// <summary>
    /// 需要确认而还没确认的。
    /// </summary>
    Unconfirmed = 3,
}

/// <summary>
/// <c>notifications.recipients</c> 的请求。标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">通知 ID。</param>
/// <param name="Page">分页（缺省第 1 页 20 条）。</param>
/// <param name="Filter">范围。</param>
public sealed record NotificationRecipientsRequest(Guid Id, PageInfo? Page = null, NotificationRecipientFilter Filter = NotificationRecipientFilter.All);

/// <summary>
/// 一个接收人。
/// </summary>
/// <param name="UserId">用户 ID。</param>
/// <param name="UserName">登录名。</param>
/// <param name="DisplayName">显示名。</param>
/// <param name="Status">阅读状态。</param>
/// <param name="ReadAt">阅读时间。</param>
/// <param name="ConfirmedAt">确认时间。</param>
/// <param name="ReceivedAt">送达时间。</param>
public sealed record NotificationRecipientResponse(Guid UserId, string UserName, string DisplayName, UserNotificationStatus Status, DateTimeOffset? ReadAt, DateTimeOffset? ConfirmedAt, DateTimeOffset ReceivedAt);
