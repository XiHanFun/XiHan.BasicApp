// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 角色继承查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "角色继承")]
public sealed class RoleHierarchyQueryService
    : SaasApplicationService, IRoleHierarchyQueryService
{
    private readonly IRoleRepository _roleRepository;

    private readonly IRoleInheritanceReader _roleInheritanceReader;

    private readonly ISuperAdminProtector _superAdminProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleHierarchyQueryService(
        IRoleRepository roleRepository,
        IRoleInheritanceReader roleInheritanceReader,
        ISuperAdminProtector superAdminProtector)
    {
        _roleRepository = roleRepository;
        _roleInheritanceReader = roleInheritanceReader;
        _superAdminProtector = superAdminProtector;
    }

    /// <summary>
    /// 获取角色的全部上级（不含自身）
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上级链，按继承深度排列</returns>
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Read)]
    public async Task<IReadOnlyList<RoleInheritanceItemDto>> GetRoleAncestorsAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await CanReadRoleAsync(roleId, cancellationToken)
            ? await _roleInheritanceReader.GetAncestorsAsync(roleId, cancellationToken)
            : [];
    }

    /// <summary>
    /// 获取角色的全部下级（不含自身）
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下级链，按继承深度排列</returns>
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Read)]
    public async Task<IReadOnlyList<RoleInheritanceItemDto>> GetRoleDescendantsAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await CanReadRoleAsync(roleId, cancellationToken)
            ? await _roleInheritanceReader.GetDescendantsAsync(roleId, cancellationToken)
            : [];
    }

    /// <summary>
    /// 获取角色从生效的上级继承来的权限绑定
    /// </summary>
    /// <remarks>内容是上级角色的权限绑定，与读取角色权限同一道权限。</remarks>
    /// <param name="roleId">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>继承来的权限绑定，同一权限来自多个上级时逐条列出</returns>
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Read)]
    public async Task<IReadOnlyList<RoleInheritedPermissionDto>> GetRoleInheritedPermissionsAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await CanReadRoleAsync(roleId, cancellationToken)
            ? await _roleInheritanceReader.GetInheritedPermissionsAsync(roleId, cancellationToken)
            : [];
    }

    /// <summary>
    /// 角色存在且对当前用户可见：非超管读取超管角色按不存在处理
    /// </summary>
    private async Task<bool> CanReadRoleAsync(long roleId, CancellationToken cancellationToken)
    {
        if (roleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), "角色主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!_superAdminProtector.IsCurrentUserSuperAdmin() && await _superAdminProtector.IsProtectedRoleAsync(roleId, cancellationToken))
        {
            return false;
        }

        _ = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");
        return true;
    }
}
