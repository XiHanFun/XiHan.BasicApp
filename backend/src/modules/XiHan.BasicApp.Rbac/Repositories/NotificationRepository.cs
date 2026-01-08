#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationRepository
// Guid:b0c1d2e3-f4a5-4b5c-6d7e-9f0a1b2c3d4e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 通知仓储实现
/// </summary>
public class NotificationRepository : SqlSugarAggregateRepository<SysNotification, long>, INotificationRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NotificationRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据用户ID获取通知列表
    /// </summary>
    public async Task<List<SysNotification>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysNotification>()
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据用户ID和通知类型获取通知列表
    /// </summary>
    public async Task<List<SysNotification>> GetByUserIdAndTypeAsync(long userId, NotificationType notificationType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysNotification>()
            .Where(n => n.UserId == userId && n.NotificationType == notificationType)
            .OrderBy(n => n.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户未读通知列表
    /// </summary>
    public async Task<List<SysNotification>> GetUnreadByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysNotification>()
            .Where(n => n.UserId == userId && n.NotificationStatus == NotificationStatus.Unread)
            .OrderBy(n => n.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户未读通知数量
    /// </summary>
    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysNotification>()
            .Where(n => n.UserId == userId && n.NotificationStatus == NotificationStatus.Unread)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    public async Task<bool> MarkAsReadAsync(long notificationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dbClient.Updateable<SysNotification>()
            .SetColumns(n => new SysNotification
            {
                NotificationStatus = NotificationStatus.Read,
                ReadTime = DateTimeOffset.UtcNow
            })
            .Where(n => n.BasicId == notificationId)
            .ExecuteCommandAsync(cancellationToken);

        return affectedRows > 0;
    }

    /// <summary>
    /// 批量标记通知为已读
    /// </summary>
    public async Task<bool> BatchMarkAsReadAsync(List<long> notificationIds, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (notificationIds == null || notificationIds.Count == 0)
        {
            return false;
        }

        var affectedRows = await _dbClient.Updateable<SysNotification>()
            .SetColumns(n => new SysNotification
            {
                NotificationStatus = NotificationStatus.Read,
                ReadTime = DateTimeOffset.UtcNow
            })
            .Where(n => notificationIds.Contains(n.BasicId))
            .ExecuteCommandAsync(cancellationToken);

        return affectedRows > 0;
    }

    /// <summary>
    /// 标记用户的所有通知为已读
    /// </summary>
    public async Task<bool> MarkAllAsReadAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dbClient.Updateable<SysNotification>()
            .SetColumns(n => new SysNotification
            {
                NotificationStatus = NotificationStatus.Read,
                ReadTime = DateTimeOffset.UtcNow
            })
            .Where(n => n.UserId == userId && n.NotificationStatus == NotificationStatus.Unread)
            .ExecuteCommandAsync(cancellationToken);

        return affectedRows > 0;
    }
}
