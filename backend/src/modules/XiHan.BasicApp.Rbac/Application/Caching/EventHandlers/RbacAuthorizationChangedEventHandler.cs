#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacAuthorizationChangedEventHandler
// Guid:d53fd8c2-73c7-4ae4-8f8a-3e3561ec7417
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 18:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Caching.Events;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Rbac.Application.Caching.EventHandlers;

/// <summary>
/// RBAC 授权变更事件处理器
/// </summary>
public class RbacAuthorizationChangedEventHandler : ILocalEventHandler<RbacAuthorizationChangedEvent>
{
    private readonly IRbacAuthorizationCacheService _authorizationCacheService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="authorizationCacheService"></param>
    public RbacAuthorizationChangedEventHandler(IRbacAuthorizationCacheService authorizationCacheService)
    {
        _authorizationCacheService = authorizationCacheService;
    }

    /// <summary>
    /// 处理授权变更事件
    /// </summary>
    /// <param name="eventData"></param>
    /// <returns></returns>
    public async Task HandleEventAsync(RbacAuthorizationChangedEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.HasPermissionChange && eventData.HasDataScopeChange)
        {
            await _authorizationCacheService.InvalidateAllAsync(eventData.TenantId);
            return;
        }

        if (eventData.HasPermissionChange)
        {
            await _authorizationCacheService.InvalidatePermissionAsync(eventData.TenantId);
        }

        if (eventData.HasDataScopeChange)
        {
            await _authorizationCacheService.InvalidateDataScopeAsync(eventData.TenantId);
        }
    }
}
