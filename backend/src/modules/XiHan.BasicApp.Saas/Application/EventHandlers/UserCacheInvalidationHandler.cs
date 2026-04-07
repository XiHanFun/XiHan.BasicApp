#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserCacheInvalidationHandler
// Guid:3a4b5c6d-7e8f-4012-cdef-340000000004
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
/// 用户缓存失效处理器
/// </summary>
public class UserCacheInvalidationHandler : ILocalEventHandler<UserChangedDomainEvent>, ITransientDependency
{
    private readonly HybridCache _hybridCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserCacheInvalidationHandler(HybridCache hybridCache)
    {
        _hybridCache = hybridCache;
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(UserChangedDomainEvent eventData)
    {
        await _hybridCache.RemoveAsync($"user:id:{eventData.EntityId}");
    }
}
