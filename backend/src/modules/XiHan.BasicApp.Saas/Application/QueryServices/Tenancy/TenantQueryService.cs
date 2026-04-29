#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantQueryService
// Guid:31584ca2-2c89-4f90-9c7a-2d39964b78d0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 租户查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "租户")]
public sealed class TenantQueryService(
    ITenantUserRepository tenantUserRepository,
    ITenantRepository tenantRepository,
    ICurrentUser currentUser)
    : SaasApplicationService, ITenantQueryService
{
    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 租户仓储
    /// </summary>
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    /// <summary>
    /// 当前用户
    /// </summary>
    private readonly ICurrentUser _currentUser = currentUser;

    /// <summary>
    /// 获取租户分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Tenant.Read)]
    public async Task<PageResultDtoBase<TenantListItemDto>> GetTenantPageAsync(TenantPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildTenantPageRequest(input);
        var tenants = await _tenantRepository.GetPagedAsync(request, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        return tenants.Map(tenant => TenantApplicationMapper.ToListItemDto(tenant, now));
    }

    /// <summary>
    /// 获取租户详情
    /// </summary>
    /// <param name="id">租户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Tenant.Read)]
    public async Task<TenantDetailDto?> GetTenantDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "租户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        return tenant is null ? null : TenantApplicationMapper.ToDetailDto(tenant, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 获取当前用户可进入的租户列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前用户可进入的租户列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Tenant.Read)]
    public async Task<IReadOnlyList<TenantSwitcherDto>> GetMyAvailableTenantsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("当前用户未登录，无法获取可进入租户。");

        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        var memberships = await _tenantUserRepository.GetActiveByUserIdAsync(userId, now, cancellationToken);
        if (memberships.Count == 0)
        {
            return [];
        }

        var accessibleKeys = memberships
            .Select(membership => membership.TenantId)
            .Distinct()
            .ToArray();

        var tenants = await _tenantRepository.GetByIdsAsync(accessibleKeys, cancellationToken);
        var tenantMap = tenants
            .Where(tenant => tenant.TenantStatus == TenantStatus.Normal)
            .ToDictionary(tenant => tenant.BasicId);

        return [.. memberships
            .Where(membership => tenantMap.ContainsKey(membership.TenantId))
            .OrderBy(membership => tenantMap[membership.TenantId].Sort)
            .ThenBy(membership => tenantMap[membership.TenantId].TenantName)
            .Select(membership => TenantApplicationMapper.ToSwitcherDto(membership, tenantMap[membership.TenantId], _currentUser.TenantId))];
    }

    /// <summary>
    /// 构建租户分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>租户分页请求</returns>
    private static BasicAppPRDto BuildTenantPageRequest(TenantPageQueryDto input)
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
                nameof(SysTenant.TenantCode),
                nameof(SysTenant.TenantName),
                nameof(SysTenant.TenantShortName),
                nameof(SysTenant.Domain));
        }

        if (input.TenantStatus.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTenant.TenantStatus), input.TenantStatus.Value);
        }

        if (input.ConfigStatus.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTenant.ConfigStatus), input.ConfigStatus.Value);
        }

        if (input.EditionId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTenant.EditionId), input.EditionId.Value);
        }

        if (input.ExpireTimeStart.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTenant.ExpireTime), input.ExpireTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.ExpireTimeEnd.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTenant.ExpireTime), input.ExpireTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort(nameof(SysTenant.Sort), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysTenant.CreatedTime), SortDirection.Descending, 1);
        return request;
    }

}
