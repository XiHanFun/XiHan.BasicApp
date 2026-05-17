#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationAppService
// Guid:0982e8eb-7416-4d39-b831-597c10e97f69
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;
using static XiHan.BasicApp.Saas.Application.AppServices.SaasCommandValidation;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统通知命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统通知")]
public sealed class NotificationAppService
    : SaasApplicationService, INotificationAppService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public NotificationAppService(
        INotificationRepository notificationRepository,
        IUserNotificationRepository userNotificationRepository,
        IUserRepository userRepository)
    {
        _notificationRepository = notificationRepository;
        _userNotificationRepository = userNotificationRepository;
        _userRepository = userRepository;
    }

    private readonly INotificationRepository _notificationRepository;
    private readonly IUserNotificationRepository _userNotificationRepository;
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 创建系统通知
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Create)]
    public async Task<NotificationDetailDto> CreateNotificationAsync(NotificationCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateNotificationInput(input.NotificationType, input.Title, input.Icon, input.Link, input.BusinessType, input.BusinessId, input.SendTime, input.ExpireTime, input.Remark);
        var notification = new SysNotification
        {
            SendUserId = input.SendUserId,
            NotificationType = input.NotificationType,
            Title = Required(input.Title, 200, nameof(input.Title), "通知标题不能超过 200 个字符。"),
            Content = NormalizeNullable(input.Content),
            Icon = Optional(input.Icon, 100, nameof(input.Icon), "通知图标不能超过 100 个字符。"),
            Link = Optional(input.Link, 500, nameof(input.Link), "通知链接不能超过 500 个字符。"),
            BusinessType = Optional(input.BusinessType, 100, nameof(input.BusinessType), "业务类型不能超过 100 个字符。"),
            BusinessId = input.BusinessId,
            SendTime = input.SendTime ?? DateTimeOffset.UtcNow,
            ExpireTime = input.ExpireTime,
            IsBroadcast = input.IsBroadcast,
            NeedConfirm = input.NeedConfirm,
            IsPublished = false,
            Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。")
        };

        var savedNotification = await _notificationRepository.AddAsync(notification, cancellationToken);
        if (input.PublishImmediately)
        {
            _ = await PublishInternalAsync(savedNotification, input.UserIds, input.IsBroadcast, cancellationToken);
            savedNotification = await _notificationRepository.UpdateAsync(savedNotification, cancellationToken);
        }

        return NotificationApplicationMapper.ToDetailDto(savedNotification);
    }

    /// <summary>
    /// 更新系统通知
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Update)]
    public async Task<NotificationDetailDto> UpdateNotificationAsync(NotificationUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统通知主键必须大于 0。");
        ValidateNotificationInput(input.NotificationType, input.Title, input.Icon, input.Link, input.BusinessType, input.BusinessId, input.SendTime, input.ExpireTime, input.Remark);

        var notification = await GetNotificationOrThrowAsync(input.BasicId, cancellationToken);
        if (notification.IsPublished)
        {
            throw new InvalidOperationException("已发布通知不能直接更新，请重新创建通知。");
        }

        notification.NotificationType = input.NotificationType;
        notification.Title = Required(input.Title, 200, nameof(input.Title), "通知标题不能超过 200 个字符。");
        notification.Content = NormalizeNullable(input.Content);
        notification.Icon = Optional(input.Icon, 100, nameof(input.Icon), "通知图标不能超过 100 个字符。");
        notification.Link = Optional(input.Link, 500, nameof(input.Link), "通知链接不能超过 500 个字符。");
        notification.BusinessType = Optional(input.BusinessType, 100, nameof(input.BusinessType), "业务类型不能超过 100 个字符。");
        notification.BusinessId = input.BusinessId;
        notification.SendTime = input.SendTime ?? notification.SendTime;
        notification.ExpireTime = input.ExpireTime;
        notification.IsBroadcast = input.IsBroadcast;
        notification.NeedConfirm = input.NeedConfirm;
        notification.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");

        var savedNotification = await _notificationRepository.UpdateAsync(notification, cancellationToken);
        return NotificationApplicationMapper.ToDetailDto(savedNotification);
    }

    /// <summary>
    /// 发布系统通知
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Publish)]
    public async Task<NotificationPublishResultDto> PublishNotificationAsync(NotificationPublishDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var notification = await GetNotificationOrThrowAsync(input.BasicId, cancellationToken);
        var recipientCount = await PublishInternalAsync(notification, input.UserIds, input.IncludeAllEnabledUsers, cancellationToken);
        _ = await _notificationRepository.UpdateAsync(notification, cancellationToken);
        return new NotificationPublishResultDto
        {
            BasicId = notification.BasicId,
            RecipientCount = recipientCount,
            SendTime = notification.SendTime
        };
    }

    /// <summary>
    /// 删除系统通知
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Delete)]
    public async Task DeleteNotificationAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var notification = await GetNotificationOrThrowAsync(id, cancellationToken);
        _ = await _userNotificationRepository.DeleteAsync(item => item.NotificationId == notification.BasicId, cancellationToken);
        if (!await _notificationRepository.DeleteAsync(notification, cancellationToken))
        {
            throw new InvalidOperationException("系统通知删除失败。");
        }
    }

    private async Task<int> PublishInternalAsync(SysNotification notification, IReadOnlyList<long> inputUserIds, bool includeAllEnabledUsers, CancellationToken cancellationToken)
    {
        if (notification.IsPublished)
        {
            throw new InvalidOperationException("系统通知已发布。");
        }

        var recipientIds = await ResolveRecipientIdsAsync(notification, inputUserIds, includeAllEnabledUsers, cancellationToken);
        if (recipientIds.Count == 0)
        {
            throw new InvalidOperationException("系统通知没有有效接收用户。");
        }

        var existingItems = await _userNotificationRepository.GetListAsync(item => item.NotificationId == notification.BasicId, cancellationToken);
        var existingUserIds = existingItems.Select(item => item.UserId).ToHashSet();
        var addItems = recipientIds
            .Where(userId => !existingUserIds.Contains(userId))
            .Select(userId => new SysUserNotification
            {
                NotificationId = notification.BasicId,
                UserId = userId,
                NotificationStatus = NotificationStatus.Unread
            })
            .ToList();
        if (addItems.Count > 0)
        {
            _ = await _userNotificationRepository.AddRangeAsync(addItems, cancellationToken);
        }

        notification.IsPublished = true;
        notification.SendTime = notification.SendTime == default ? DateTimeOffset.UtcNow : notification.SendTime;
        return recipientIds.Count;
    }

    private async Task<IReadOnlyCollection<long>> ResolveRecipientIdsAsync(SysNotification notification, IReadOnlyList<long> inputUserIds, bool includeAllEnabledUsers, CancellationToken cancellationToken)
    {
        if (notification.IsBroadcast || includeAllEnabledUsers)
        {
            var users = await _userRepository.GetListAsync(user => user.Status == EnableStatus.Enabled, cancellationToken);
            return users.Select(user => user.BasicId).Distinct().ToArray();
        }

        var userIds = inputUserIds
            .Where(userId => userId > 0)
            .Distinct()
            .ToArray();
        if (userIds.Length != inputUserIds.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(inputUserIds), "接收用户主键必须大于 0 且不能重复。");
        }

        return userIds;
    }

    private async Task<SysNotification> GetNotificationOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统通知主键必须大于 0。");
        return await _notificationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统通知不存在。");
    }

    private static void ValidateNotificationInput(
        NotificationType notificationType,
        string title,
        string? icon,
        string? link,
        string? businessType,
        long? businessId,
        DateTimeOffset? sendTime,
        DateTimeOffset? expireTime,
        string? remark)
    {
        EnsureEnum(notificationType, nameof(notificationType));
        _ = Required(title, 200, nameof(title), "通知标题不能超过 200 个字符。");
        _ = Optional(icon, 100, nameof(icon), "通知图标不能超过 100 个字符。");
        _ = Optional(link, 500, nameof(link), "通知链接不能超过 500 个字符。");
        _ = Optional(businessType, 100, nameof(businessType), "业务类型不能超过 100 个字符。");
        _ = Optional(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
        EnsureOptionalId(businessId, nameof(businessId), "业务主键必须大于 0。");
        if (sendTime.HasValue && expireTime.HasValue && expireTime.Value <= sendTime.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(expireTime), "过期时间必须晚于发送时间。");
        }
    }
}
