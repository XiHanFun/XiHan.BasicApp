#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictCacheInvalidationHandler
// Guid:2a3b4c5d-6e7f-8901-abcd-ef0123456706
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
/// 字典缓存失效处理器
/// </summary>
public class DictCacheInvalidationHandler : ILocalEventHandler<DictChangedDomainEvent>, ITransientDependency
{
    private readonly HybridCache _hybridCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DictCacheInvalidationHandler(HybridCache hybridCache)
    {
        _hybridCache = hybridCache;
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(DictChangedDomainEvent eventData)
    {
        await _hybridCache.RemoveAsync($"dict:id:{eventData.EntityId}");

        if (eventData.DictCode is not null)
        {
            await _hybridCache.RemoveAsync($"dict:code:{eventData.DictCode}");
        }
    }
}
