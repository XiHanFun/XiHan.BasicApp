// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户通知仓储接口
/// </summary>
public interface IUserNotificationRepository : ISaasRepository<SysUserNotification>
{
    /// <summary>
    /// 获取用户未读通知
    /// </summary>
    Task<IReadOnlyList<SysUserNotification>> GetUnreadByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量标记已读
    /// </summary>
    Task<int> MarkAsReadAsync(long userId, IEnumerable<long> notificationIds, CancellationToken cancellationToken = default);
}
