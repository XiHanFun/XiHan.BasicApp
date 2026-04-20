#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigDomainService
// Guid:d5e6f7a8-b9c0-1234-ef01-23456789abcd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// 配置领域服务
/// </summary>
public class ConfigDomainService : IConfigDomainService, IScopedDependency
{
    private readonly IConfigRepository _configRepository;
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigDomainService(IConfigRepository configRepository, ILocalEventBus localEventBus)
    {
        _configRepository = configRepository;
        _localEventBus = localEventBus;
    }

    /// <inheritdoc />
    public async Task<SysConfig> CreateAsync(SysConfig config)
    {
        await EnsureConfigKeyUniqueAsync(config.ConfigKey, null, config.TenantId);
        var created = await _configRepository.AddAsync(config);
        await _localEventBus.PublishAsync(new ConfigChangedDomainEvent(created.BasicId, created.ConfigKey, created.TenantId, created.ConfigGroup));
        return created;
    }

    /// <inheritdoc />
    public async Task<SysConfig> UpdateAsync(SysConfig config)
    {
        await EnsureConfigKeyUniqueAsync(config.ConfigKey, config.BasicId, config.TenantId);
        var updated = await _configRepository.UpdateAsync(config);
        await _localEventBus.PublishAsync(new ConfigChangedDomainEvent(updated.BasicId, updated.ConfigKey, updated.TenantId, updated.ConfigGroup));
        return updated;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id)
    {
        var config = await _configRepository.GetByIdAsync(id);
        if (config == null)
        {
            return false;
        }

        var result = await _configRepository.DeleteAsync(config);
        if (result)
        {
            await _localEventBus.PublishAsync(new ConfigChangedDomainEvent(id, config.ConfigKey, config.TenantId, config.ConfigGroup));
        }
        return result;
    }

    private async Task EnsureConfigKeyUniqueAsync(string configKey, long? excludeId, long? tenantId)
    {
        var existing = await _configRepository.GetByConfigKeyAsync(configKey.Trim(), tenantId);
        if (existing is not null && (!excludeId.HasValue || existing.BasicId != excludeId.Value))
        {
            throw new BusinessException(message: $"配置键 '{configKey}' 已存在");
        }
    }
}
