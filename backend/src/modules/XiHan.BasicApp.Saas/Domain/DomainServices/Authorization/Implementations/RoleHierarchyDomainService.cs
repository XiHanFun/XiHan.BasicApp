// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 角色继承领域服务实现
/// </summary>
public sealed class RoleHierarchyDomainService
    : IRoleHierarchyDomainService
{
    private readonly IRoleHierarchyRepository _roleHierarchyRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleHierarchyDomainService(IRoleHierarchyRepository roleHierarchyRepository)
    {
        _roleHierarchyRepository = roleHierarchyRepository;
    }

    /// <inheritdoc />
    public async Task<bool> WouldCreateCycleAsync(long parentRoleId, long childRoleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (parentRoleId == childRoleId)
        {
            return true;
        }

        // 查 childRoleId 的所有祖先，若包含 parentRoleId 则形成环路
        var ancestorIds = await _roleHierarchyRepository.GetAncestorIdsAsync([childRoleId], includeSelf: false, cancellationToken);
        return ancestorIds.Contains(parentRoleId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<long>> ExpandRoleHierarchyAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ids = roleIds.ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await _roleHierarchyRepository.GetAncestorIdsAsync(ids, includeSelf: true, cancellationToken);
    }
}
