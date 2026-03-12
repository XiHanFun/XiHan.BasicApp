#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleDataScopeChangedDomainEventHandler
// Guid:58db06e2-dd8d-4f7e-8ae0-c6f4badf0352
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 19:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Caching.Events;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Application.Caching.EventHandlers;

/// <summary>
/// 角色数据范围变更领域事件处理器（桥接到统一授权变更事件）
/// </summary>
public class RoleDataScopeChangedDomainEventHandler : ILocalEventHandler<RoleDataScopeChangedDomainEvent>
{
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="localEventBus"></param>
    public RoleDataScopeChangedDomainEventHandler(ILocalEventBus localEventBus)
    {
        _localEventBus = localEventBus;
    }

    /// <summary>
    /// 处理角色数据范围变更事件
    /// </summary>
    /// <param name="eventData"></param>
    /// <returns></returns>
    public async Task HandleEventAsync(RoleDataScopeChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        await _localEventBus.PublishAsync(
            new RbacAuthorizationChangedEvent(eventData.TenantId, AuthorizationChangeType.DataScope),
            onUnitOfWorkComplete: false);
    }
}
