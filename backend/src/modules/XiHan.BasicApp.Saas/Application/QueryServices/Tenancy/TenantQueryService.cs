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
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
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
            .Select(membership => MapTenantSwitcherDto(membership, tenantMap[membership.TenantId]))];
    }

    /// <summary>
    /// 映射租户切换项
    /// </summary>
    /// <param name="membership">租户成员关系</param>
    /// <param name="tenant">租户</param>
    /// <returns>租户切换项 DTO</returns>
    private TenantSwitcherDto MapTenantSwitcherDto(SysTenantUser membership, SysTenant tenant)
    {
        return new TenantSwitcherDto
        {
            TenantId = tenant.BasicId,
            TenantCode = tenant.TenantCode,
            TenantName = tenant.TenantName,
            TenantShortName = tenant.TenantShortName,
            Logo = tenant.Logo,
            Domain = tenant.Domain,
            TenantStatus = tenant.TenantStatus,
            ConfigStatus = tenant.ConfigStatus,
            ExpireTime = tenant.ExpireTime,
            MembershipId = membership.BasicId,
            MemberType = membership.MemberType,
            InviteStatus = membership.InviteStatus,
            MembershipExpirationTime = membership.ExpirationTime,
            IsCurrent = _currentUser.TenantId == tenant.BasicId
        };
    }
}
