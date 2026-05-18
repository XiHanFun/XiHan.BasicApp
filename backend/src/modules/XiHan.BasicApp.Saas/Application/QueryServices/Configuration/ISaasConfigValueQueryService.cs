#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISaasConfigValueQueryService
// Guid:3a89fb87-d576-43a6-860a-04d998802f8b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
