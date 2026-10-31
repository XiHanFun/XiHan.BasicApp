// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Workbench;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Queries;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Notification.Infrastructure;

/// <summary>
/// 通知包给工作台的小组件。
/// </summary>
/// <remarks>
/// 公告取本人收件箱里最近的几条（与收件箱同一口径，人人可见）；消息发送统计只看当前一侧的发件记录。
/// </remarks>
/// <param name="inbox">收件箱查询。</param>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
/// <param name="permissions">权限判定（短信发件记录另有权限）。</param>
/// <param name="clock">时钟。</param>
public sealed class NotificationWidgets(InboxQueries inbox, SqlSugarUnitOfWork unitOfWork, IPermissionChecker permissions, TimeProvider clock) : IWorkbenchWidgetProvider
{
    /// <summary>
    /// 公告（本人收件箱里最近的几条）。
    /// </summary>
    public const string Announcements = "notification.announcements";

    /// <summary>
    /// 消息发送（邮件与短信按发送状态的条数）。
    /// </summary>
    public const string Deliveries = "notification.deliveries";

    /// <summary>
    /// 公告最多几条。
    /// </summary>
    private const int AnnouncementLimit = 6;

    /// <inheritdoc />
    public IReadOnlyList<WorkbenchWidget> Widgets { get; } =
    [
        new(Announcements, WorkbenchWidgetKind.List, null),
        new(Deliveries, WorkbenchWidgetKind.Chart, NotificationPermissions.EmailsRead),
    ];

    /// <inheritdoc />
    public async ValueTask<Result<WorkbenchWidgetData>> GetAsync(string key, WorkbenchWidgetQuery query, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);
        return key switch
        {
            Announcements => await AnnouncementsAsync(context).ConfigureAwait(false),
            Deliveries => await DeliveriesAsync(query, context).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null),
        };
    }

    /// <summary>
    /// 本人收件箱里最近发布的几条。
    /// </summary>
    /// <remarks>
    /// 条目的标识是收件箱这一条的 ID，类别是通知类型，时间是发布时间。
    /// </remarks>
    private async Task<Result<WorkbenchWidgetData>> AnnouncementsAsync(InvocationContext context)
    {
        var page = await inbox.ListAsync(new PageRequest { Page = new PageInfo { Index = 1, Size = AnnouncementLimit } }, context).ConfigureAwait(false);
        if (!page.TryGetValue(out var items))
        {
            return page.Error!;
        }

        return new WorkbenchWidgetData(Items: items.Items
            .Select(static i => new WorkbenchItem(i.Id.ToString(), i.Title, i.NotificationType.ToString(), i.PublishedAt, i.Link))
            .ToList());
    }

    /// <summary>
    /// 统计区间里邮件与短信按发送状态的条数。
    /// </summary>
    /// <remarks>
    /// 类目是 <c>email</c> 与 <c>sms</c>（没有短信发件记录的权限时只有邮件），序列是成功、失败、待发（含发送中）与取消。
    /// </remarks>
    private async Task<WorkbenchWidgetData> DeliveriesAsync(WorkbenchWidgetQuery query, InvocationContext context)
    {
        var owner = SideRows.SideOf(context);
        var from = query.StartOfRange(clock.GetUtcNow());
        var channels = new List<string> { "email" };
        var emails = await unitOfWork.ConnectionFor(typeof(SysEmail)).Queryable<SysEmail>()
            .Where(e => !e.IsDeleted && e.OwnerTenantId == owner && e.CreatedAt >= from)
            .GroupBy(e => e.Status)
            .Select(e => new StatusCount { Status = e.Status, Count = SqlFunc.AggregateCount(e.Id) })
            .ToListAsync(context.CancellationToken).ConfigureAwait(false);
        var counts = new List<List<StatusCount>> { emails };
        if (await permissions.HasPermissionAsync(context.GetSecurity(), NotificationPermissions.SmsRead, context.CancellationToken).ConfigureAwait(false))
        {
            channels.Add("sms");
            counts.Add(await unitOfWork.ConnectionFor(typeof(SysSms)).Queryable<SysSms>()
                .Where(s => !s.IsDeleted && s.OwnerTenantId == owner && s.CreatedAt >= from)
                .GroupBy(s => s.Status)
                .Select(s => new StatusCount { Status = s.Status, Count = SqlFunc.AggregateCount(s.Id) })
                .ToListAsync(context.CancellationToken).ConfigureAwait(false));
        }

        static long Of(List<StatusCount> rows, params MessageSendStatus[] statuses) => rows.Where(r => statuses.Contains(r.Status)).Sum(static r => r.Count);
        return new WorkbenchWidgetData(Series: new WorkbenchSeries(channels,
        [
            new("succeeded", counts.Select(c => Of(c, MessageSendStatus.Success)).ToList()),
            new("failed", counts.Select(c => Of(c, MessageSendStatus.Failed)).ToList()),
            new("pending", counts.Select(c => Of(c, MessageSendStatus.Pending, MessageSendStatus.Sending)).ToList()),
            new("cancelled", counts.Select(c => Of(c, MessageSendStatus.Cancelled)).ToList()),
        ]));
    }

    /// <summary>
    /// 一种发送状态的条数。
    /// </summary>
    private sealed class StatusCount
    {
        public MessageSendStatus Status { get; set; }

        public long Count { get; set; }
    }
}
