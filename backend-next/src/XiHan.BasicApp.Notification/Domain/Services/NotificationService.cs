// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Domain.Services;

/// <summary>
/// 新建与修改共用的通知内容。
/// </summary>
/// <param name="Title">标题。</param>
/// <param name="Content">正文。</param>
/// <param name="ContentFormat">正文格式。</param>
/// <param name="NotificationType">类型。</param>
/// <param name="Priority">优先级。</param>
/// <param name="TargetType">投递目标。</param>
/// <param name="TargetIds">目标 ID。</param>
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
/// <param name="Channels">投递渠道。</param>
public sealed record NotificationDraft(
    string Title,
    string? Content,
    NotificationContentFormat ContentFormat,
    NotificationType NotificationType,
    NotificationPriority Priority,
    NotificationTargetType TargetType,
    IReadOnlyList<Guid> TargetIds,
    string? Icon,
    string? Link,
    string? BusinessType,
    string? BusinessId,
    bool NeedConfirm,
    bool IsMandatory,
    bool IsBanner,
    bool IsPopup,
    DateTimeOffset? StartsAt,
    DateTimeOffset? ExpiresAt,
    string? Remark,
    MessageChannels Channels);

/// <summary>
/// 投递渠道在请求与响应里按名称列出，在实体里按位组合。
/// </summary>
public static class NotificationChannelSet
{
    /// <summary>
    /// 列表 → 按位组合；没给即只有站内信。
    /// </summary>
    /// <param name="channels">渠道。</param>
    /// <returns>按位组合。</returns>
    public static MessageChannels FromList(IReadOnlyList<DeliveryChannel>? channels)
        => channels is null ? MessageChannels.InApp : channels.Aggregate(MessageChannels.None, static (all, c) => all | (MessageChannels)(int)c);

    /// <summary>
    /// 按位组合 → 列表（4.x 的老数据为 0 时按只有站内信）。
    /// </summary>
    /// <param name="channels">按位组合。</param>
    /// <returns>渠道。</returns>
    public static IReadOnlyList<DeliveryChannel> ToList(MessageChannels channels)
    {
        var effective = channels == MessageChannels.None ? MessageChannels.InApp : channels;
        return [.. Enum.GetValues<DeliveryChannel>().Where(c => effective.HasFlag((MessageChannels)(int)c))];
    }
}

/// <summary>
/// 目标 ID 在实体里存成 JSON 数组。
/// </summary>
public static class NotificationTargets
{
    /// <summary>
    /// 解析；空或坏的 JSON 即空列表。
    /// </summary>
    /// <param name="json">JSON。</param>
    /// <returns>目标 ID。</returns>
    public static IReadOnlyList<Guid> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize(json, NotificationJsonContext.Default.ListGuid) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>
    /// 序列化；全部目标或空列表存为 <see langword="null"/>。
    /// </summary>
    /// <param name="type">投递目标。</param>
    /// <param name="ids">目标 ID。</param>
    /// <returns>JSON。</returns>
    public static string? Serialize(NotificationTargetType type, IReadOnlyList<Guid> ids)
        => type == NotificationTargetType.All || ids.Count == 0 ? null : JsonSerializer.Serialize(ids.Distinct().ToList(), NotificationJsonContext.Default.ListGuid);
}

/// <summary>
/// <c>ReceiveNotification</c> 的载荷（与壳层 <c>use-signalr-integration</c> 读的字段一致）：<see cref="Type"/> 是提示的色调，
/// 由类型换算（紧急 → Error、安全 → Warning、业务 → Success、其余 Info）；各人收件箱的那一条 ID 不同，载荷里不带，客户端收到后重取收件箱。
/// </summary>
/// <param name="Type">提示色调：Info、Success、Warning、Error。</param>
/// <param name="Title">标题。</param>
/// <param name="Content">正文。</param>
/// <param name="ContentFormat">正文格式。</param>
/// <param name="NotificationId">通知 ID。</param>
/// <param name="NotificationType">类型。</param>
/// <param name="NotificationStatus">阅读状态（推送时都是未读）。</param>
/// <param name="SendTime">发布时间。</param>
public sealed record NotificationPushPayload(string Type, string Title, string? Content, NotificationContentFormat ContentFormat, Guid NotificationId, NotificationType NotificationType, UserNotificationStatus NotificationStatus, DateTimeOffset SendTime);

