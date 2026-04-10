#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:INotificationRepository
// Guid:bc8e4b27-56f4-4c3f-9c67-b45ce2497a01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 10:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 通知仓储接口
/// </summary>
public interface INotificationRepository : IAggregateRootRepository<SysNotification, long>
{
    /// <summary>
    /// 获取用户通知收件箱（通知内容 + 用户级已读状态合并返回）
    /// </summary>
    Task<IReadOnlyList<SysNotification>> GetUserNotificationsAsync(
        long userId,
        bool includeRead = true,
        long? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户未读通知数量
    /// </summary>
    Task<int> GetUnreadCountAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记通知为已读（更新 SysUserNotification）
    /// </summary>
    Task<bool> MarkAsReadAsync(long notificationId, long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记用户全部通知为已读（批量更新 SysUserNotification）
    /// </summary>
    Task<int> MarkAllAsReadAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 确认通知（更新 SysUserNotification 的确认时间和已读状态）
    /// </summary>
    Task<bool> ConfirmRecipientAsync(long notificationId, long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量创建用户通知接收记录
    /// </summary>
    Task AddRecipientsAsync(IEnumerable<SysUserNotification> recipients, CancellationToken cancellationToken = default);
}
