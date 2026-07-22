// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Caching;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// SaaS 配置值查询服务
/// </summary>
public interface ISaasConfigValueQueryService
{
    /// <summary>
    /// 获取配置值缓存项
    /// </summary>
    Task<SaasConfigValueCacheItem> GetValueItemAsync(string configKey, CancellationToken cancellationToken = default);
}
