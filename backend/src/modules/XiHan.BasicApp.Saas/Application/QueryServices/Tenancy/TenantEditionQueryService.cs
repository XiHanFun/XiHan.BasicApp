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
/// 租户版本查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "租户版本")]
public sealed class TenantEditionQueryService(ITenantEditionRepository tenantEditionRepository)
    : SaasApplicationService, ITenantEditionQueryService
{
    /// <summary>
    /// 租户版本仓储
    /// </summary>
    private readonly ITenantEditionRepository _tenantEditionRepository = tenantEditionRepository;

    /// <summary>
    /// 获取租户版本分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.TenantEdition.Read)]
    public async Task<PageResultDtoBase<TenantEditionListItemDto>> GetTenantEditionPageAsync(TenantEditionPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildTenantEditionPageRequest(input);
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
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysTenantEdition.EditionCode),
                nameof(SysTenantEdition.EditionName),
                nameof(SysTenantEdition.Description));
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTenantEdition.Status), input.Status.Value);
        }

        if (input.IsFree.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTenantEdition.IsFree), input.IsFree.Value);
        }

        if (input.IsDefault.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTenantEdition.IsDefault), input.IsDefault.Value);
        }

        request.Conditions.AddSort(nameof(SysTenantEdition.IsDefault), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysTenantEdition.Sort), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysTenantEdition.CreatedTime), SortDirection.Descending, 2);
        return request;
    }
}
