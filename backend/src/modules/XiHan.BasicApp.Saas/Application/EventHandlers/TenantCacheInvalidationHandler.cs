#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantCacheInvalidationHandler
// Guid:2a3b4c5d-6e7f-4901-bcde-240000000004
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
/// 租户缓存失效处理器
/// </summary>
public class TenantCacheInvalidationHandler : ILocalEventHandler<TenantChangedDomainEvent>, ITransientDependency
{
    private readonly HybridCache _hybridCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantCacheInvalidationHandler(HybridCache hybridCache)
    {
        _hybridCache = hybridCache;
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(TenantChangedDomainEvent eventData)
    {
        await _hybridCache.RemoveAsync($"tenant:id:{eventData.EntityId}");
    }
}
