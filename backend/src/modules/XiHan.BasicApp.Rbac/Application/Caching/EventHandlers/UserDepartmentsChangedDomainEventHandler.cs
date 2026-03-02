#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDepartmentsChangedDomainEventHandler
// Guid:40e67579-8164-4d06-b41f-a310991f9696
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
/// 用户部门关系变更领域事件处理器（桥接到统一授权变更事件）
/// </summary>
public class UserDepartmentsChangedDomainEventHandler : ILocalEventHandler<UserDepartmentsChangedDomainEvent>
{
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="localEventBus"></param>
    public UserDepartmentsChangedDomainEventHandler(ILocalEventBus localEventBus)
    {
        _localEventBus = localEventBus;
    }

    /// <summary>
    /// 处理用户部门关系变更事件
    /// </summary>
    /// <param name="eventData"></param>
    /// <returns></returns>
    public async Task HandleEventAsync(UserDepartmentsChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        await _localEventBus.PublishAsync(
            new RbacAuthorizationChangedEvent(eventData.TenantId, AuthorizationChangeType.DataScope),
            onUnitOfWorkComplete: false);
    }
}
