// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Persistence.SqlSugar;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 通知偏好的查询：按用户取偏好行（平台库，不分租户），没有行的按缺省全收。
/// </summary>
/// <param name="unitOfWork">工作单元。</param>
public sealed class NotificationPreferences(SqlSugarUnitOfWork unitOfWork) : INotificationPreferences
{
    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> AcceptingAsync(IReadOnlyCollection<Guid> userIds, NotificationPreferenceChannel channel, NotificationPreferenceTopic topic, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var rows = (await unitOfWork.ConnectionFor(typeof(SysUserNotificationPreference)).Queryable<SysUserNotificationPreference>().Where(p => ids.Contains(p.UserId)).ToListAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(static p => p.UserId);
        return ids.Where(id => !rows.TryGetValue(id, out var preference) || Accepts(preference, channel, topic)).ToHashSet();
    }

    /// <summary>
    /// 一行偏好是否收：渠道与类别都开着。
    /// </summary>
    public static bool Accepts(SysUserNotificationPreference preference, NotificationPreferenceChannel channel, NotificationPreferenceTopic topic)
    {
        ArgumentNullException.ThrowIfNull(preference);
        var byChannel = channel switch
        {
            NotificationPreferenceChannel.InApp => preference.ChannelInApp,
            NotificationPreferenceChannel.Email => preference.ChannelEmail,
            NotificationPreferenceChannel.Sms => preference.ChannelSms,
            _ => true,
        };
        var byTopic = topic switch
        {
            NotificationPreferenceTopic.Task => preference.TypeTask,
            NotificationPreferenceTopic.Security => preference.TypeSecurity,
            _ => preference.TypeAnnouncement,
        };
        return byChannel && byTopic;
    }
}
