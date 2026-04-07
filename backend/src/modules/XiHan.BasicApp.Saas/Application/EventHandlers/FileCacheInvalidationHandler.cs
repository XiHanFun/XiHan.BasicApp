#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileCacheInvalidationHandler
// Guid:6e7f8091-0213-2345-ef01-23456789ab06
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
/// 文件缓存失效处理器
/// </summary>
public class FileCacheInvalidationHandler : ILocalEventHandler<FileChangedDomainEvent>, ITransientDependency
{
    private readonly HybridCache _hybridCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileCacheInvalidationHandler(HybridCache hybridCache)
    {
        _hybridCache = hybridCache;
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(FileChangedDomainEvent eventData)
    {
        await _hybridCache.RemoveAsync($"file:id:{eventData.EntityId}");
    }
}
