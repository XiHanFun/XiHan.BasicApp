#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserInboxAppService
// Guid:b5d8e6f7-2c0a-43b9-c7e1-6f9d4a8b3e02
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/11 20:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 用户收件箱服务 —— 站内信查询 + 已读/确认操作，
/// 与通知管理（NotificationAppService）完全独立
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
public class UserInboxAppService : ApplicationServiceBase, IUserInboxAppService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IMessageCacheService _messageCacheService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserInboxAppService(
        INotificationRepository notificationRepository,
        IMessageCacheService messageCacheService)
    {
        _notificationRepository = notificationRepository;
        _messageCacheService = messageCacheService;
    }

    /// <summary>
    /// 获取用户通知列表
    /// </summary>
    public async Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(long userId, bool includeRead = true, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        var resolvedTenantId = NormalizeTenantId(tenantId);
        var infos = await _notificationRepository.GetUserNotificationsAsync(userId, includeRead, resolvedTenantId);
        return infos.Select(static info => info.Adapt<NotificationDto>()!).ToArray();
    }

    /// <summary>
    /// 获取用户需关注的通知数量（未读 + 需确认未确认）
    /// </summary>
    public async Task<int> GetUnreadCountAsync(long userId, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        var resolvedTenantId = NormalizeTenantId(tenantId);
        return await _messageCacheService.GetUnreadCountAsync(
            userId,
            resolvedTenantId,
            token => _notificationRepository.GetUnreadCountAsync(userId, resolvedTenantId, token));
    }

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    public async Task<bool> MarkAsReadAsync(long notificationId, long userId, long? tenantId = null)
    {
        if (notificationId <= 0 || userId <= 0)
        {
            throw new ArgumentException("通知 ID 或用户 ID 无效");
        }

        var resolvedTenantId = NormalizeTenantId(tenantId);
        var changed = await _notificationRepository.MarkAsReadAsync(notificationId, userId, resolvedTenantId);
        if (changed)
        {
            await _messageCacheService.InvalidateUnreadCountAsync(resolvedTenantId);
        }

        return changed;
    }

    /// <summary>
    /// 标记全部通知为已读
    /// </summary>
    public async Task<int> MarkAllAsReadAsync(long userId, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        var resolvedTenantId = NormalizeTenantId(tenantId);
        var count = await _notificationRepository.MarkAllAsReadAsync(userId, resolvedTenantId);
        if (count > 0)
        {
            await _messageCacheService.InvalidateUnreadCountAsync(resolvedTenantId);
        }

        return count;
    }

    /// <summary>
    /// 确认通知
    /// </summary>
    public async Task<bool> ConfirmAsync(long notificationId, long userId, long? tenantId = null)
    {
        if (notificationId <= 0 || userId <= 0)
        {
            throw new ArgumentException("通知 ID 或用户 ID 无效");
        }

        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification is null || !notification.IsActive() || !notification.NeedConfirm)
        {
            return false;
        }

        var resolvedTenantId = NormalizeTenantId(tenantId);
        if (resolvedTenantId.HasValue && notification.TenantId != 0
            && notification.TenantId != resolvedTenantId.Value)
        {
            return false;
        }

        var confirmed = await _notificationRepository.ConfirmRecipientAsync(notificationId, userId);
        if (confirmed)
        {
            await _messageCacheService.InvalidateUnreadCountAsync(notification.TenantId);
        }

        return confirmed;
    }

    private static long? NormalizeTenantId(long? tenantId)
    {
        return tenantId.HasValue && tenantId.Value > 0 ? tenantId.Value : null;
    }
}
