#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysNotificationRepository
// Guid:acb2c3d4-e5f6-7890-abcd-ef123456789b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstractions;

/// <summary>
/// 系统通知仓储接口
/// </summary>
public interface ISysNotificationRepository : IRepositoryBase<SysNotification, XiHanBasicAppIdType>
{
    /// <summary>
    /// 根据用户ID获取通知列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<SysNotification>> GetByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 根据通知类型获取通知列表
    /// </summary>
    /// <param name="notificationType">通知类型</param>
    /// <returns></returns>
    Task<List<SysNotification>> GetByTypeAsync(NotificationType notificationType);

    /// <summary>
    /// 根据通知状态获取通知列表
    /// </summary>
    /// <param name="notificationStatus">通知状态</param>
    /// <returns></returns>
    Task<List<SysNotification>> GetByStatusAsync(NotificationStatus notificationStatus);

    /// <summary>
    /// 根据发送者ID获取通知列表
    /// </summary>
    /// <param name="senderId">发送者ID</param>
    /// <returns></returns>
    Task<List<SysNotification>> GetBySenderIdAsync(XiHanBasicAppIdType senderId);

    /// <summary>
    /// 获取用户的未读通知数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<int> GetUnreadCountAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 获取全局通知列表
    /// </summary>
    /// <returns></returns>
    Task<List<SysNotification>> GetGlobalNotificationsAsync();
}