/// <summary>
/// 通知公告的规则：草稿可改可删；发布时按目标解析出当前一侧此刻正常的接收人（角色、部门的候选再经 <see cref="RecipientKind.User"/> 筛一遍），
/// 写进各自的收件箱，调用提交之后经实时连接推送；提醒只推给未读或未确认的；删除已发布的通知同时从收件箱里移除。
/// </summary>
/// <param name="rows">按侧读写。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="clock">时钟。</param>
/// <param name="sources">接收人来源。</param>
/// <param name="pushes">实时推送（没装 Realtime 即没有）。</param>
/// <param name="sender">邮件与短信的发送（扇出）。</param>
/// <param name="directory">用户目录（接收人的邮箱与手机号）。</param>
/// <param name="bots">机器人渠道的广播。</param>
/// <param name="preferences">用户的通知偏好（没装 Identity 即不过滤）。</param>
public sealed class NotificationService(SideRows rows, IIdGenerator<Guid> ids, TimeProvider clock, IEnumerable<IRecipientSource> sources, IEnumerable<IRealtimePush> pushes, IMessageSender sender, IEnumerable<IUserDirectory> directory, BotBroadcaster bots, IEnumerable<INotificationPreferences> preferences)
{
    /// <summary>
    /// 通知邮件的模板编码（4.x 同）。
    /// </summary>
    public const string EmailTemplate = "notification-email";

    /// <summary>
    /// 通知短信的模板编码（4.x 同）。
    /// </summary>
    public const string SmsTemplate = "notification-sms";

    private readonly IRecipientSource[] _sources = sources.ToArray();
    private readonly IRealtimePush? _push = pushes.FirstOrDefault();
    private readonly IUserDirectory? _directory = directory.FirstOrDefault();
    private readonly INotificationPreferences? _preferences = preferences.FirstOrDefault();

    /// <summary>
    /// 调用方本人（访问令牌的 <c>sub</c>）。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <returns>用户 ID；外部令牌不是本应用用户即为 <see langword="null"/>。</returns>
    public static Guid? CallerOf(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Guid.TryParse(context.GetSecurity().Principal.Id, out var id) ? id : null;
    }

    /// <summary>
    /// 校验草稿内容。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="draft">草稿。</param>
    /// <returns>结果。</returns>
    public static Result Validate(InvocationContext context, NotificationDraft draft)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(draft);
        if (string.IsNullOrWhiteSpace(draft.Title))
        {
            return Error.Validation(NotificationErrors.TitleBlank, NotificationMessages.TitleBlank);
        }

        if (draft.StartsAt is { } starts && draft.ExpiresAt is { } expires && expires <= starts)
        {
            return Error.Validation(NotificationErrors.WindowInvalid, NotificationMessages.WindowInvalid);
        }

        if (draft.TargetType != NotificationTargetType.All && draft.TargetIds.Count == 0)
        {
            return Error.Validation(NotificationErrors.TargetsRequired, NotificationMessages.TargetsRequired, MessageArguments.Of(("targetType", draft.TargetType.ToString())));
        }

        if (!draft.Channels.HasFlag(MessageChannels.InApp))
        {
            return Error.Validation(NotificationErrors.ChannelsInvalid, NotificationMessages.ChannelsInvalid);
        }

        if (draft.TargetType == NotificationTargetType.Department && context.GetSecurity().Tenant is null)
        {
            return Error.Validation(NotificationErrors.DepartmentNeedsTenant, NotificationMessages.DepartmentNeedsTenant);
        }

