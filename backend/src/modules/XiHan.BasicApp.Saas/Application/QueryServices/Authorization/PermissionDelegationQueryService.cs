#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionDelegationQueryService
// Guid:e3c95dd9-ff53-46e4-bf52-37c17a43040b
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
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 权限委托查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限委托")]
public sealed class PermissionDelegationQueryService(
    IPermissionDelegationRepository permissionDelegationRepository,
    ITenantUserRepository tenantUserRepository,
    IPermissionRepository permissionRepository,
    IRoleRepository roleRepository)
    : SaasApplicationService, IPermissionDelegationQueryService
{
    /// <summary>
    /// 权限委托仓储
    /// </summary>
    private readonly IPermissionDelegationRepository _permissionDelegationRepository = permissionDelegationRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 获取权限委托分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限委托分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Read)]
    public async Task<PageResultDtoBase<PermissionDelegationListItemDto>> GetPermissionDelegationPageAsync(PermissionDelegationPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPermissionDelegationPageRequest(input);
        var delegations = await _permissionDelegationRepository.GetPagedAsync(request, cancellationToken);
        if (delegations.Items.Count == 0)
        {
            return new PageResultDtoBase<PermissionDelegationListItemDto>([], delegations.Page);
        }

        var now = DateTimeOffset.UtcNow;
        var tenantMemberMap = await BuildTenantMemberMapAsync(
            delegations.Items
                .SelectMany(delegation => new[] { delegation.DelegatorUserId, delegation.DelegateeUserId }),
            cancellationToken);
        var permissionMap = await BuildPermissionMapAsync(
            delegations.Items
                .Where(delegation => delegation.PermissionId.HasValue)
                .Select(delegation => delegation.PermissionId!.Value),
            cancellationToken);
        var roleMap = await BuildRoleMapAsync(
            delegations.Items
                .Where(delegation => delegation.RoleId.HasValue)
                .Select(delegation => delegation.RoleId!.Value),
            cancellationToken);

        var items = delegations.Items
            .Select(delegation => PermissionDelegationApplicationMapper.ToListItemDto(
                delegation,
                tenantMemberMap.GetValueOrDefault(delegation.DelegatorUserId),
                tenantMemberMap.GetValueOrDefault(delegation.DelegateeUserId),
                delegation.PermissionId.HasValue ? permissionMap.GetValueOrDefault(delegation.PermissionId.Value) : null,
                delegation.RoleId.HasValue ? roleMap.GetValueOrDefault(delegation.RoleId.Value) : null,
                now))
            .ToList();

        return new PageResultDtoBase<PermissionDelegationListItemDto>(items, delegations.Page);
    }

    /// <summary>
    /// 获取权限委托详情
    /// </summary>
    /// <param name="id">权限委托主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限委托详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Read)]
    public async Task<PermissionDelegationDetailDto?> GetPermissionDelegationDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限委托主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var delegation = await _permissionDelegationRepository.GetByIdAsync(id, cancellationToken);
        if (delegation is null)
        {
            return null;
        }

        var delegator = await _tenantUserRepository.GetMembershipAsync(delegation.DelegatorUserId, cancellationToken);
        var delegatee = await _tenantUserRepository.GetMembershipAsync(delegation.DelegateeUserId, cancellationToken);
        var permission = delegation.PermissionId.HasValue
            ? await _permissionRepository.GetByIdAsync(delegation.PermissionId.Value, cancellationToken)
            : null;
        var role = delegation.RoleId.HasValue
            ? await _roleRepository.GetByIdAsync(delegation.RoleId.Value, cancellationToken)
            : null;

        return PermissionDelegationApplicationMapper.ToDetailDto(
            delegation,
            delegator,
            delegatee,
            permission,
            role,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 构建权限委托分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>权限委托分页请求</returns>
    private static BasicAppPRDto BuildPermissionDelegationPageRequest(PermissionDelegationPageQueryDto input)
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
                nameof(SysPermissionDelegation.DelegationReason),
                nameof(SysPermissionDelegation.Remark));
        }

        if (input.DelegatorUserId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermissionDelegation.DelegatorUserId), input.DelegatorUserId.Value);
        }

        if (input.DelegateeUserId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermissionDelegation.DelegateeUserId), input.DelegateeUserId.Value);
        }

        if (input.PermissionId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermissionDelegation.PermissionId), input.PermissionId.Value);
        }

        if (input.RoleId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermissionDelegation.RoleId), input.RoleId.Value);
        }

        if (input.DelegationStatus.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermissionDelegation.DelegationStatus), input.DelegationStatus.Value);
        }

        request.Conditions.AddSort(nameof(SysPermissionDelegation.ExpirationTime), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysPermissionDelegation.CreatedTime), SortDirection.Descending, 1);
        return request;
    }

    /// <summary>
    /// 构建租户成员映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysTenantUser>> BuildTenantMemberMapAsync(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(userId => userId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysTenantUser>();
        }

        var tenantMembers = await _tenantUserRepository.GetListAsync(
            tenantMember => ids.Contains(tenantMember.UserId),
            tenantMember => tenantMember.CreatedTime,
            cancellationToken);
        return tenantMembers.ToDictionary(tenantMember => tenantMember.UserId);
    }

    /// <summary>
    /// 构建权限映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysPermission>> BuildPermissionMapAsync(IEnumerable<long> permissionIds, CancellationToken cancellationToken)
    {
        var ids = permissionIds
            .Where(permissionId => permissionId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysPermission>();
        }

        var permissions = await _permissionRepository.GetByIdsAsync(ids, cancellationToken);
        return permissions.ToDictionary(permission => permission.BasicId);
    }

    /// <summary>
    /// 构建角色映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysRole>> BuildRoleMapAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken)
    {
        var ids = roleIds
            .Where(roleId => roleId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysRole>();
        }

        var roles = await _roleRepository.GetByIdsAsync(ids, cancellationToken);
        return roles.ToDictionary(role => role.BasicId);
    }
}
