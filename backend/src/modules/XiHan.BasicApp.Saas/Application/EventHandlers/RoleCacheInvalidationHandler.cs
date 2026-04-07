#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleCacheInvalidationHandler
// Guid:e1b7c5d4-6f9a-7b8c-2d4e-5f6a7b8c9d04
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Caching.Hybrid;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// 角色缓存失效处理器
/// </summary>
public class RoleCacheInvalidationHandler : ILocalEventHandler<RoleChangedDomainEvent>, ITransientDependency
{
    private readonly HybridCache _hybridCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleCacheInvalidationHandler(HybridCache hybridCache)
    {
        _hybridCache = hybridCache;
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(RoleChangedDomainEvent eventData)
    {
        await _hybridCache.RemoveAsync($"role:id:{eventData.EntityId}");
    }
}
