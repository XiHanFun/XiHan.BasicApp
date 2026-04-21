#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantQueryService
// Guid:2a3b4c5d-6e7f-4901-bcde-220000000002
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

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 租户查询服务
/// </summary>
public class TenantQueryService : ITenantQueryService, ITransientDependency
{
    private readonly ITenantRepository _tenantRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantQueryService(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = QueryCacheKeys.TenantById, ExpireSeconds = 300)]
    public async Task<TenantDto?> GetByIdAsync(long id)
    {
        var entity = await _tenantRepository.GetByIdAsync(id);
        return entity?.Adapt<TenantDto>();
    }

    /// <inheritdoc />
    public async Task<TenantDto?> GetByCodeAsync(string tenantCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);
        var entity = await _tenantRepository.GetByTenantCodeAsync(tenantCode);
        return entity?.Adapt<TenantDto>();
    }
}
