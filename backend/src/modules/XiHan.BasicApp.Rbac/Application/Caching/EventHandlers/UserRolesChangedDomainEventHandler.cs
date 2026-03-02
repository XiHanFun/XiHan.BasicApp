#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserRolesChangedDomainEventHandler
// Guid:4b9eb03e-4f53-4752-84f4-a94bcace7f22
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 19:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Caching.Events;
using XiHan.BasicApp.Rbac.Domain.Events;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Rbac.Application.Caching.EventHandlers;

/// <summary>
/// 用户角色变更领域事件处理器（桥接到统一授权变更事件）
/// </summary>
public class UserRolesChangedDomainEventHandler : ILocalEventHandler<UserRolesChangedDomainEvent>
{
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="localEventBus"></param>
    public UserRolesChangedDomainEventHandler(ILocalEventBus localEventBus)
    {
        _localEventBus = localEventBus;
    }

    /// <summary>
    /// 处理用户角色变更事件
    /// </summary>
    /// <param name="eventData"></param>
    /// <returns></returns>
    public async Task HandleEventAsync(UserRolesChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        await _localEventBus.PublishAsync(
            new RbacAuthorizationChangedEvent(eventData.TenantId, AuthorizationChangeType.All),
            onUnitOfWorkComplete: false);
    }
}
