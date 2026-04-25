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

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.ReadModels;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;

using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 通知仓储实现
/// </summary>
public class NotificationRepository : SqlSugarAggregateRepository<SysNotification, long>, INotificationRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public NotificationRepository(
        ISqlSugarClientResolver clientResolver,
        IUnitOfWorkManager unitOfWorkManager)
        : base(clientResolver, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 获取用户通知列表：JOIN SysUserNotification + SysNotification，一次查出投影到 UserNotificationInfo
    /// </summary>
    public async Task<IReadOnlyList<UserNotificationInfo>> GetUserNotificationsAsync(
        long userId,
        bool includeRead = true,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return [];
        }

        var resolvedTenantId = NormalizeTenantId(tenantId);
        var now = DateTimeOffset.UtcNow;

        return await DbClient.Queryable<SysUserNotification>()
            .InnerJoin<SysNotification>((un, n) => un.NotificationId == n.BasicId)
            .Where((un, n) => un.UserId == userId)
            .Where((un, n) => n.IsPublished == true)
            .Where((un, n) => n.ExpireTime == null || n.ExpireTime > now)
            .WhereIF(!includeRead, (un, n) =>
                un.NotificationStatus == NotificationStatus.Unread
                || (n.NeedConfirm == true && un.ConfirmTime == null))
            .WhereIF(resolvedTenantId.HasValue, (un, n) => n.TenantId == resolvedTenantId || n.TenantId == 0)
            .WhereIF(!resolvedTenantId.HasValue, (un, n) => n.TenantId == 0)
            .OrderBy((un, n) => n.SendTime, OrderByType.Desc)
            .Select((un, n) => new UserNotificationInfo
            {
                BasicId = n.BasicId,
                TenantId = n.TenantId,
                SendUserId = n.SendUserId,
                NotificationType = n.NotificationType,
                Title = n.Title,
                Content = n.Content,
                Icon = n.Icon,
                Link = n.Link,
                BusinessType = n.BusinessType,
                BusinessId = n.BusinessId,
                SendTime = n.SendTime,
                ExpireTime = n.ExpireTime,
                IsBroadcast = n.IsBroadcast,
                NeedConfirm = n.NeedConfirm,
                IsPublished = n.IsPublished,
                Remark = n.Remark,
                NotificationStatus = un.NotificationStatus,
                ReadTime = un.ReadTime,
                ConfirmTime = un.ConfirmTime,
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户需关注的通知数量（未读 + 需确认但未确认）
    /// </summary>
    public async Task<int> GetUnreadCountAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return 0;
        }

        var resolvedTenantId = NormalizeTenantId(tenantId);
        var now = DateTimeOffset.UtcNow;

        return await DbClient.Queryable<SysUserNotification>()
            .InnerJoin<SysNotification>((un, n) => un.NotificationId == n.BasicId)
            .Where((un, n) =>
                un.UserId == userId
                && n.IsPublished == true
                && (n.ExpireTime == null || n.ExpireTime > now))
            .Where((un, n) =>
                un.NotificationStatus == NotificationStatus.Unread
                || (n.NeedConfirm == true && un.ConfirmTime == null))
            .WhereIF(resolvedTenantId.HasValue, (un, n) => n.TenantId == resolvedTenantId || n.TenantId == 0)
            .WhereIF(!resolvedTenantId.HasValue, (un, n) => n.TenantId == 0)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    public async Task<bool> MarkAsReadAsync(long notificationId, long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (notificationId <= 0 || userId <= 0)
        {
            return false;
        }

        var readTime = DateTimeOffset.UtcNow;
        var affected = await DbClient.Updateable<SysUserNotification>()
            .SetColumns(r => r.NotificationStatus, NotificationStatus.Read)
            .SetColumns(r => r.ReadTime, readTime)
            .Where(r =>
                r.NotificationId == notificationId
                && r.UserId == userId
                && r.NotificationStatus == NotificationStatus.Unread)
            .ExecuteCommandAsync(cancellationToken);

        return affected > 0;
    }

    /// <summary>
    /// 标记用户全部通知为已读
    /// </summary>
    public async Task<int> MarkAllAsReadAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return 0;
        }

        var readTime = DateTimeOffset.UtcNow;
        return await DbClient.Updateable<SysUserNotification>()
            .SetColumns(r => r.NotificationStatus, NotificationStatus.Read)
            .SetColumns(r => r.ReadTime, readTime)
            .Where(r => r.UserId == userId && r.NotificationStatus == NotificationStatus.Unread)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 确认通知
    /// </summary>
    public async Task<bool> ConfirmRecipientAsync(long notificationId, long userId, CancellationToken cancellationToken = default)
    {
        if (notificationId <= 0 || userId <= 0)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var affected = await DbClient.Updateable<SysUserNotification>()
            .SetColumns(r => r.NotificationStatus, NotificationStatus.Read)
            .SetColumns(r => r.ReadTime, now)
            .SetColumns(r => r.ConfirmTime, now)
            .Where(r =>
                r.NotificationId == notificationId
                && r.UserId == userId
                && r.ConfirmTime == null)
            .ExecuteCommandAsync(cancellationToken);

        return affected > 0;
    }

    /// <summary>
    /// 批量创建用户通知接收记录
    /// </summary>
    public async Task AddRecipientsAsync(IEnumerable<SysUserNotification> recipients, CancellationToken cancellationToken = default)
    {
        var list = recipients.ToList();
        if (list.Count == 0)
        {
            return;
        }

        await DbClient.Insertable(list).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取通知的所有接收用户ID
    /// </summary>
    public async Task<IReadOnlyList<long>> GetRecipientUserIdsByNotificationIdAsync(long notificationId, CancellationToken cancellationToken = default)
    {
        if (notificationId <= 0)
        {
            return [];
        }

        return await DbClient.Queryable<SysUserNotification>()
            .Where(r => r.NotificationId == notificationId)
            .Select(r => r.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 删除通知的所有接收记录（用于未发布草稿的编辑/删除）
    /// </summary>
    public async Task<int> DeleteRecipientsByNotificationIdAsync(long notificationId, CancellationToken cancellationToken = default)
    {
        if (notificationId <= 0)
        {
            return 0;
        }

        return await DbClient.Deleteable<SysUserNotification>()
            .Where(r => r.NotificationId == notificationId)
            .ExecuteCommandAsync(cancellationToken);
    }

    private static long? NormalizeTenantId(long? tenantId)
    {
        return tenantId.HasValue && tenantId.Value > 0 ? tenantId.Value : null;
    }
}
