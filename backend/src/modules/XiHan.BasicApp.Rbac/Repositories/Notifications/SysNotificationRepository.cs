#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysNotificationRepository
// Guid:bcb2c3d4-e5f6-7890-abcd-ef123456789b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Notifications;

/// <summary>
/// 系统通知仓储实现
/// </summary>
public class SysNotificationRepository : SqlSugarRepositoryBase<SysNotification, long>, ISysNotificationRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysNotificationRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据用户ID获取通知列表
    /// </summary>
    public async Task<List<SysNotification>> GetByUserIdAsync(long userId)
    {
        var result = await GetListAsync(n => n.UserId == userId || n.IsGlobal);
        return [.. result.OrderByDescending(n => n.SendTime)];
    }

    /// <summary>
    /// 根据通知类型获取通知列表
    /// </summary>
    public async Task<List<SysNotification>> GetByTypeAsync(NotificationType notificationType)
    {
        var result = await GetListAsync(n => n.NotificationType == notificationType);
        return [.. result.OrderByDescending(n => n.SendTime)];
    }

    /// <summary>
    /// 根据通知状态获取通知列表
    /// </summary>
    public async Task<List<SysNotification>> GetByStatusAsync(NotificationStatus notificationStatus)
    {
        var result = await GetListAsync(n => n.NotificationStatus == notificationStatus);
        return [.. result.OrderByDescending(n => n.SendTime)];
    }

    /// <summary>
    /// 根据发送者ID获取通知列表
    /// </summary>
    public async Task<List<SysNotification>> GetBySenderIdAsync(long senderId)
    {
        var result = await GetListAsync(n => n.SenderId == senderId);
        return [.. result.OrderByDescending(n => n.SendTime)];
    }

    /// <summary>
    /// 获取用户的未读通知数量
    /// </summary>
    public async Task<int> GetUnreadCountAsync(long userId)
    {
        return await _dbContext.GetClient()
            .Queryable<SysNotification>()
            .Where(n => (n.UserId == userId || n.IsGlobal) && n.NotificationStatus == NotificationStatus.Unread)
            .CountAsync();
    }

    /// <summary>
    /// 获取全局通知列表
    /// </summary>
    public async Task<List<SysNotification>> GetGlobalNotificationsAsync()
    {
        var result = await GetListAsync(n => n.IsGlobal);
        return [.. result.OrderByDescending(n => n.SendTime)];
    }
}
