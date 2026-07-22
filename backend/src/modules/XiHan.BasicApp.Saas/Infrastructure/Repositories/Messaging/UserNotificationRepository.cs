// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户通知仓储实现
/// </summary>
public sealed class UserNotificationRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserNotification>(clientResolver), IUserNotificationRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysUserNotification>> GetUnreadByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(n => n.UserId == userId && n.NotificationStatus == NotificationStatus.Unread)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> MarkAsReadAsync(long userId, IEnumerable<long> notificationIds, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ids = notificationIds.ToList();

        return await DbClient.Updateable<SysUserNotification>()
            .SetColumns(n => n.NotificationStatus == NotificationStatus.Read)
            .SetColumns(n => n.ReadTime == DateTimeOffset.UtcNow)
            .Where(n => n.UserId == userId && ids.Contains(n.NotificationId) && n.NotificationStatus == NotificationStatus.Unread)
            .ExecuteCommandAsync(cancellationToken);
    }
}
