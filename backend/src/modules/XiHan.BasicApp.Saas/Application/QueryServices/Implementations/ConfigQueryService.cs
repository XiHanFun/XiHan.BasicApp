#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigQueryService
// Guid:a2b3c4d5-e6f7-8901-bcde-f12345678901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices.Implementations;

/// <summary>
/// 配置查询服务
/// </summary>
public class ConfigQueryService : IConfigQueryService, ITransientDependency
{
    private readonly IConfigRepository _configRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigQueryService(IConfigRepository configRepository)
    {
        _configRepository = configRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = ConfigCacheKeys.ByKeyTemplate, ExpireSeconds = 300)]
    public async Task<string?> GetValueAsync(string configKey, long? tenantId = null)
    {
        var entity = await _configRepository.GetByConfigKeyAsync(configKey, tenantId);
        return entity?.ConfigValue ?? entity?.DefaultValue;
    }

    /// <inheritdoc />
    [Cacheable(Key = ConfigCacheKeys.ByKeyTemplate, ExpireSeconds = 300)]
    public async Task<ConfigDto?> GetByKeyAsync(string configKey, long? tenantId = null)
    {
        var entity = await _configRepository.GetByConfigKeyAsync(configKey, tenantId);
        return entity?.Adapt<ConfigDto>();
    }

    /// <inheritdoc />
    [Cacheable(Key = ConfigCacheKeys.ByGroupTemplate, ExpireSeconds = 300)]
    public async Task<IReadOnlyList<ConfigDto>> GetByGroupAsync(string configGroup, long? tenantId = null)
    {
        var entities = await _configRepository.GetByGroupAsync(configGroup, tenantId);
        return entities.Select(e => e.Adapt<ConfigDto>()!).ToArray();
    }
}
