#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResourceQueryService
// Guid:9c86ca96-c82d-4f3e-953c-e70fe5b67cb9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 资源查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "资源")]
public sealed class ResourceQueryService(IResourceRepository resourceRepository)
    : SaasApplicationService, IResourceQueryService
{
    /// <summary>
    /// 资源仓储
    /// </summary>
    private readonly IResourceRepository _resourceRepository = resourceRepository;

    /// <summary>
    /// 获取资源分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Resource.Read)]
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

        var request = BuildResourceSelectRequest(input);
        var resources = await _resourceRepository.GetPagedAsync(request, cancellationToken);

        return [.. resources.Items.Select(ResourceApplicationMapper.ToSelectItemDto)];
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
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        ApplyCommonResourceFilters(
            request,
            input.Keyword,
            input.ResourceType,
            input.AccessLevel,
            input.IsGlobal,
            input.Status);

        request.Conditions.AddSort(nameof(SysResource.ResourceType), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysResource.Sort), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysResource.ResourceCode), SortDirection.Ascending, 2);
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

        request.Conditions.AddSort(nameof(SysResource.ResourceType), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysResource.Sort), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysResource.ResourceCode), SortDirection.Ascending, 2);
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
            request.Conditions.SetKeyword(
                keyword.Trim(),
                nameof(SysResource.ResourceCode),
                nameof(SysResource.ResourceName),
                nameof(SysResource.ResourcePath),
                nameof(SysResource.Description),
                nameof(SysResource.Metadata));
        }

        if (resourceType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysResource.ResourceType), resourceType.Value);
        }

        if (accessLevel.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysResource.AccessLevel), accessLevel.Value);
        }

        if (isGlobal.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysResource.IsGlobal), isGlobal.Value);
        }

        if (status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysResource.Status), status.Value);
        }
    }
}
