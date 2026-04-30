#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionConditionQueryService
// Guid:4622993f-4981-46e0-b32a-6ac1c2b5306d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 权限 ABAC 条件查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限ABAC条件")]
public sealed class PermissionConditionQueryService(
    IPermissionConditionRepository permissionConditionRepository,
    IRolePermissionRepository rolePermissionRepository,
    IUserPermissionRepository userPermissionRepository,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, IPermissionConditionQueryService
{
    /// <summary>
    /// 权限 ABAC 条件仓储
    /// </summary>
    private readonly IPermissionConditionRepository _permissionConditionRepository = permissionConditionRepository;

    /// <summary>
    /// 角色权限仓储
    /// </summary>
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;

    /// <summary>
    /// 用户直授权限仓储
    /// </summary>
    private readonly IUserPermissionRepository _userPermissionRepository = userPermissionRepository;

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 获取角色权限绑定的 ABAC 条件
    /// </summary>
    /// <param name="rolePermissionId">角色权限绑定主键</param>
    /// <param name="onlyValid">是否仅返回有效条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Read)]
    public async Task<IReadOnlyList<PermissionConditionListItemDto>> GetRolePermissionConditionsAsync(
        long rolePermissionId,
        bool onlyValid = false,
        CancellationToken cancellationToken = default)
    {
        if (rolePermissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rolePermissionId), "角色权限绑定主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var rolePermission = await _rolePermissionRepository.GetByIdAsync(rolePermissionId, cancellationToken)
            ?? throw new InvalidOperationException("角色权限绑定不存在。");
        var conditions = onlyValid
            ? await _permissionConditionRepository.GetValidByAuthorizationIdsAsync([rolePermissionId], [], cancellationToken)
            : await _permissionConditionRepository.GetListAsync(
                condition => condition.RolePermissionId == rolePermissionId,
                condition => condition.ConditionGroup,
                cancellationToken);

        if (conditions.Count == 0)
        {
            return [];
        }

        var role = await _roleRepository.GetByIdAsync(rolePermission.RoleId, cancellationToken);
        var permission = await _permissionRepository.GetByIdAsync(rolePermission.PermissionId, cancellationToken);

        return [.. conditions
            .Select(condition => PermissionConditionApplicationMapper.ToListItemDto(
                condition,
                rolePermission,
                null,
                permission,
                role,
                null))
            .OrderBy(item => item.ConditionGroup)
            .ThenBy(item => item.AttributeName)];
    }

    /// <summary>
    /// 获取用户直授权限绑定的 ABAC 条件
    /// </summary>
    /// <param name="userPermissionId">用户直授权限绑定主键</param>
    /// <param name="onlyValid">是否仅返回有效条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Read)]
    public async Task<IReadOnlyList<PermissionConditionListItemDto>> GetUserPermissionConditionsAsync(
        long userPermissionId,
        bool onlyValid = false,
        CancellationToken cancellationToken = default)
    {
        if (userPermissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userPermissionId), "用户直授权限绑定主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var userPermission = await _userPermissionRepository.GetByIdAsync(userPermissionId, cancellationToken)
            ?? throw new InvalidOperationException("用户直授权限绑定不存在。");
        var conditions = onlyValid
            ? await _permissionConditionRepository.GetValidByAuthorizationIdsAsync([], [userPermissionId], cancellationToken)
            : await _permissionConditionRepository.GetListAsync(
                condition => condition.UserPermissionId == userPermissionId,
                condition => condition.ConditionGroup,
                cancellationToken);

        if (conditions.Count == 0)
        {
            return [];
        }

        var permission = await _permissionRepository.GetByIdAsync(userPermission.PermissionId, cancellationToken);
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userPermission.UserId, cancellationToken);

        return [.. conditions
            .Select(condition => PermissionConditionApplicationMapper.ToListItemDto(
                condition,
                null,
                userPermission,
                permission,
                null,
                tenantMember))
            .OrderBy(item => item.ConditionGroup)
            .ThenBy(item => item.AttributeName)];
    }

    /// <summary>
    /// 获取权限 ABAC 条件详情
    /// </summary>
    /// <param name="id">ABAC 条件主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Read)]
    public async Task<PermissionConditionDetailDto?> GetPermissionConditionDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限 ABAC 条件主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var condition = await _permissionConditionRepository.GetByIdAsync(id, cancellationToken);
        if (condition is null)
        {
            return null;
        }

        var related = await BuildRelatedFactsAsync(condition, cancellationToken);
        return PermissionConditionApplicationMapper.ToDetailDto(
            condition,
            related.RolePermission,
            related.UserPermission,
            related.Permission,
            related.Role,
            related.TenantMember);
    }

    /// <summary>
    /// 构建关联事实
    /// </summary>
    private async Task<RelatedFacts> BuildRelatedFactsAsync(SysPermissionCondition condition, CancellationToken cancellationToken)
    {
        SysRolePermission? rolePermission = null;
        SysUserPermission? userPermission = null;
        SysPermission? permission = null;
        SysRole? role = null;
        SysTenantUser? tenantMember = null;

        if (condition.RolePermissionId.HasValue)
        {
            rolePermission = await _rolePermissionRepository.GetByIdAsync(condition.RolePermissionId.Value, cancellationToken);
            if (rolePermission is not null)
            {
                role = await _roleRepository.GetByIdAsync(rolePermission.RoleId, cancellationToken);
                permission = await _permissionRepository.GetByIdAsync(rolePermission.PermissionId, cancellationToken);
            }
        }

        if (condition.UserPermissionId.HasValue)
        {
            userPermission = await _userPermissionRepository.GetByIdAsync(condition.UserPermissionId.Value, cancellationToken);
            if (userPermission is not null)
            {
                tenantMember = await _tenantUserRepository.GetMembershipAsync(userPermission.UserId, cancellationToken);
                permission ??= await _permissionRepository.GetByIdAsync(userPermission.PermissionId, cancellationToken);
            }
        }

        return new RelatedFacts(rolePermission, userPermission, permission, role, tenantMember);
    }

    private sealed record RelatedFacts(
        SysRolePermission? RolePermission,
        SysUserPermission? UserPermission,
        SysPermission? Permission,
        SysRole? Role,
        SysTenantUser? TenantMember);
}
