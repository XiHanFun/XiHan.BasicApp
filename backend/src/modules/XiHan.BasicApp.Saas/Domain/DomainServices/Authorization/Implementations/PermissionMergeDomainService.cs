#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionMergeDomainService
// Guid:d4e5f6a7-4444-5555-6666-777788889999
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.ValueObjects;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限合并领域服务实现
/// </summary>
public sealed class PermissionMergeDomainService(
    IRolePermissionRepository rolePermissionRepository,
    IUserPermissionRepository userPermissionRepository,
    IPermissionDelegationRepository permissionDelegationRepository,
    IPermissionRepository permissionRepository)
    : IPermissionMergeDomainService
{
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;
    private readonly IUserPermissionRepository _userPermissionRepository = userPermissionRepository;
    private readonly IPermissionDelegationRepository _permissionDelegationRepository = permissionDelegationRepository;
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionGrantSnapshot>> MergePermissionGrantsAsync(
        long userId,
        IEnumerable<long> roleIds,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var grants = new List<PermissionGrantSnapshot>();
        var allPermissionIds = new HashSet<long>();
        var roleIdList = roleIds.ToList();

        // 第一阶段：收集所有需要的 PermissionId
        IReadOnlyList<SysRolePermission> rolePermissions = [];
        if (roleIdList.Count > 0)
        {
            rolePermissions = await _rolePermissionRepository.GetValidByRoleIdsAsync(roleIdList, now, cancellationToken);
            allPermissionIds.UnionWith(rolePermissions.Select(rp => rp.PermissionId));
        }

        var userPermissions = await _userPermissionRepository.GetValidByUserIdAsync(userId, now, cancellationToken);
        allPermissionIds.UnionWith(userPermissions.Select(up => up.PermissionId));

        var delegations = await _permissionDelegationRepository.GetActiveByDelegateeIdAsync(userId, now, cancellationToken);
        allPermissionIds.UnionWith(delegations.Where(d => d.PermissionId.HasValue).Select(d => d.PermissionId!.Value));

        if (allPermissionIds.Count == 0)
        {
            return grants;
        }

        // 第二阶段：批量加载权限实体（避免多次 DB 查询）
        var permissions = await _permissionRepository.GetByIdsAsync(allPermissionIds.ToList(), cancellationToken);
        var permissionMap = permissions.ToDictionary(p => p.BasicId);

        // 合并角色授权
        foreach (var rp in rolePermissions)
        {
            if (permissionMap.TryGetValue(rp.PermissionId, out var permission))
            {
                grants.Add(new PermissionGrantSnapshot(
                    PermissionId: permission.BasicId,
                    PermissionCode: permission.PermissionCode,
                    Action: rp.PermissionAction,
                    Source: AuthorizationGrantSource.Role,
                    Priority: permission.Priority,
                    Period: new EffectivePeriod(rp.EffectiveTime, rp.ExpirationTime),
                    IsEnabled: permission.Status == EnableStatus.Enabled));
            }
        }

        // 合并用户直授
        foreach (var up in userPermissions)
        {
            if (permissionMap.TryGetValue(up.PermissionId, out var permission))
            {
                grants.Add(new PermissionGrantSnapshot(
                    PermissionId: permission.BasicId,
                    PermissionCode: permission.PermissionCode,
                    Action: up.PermissionAction,
                    Source: AuthorizationGrantSource.User,
                    Priority: permission.Priority,
                    Period: new EffectivePeriod(up.EffectiveTime, up.ExpirationTime),
                    IsEnabled: permission.Status == EnableStatus.Enabled));
            }
        }

        // 合并委派授权（委派视为 Grant，来源标记 Delegation）
        foreach (var delegation in delegations)
        {
            if (delegation.PermissionId.HasValue && permissionMap.TryGetValue(delegation.PermissionId.Value, out var permission))
            {
                grants.Add(new PermissionGrantSnapshot(
                    PermissionId: permission.BasicId,
                    PermissionCode: permission.PermissionCode,
                    Action: PermissionAction.Grant,
                    Source: AuthorizationGrantSource.Delegation,
                    Priority: permission.Priority,
                    Period: new EffectivePeriod(delegation.EffectiveTime, delegation.ExpirationTime),
                    IsEnabled: permission.Status == EnableStatus.Enabled));
            }
        }

        return grants;
    }
}
