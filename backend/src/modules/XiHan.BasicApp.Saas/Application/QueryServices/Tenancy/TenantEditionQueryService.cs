#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantEditionQueryService
// Guid:2c7d404b-0c72-4d66-99fb-3f0adf9f0a2b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 租户版本查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "租户版本")]
public sealed class TenantEditionQueryService
    : SaasApplicationService, ITenantEditionQueryService
{
    /// <summary>
    /// 租户版本仓储
    /// </summary>
    private readonly ITenantEditionRepository _tenantEditionRepository;

    /// <summary>
    /// 已启用租户版本列表缓存
    /// </summary>
    private readonly IDistributedCache<SaasEnabledEditionsCacheItem, string> _enabledEditionsCache;

    /// <summary>
    /// 字段级安全（排序门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantEditionQueryService(
        ITenantEditionRepository tenantEditionRepository,
        IDistributedCache<SaasEnabledEditionsCacheItem, string> enabledEditionsCache,
        IFieldSecurityService fieldSecurityService)
    {
        _tenantEditionRepository = tenantEditionRepository;
        _enabledEditionsCache = enabledEditionsCache;
        _fieldSecurity = fieldSecurityService;
    }

    /// <summary>
    /// 获取租户版本分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.TenantEdition.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<TenantEditionListItemDto>> GetTenantEditionPageAsync(TenantEditionPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildTenantEditionPageRequest(input);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysTenantEdition", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyTenantEditionSorts(request);
        }

        var editions = await _tenantEditionRepository.GetPagedAsync(request, cancellationToken);

        return editions.Map(TenantEditionApplicationMapper.ToListItemDto);
    }

    /// <summary>
    /// 获取租户版本详情
    /// </summary>
    /// <param name="id">租户版本主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.TenantEdition.Read)]
    public async Task<TenantEditionDetailDto?> GetTenantEditionDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "租户版本主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var edition = await _tenantEditionRepository.GetByIdAsync(id, cancellationToken);
        return edition is null ? null : TenantEditionApplicationMapper.ToDetailDto(edition);
    }

    /// <summary>
    /// 获取已启用租户版本列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已启用租户版本列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.TenantEdition.Read)]
    public async Task<IReadOnlyList<TenantEditionListItemDto>> GetEnabledTenantEditionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 分布式缓存：版本是平台级低频数据，全平台共享单键缓存（TTL 30min）。
        // 失效由 TenantEditionAppService 的版本增删改启停触发 InvalidateTenantEditionAsync。
        var item = await _enabledEditionsCache.GetOrAddAsync(
            SaasCacheKeys.EnabledTenantEditions(),
            async () => new SaasEnabledEditionsCacheItem
            {
                Items = [.. await QueryEnabledTenantEditionsAsync(cancellationToken)],
                CachedAt = DateTimeOffset.UtcNow
            },
            CreateCacheOptions,
            hideErrors: true,
            token: cancellationToken);

        return item is null
            ? await QueryEnabledTenantEditionsAsync(cancellationToken)
            : item.Items;
    }

    private static DistributedCacheEntryOptions CreateCacheOptions()
    {
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };
    }

    /// <summary>
    /// 构建租户版本分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>租户版本分页请求</returns>
    private static BasicAppPRDto BuildTenantEditionPageRequest(TenantEditionPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysTenantEdition>(
                input.Keyword.Trim(),
                edition => edition.EditionCode,
                edition => edition.EditionName,
                edition => edition.Description);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysTenantEdition edition) => edition.Status, input.Status.Value);
        }

        if (input.IsFree.HasValue)
        {
            request.Conditions.AddFilter((SysTenantEdition edition) => edition.IsFree, input.IsFree.Value);
        }

        if (input.IsDefault.HasValue)
        {
            request.Conditions.AddFilter((SysTenantEdition edition) => edition.IsDefault, input.IsDefault.Value);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetTenantEditionPageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }
        return request;
    }

    /// <summary>
    /// 应用租户版本默认排序（无前端排序时的兜底）
    /// </summary>
    private static void ApplyTenantEditionSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysTenantEdition edition) => edition.IsDefault, SortDirection.Descending, 0);
        request.Conditions.AddSort((SysTenantEdition edition) => edition.Sort, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysTenantEdition edition) => edition.CreatedTime, SortDirection.Descending, 2);
    }

    /// <summary>
    /// 实时查询已启用租户版本列表（缓存未命中时执行）。
    /// </summary>
    private async Task<IReadOnlyList<TenantEditionListItemDto>> QueryEnabledTenantEditionsAsync(CancellationToken cancellationToken)
    {
        var editions = await _tenantEditionRepository.GetListAsync(
            edition => edition.Status == EnableStatus.Enabled,
            edition => edition.Sort,
            cancellationToken);

        return [.. editions
            .OrderByDescending(edition => edition.IsDefault)
            .ThenBy(edition => edition.Sort)
            .ThenBy(edition => edition.EditionName)
            .Select(TenantEditionApplicationMapper.ToListItemDto)];
    }
}
