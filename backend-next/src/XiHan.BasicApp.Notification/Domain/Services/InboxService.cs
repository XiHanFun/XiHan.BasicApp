// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Domain.Services;

/// <summary>
/// 本人收件箱的写入。
/// </summary>
/// <remarks>
/// 标已读、全部已读、确认（同时标已读）、记下弹窗已显示、删除（软删除，通知本身不受影响）。
/// 都只动调用方本人在当前一侧的条目，别人的按不存在处理。
/// </remarks>
/// <param name="rows">按侧读写。</param>
/// <param name="clock">时钟。</param>
public sealed class InboxService(SideRows rows, TimeProvider clock)
{
    /// <summary>
    /// 取本人的一条。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="id">收件箱条目 ID。</param>
    /// <returns>条目；不存在即 <c>NOTIFICATION_NOTFOUND_002</c>。</returns>
    public async Task<Result<SysUserNotification>> FindMineAsync(InvocationContext context, Guid id)
    {
        ArgumentNullException.ThrowIfNull(context);
        var me = NotificationService.CallerOf(context) ?? Guid.Empty;
        var found = await rows.FindAsync<SysUserNotification>(SideRows.SideOf(context), id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var row) && row.UserId == me
            ? row
            : Error.NotFound(NotificationErrors.InboxItemNotFound, NotificationMessages.InboxItemNotFound, MessageArguments.Of(("id", id)));
    }

    /// <summary>
    /// 标已读（已读的照常算成功）。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="ids">收件箱条目 ID。</param>
    /// <returns>批量结果。</returns>
    public async Task<BatchResult> MarkReadAsync(InvocationContext context, IReadOnlyList<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(ids);
        var (mine, failed) = await MineAsync(context, ids).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        var changed = mine.Where(static r => r.Status == UserNotificationStatus.Unread).ToList();
        foreach (var row in changed)
        {
            Read(row, now);
        }

        await rows.UpdateRangeAsync(changed, context.CancellationToken).ConfigureAwait(false);
        return new BatchResult(mine.Select(static r => r.Id).ToList(), failed);
    }

    /// <summary>
    /// 把本人此刻可见的未读全部标为已读。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="visibleUnread">此刻可见的未读条目（由收件箱查询给出）。</param>
    /// <returns>标了多少条。</returns>
    public async Task<int> MarkAllReadAsync(InvocationContext context, IReadOnlyList<Guid> visibleUnread)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(visibleUnread);
        if (visibleUnread.Count == 0)
        {
            return 0;
        }

        var me = NotificationService.CallerOf(context) ?? Guid.Empty;
        var wanted = visibleUnread.ToList();
        var unread = await rows.Query<SysUserNotification>(SideRows.SideOf(context))
            .Where(u => u.UserId == me && u.Status == UserNotificationStatus.Unread && wanted.Contains(u.Id))
            .ToListAsync(context.CancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        foreach (var row in unread)
        {
            Read(row, now);
        }

        await rows.UpdateRangeAsync(unread, context.CancellationToken).ConfigureAwait(false);
        return unread.Count;
    }

    /// <summary>
    /// 确认一条（同时标已读）。
    /// </summary>
    /// <remarks>
    /// 确认过的保留原确认时间。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="row">本人的条目。</param>
    /// <returns>结果。</returns>
    public async Task ConfirmAsync(InvocationContext context, SysUserNotification row)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(row);
        var now = clock.GetUtcNow();
        Read(row, now);
        row.ConfirmedAt ??= now;
        await rows.UpdateAsync(row, context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 记下弹窗已显示（之后不再弹出）。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="row">本人的条目。</param>
    /// <returns>结果。</returns>
    public async Task MarkPopupShownAsync(InvocationContext context, SysUserNotification row)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(row);
        if (row.PopupShownAt is not null)
        {
            return;
        }

        row.PopupShownAt = clock.GetUtcNow();
        await rows.UpdateAsync(row, context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 从本人收件箱里删除。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="ids">收件箱条目 ID。</param>
    /// <returns>批量结果。</returns>
    public async Task<BatchResult> DeleteAsync(InvocationContext context, IReadOnlyList<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(ids);
        var (mine, failed) = await MineAsync(context, ids).ConfigureAwait(false);
        foreach (var row in mine)
        {
            await rows.SoftDeleteAsync(row, context.CancellationToken).ConfigureAwait(false);
        }

        return new BatchResult(mine.Select(static r => r.Id).ToList(), failed);
    }

    private static void Read(SysUserNotification row, DateTimeOffset now)
    {
        if (row.Status == UserNotificationStatus.Read)
        {
            return;
        }

        row.Status = UserNotificationStatus.Read;
        row.ReadAt = now;
    }

    private async Task<(List<SysUserNotification> Mine, List<BatchFailure> Failed)> MineAsync(InvocationContext context, IReadOnlyList<Guid> ids)
    {
        var me = NotificationService.CallerOf(context) ?? Guid.Empty;
        var wanted = ids.Distinct().ToList();
        var found = (await rows.Query<SysUserNotification>(SideRows.SideOf(context))
            .Where(u => wanted.Contains(u.Id) && u.UserId == me)
            .ToListAsync(context.CancellationToken).ConfigureAwait(false)).ToDictionary(static u => u.Id);
        var failed = wanted.Where(id => !found.ContainsKey(id)).Select(static id => new BatchFailure(id, NotificationErrors.InboxItemNotFound)).ToList();
        return ([.. wanted.Where(found.ContainsKey).Select(id => found[id])], failed);
    }
}
