#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictQueryService
// Guid:2a3b4c5d-6e7f-8901-abcd-ef0123456702
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices.Implementations;

/// <summary>
/// 字典查询服务
/// </summary>
public class DictQueryService : IDictQueryService, ITransientDependency
{
    private readonly IDictRepository _dictRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DictQueryService(IDictRepository dictRepository)
    {
        _dictRepository = dictRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = "dict:id:{id}", ExpireSeconds = 300)]
    public async Task<DictDto?> GetByIdAsync(long id)
    {
        var entity = await _dictRepository.GetByIdAsync(id);
        return entity?.Adapt<DictDto>();
    }

    /// <inheritdoc />
    [Cacheable(Key = "dict:code:{code}", ExpireSeconds = 300)]
    public async Task<DictDto?> GetByCodeAsync(string code)
    {
        var entity = await _dictRepository.GetByDictCodeAsync(code);
        return entity?.Adapt<DictDto>();
    }
}
