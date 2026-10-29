// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Notification;

/// <summary>
/// 用户能关掉的通知渠道（个人中心「通知偏好」）。
/// </summary>
public enum NotificationPreferenceChannel
{
    /// <summary>
    /// 站内信（收件箱与实时推送）。
    /// </summary>
    InApp = 0,

    /// <summary>
    /// 邮件。
    /// </summary>
    Email = 1,

    /// <summary>
    /// 短信。
    /// </summary>
    Sms = 2,
}

/// <summary>
/// 用户能关掉的通知类别。
/// </summary>
/// <remarks>
/// 4.x 同：安全、待办各一类，其余都算公告。
/// </remarks>
public enum NotificationPreferenceTopic
{
    /// <summary>
    /// 公告（系统、业务等）。
    /// </summary>
    Announcement = 0,

    /// <summary>
    /// 待办。
    /// </summary>
    Task = 1,

    /// <summary>
    /// 安全。
    /// </summary>
    Security = 2,
}

/// <summary>
/// 用户的通知偏好（Identity 实现）。
/// </summary>
/// <remarks>
/// Notification 发布通知、按渠道扇出时据此过滤接收人；强制阅读与紧急通知由调用方跳过过滤（4.x 同）。
/// 没有实现即不过滤。
/// </remarks>
public interface INotificationPreferences
{
    /// <summary>
    /// 一批用户里愿意从这个渠道收这类通知的那些（没设过偏好的按缺省，全收）。
    /// </summary>
    /// <param name="userIds">候选接收人。</param>
    /// <param name="channel">渠道。</param>
    /// <param name="topic">类别。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>愿意收的用户。</returns>
    Task<IReadOnlySet<Guid>> AcceptingAsync(IReadOnlyCollection<Guid> userIds, NotificationPreferenceChannel channel, NotificationPreferenceTopic topic, CancellationToken cancellationToken);
}
