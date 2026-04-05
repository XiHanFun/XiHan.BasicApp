#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationDomainService
// Guid:80912031-2435-4567-0123-456789abcd04
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// 通知领域服务
/// </summary>
public class NotificationDomainService : INotificationDomainService, ITransientDependency
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NotificationDomainService(INotificationRepository notificationRepository, ILocalEventBus localEventBus)
    {
        _notificationRepository = notificationRepository;
        _localEventBus = localEventBus;
    }

    /// <inheritdoc />
    public async Task<SysNotification> CreateAsync(SysNotification notification)
    {
        var created = await _notificationRepository.AddAsync(notification);
        await _localEventBus.PublishAsync(new NotificationChangedDomainEvent(created.BasicId));
        return created;
    }

    /// <inheritdoc />
    public async Task<SysNotification> UpdateAsync(SysNotification notification)
    {
        var updated = await _notificationRepository.UpdateAsync(notification);
        await _localEventBus.PublishAsync(new NotificationChangedDomainEvent(updated.BasicId));
        return updated;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id)
    {
        var notification = await _notificationRepository.GetByIdAsync(id);
        if (notification == null) return false;

        var result = await _notificationRepository.DeleteAsync(notification);
        if (result)
        {
            await _localEventBus.PublishAsync(new NotificationChangedDomainEvent(id));
        }
        return result;
    }
}
