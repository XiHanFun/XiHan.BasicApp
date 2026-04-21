#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthCacheInvalidationHandler
// Guid:6a7b8c9d-0e1f-4345-f012-660000000006
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Caching.Hybrid;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// OAuth 应用缓存失效处理器
/// </summary>
public class OAuthCacheInvalidationHandler : ILocalEventHandler<OAuthChangedDomainEvent>, ITransientDependency
{
    private readonly HybridCache _hybridCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthCacheInvalidationHandler(HybridCache hybridCache)
    {
        _hybridCache = hybridCache;
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(OAuthChangedDomainEvent eventData)
    {
        await _hybridCache.RemoveAsync(QueryCacheKeys.OAuthAppByIdValue(eventData.EntityId));
    }
}
