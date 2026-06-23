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
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Specifications;
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
public sealed class TenantQueryService
    : SaasApplicationService, ITenantQueryService
{
    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository;

    /// <summary>
    /// 租户仓储
    /// </summary>
    private readonly ITenantRepository _tenantRepository;

    /// <summary>
    /// 当前用户
    /// </summary>
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 超级管理员角色编码（与种子/授权快照/SwitchTenant 约定一致，运行时特判可进入任意租户）
    /// </summary>
    private const string SuperAdminRoleCode = "super_admin";

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantQueryService(
        ITenantUserRepository tenantUserRepository,
        ITenantRepository tenantRepository,
        ICurrentUser currentUser)
    {
        _tenantUserRepository = tenantUserRepository;
        _tenantRepository = tenantRepository;
        _currentUser = currentUser;
    }

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
    /// <remarks>
    /// 仅要求登录（不挂权限码）：数据自限定于当前用户自身的有效成员关系；
    /// 登录后控制中心选租户阶段用户尚未进入任何租户、不持有任何权限码，挂权限码会直接阻断选择流程。
    /// </remarks>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前用户可进入的租户列表</returns>
    public async Task<IReadOnlyList<TenantSwitcherDto>> GetMyAvailableTenantsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("当前用户未登录，无法获取可进入租户。");

        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;

        // 超级管理员是平台账号、无 SysTenantUser 成员关系，但设计上可进入任意租户（SwitchTenant 同样对其放行）。
        // 故此处不按成员关系、而是返回全部可用租户（正常态、未过期）作为可切换项，避免切换器对超管为空。
        if (_currentUser.IsInRole(SuperAdminRoleCode))
        {
            var availableSpec = new AvailableTenantSpecification(now);
            var allTenants = await _tenantRepository.GetListAsync(availableSpec.ToExpression(), cancellationToken);
            return [.. allTenants
                .OrderBy(tenant => tenant.Sort)
                .ThenBy(tenant => tenant.TenantName)
                .Select(tenant => TenantApplicationMapper.ToSwitcherDto(tenant, _currentUser.TenantId))];
        }

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
            request.Conditions.SetKeyword<SysTenant>(
                input.Keyword.Trim(),
                tenant => tenant.TenantCode,
                tenant => tenant.TenantName,
                tenant => tenant.TenantShortName,
                tenant => tenant.Domain);
        }

        if (input.TenantStatus.HasValue)
        {
            request.Conditions.AddFilter((SysTenant tenant) => tenant.TenantStatus, input.TenantStatus.Value);
        }

        if (input.ConfigStatus.HasValue)
        {
            request.Conditions.AddFilter((SysTenant tenant) => tenant.ConfigStatus, input.ConfigStatus.Value);
        }

        if (input.EditionId.HasValue)
        {
            request.Conditions.AddFilter((SysTenant tenant) => tenant.EditionId, input.EditionId.Value);
        }

        if (input.ExpirationTimeStart.HasValue)
        {
            request.Conditions.AddFilter((SysTenant tenant) => tenant.ExpirationTime, input.ExpirationTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.ExpirationTimeEnd.HasValue)
        {
            request.Conditions.AddFilter((SysTenant tenant) => tenant.ExpirationTime, input.ExpirationTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort((SysTenant tenant) => tenant.Sort, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysTenant tenant) => tenant.CreatedTime, SortDirection.Descending, 1);
        return request;
    }
}
