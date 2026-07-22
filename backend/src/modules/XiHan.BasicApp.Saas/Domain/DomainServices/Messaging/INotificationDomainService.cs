// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 通知领域服务
/// </summary>
public interface INotificationDomainService
{
    /// <summary>
    /// 创建通知
    /// </summary>
    Task<NotificationCommandResult> CreateNotificationAsync(NotificationCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解析通知的邮箱/短信渠道收件人（按投递渠道位；复用定向解析 + 偏好门控单一事实源）
    /// </summary>
    /// <remarks>
    /// 供发布后的多渠道扇出使用：从已发布通知的 TargetType/TargetValue 还原目标并展开为用户集合，
    /// 再按 邮箱/短信 渠道分别过偏好门控（强制阅读/紧急通知豁免语义与站内信一致）。
    /// 机器人渠道为通知级广播、无用户维度，不在此解析。
    /// </remarks>
    Task<NotificationChannelRecipientsResult> ResolveChannelRecipientsAsync(SysNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除通知
    /// </summary>
    Task DeleteNotificationAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发布通知
    /// </summary>
    Task<NotificationPublishCommandResult> PublishNotificationAsync(NotificationPublishCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新通知
    /// </summary>
    Task<NotificationCommandResult> UpdateNotificationAsync(NotificationUpdateCommand command, CancellationToken cancellationToken = default);
}
