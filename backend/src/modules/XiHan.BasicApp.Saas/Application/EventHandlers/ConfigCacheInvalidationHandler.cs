#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigCacheInvalidationHandler
// Guid:e6f7a8b9-c0d1-2345-f012-3456789abcde
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
/// 配置缓存失效处理器
/// </summary>
public class ConfigCacheInvalidationHandler : ILocalEventHandler<ConfigChangedDomainEvent>, ITransientDependency
{
    private readonly HybridCache _hybridCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigCacheInvalidationHandler(HybridCache hybridCache)
    {
        _hybridCache = hybridCache;
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(ConfigChangedDomainEvent eventData)
    {
        if (eventData.ConfigKey is not null)
        {
            await _hybridCache.RemoveAsync(ConfigCacheKeys.ByKey(eventData.TenantId, eventData.ConfigKey));
        }

        // 分组列表缓存：实体无分组时与查询侧一致，归一为 Default
        await _hybridCache.RemoveAsync(ConfigCacheKeys.ByGroup(eventData.TenantId, eventData.ConfigGroup));
    }
}
