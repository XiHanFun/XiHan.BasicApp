#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserPermissionsChangedDomainEventHandler
// Guid:88fc0e0f-ce26-4b3f-b53f-cf76069527f2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 19:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Caching.Events;
using XiHan.BasicApp.Rbac.Domain.Events;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Rbac.Application.Caching.EventHandlers;

/// <summary>
/// 用户直授权限变更领域事件处理器（桥接到统一授权变更事件）
/// </summary>
public class UserPermissionsChangedDomainEventHandler : ILocalEventHandler<UserPermissionsChangedDomainEvent>
{
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="localEventBus"></param>
    public UserPermissionsChangedDomainEventHandler(ILocalEventBus localEventBus)
    {
        _localEventBus = localEventBus;
    }

    /// <summary>
    /// 处理用户直授权限变更事件
    /// </summary>
    /// <param name="eventData"></param>
    /// <returns></returns>
    public async Task HandleEventAsync(UserPermissionsChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        await _localEventBus.PublishAsync(
            new RbacAuthorizationChangedEvent(eventData.TenantId, AuthorizationChangeType.Permission),
            onUnitOfWorkComplete: false);
    }
}
