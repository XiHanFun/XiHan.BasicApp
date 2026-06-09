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
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统通知命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统通知")]
public sealed class NotificationAppService
    : SaasApplicationService, INotificationAppService
{
    private readonly INotificationDomainService _notificationDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NotificationAppService(INotificationDomainService notificationDomainService)
    {
        _notificationDomainService = notificationDomainService;
    }

    /// <summary>
    /// 创建系统通知
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Create)]
    public async Task<NotificationDetailDto> CreateNotificationAsync(NotificationCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _notificationDomainService.CreateNotificationAsync(NotificationApplicationMapper.ToCreateCommand(input), cancellationToken);
        return NotificationApplicationMapper.ToDetailDto(result.Notification);
    }

    /// <summary>
    /// 删除系统通知
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Delete)]
    public async Task DeleteNotificationAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _notificationDomainService.DeleteNotificationAsync(id, cancellationToken);
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

        var result = await _notificationDomainService.PublishNotificationAsync(NotificationApplicationMapper.ToPublishCommand(input), cancellationToken);
        return NotificationApplicationMapper.ToPublishResultDto(result);
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

        var result = await _notificationDomainService.UpdateNotificationAsync(NotificationApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return NotificationApplicationMapper.ToDetailDto(result.Notification);
    }
}
