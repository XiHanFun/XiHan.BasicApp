#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleHierarchyQueryService
// Guid:38e3ae61-e5b9-4ff5-bc63-1e5e06255f81
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
public sealed class RoleHierarchyQueryService(
    IRoleRepository roleRepository,
    IRoleHierarchyRepository roleHierarchyRepository)
    : SaasApplicationService, IRoleHierarchyQueryService
{
    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 角色层级仓储
    /// </summary>
    private readonly IRoleHierarchyRepository _roleHierarchyRepository = roleHierarchyRepository;

    /// <summary>
    /// 获取角色祖先链
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="includeSelf">是否包含自己</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色祖先链</returns>
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Read)]
    public Task<IReadOnlyList<RoleHierarchyListItemDto>> GetRoleAncestorsAsync(long roleId, bool includeSelf = true, CancellationToken cancellationToken = default)
    {
        return GetRoleHierarchyListAsync(roleId, includeSelf, isAncestorQuery: true, cancellationToken);
    }

    /// <summary>
    /// 获取角色后代链
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="includeSelf">是否包含自己</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色后代链</returns>
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Read)]
    public Task<IReadOnlyList<RoleHierarchyListItemDto>> GetRoleDescendantsAsync(long roleId, bool includeSelf = true, CancellationToken cancellationToken = default)
    {
        return GetRoleHierarchyListAsync(roleId, includeSelf, isAncestorQuery: false, cancellationToken);
    }

    /// <summary>
    /// 获取角色继承详情
    /// </summary>
    /// <param name="id">角色继承主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色继承详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Read)]
    public async Task<RoleHierarchyDetailDto?> GetRoleHierarchyDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "角色继承主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var hierarchy = await _roleHierarchyRepository.GetByIdAsync(id, cancellationToken);
        if (hierarchy is null)
        {
            return null;
        }

        var roleMap = await BuildRoleMapAsync([hierarchy.AncestorId, hierarchy.DescendantId], cancellationToken);
        return RoleHierarchyApplicationMapper.ToDetailDto(
            hierarchy,
            roleMap.GetValueOrDefault(hierarchy.AncestorId),
            roleMap.GetValueOrDefault(hierarchy.DescendantId));
    }

    /// <summary>
    /// 获取角色继承列表
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="includeSelf">是否包含自己</param>
    /// <param name="isAncestorQuery">是否查询祖先链</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色继承列表</returns>
    private async Task<IReadOnlyList<RoleHierarchyListItemDto>> GetRoleHierarchyListAsync(
        long roleId,
        bool includeSelf,
        bool isAncestorQuery,
        CancellationToken cancellationToken)
    {
        if (roleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), "角色主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        _ = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        var hierarchies = isAncestorQuery
            ? await _roleHierarchyRepository.GetListAsync(
                hierarchy => hierarchy.DescendantId == roleId && (includeSelf || hierarchy.Depth > 0),
                hierarchy => hierarchy.Depth,
                cancellationToken)
            : await _roleHierarchyRepository.GetListAsync(
                hierarchy => hierarchy.AncestorId == roleId && (includeSelf || hierarchy.Depth > 0),
                hierarchy => hierarchy.Depth,
                cancellationToken);

        if (hierarchies.Count == 0)
        {
            return [];
        }

        var roleMap = await BuildRoleMapAsync(
            hierarchies.SelectMany(hierarchy => new[] { hierarchy.AncestorId, hierarchy.DescendantId }),
            cancellationToken);

        return [.. hierarchies
            .Select(hierarchy => RoleHierarchyApplicationMapper.ToListItemDto(
                hierarchy,
                roleMap.GetValueOrDefault(hierarchy.AncestorId),
                roleMap.GetValueOrDefault(hierarchy.DescendantId)))
            .OrderBy(item => item.Depth)
            .ThenBy(item => isAncestorQuery ? item.AncestorRoleCode : item.DescendantRoleCode)
            .ThenBy(item => isAncestorQuery ? item.AncestorId : item.DescendantId)];
    }

    /// <summary>
    /// 构建角色映射
    /// </summary>
    /// <param name="roleIds">角色主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色映射</returns>
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
