#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:INotificationDomainService
// Guid:6d4888a2-fc44-43e5-81e4-557c9d78e454
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
