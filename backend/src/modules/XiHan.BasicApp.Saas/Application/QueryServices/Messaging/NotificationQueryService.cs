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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SqlSugar;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
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
public sealed class NotificationQueryService
    : SaasApplicationService, INotificationQueryService
{
    /// <summary>
    /// 系统通知仓储
    /// </summary>
    private readonly INotificationRepository _notificationRepository;

    /// <summary>
    /// 用户通知仓储
    /// </summary>
    private readonly IUserNotificationRepository _userNotificationRepository;

    /// <summary>
    /// 字段级安全（排序门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NotificationQueryService(
        INotificationRepository notificationRepository,
        IUserNotificationRepository userNotificationRepository,
        IFieldSecurityService fieldSecurityService)
    {
        _notificationRepository = notificationRepository;
        _userNotificationRepository = userNotificationRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <summary>
    /// 获取系统通知分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统通知分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Message.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<NotificationListItemDto>> GetNotificationPageAsync(NotificationPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildNotificationPageRequest(input);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysNotification", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyNotificationSorts(request);
        }

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
    [HttpPost]
    public async Task<PageResultDtoBase<UserNotificationListItemDto>> GetUserNotificationPageAsync(UserNotificationPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildUserNotificationPageRequest(input);
        var userNotifications = await _userNotificationRepository.GetPagedAsync(request, cancellationToken);
        if (userNotifications.Items.Count == 0)
        {
            return new PageResultDtoBase<UserNotificationListItemDto>([], userNotifications.Page);
        }

        var notificationMap = await LoadNotificationMapAsync(userNotifications.Items, cancellationToken);
        var items = userNotifications.Items
            .Select(item => NotificationApplicationMapper.ToUserListItemDto(
                item,
                notificationMap.GetValueOrDefault(item.NotificationId)))
            .ToList();
        return new PageResultDtoBase<UserNotificationListItemDto>(items, userNotifications.Page);
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
        if (userNotification is null)
        {
            return null;
        }

        var notification = await _notificationRepository.GetByIdAsync(userNotification.NotificationId, cancellationToken);
        return NotificationApplicationMapper.ToUserDetailDto(userNotification, notification);
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
            request.Conditions.SetKeyword<SysNotification>(
                input.Keyword.Trim(),
                notification => notification.Title,
                notification => notification.BusinessType);
        }

        if (input.SendUserId.HasValue && input.SendUserId.Value > 0)
        {
            request.Conditions.AddFilter((SysNotification notification) => notification.SendUserId, input.SendUserId.Value);
        }

        if (input.NotificationType.HasValue)
        {
            request.Conditions.AddFilter((SysNotification notification) => notification.NotificationType, input.NotificationType.Value);
        }

        if (input.TargetType.HasValue)
        {
            request.Conditions.AddFilter((SysNotification notification) => notification.TargetType, input.TargetType.Value);
        }

        if (input.NeedConfirm.HasValue)
        {
            request.Conditions.AddFilter((SysNotification notification) => notification.NeedConfirm, input.NeedConfirm.Value);
        }

        if (input.IsPublished.HasValue)
        {
            request.Conditions.AddFilter((SysNotification notification) => notification.IsPublished, input.IsPublished.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.BusinessType))
        {
            request.Conditions.AddFilter((SysNotification notification) => notification.BusinessType, input.BusinessType.Trim());
        }

        if (input.BusinessId.HasValue && input.BusinessId.Value > 0)
        {
            request.Conditions.AddFilter((SysNotification notification) => notification.BusinessId, input.BusinessId.Value);
        }

        AddTimeRange(request, nameof(SysNotification.SendTime), input.SendTimeStart, input.SendTimeEnd);
        AddTimeRange(request, nameof(SysNotification.ExpirationTime), input.ExpirationTimeStart, input.ExpirationTimeEnd);
        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetNotificationPageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }
        return request;
    }

    /// <summary>
    /// 应用系统通知默认排序
    /// </summary>
    private static void ApplyNotificationSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysNotification notification) => notification.SendTime, SortDirection.Descending, 0);
        request.Conditions.AddSort((SysNotification notification) => notification.CreatedTime, SortDirection.Descending, 1);
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
            request.Conditions.AddFilter((SysUserNotification userNotification) => userNotification.NotificationId, input.NotificationId.Value);
        }

        if (input.UserId.HasValue && input.UserId.Value > 0)
        {
            request.Conditions.AddFilter((SysUserNotification userNotification) => userNotification.UserId, input.UserId.Value);
        }

        if (input.NotificationStatus.HasValue)
        {
            request.Conditions.AddFilter((SysUserNotification userNotification) => userNotification.NotificationStatus, input.NotificationStatus.Value);
        }

        AddTimeRange(request, nameof(SysUserNotification.ReadTime), input.ReadTimeStart, input.ReadTimeEnd);
        AddTimeRange(request, nameof(SysUserNotification.ConfirmTime), input.ConfirmTimeStart, input.ConfirmTimeEnd);
        request.Conditions.AddSort((SysUserNotification userNotification) => userNotification.CreatedTime, SortDirection.Descending, 0);
        request.Conditions.AddSort((SysUserNotification userNotification) => userNotification.UserId, SortDirection.Ascending, 1);
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

    /// <summary>
    /// 加载通知内容映射
    /// </summary>
    private async Task<Dictionary<long, SysNotification>> LoadNotificationMapAsync(IList<SysUserNotification> userNotifications, CancellationToken cancellationToken)
    {
        var notificationIds = userNotifications
            .Select(item => item.NotificationId)
            .Distinct()
            .ToArray();
        if (notificationIds.Length == 0)
        {
            return [];
        }

        var notifications = await _notificationRepository.GetListAsync(
            item => SqlFunc.ContainsArray(notificationIds, item.BasicId),
            cancellationToken);
        return notifications.ToDictionary(item => item.BasicId);
    }
}
