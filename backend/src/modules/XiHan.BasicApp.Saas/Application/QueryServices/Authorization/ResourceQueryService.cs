// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
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
/// 资源查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "资源")]
public sealed class ResourceQueryService
    : SaasApplicationService, IResourceQueryService
{
    /// <summary>
    /// 资源仓储
    /// </summary>
    private readonly IResourceRepository _resourceRepository;

    /// <summary>
    /// 可选资源选择项缓存
    /// </summary>
    private readonly IDistributedCache<SaasResourceSelectCacheItem, string> _resourceSelectCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ResourceQueryService(
        IResourceRepository resourceRepository,
        IDistributedCache<SaasResourceSelectCacheItem, string> resourceSelectCache)
    {
        _resourceRepository = resourceRepository;
        _resourceSelectCache = resourceSelectCache;
    }

    /// <summary>
    /// 获取资源分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Resource.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<ResourceListItemDto>> GetResourcePageAsync(ResourcePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildResourcePageRequest(input);
        var resources = await _resourceRepository.GetPagedAsync(request, cancellationToken);

        return resources.Map(ResourceApplicationMapper.ToListItemDto);
    }

    /// <summary>
    /// 获取资源详情
    /// </summary>
    /// <param name="id">资源主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Resource.Read)]
    public async Task<ResourceDetailDto?> GetResourceDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "资源主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var resource = await _resourceRepository.GetByIdAsync(id, cancellationToken);
        return resource is null ? null : ResourceApplicationMapper.ToDetailDto(resource);
    }

    /// <summary>
    /// 获取可选全局资源列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可选全局资源列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Resource.Read)]
    public async Task<IReadOnlyList<ResourceSelectItemDto>> GetAvailableGlobalResourcesAsync(ResourceSelectQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 带关键字的搜索命中率低，直接查库；仅缓存无关键字的（类型/上限）筛选组合。
        // 失效由资源定义写路径触发——ResourceAppService 增删改启停调 InvalidateResourceDefinitionAsync。
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            return await QueryAvailableGlobalResourcesAsync(input, cancellationToken);
        }

        var cacheKey = SaasCacheKeys.ResourceSelect((int?)input.ResourceType, input.Limit);
        var item = await _resourceSelectCache.GetOrAddAsync(
            cacheKey,
            async () => new SaasResourceSelectCacheItem
            {
                Items = [.. await QueryAvailableGlobalResourcesAsync(input, cancellationToken)],
                CachedAt = DateTimeOffset.UtcNow
            },
            CreateCacheOptions,
            hideErrors: true,
            token: cancellationToken);

        return item is null
            ? await QueryAvailableGlobalResourcesAsync(input, cancellationToken)
            : item.Items;
    }

    private static DistributedCacheEntryOptions CreateCacheOptions()
    {
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
    }

    /// <summary>
    /// 构建资源分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>资源分页请求</returns>
    private static BasicAppPRDto BuildResourcePageRequest(ResourcePageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        ApplyCommonResourceFilters(
            request,
            input.Keyword,
            input.ResourceType,
            input.AccessLevel,
            input.IsGlobal,
            input.Status);

        request.Conditions.AddSort((SysResource resource) => resource.ResourceType, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysResource resource) => resource.Sort, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysResource resource) => resource.ResourceCode, SortDirection.Ascending, 2);
        return request;
    }

    /// <summary>
    /// 构建资源选择请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>资源选择请求</returns>
    private static BasicAppPRDto BuildResourceSelectRequest(ResourceSelectQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Conditions = new QueryConditions()
        };

        request.Page.PageSize = Math.Clamp(input.Limit, 1, 500);

        ApplyCommonResourceFilters(
            request,
            input.Keyword,
            input.ResourceType,
            accessLevel: null,
            isGlobal: true,
            status: EnableStatus.Enabled);

        request.Conditions.AddSort((SysResource resource) => resource.ResourceType, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysResource resource) => resource.Sort, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysResource resource) => resource.ResourceCode, SortDirection.Ascending, 2);
        return request;
    }

    /// <summary>
    /// 应用资源通用筛选条件
    /// </summary>
    private static void ApplyCommonResourceFilters(
        BasicAppPRDto request,
        string? keyword,
        ResourceType? resourceType,
        ResourceAccessLevel? accessLevel,
        bool? isGlobal,
        EnableStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            request.Conditions.SetKeyword<SysResource>(
                keyword.Trim(),
                resource => resource.ResourceCode,
                resource => resource.ResourceName,
                resource => resource.ResourcePath,
                resource => resource.Description,
                resource => resource.Metadata);
        }

        if (resourceType.HasValue)
        {
            request.Conditions.AddFilter((SysResource resource) => resource.ResourceType, resourceType.Value);
        }

        if (accessLevel.HasValue)
        {
            request.Conditions.AddFilter((SysResource resource) => resource.AccessLevel, accessLevel.Value);
        }

        if (isGlobal.HasValue)
        {
            // IsGlobal 为派生属性（TenantId == 0），不落库，故按租户列过滤：全局=租户0，非全局=非租户0
            request.Conditions.AddFilter(
                (SysResource resource) => resource.TenantId,
                0L,
                isGlobal.Value ? QueryOperator.Equal : QueryOperator.NotEqual);
        }

        if (status.HasValue)
        {
            request.Conditions.AddFilter((SysResource resource) => resource.Status, status.Value);
        }
    }

    /// <summary>
    /// 实时查询可选全局资源列表（缓存未命中或带关键字时执行）。
    /// </summary>
    private async Task<IReadOnlyList<ResourceSelectItemDto>> QueryAvailableGlobalResourcesAsync(ResourceSelectQueryDto input, CancellationToken cancellationToken)
    {
        var request = BuildResourceSelectRequest(input);
        var resources = await _resourceRepository.GetPagedAsync(request, cancellationToken);

        return [.. resources.Items.Select(ResourceApplicationMapper.ToSelectItemDto)];
    }
}
