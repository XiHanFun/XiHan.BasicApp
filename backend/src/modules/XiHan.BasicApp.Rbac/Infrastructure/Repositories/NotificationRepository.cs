#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationRepository
// Guid:d0f27f37-08df-40dd-a026-f73674a74c09
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 10:46:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 通知仓储实现
/// </summary>
public class NotificationRepository : SqlSugarAggregateRepository<SysNotification, long>, INotificationRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="currentTenant"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="unitOfWorkManager"></param>
    public NotificationRepository(
        ISqlSugarClientProvider clientProvider,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(clientProvider, currentTenant, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 获取用户通知收件箱
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="includeRead"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysNotification>> GetUserNotificationsAsync(
        long userId,
        bool includeRead = true,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return [];
        }

        var resolvedTenantId = tenantId ?? CurrentTenantId;
        var now = DateTimeOffset.UtcNow;

        var query = CreateTenantQueryable()
            .Where(notification =>
                notification.Status == YesOrNo.Yes
                && notification.NotificationStatus != NotificationStatus.Deleted
                && (!notification.ExpireTime.HasValue || notification.ExpireTime > now)
                && (notification.IsGlobal || notification.RecipientUserId == userId));

        if (!includeRead)
        {
            query = query.Where(notification => notification.NotificationStatus == NotificationStatus.Unread);
        }

        if (resolvedTenantId.HasValue)
        {
            query = query.Where(notification => notification.TenantId == resolvedTenantId.Value);
        }
        else
        {
            query = query.Where(notification => notification.TenantId == null);
        }

        return await query
            .OrderByDescending(notification => notification.SendTime)
            .OrderByDescending(notification => notification.CreatedTime)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户未读通知数量
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int> GetUnreadCountAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return 0;
        }

        var resolvedTenantId = tenantId ?? CurrentTenantId;
        var now = DateTimeOffset.UtcNow;

        var query = CreateTenantQueryable()
            .Where(notification =>
                notification.Status == YesOrNo.Yes
                && notification.NotificationStatus == NotificationStatus.Unread
                && (!notification.ExpireTime.HasValue || notification.ExpireTime > now)
                && (notification.IsGlobal || notification.RecipientUserId == userId));

        if (resolvedTenantId.HasValue)
        {
            query = query.Where(notification => notification.TenantId == resolvedTenantId.Value);
        }
        else
        {
            query = query.Where(notification => notification.TenantId == null);
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    /// <param name="notificationId"></param>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> MarkAsReadAsync(long notificationId, long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (notificationId <= 0 || userId <= 0)
        {
            return false;
        }

        var resolvedTenantId = tenantId ?? CurrentTenantId;
        var now = DateTimeOffset.UtcNow;

        var query = CreateTenantQueryable()
            .Where(notification =>
                notification.BasicId == notificationId
                && notification.Status == YesOrNo.Yes
                && notification.NotificationStatus != NotificationStatus.Deleted
                && (!notification.ExpireTime.HasValue || notification.ExpireTime > now)
                && (notification.IsGlobal || notification.RecipientUserId == userId));

        if (resolvedTenantId.HasValue)
        {
            query = query.Where(notification => notification.TenantId == resolvedTenantId.Value);
        }
        else
        {
            query = query.Where(notification => notification.TenantId == null);
        }

        var entity = await query.FirstAsync(cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.MarkRead();
        await DbClient.Updateable(entity).ExecuteCommandAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 标记用户全部通知为已读
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int> MarkAllAsReadAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return 0;
        }

        var resolvedTenantId = tenantId ?? CurrentTenantId;
        var now = DateTimeOffset.UtcNow;

        var query = CreateTenantQueryable()
            .Where(notification =>
                notification.Status == YesOrNo.Yes
                && notification.NotificationStatus == NotificationStatus.Unread
                && (!notification.ExpireTime.HasValue || notification.ExpireTime > now)
                && (notification.IsGlobal || notification.RecipientUserId == userId));

        if (resolvedTenantId.HasValue)
        {
            query = query.Where(notification => notification.TenantId == resolvedTenantId.Value);
        }
        else
        {
            query = query.Where(notification => notification.TenantId == null);
        }

        var entities = await query.ToListAsync(cancellationToken);
        if (entities.Count == 0)
        {
            return 0;
        }

        foreach (var entity in entities)
        {
            entity.MarkRead();
        }

        await DbClient.Updateable(entities).ExecuteCommandAsync(cancellationToken);
        return entities.Count;
    }
}
