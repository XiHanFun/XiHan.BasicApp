#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RolePermissionsChangedDomainEventHandler
// Guid:3f5d9d42-0661-47f4-8494-c64c5e16e97f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 19:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Caching.Events;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Application.Caching.EventHandlers;

/// <summary>
/// 角色权限变更领域事件处理器（桥接到统一授权变更事件）
/// </summary>
public class RolePermissionsChangedDomainEventHandler : ILocalEventHandler<RolePermissionsChangedDomainEvent>
{
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="localEventBus"></param>
    public RolePermissionsChangedDomainEventHandler(ILocalEventBus localEventBus)
    {
        _localEventBus = localEventBus;
    }

    /// <summary>
    /// 处理角色权限变更事件
    /// </summary>
    /// <param name="eventData"></param>
    /// <returns></returns>
    public async Task HandleEventAsync(RolePermissionsChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        await _localEventBus.PublishAsync(
            new RbacAuthorizationChangedEvent(eventData.TenantId, AuthorizationChangeType.Permission),
            onUnitOfWorkComplete: false);
    }
}
