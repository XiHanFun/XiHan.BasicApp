// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 角色继承读取实现
/// </summary>
public sealed class RoleInheritanceReader : IRoleInheritanceReader
{
    private readonly IRoleHierarchyDomainService _roleHierarchyDomainService;

    private readonly IRoleRepository _roleRepository;

    private readonly IRolePermissionRepository _rolePermissionRepository;

    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleInheritanceReader(
        IRoleHierarchyDomainService roleHierarchyDomainService,
        IRoleRepository roleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IPermissionRepository permissionRepository)
    {
        _roleHierarchyDomainService = roleHierarchyDomainService;
        _roleRepository = roleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _permissionRepository = permissionRepository;
    }

    /// <summary>
    /// 角色的全部上级（不含自身）
    /// </summary>
    public async Task<List<RoleInheritanceItemDto>> GetAncestorsAsync(long roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var graph = await _roleHierarchyDomainService.GetGraphAsync(cancellationToken);
        var ancestors = graph.AncestorsOf(roleId);
        if (ancestors.Count == 0)
        {
            return [];
        }

        var roleMap = await BuildRoleMapAsync(ancestors.Keys.Append(roleId), cancellationToken);
        var enabledIds = EnabledIds(roleMap);
        var effective = graph.AncestorsOf(roleId, enabledIds.Contains);

        return [.. ancestors
            .Select(pair => ToItem(pair.Key, effective.GetValueOrDefault(pair.Key) ?? pair.Value, effective.ContainsKey(pair.Key), roleMap))
            .OrderBy(item => item.Depth)
            .ThenBy(item => item.RoleCode, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// 角色的全部下级（不含自身）
    /// </summary>
    public async Task<List<RoleInheritanceItemDto>> GetDescendantsAsync(long roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var graph = await _roleHierarchyDomainService.GetGraphAsync(cancellationToken);
        var descendants = graph.DescendantsOf(roleId);
        if (descendants.Count == 0)
        {
            return [];
        }

        var roleMap = await BuildRoleMapAsync(descendants.Keys.Append(roleId), cancellationToken);
        var enabledIds = EnabledIds(roleMap);

        var items = new List<RoleInheritanceItemDto>(descendants.Count);
        foreach (var (descendantId, path) in descendants)
        {
            // 本角色的权限能否传到该下级：从下级往上经启用角色走得到本角色（本角色自身启停不计）
            var effective = graph.AncestorsOf(descendantId, id => id == roleId || enabledIds.Contains(id));
            var isEffective = effective.TryGetValue(roleId, out var effectivePath);
            items.Add(ToItem(descendantId, isEffective ? effectivePath! : path, isEffective, roleMap));
        }

        return [.. items
            .OrderBy(item => item.Depth)
            .ThenBy(item => item.RoleCode, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// 角色从生效的上级继承来的权限绑定
    /// </summary>
    public async Task<List<RoleInheritedPermissionDto>> GetInheritedPermissionsAsync(long roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var effectiveAncestors = (await _roleHierarchyDomainService.GetEffectiveAncestorsAsync([roleId], cancellationToken))
            .GetValueOrDefault(roleId);
        if (effectiveAncestors is not { Count: > 0 })
        {
            return [];
        }

        var bindings = await _rolePermissionRepository.GetValidByRoleIdsAsync(effectiveAncestors.Keys, DateTimeOffset.UtcNow, cancellationToken);
        if (bindings.Count == 0)
        {
            return [];
        }

        var permissionMap = (await _permissionRepository.GetByIdsAsync(bindings.Select(binding => binding.PermissionId).Distinct(), cancellationToken))
            .ToDictionary(permission => permission.BasicId);
        var roleMap = await BuildRoleMapAsync(effectiveAncestors.Keys, cancellationToken);

        return [.. bindings
            .Where(binding => permissionMap.ContainsKey(binding.PermissionId))
            .Select(binding =>
            {
                var permission = permissionMap[binding.PermissionId];
                var sourceRole = roleMap.GetValueOrDefault(binding.RoleId);
                return new RoleInheritedPermissionDto
                {
                    PermissionId = binding.PermissionId,
                    PermissionCode = permission.PermissionCode,
                    PermissionName = permission.PermissionName,
                    PermissionAction = binding.PermissionAction,
                    SourceRoleId = binding.RoleId,
                    SourceRoleCode = sourceRole?.RoleCode,
                    SourceRoleName = sourceRole?.RoleName,
                    Depth = effectiveAncestors[binding.RoleId].Depth
                };
            })
            .OrderBy(item => item.PermissionCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Depth)
            .ThenBy(item => item.SourceRoleCode, StringComparer.OrdinalIgnoreCase)];
    }

    private static HashSet<long> EnabledIds(IReadOnlyDictionary<long, SysRole> roleMap) =>
        [.. roleMap.Values.Where(role => role.Status == EnableStatus.Enabled).Select(role => role.BasicId)];

    private static RoleInheritanceItemDto ToItem(long roleId, RoleInheritancePath path, bool isEffective, IReadOnlyDictionary<long, SysRole> roleMap)
    {
        var role = roleMap.GetValueOrDefault(roleId);
        return new RoleInheritanceItemDto
        {
            RoleId = roleId,
            RoleCode = role?.RoleCode ?? string.Empty,
            RoleName = role?.RoleName ?? roleId.ToString(),
            RoleType = role?.RoleType ?? RoleType.Custom,
            Status = role?.Status ?? EnableStatus.Disabled,
            IsGlobal = role?.IsGlobal ?? false,
            Depth = path.Depth,
            PathRoleNames = [.. path.RoleIds.Select(id => roleMap.GetValueOrDefault(id)?.RoleName ?? id.ToString())],
            IsEffective = isEffective
        };
    }

    private async Task<IReadOnlyDictionary<long, SysRole>> BuildRoleMapAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken)
    {
        var ids = roleIds.Where(id => id > 0).Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<long, SysRole>();
        }

        var roles = await _roleRepository.GetByIdsAsync(ids, cancellationToken);
        return roles.ToDictionary(role => role.BasicId);
    }
}
