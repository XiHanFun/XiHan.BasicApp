#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationQueryService
// Guid:f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f8a9b0c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 通知查询服务（处理通知的读操作 - CQRS）
/// </summary>
public class NotificationQueryService : ApplicationServiceBase
{
    private readonly INotificationRepository _notificationRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NotificationQueryService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    /// <summary>
    /// 根据ID获取通知
    /// </summary>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var notification = await _notificationRepository.GetByIdAsync(id);
        return notification?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 获取用户的通知列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="unreadOnly">是否只获取未读通知</param>
    public async Task<List<RbacDtoBase>> GetUserNotificationsAsync(long userId, bool unreadOnly = false)
    {
        var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, unreadOnly);
        return notifications.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取用户的未读通知数量
    /// </summary>
    public async Task<int> GetUnreadCountAsync(long userId)
    {
        return await _notificationRepository.GetUnreadCountAsync(userId);
    }

    /// <summary>
    /// 根据通知类型获取通知列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetByNotificationTypeAsync(string notificationType)
    {
        var notifications = await _notificationRepository.GetByNotificationTypeAsync(notificationType);
        return notifications.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _notificationRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
