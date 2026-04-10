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
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;
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
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, splitTableExecutor, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 获取用户通知收件箱：通过 SysUserNotification 查询用户的接收记录，
    /// 再关联 SysNotification 获取通知内容，合并后返回
    /// </summary>
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

        var resolvedTenantId = NormalizeTenantId(tenantId);
        var now = DateTimeOffset.UtcNow;

        // 第一步：查询该用户的接收记录
        var recipientQuery = DbClient.Queryable<SysUserNotification>()
            .Where(r => r.UserId == userId);

        if (!includeRead)
        {
            recipientQuery = recipientQuery.Where(r => r.NotificationStatus == NotificationStatus.Unread);
        }

        var recipients = await recipientQuery.ToListAsync(cancellationToken);
        if (recipients.Count == 0)
        {
            return [];
        }

        var notificationIds = recipients.Select(r => r.NotificationId).ToArray();

        // 第二步：查询对应的通知内容（过滤有效、未过期、租户匹配的通知）
        var notifications = await CreateTenantQueryable()
            .Where(n => notificationIds.Contains(n.BasicId))
            .Where(n => n.Status == YesOrNo.Yes)
            .Where(n => n.ExpireTime == null || n.ExpireTime > now)
            .WhereIF(resolvedTenantId.HasValue, n => n.TenantId == resolvedTenantId || n.TenantId == null)
            .WhereIF(!resolvedTenantId.HasValue, n => n.TenantId == null)
            .OrderBy(n => n.SendTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        // 第三步：将用户级已读状态合并到通知实体上（仅内存操作，不回写 DB）
        var recipientMap = recipients.ToDictionary(r => r.NotificationId);
        foreach (var notification in notifications)
        {
            if (recipientMap.TryGetValue(notification.BasicId, out var recipient))
            {
                notification.NotificationStatus = recipient.NotificationStatus;
                notification.ReadTime = recipient.ReadTime;
            }
        }

        return notifications;
    }

    /// <summary>
    /// 获取用户未读通知数量：通过 JOIN 确保只计算有效通知
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
            .InnerJoin<SysNotification>((r, n) => r.NotificationId == n.BasicId)
            .Where((r, n) =>
                r.UserId == userId
                && r.NotificationStatus == NotificationStatus.Unread
                && n.Status == YesOrNo.Yes
                && (n.ExpireTime == null || n.ExpireTime > now))
            .WhereIF(resolvedTenantId.HasValue, (r, n) => n.TenantId == resolvedTenantId || n.TenantId == null)
            .WhereIF(!resolvedTenantId.HasValue, (r, n) => n.TenantId == null)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// 标记通知为已读：直接更新 SysUserNotification
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
    /// 标记用户全部通知为已读：批量更新 SysUserNotification
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
    /// 确认通知：更新 SysUserNotification 的确认时间和已读状态
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

    private static long? NormalizeTenantId(long? tenantId)
    {
        return tenantId.HasValue && tenantId.Value > 0 ? tenantId.Value : null;
    }
}
