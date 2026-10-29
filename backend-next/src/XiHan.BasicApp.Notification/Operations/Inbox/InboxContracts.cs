// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Notification.Domain.Entities;

namespace XiHan.BasicApp.Notification.Operations.Inbox;

/// <summary>
/// 收件箱的一条（本人的，带通知的展示字段）。
/// </summary>
/// <param name="Id">收件箱这一条的 ID（标已读、确认、删除都用它）。</param>
/// <param name="NotificationId">通知 ID。</param>
/// <param name="Title">标题。</param>
/// <param name="Content">正文。</param>
/// <param name="ContentFormat">正文格式。</param>
/// <param name="NotificationType">类型。</param>
/// <param name="Priority">优先级。</param>
/// <param name="Status">阅读状态。</param>
/// <param name="PublishedAt">发布时间。</param>
/// <param name="ReadAt">阅读时间。</param>
/// <param name="ConfirmedAt">确认时间。</param>
/// <param name="IsGlobal">是否发给当前一侧的全部账号。</param>
/// <param name="NeedConfirm">需要确认。</param>
/// <param name="IsMandatory">强制阅读。</param>
/// <param name="IsBanner">横幅。</param>
/// <param name="IsPopup">登录后弹窗。</param>
/// <param name="Icon">图标。</param>
/// <param name="Link">链接。</param>
/// <param name="ExpiresAt">到期时间。</param>
public sealed record InboxItemResponse(
    Guid Id,
    Guid NotificationId,
    string Title,
    string? Content,
    NotificationContentFormat ContentFormat,
    NotificationType NotificationType,
    NotificationPriority Priority,
    UserNotificationStatus Status,
    DateTimeOffset PublishedAt,
    DateTimeOffset? ReadAt,
    DateTimeOffset? ConfirmedAt,
    bool IsGlobal,
    bool NeedConfirm,
    bool IsMandatory,
    bool IsBanner,
    bool IsPopup,
    string? Icon,
    string? Link,
    DateTimeOffset? ExpiresAt);

/// <summary>
/// 横幅、弹窗与强制阅读的请求（没有参数）。
/// </summary>
public sealed record InboxShowcaseRequest;

/// <summary>
/// 横幅、弹窗与强制阅读的结果（至多 100 条，发布时间倒序）。
/// </summary>
/// <param name="Items">条目。</param>
public sealed record InboxItemsResponse(IReadOnlyList<InboxItemResponse> Items);

/// <summary>
/// 按 ID 操作收件箱一条的请求。
/// </summary>
/// <remarks>
/// 标识由路由值 <c>{id}</c> 绑定。
/// </remarks>
/// <param name="Id">收件箱这一条的 ID。</param>
public sealed record InboxIdRequest(Guid Id);

/// <summary>
/// 批量操作收件箱的请求。
/// </summary>
/// <param name="Ids">收件箱条目 ID，1 到 100 个。</param>
public sealed record InboxIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// <c>inbox.mark-all-read</c> 的请求（没有参数）。
/// </summary>
public sealed record MarkAllInboxReadRequest;

/// <summary>
/// 影响的条数。
/// </summary>
/// <param name="Count">条数。</param>
public sealed record InboxCountResponse(int Count);
