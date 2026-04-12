#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserInboxAppService
// Guid:a3c7d4e5-1b9f-42a8-b6d0-5e8c3f7a2d91
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/11 20:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户收件箱服务（站内信查询 + 已读/确认操作）
/// </summary>
public interface IUserInboxAppService
{
    /// <summary>
    /// 获取用户通知列表
    /// </summary>
    Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(long userId, bool includeRead = true, long? tenantId = null);

    /// <summary>
    /// 获取用户需关注的通知数量（未读 + 需确认未确认）
    /// </summary>
    Task<int> GetUnreadCountAsync(long userId, long? tenantId = null);

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    Task<bool> MarkAsReadAsync(long notificationId, long userId, long? tenantId = null);

    /// <summary>
    /// 标记全部通知为已读
    /// </summary>
    Task<int> MarkAllAsReadAsync(long userId, long? tenantId = null);

    /// <summary>
    /// 确认通知
    /// </summary>
    Task<bool> ConfirmAsync(long notificationId, long userId, long? tenantId = null);
}
