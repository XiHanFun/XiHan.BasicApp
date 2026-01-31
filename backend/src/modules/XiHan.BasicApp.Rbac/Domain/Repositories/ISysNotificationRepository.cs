#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysNotificationRepository
// Guid:f2a3b4c5-d6e7-89ab-cdef-123456789012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/31 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 系统通知仓储接口
/// </summary>
public interface ISysNotificationRepository : IAggregateRootRepository<SysNotification, long>
{
    /// <summary>
    /// 获取用户的未读通知列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>通知列表</returns>
    Task<List<SysNotification>> GetUnreadNotificationsByUserAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的通知列表（分页）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>通知列表</returns>
    Task<List<SysNotification>> GetNotificationsByUserAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户未读通知数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>未读数量</returns>
    Task<int> GetUnreadCountAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存通知
    /// </summary>
    /// <param name="notification">通知实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的通知实体</returns>
    Task<SysNotification> SaveAsync(SysNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    /// <param name="notificationId">通知ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task MarkAsReadAsync(long notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记用户所有通知为已读
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task MarkAllAsReadAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除通知
    /// </summary>
    /// <param name="notificationId">通知ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RemoveAsync(long notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期通知
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task CleanupExpiredNotificationsAsync(CancellationToken cancellationToken = default);
}
