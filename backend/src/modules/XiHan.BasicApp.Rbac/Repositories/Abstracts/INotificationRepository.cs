#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:INotificationRepository
// Guid:b0c1d2e3-f4a5-4b5c-6d7e-9f0a1b2c3d4e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 通知仓储接口
/// </summary>
public interface INotificationRepository : IAggregateRootRepository<SysNotification, long>
{
    /// <summary>
    /// 根据用户ID获取通知列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>通知列表</returns>
    Task<List<SysNotification>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID和通知类型获取通知列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="notificationType">通知类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>通知列表</returns>
    Task<List<SysNotification>> GetByUserIdAndTypeAsync(long userId, NotificationType notificationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户未读通知列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>通知列表</returns>
    Task<List<SysNotification>> GetUnreadByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户未读通知数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>未读数量</returns>
    Task<int> GetUnreadCountAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    /// <param name="notificationId">通知ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> MarkAsReadAsync(long notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量标记通知为已读
    /// </summary>
    /// <param name="notificationIds">通知ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> BatchMarkAsReadAsync(List<long> notificationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记用户的所有通知为已读
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> MarkAllAsReadAsync(long userId, CancellationToken cancellationToken = default);
}
