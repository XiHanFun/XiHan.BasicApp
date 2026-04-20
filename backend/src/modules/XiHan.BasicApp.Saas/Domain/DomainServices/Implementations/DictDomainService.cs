#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictDomainService
// Guid:2a3b4c5d-6e7f-8901-abcd-ef0123456704
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
/// 字典领域服务
/// </summary>
public class DictDomainService : IDictDomainService, IScopedDependency
{
    private readonly IDictRepository _dictRepository;
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DictDomainService(IDictRepository dictRepository, ILocalEventBus localEventBus)
    {
        _dictRepository = dictRepository;
        _localEventBus = localEventBus;
    }

    /// <inheritdoc />
    public async Task<SysDict> CreateAsync(SysDict dict)
    {
        await EnsureDictCodeUniqueAsync(dict.DictCode, null);
        var created = await _dictRepository.AddAsync(dict);
        await _localEventBus.PublishAsync(new DictChangedDomainEvent(created.BasicId, created.DictCode));
        return created;
    }

    /// <inheritdoc />
    public async Task<SysDict> UpdateAsync(SysDict dict)
    {
        await EnsureDictCodeUniqueAsync(dict.DictCode, dict.BasicId);
        var updated = await _dictRepository.UpdateAsync(dict);
        await _localEventBus.PublishAsync(new DictChangedDomainEvent(updated.BasicId, updated.DictCode));
        return updated;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id)
    {
        var dict = await _dictRepository.GetByIdAsync(id);
        if (dict == null)
        {
            return false;
        }

        var result = await _dictRepository.DeleteAsync(dict);
        if (result)
        {
            await _localEventBus.PublishAsync(new DictChangedDomainEvent(id, dict.DictCode));
        }
        return result;
    }

    private async Task EnsureDictCodeUniqueAsync(string dictCode, long? excludeId)
    {
        var existing = await _dictRepository.GetByDictCodeAsync(dictCode.Trim());
        if (existing is not null && (!excludeId.HasValue || existing.BasicId != excludeId.Value))
        {
            throw new BusinessException(message: $"字典编码 '{dictCode}' 已存在");
        }
    }
}