        return Result.Success();
    }

    /// <summary>
    /// 新建草稿。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="draft">内容。</param>
    /// <returns>新通知。</returns>
    public async Task<Result<SysNotification>> CreateAsync(InvocationContext context, NotificationDraft draft)
    {
        ArgumentNullException.ThrowIfNull(context);
        var valid = Validate(context, draft);
        if (valid.IsFailure)
        {
            return valid.Error!;
        }

        var notification = new SysNotification { Id = ids.NewId(), TenantId = SideRows.SideOf(context) };
        Apply(notification, draft);
        await rows.InsertAsync(notification, context.CancellationToken).ConfigureAwait(false);
        return notification;
    }

    /// <summary>
    /// 修改草稿；已发布的不能改。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="notification">通知。</param>
    /// <param name="draft">新内容。</param>
    /// <returns>结果。</returns>
    public async Task<Result> UpdateAsync(InvocationContext context, SysNotification notification, NotificationDraft draft)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(notification);
        if (notification.IsPublished)
        {
            return Published(notification.Id);
        }

        var valid = Validate(context, draft);
        if (valid.IsFailure)
        {
            return valid;
        }

        Apply(notification, draft);
        await rows.UpdateAsync(notification, context.CancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 按 ID 取当前一侧的通知。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="id">通知 ID。</param>
    /// <returns>通知；不存在即 <c>NOTIFICATION_NOTFOUND_001</c>。</returns>
    public async Task<Result<SysNotification>> FindAsync(InvocationContext context, Guid id)
    {
        ArgumentNullException.ThrowIfNull(context);
        var found = await rows.FindAsync<SysNotification>(SideRows.SideOf(context), id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var notification)
            ? notification
            : Error.NotFound(NotificationErrors.NotificationNotFound, NotificationMessages.NotificationNotFound, MessageArguments.Of(("id", id)));
    }

    /// <summary>
    /// 发布：解析接收人、写收件箱、提交后推送。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="notification">草稿。</param>
    /// <returns>送达结果。</returns>
    public async Task<Result<int>> PublishAsync(InvocationContext context, SysNotification notification)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(notification);
        if (notification.IsPublished)
        {
            return Published(notification.Id);
        }

        var resolved = await RecipientsAsync(context, notification).ConfigureAwait(false);
        if (!resolved.TryGetValue(out var recipients))
        {
            return resolved.Error!;
        }

        if (recipients.Count == 0)
        {
            return Error.Validation(NotificationErrors.NoRecipients, NotificationMessages.NoRecipients);
        }

        // 渠道先核对能发再写：开了邮件或短信却没有可用的配置与模板即整次发布失败，不悄悄只发站内信
        var channels = notification.DeliveryChannels == MessageChannels.None ? MessageChannels.InApp : notification.DeliveryChannels;
        foreach (var (flag, kind, template) in new[] { (MessageChannels.Email, MessageKind.Email, EmailTemplate), (MessageChannels.Sms, MessageKind.Sms, SmsTemplate) })
        {
            if (channels.HasFlag(flag) && !await sender.CanSendAsync(context, kind, template).ConfigureAwait(false))
            {
                return Error.Unavailable(NotificationErrors.ChannelUnavailable, NotificationMessages.ChannelUnavailable, MessageArguments.Of(("kind", kind.ToString())));
            }
        }

        if (channels.HasFlag(MessageChannels.Bot) && !await bots.CanBroadcastAsync(context).ConfigureAwait(false))
        {
            return Error.Unavailable(NotificationErrors.ChannelUnavailable, NotificationMessages.ChannelUnavailable, MessageArguments.Of(("kind", "Bot")));
        }

        // 通知偏好（4.x 同）：站内信只发给愿意收站内信的人，邮件与短信在扇出时各按自己的渠道筛；强制阅读与紧急通知不筛
        var side = SideRows.SideOf(context);
        var readers = await AcceptingAsync(notification, recipients, NotificationPreferenceChannel.InApp, context.CancellationToken).ConfigureAwait(false);
        var inbox = readers.Select(userId => new SysUserNotification { Id = ids.NewId(), TenantId = side, NotificationId = notification.Id, UserId = userId }).ToList();
        if (inbox.Count > 0)
        {
            await rows.InsertRangeAsync(inbox, context.CancellationToken).ConfigureAwait(false);
        }

        notification.IsPublished = true;
        notification.PublishedAt = clock.GetUtcNow();
        notification.PublisherId = CallerOf(context);
        notification.RecipientCount = readers.Count;
        await rows.UpdateAsync(notification, context.CancellationToken).ConfigureAwait(false);
        var fanned = await FanOutAsync(context, notification, channels, recipients).ConfigureAwait(false);
        if (fanned.IsFailure)
        {
            return fanned.Error!;
        }

        await PushAsync(context, notification, readers).ConfigureAwait(false);
        return readers.Count;
    }

    /// <summary>
    /// 提醒：再推给未读或（需要确认时）未确认的接收人。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="notification">已发布的通知。</param>
    /// <returns>推送的人数。</returns>
    public async Task<Result<int>> RemindAsync(InvocationContext context, SysNotification notification)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(notification);
        if (!notification.IsPublished)
        {
            return Error.Conflict(NotificationErrors.NotPublished, NotificationMessages.NotPublished, MessageArguments.Of(("id", notification.Id)));
        }

        var pending = await rows.Query<SysUserNotification>(SideRows.SideOf(context))
            .Where(u => u.NotificationId == notification.Id)
            .ToListAsync(context.CancellationToken).ConfigureAwait(false);
        var users = pending.Where(u => u.Status == UserNotificationStatus.Unread || (notification.NeedConfirm && u.ConfirmedAt is null)).Select(static u => u.UserId).ToList();
        await PushAsync(context, notification, users).ConfigureAwait(false);
        return users.Count;
    }

    /// <summary>
    /// 批量删除；已发布的连同收件箱里的那一条一起软删除。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="notificationIds">通知 ID。</param>
    /// <returns>批量结果。</returns>
    public async Task<BatchResult> DeleteAsync(InvocationContext context, IReadOnlyList<Guid> notificationIds)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(notificationIds);
        var side = SideRows.SideOf(context);
        var wanted = notificationIds.Distinct().ToList();
        var found = (await rows.Query<SysNotification>(side).Where(n => wanted.Contains(n.Id)).ToListAsync(context.CancellationToken).ConfigureAwait(false)).ToDictionary(static n => n.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in wanted)
        {
            if (!found.TryGetValue(id, out var notification))
            {
                failed.Add(new BatchFailure(id, NotificationErrors.NotificationNotFound));
                continue;
            }

            await rows.SoftDeleteAsync(notification, context.CancellationToken).ConfigureAwait(false);
            succeeded.Add(id);
        }

        var published = succeeded.Where(id => found[id].IsPublished).ToList();
        if (published.Count > 0)
        {
            var inbox = await rows.Query<SysUserNotification>(side).Where(u => published.Contains(u.NotificationId)).ToListAsync(context.CancellationToken).ConfigureAwait(false);
            var now = clock.GetUtcNow();
            foreach (var row in inbox)
            {
                row.IsDeleted = true;
                row.DeletedAt = now;
            }

            await rows.UpdateRangeAsync(inbox, context.CancellationToken).ConfigureAwait(false);
        }

        return new BatchResult(succeeded, failed);
    }

    private static Error Published(Guid id) => Error.Conflict(NotificationErrors.AlreadyPublished, NotificationMessages.AlreadyPublished, MessageArguments.Of(("id", id)));

    private static void Apply(SysNotification notification, NotificationDraft draft)
    {
        notification.Title = draft.Title.Trim();
        notification.Content = draft.Content;
        notification.ContentFormat = draft.ContentFormat;
        notification.NotificationType = draft.NotificationType;
        notification.Priority = draft.Priority;
        notification.TargetType = draft.TargetType;
        notification.TargetIds = NotificationTargets.Serialize(draft.TargetType, draft.TargetIds);
        notification.Icon = Blank(draft.Icon);
        notification.Link = Blank(draft.Link);
        notification.BusinessType = Blank(draft.BusinessType);
        notification.BusinessId = Blank(draft.BusinessId);
        notification.NeedConfirm = draft.NeedConfirm;
        notification.IsMandatory = draft.IsMandatory;
        notification.IsBanner = draft.IsBanner;
        notification.IsPopup = draft.IsPopup;
        notification.StartsAt = draft.StartsAt;
        notification.ExpiresAt = draft.ExpiresAt;
        notification.Remark = Blank(draft.Remark);
        notification.DeliveryChannels = draft.Channels;
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// 按目标找人：全部、角色与部门先各自给出候选，再与指定用户一样经 <see cref="RecipientKind.User"/> 只留当前一侧此刻正常的账号。
    /// </summary>
    private async Task<Result<IReadOnlyCollection<Guid>>> RecipientsAsync(InvocationContext context, SysNotification notification)
    {
        var kind = notification.TargetType switch
        {
            NotificationTargetType.Role => RecipientKind.Role,
            NotificationTargetType.Department => RecipientKind.Department,
            NotificationTargetType.User => RecipientKind.User,
            _ => RecipientKind.All,
        };
        var targets = NotificationTargets.Parse(notification.TargetIds);
        var draft = new NotificationDraft(notification.Title, null, notification.ContentFormat, notification.NotificationType, notification.Priority, notification.TargetType, targets, null, null, null, null, false, false, false, false, notification.StartsAt, notification.ExpiresAt, null, MessageChannels.InApp);
        var valid = Validate(context, draft);
        if (valid.IsFailure)
        {
            return valid.Error!;
        }

        var filter = _sources.FirstOrDefault(static s => s.Kind == RecipientKind.User);
        var source = _sources.FirstOrDefault(s => s.Kind == kind);
        if (filter is null || source is null)
        {
            return Error.Unavailable(NotificationErrors.RecipientSourceMissing, NotificationMessages.RecipientSourceMissing, MessageArguments.Of(("targetType", notification.TargetType.ToString())));
        }

        var candidates = await source.ResolveAsync(context, targets).ConfigureAwait(false);
        return kind is RecipientKind.User or RecipientKind.All
            ? Result.Success(candidates)
            : Result.Success(await filter.ResolveAsync(context, candidates).ConfigureAwait(false));
    }

    /// <summary>
    /// 机器人消息的纯文本：标题、正文与链接各一段（4.x 发标题与正文）。
    /// </summary>
    /// <param name="notification">通知。</param>
    /// <returns>正文。</returns>
    public static string BotTextOf(SysNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var parts = new List<string> { notification.Title };
        if (!string.IsNullOrWhiteSpace(notification.Content))
        {
            parts.Add(notification.Content.Trim());
        }

        if (!string.IsNullOrWhiteSpace(notification.Link))
        {
            parts.Add(notification.Link.Trim());
        }

        return string.Join("\n\n", parts);
    }

    /// <summary>
    /// 邮件、短信与机器人的扇出：给有邮箱（手机号）的接收人各排队一封（一条），没有的跳过；机器人按本侧每个服务商的缺省机器人各一条；随发布一起提交，由发送循环发出。
    /// </summary>
    private async Task<Result> FanOutAsync(InvocationContext context, SysNotification notification, MessageChannels channels, IReadOnlyCollection<Guid> recipients)
    {
        if (channels.HasFlag(MessageChannels.Bot))
        {
            var broadcast = await bots.EnqueueAsync(context, BotTextOf(notification), "notification", notification.Id.ToString("D")).ConfigureAwait(false);
            if (broadcast.IsFailure)
            {
                return broadcast.Error!;
            }
        }

        if (!channels.HasFlag(MessageChannels.Email) && !channels.HasFlag(MessageChannels.Sms))
        {
            return Result.Success();
        }

        var byEmail = channels.HasFlag(MessageChannels.Email) ? (await AcceptingAsync(notification, recipients, NotificationPreferenceChannel.Email, context.CancellationToken).ConfigureAwait(false)).ToHashSet() : [];
        var bySms = channels.HasFlag(MessageChannels.Sms) ? (await AcceptingAsync(notification, recipients, NotificationPreferenceChannel.Sms, context.CancellationToken).ConfigureAwait(false)).ToHashSet() : [];
        var wanted = byEmail.Union(bySms).ToList();
        var contacts = _directory is null || wanted.Count == 0 ? new Dictionary<Guid, UserSummary>() : await _directory.FindAsync(wanted, context.CancellationToken).ConfigureAwait(false);
        var id = notification.Id.ToString("D");
        foreach (var user in contacts.Values)
        {
            if (byEmail.Contains(user.Id) && !string.IsNullOrWhiteSpace(user.Email))
            {
                var queued = await sender.EnqueueAsync(context, new OutgoingMessage(MessageKind.Email, user.Email, EmailTemplate, [new("title", notification.Title), new("content", notification.Content), new("link", notification.Link)], user.Id, "notification", id)).ConfigureAwait(false);
                if (queued.IsFailure)
                {
                    return queued.Error!;
                }
            }

            if (bySms.Contains(user.Id) && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                var queued = await sender.EnqueueAsync(context, new OutgoingMessage(MessageKind.Sms, user.PhoneNumber, SmsTemplate, [new("title", notification.Title)], user.Id, "notification", id)).ConfigureAwait(false);
                if (queued.IsFailure)
                {
                    return queued.Error!;
                }
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 接收人里愿意从这个渠道收这类通知的那些（保持原顺序）：强制阅读、紧急通知或没装偏好即全部。
    /// </summary>
    private async Task<IReadOnlyCollection<Guid>> AcceptingAsync(SysNotification notification, IReadOnlyCollection<Guid> users, NotificationPreferenceChannel channel, CancellationToken cancellationToken)
    {
        if (_preferences is null || users.Count == 0 || notification.IsMandatory || notification.NotificationType == NotificationType.Emergency)
        {
            return users;
        }

        var topic = notification.NotificationType switch
        {
            NotificationType.Security => NotificationPreferenceTopic.Security,
            NotificationType.Todo => NotificationPreferenceTopic.Task,
            _ => NotificationPreferenceTopic.Announcement,
        };
        var accepting = await _preferences.AcceptingAsync(users, channel, topic, cancellationToken).ConfigureAwait(false);
        return [.. users.Where(accepting.Contains)];
    }

    private async Task PushAsync(InvocationContext context, SysNotification notification, IReadOnlyCollection<Guid> users)
    {
        if (_push is null || users.Count == 0)
        {
            return;
        }

        var tone = notification.NotificationType switch
        {
            NotificationType.Emergency => "Error",
            NotificationType.Security => "Warning",
            NotificationType.Business => "Success",
            _ => "Info",
        };
        var payload = new NotificationPushPayload(tone, notification.Title, notification.Content, notification.ContentFormat, notification.Id, notification.NotificationType, UserNotificationStatus.Unread, notification.PublishedAt ?? clock.GetUtcNow());
        await _push.SendAsync(RealtimeAudience.UsersIn(SideRows.SideOf(context), users), RealtimeMethods.ReceiveNotification, payload, NotificationJsonContext.Default.NotificationPushPayload, context.CancellationToken).ConfigureAwait(false);
    }
}
