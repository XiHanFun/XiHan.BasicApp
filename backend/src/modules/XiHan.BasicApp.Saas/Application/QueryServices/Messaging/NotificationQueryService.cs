#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationQueryService
// Guid:0e1f1a50-4d39-4451-9210-182658928241
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 系统通知查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统通知")]
public sealed class NotificationQueryService(
    INotificationRepository notificationRepository,
    IUserNotificationRepository userNotificationRepository)
    : SaasApplicationService, INotificationQueryService
{
    /// <summary>
    /// 系统通知仓储
    /// </summary>
    private readonly INotificationRepository _notificationRepository = notificationRepository;

    /// <summary>
    /// 用户通知仓储
    /// </summary>
    private readonly IUserNotificationRepository _userNotificationRepository = userNotificationRepository;

    /// <summary>
    /// 获取系统通知分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统通知分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Message.Read)]
    public async Task<PageResultDtoBase<NotificationListItemDto>> GetNotificationPageAsync(NotificationPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildNotificationPageRequest(input);
        var notifications = await _notificationRepository.GetPagedAsync(request, cancellationToken);
        return notifications.Map(NotificationApplicationMapper.ToListItemDto);
    }

    /// <summary>
    /// 获取系统通知详情
    /// </summary>
    /// <param name="id">系统通知主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统通知详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Message.Read)]
    public async Task<NotificationDetailDto?> GetNotificationDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统通知主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var notification = await _notificationRepository.GetByIdAsync(id, cancellationToken);
        return notification is null ? null : NotificationApplicationMapper.ToDetailDto(notification);
    }

    /// <summary>
    /// 获取用户通知分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户通知分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Message.Read)]
    public async Task<PageResultDtoBase<UserNotificationListItemDto>> GetUserNotificationPageAsync(UserNotificationPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildUserNotificationPageRequest(input);
        var userNotifications = await _userNotificationRepository.GetPagedAsync(request, cancellationToken);
        return userNotifications.Map(NotificationApplicationMapper.ToUserListItemDto);
    }

    /// <summary>
    /// 获取用户通知详情
    /// </summary>
    /// <param name="id">用户通知主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户通知详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Message.Read)]
    public async Task<UserNotificationDetailDto?> GetUserNotificationDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户通知主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var userNotification = await _userNotificationRepository.GetByIdAsync(id, cancellationToken);
        return userNotification is null ? null : NotificationApplicationMapper.ToUserDetailDto(userNotification);
    }

    /// <summary>
    /// 构建系统通知分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>系统通知分页请求</returns>
    private static BasicAppPRDto BuildNotificationPageRequest(NotificationPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysNotification.Title),
                nameof(SysNotification.BusinessType));
        }

        if (input.SendUserId.HasValue && input.SendUserId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysNotification.SendUserId), input.SendUserId.Value);
        }

        if (input.NotificationType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysNotification.NotificationType), input.NotificationType.Value);
        }

        if (input.IsBroadcast.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysNotification.IsBroadcast), input.IsBroadcast.Value);
        }

        if (input.NeedConfirm.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysNotification.NeedConfirm), input.NeedConfirm.Value);
        }

        if (input.IsPublished.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysNotification.IsPublished), input.IsPublished.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.BusinessType))
        {
            request.Conditions.AddFilter(nameof(SysNotification.BusinessType), input.BusinessType.Trim());
        }

        if (input.BusinessId.HasValue && input.BusinessId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysNotification.BusinessId), input.BusinessId.Value);
        }

        AddTimeRange(request, nameof(SysNotification.SendTime), input.SendTimeStart, input.SendTimeEnd);
        AddTimeRange(request, nameof(SysNotification.ExpireTime), input.ExpireTimeStart, input.ExpireTimeEnd);
        request.Conditions.AddSort(nameof(SysNotification.SendTime), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysNotification.CreatedTime), SortDirection.Descending, 1);
        return request;
    }

    /// <summary>
    /// 构建用户通知分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>用户通知分页请求</returns>
    private static BasicAppPRDto BuildUserNotificationPageRequest(UserNotificationPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (input.NotificationId.HasValue && input.NotificationId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysUserNotification.NotificationId), input.NotificationId.Value);
        }

        if (input.UserId.HasValue && input.UserId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysUserNotification.UserId), input.UserId.Value);
        }

        if (input.NotificationStatus.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserNotification.NotificationStatus), input.NotificationStatus.Value);
        }

        AddTimeRange(request, nameof(SysUserNotification.ReadTime), input.ReadTimeStart, input.ReadTimeEnd);
        AddTimeRange(request, nameof(SysUserNotification.ConfirmTime), input.ConfirmTimeStart, input.ConfirmTimeEnd);
        request.Conditions.AddSort(nameof(SysUserNotification.CreatedTime), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysUserNotification.UserId), SortDirection.Ascending, 1);
        return request;
    }

    /// <summary>
    /// 添加时间范围筛选
    /// </summary>
    private static void AddTimeRange(BasicAppPRDto request, string fieldName, DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start.HasValue)
        {
            request.Conditions.AddFilter(fieldName, start.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (end.HasValue)
        {
            request.Conditions.AddFilter(fieldName, end.Value, QueryOperator.LessThanOrEqual);
        }
    }
}
